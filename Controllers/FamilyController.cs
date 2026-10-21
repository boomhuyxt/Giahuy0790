using System.Security.Claims;
using Giahuy0790.Data;
using Giahuy0790.Models;
using Giahuy0790.Repositories;
using Giahuy0790.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Giahuy0790.Controllers;

/// <summary>
/// Controller API quản lý Nhóm Gia Đình (Family Group):
/// Chỉ cần nhập Email của tài khoản đã đăng ký để thêm con hoặc thành viên vào nhóm gia đình cùng quản lý trẻ.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FamilyController(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IEmailService emailService) : ControllerBase
{
    /// <summary>
    /// Lấy thông tin Nhóm Gia Đình của người dùng đang đăng nhập (kèm danh sách thành viên và các bé)
    /// </summary>
    [HttpGet("my-family")]
    public async Task<IActionResult> GetMyFamily()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để xem nhóm gia đình." });
        }

        var group = await GetOrCreateFamilyGroupAsync(currentUserId);

        var members = await dbContext.FamilyMembers
            .AsNoTracking()
            .Where(m => m.FamilyGroupId == group.Id)
            .Include(m => m.User)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();

        var kids = await dbContext.Kids
            .AsNoTracking()
            .Where(k => k.FamilyGroupId == group.Id || k.ParentId == group.OwnerId)
            .Include(k => k.KidUser)
            .Include(k => k.BlockedItems)
            .OrderByDescending(k => k.Id)
            .ToListAsync();

        var owner = await userManager.FindByIdAsync(group.OwnerId);

        return Ok(new
        {
            group = new
            {
                group.Id,
                group.Name,
                group.Description,
                group.OwnerId,
                ownerName = owner?.FullName ?? owner?.UserName ?? "Chủ gia đình",
                group.CreatedAt
            },
            members = members.Select(m => new
            {
                m.Id,
                m.UserId,
                username = m.User?.UserName,
                email = m.User?.Email,
                fullName = m.User?.FullName ?? m.User?.UserName,
                avatarUrl = m.User?.AvatarUrl,
                m.RoleInFamily,
                m.JoinedAt
            }),
            kids = kids.Select(k => new
            {
                k.Id,
                k.FullName,
                k.Nickname,
                k.DateOfBirth,
                k.Gender,
                k.AvatarUrl,
                k.SchoolName,
                k.TotalPoints,
                k.KidUserId,
                kidUserName = k.KidUser?.UserName,
                kidUserEmail = k.KidUser?.Email,
                hasKidAccount = !string.IsNullOrEmpty(k.KidUserId),
                blockedItemsCount = k.BlockedItems?.Count(b => b.IsBlocked) ?? 0
            })
        });
    }

    /// <summary>
    /// Kiểm tra xem địa chỉ email đã được đăng ký tài khoản trên hệ thống hay chưa
    /// </summary>
    [HttpPost("check-email")]
    public async Task<IActionResult> CheckEmail([FromBody] CheckEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new { message = "Vui lòng nhập địa chỉ email cần kiểm tra." });
        }

        var email = dto.Email.Trim();
        var user = await userManager.FindByEmailAsync(email) ?? await userManager.FindByNameAsync(email);

        if (user == null)
        {
            return Ok(new
            {
                exists = false,
                message = $"Chưa có tài khoản nào đăng ký với email '{email}'. Bạn có thể gửi email mời người này đăng ký tham gia!"
            });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSelf = currentUserId == user.Id;

        // Kiểm tra xem đã là thành viên trong nhóm gia đình chưa
        var isAlreadyMember = false;
        string? memberRole = null;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            var myGroup = await dbContext.FamilyGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.OwnerId == currentUserId || g.Members.Any(m => m.UserId == currentUserId));

            if (myGroup != null)
            {
                var member = myGroup.Members.FirstOrDefault(m => m.UserId == user.Id);
                if (member != null)
                {
                    isAlreadyMember = true;
                    memberRole = member.RoleInFamily;
                }
            }
        }

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new
        {
            exists = true,
            isSelf,
            isAlreadyMember,
            memberRole,
            user = new
            {
                user.Id,
                username = user.UserName,
                email = user.Email,
                fullName = user.FullName ?? user.UserName,
                avatarUrl = user.AvatarUrl,
                roles
            }
        });
    }

    /// <summary>
    /// CHỨC NĂNG CHÍNH: Chỉ cần nhập Email của tài khoản đã đăng ký để thêm vào nhóm gia đình quản lý trẻ
    /// </summary>
    [HttpPost("add-by-email")]
    public async Task<IActionResult> AddByEmail([FromBody] AddFamilyMemberByEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new { message = "Vui lòng nhập địa chỉ Email của tài khoản đã đăng ký." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để thực hiện chức năng này." });
        }

        var currentUser = await userManager.FindByIdAsync(currentUserId);
        var currentUserName = currentUser?.FullName ?? currentUser?.UserName ?? "Phụ huynh";

        // 1. Tìm tài khoản người dùng theo email (hoặc username)
        var email = dto.Email.Trim();
        var targetUser = await userManager.FindByEmailAsync(email) ?? await userManager.FindByNameAsync(email);

        if (targetUser == null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy tài khoản đã đăng ký nào với email '{email}'. Hãy đảm bảo người này đã tạo tài khoản trên KidCare hoặc dùng tính năng 'Mời qua Gmail' để mời họ tham gia."
            });
        }

        // 2. Không cho phép tự thêm chính mình
        if (targetUser.Id == currentUserId)
        {
            return BadRequest(new { message = "Bạn là người tạo nhóm gia đình này, không thể thêm chính mình." });
        }

        // 3. Lấy hoặc tự động tạo Nhóm Gia Đình cho Phụ huynh
        var familyGroup = await GetOrCreateFamilyGroupAsync(currentUserId);

        // 4. Kiểm tra xem người này đã trong nhóm gia đình chưa
        var existingMember = await dbContext.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyGroupId == familyGroup.Id && m.UserId == targetUser.Id);

        if (existingMember != null)
        {
            return BadRequest(new
            {
                message = $"Tài khoản '{targetUser.UserName}' ({targetUser.Email}) đã là thành viên trong nhóm gia đình của bạn với vai trò '{existingMember.RoleInFamily}'."
            });
        }

        // 5. Xác định vai trò: "Child" (Con) hoặc "Parent" / "Guardian" (Đồng quản lý)
        var requestedRole = string.IsNullOrWhiteSpace(dto.RoleInFamily) ? "Child" : dto.RoleInFamily.Trim();
        var isChild = requestedRole.Equals("Child", StringComparison.OrdinalIgnoreCase);

        if (isChild)
        {
            // Đảm bảo user có role Kid
            if (!await roleManager.RoleExistsAsync("Kid")) await roleManager.CreateAsync(new IdentityRole("Kid"));
            if (!await userManager.IsInRoleAsync(targetUser, "Kid")) await userManager.AddToRoleAsync(targetUser, "Kid");

            // Kiểm tra xem bé đã có hồ sơ Kid chưa
            var existingKid = await dbContext.Kids.FirstOrDefaultAsync(k => k.KidUserId == targetUser.Id);
            if (existingKid != null)
            {
                existingKid.FamilyGroupId = familyGroup.Id;
                existingKid.ParentId ??= currentUserId;
                existingKid.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newKid = new Kid
                {
                    FullName = !string.IsNullOrWhiteSpace(dto.KidFullName)
                        ? dto.KidFullName.Trim()
                        : (!string.IsNullOrWhiteSpace(targetUser.FullName) ? targetUser.FullName.Trim() : targetUser.UserName ?? "Bé yêu"),
                    AvatarUrl = targetUser.AvatarUrl,
                    ParentId = currentUserId,
                    KidUserId = targetUser.Id,
                    FamilyGroupId = familyGroup.Id,
                    TotalPoints = 0,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Kids.Add(newKid);
            }
        }
        else
        {
            // Đảm bảo user có role Parents nếu thêm làm người đồng quản lý
            if (!await roleManager.RoleExistsAsync("Parents")) await roleManager.CreateAsync(new IdentityRole("Parents"));
            if (!await userManager.IsInRoleAsync(targetUser, "Parents")) await userManager.AddToRoleAsync(targetUser, "Parents");
        }

        // 6. Thêm vào bảng FamilyMembers
        var familyMember = new FamilyMember
        {
            FamilyGroupId = familyGroup.Id,
            UserId = targetUser.Id,
            RoleInFamily = isChild ? "Child" : (requestedRole.Equals("Guardian", StringComparison.OrdinalIgnoreCase) ? "Guardian" : "Parent"),
            JoinedAt = DateTime.UtcNow
        };
        dbContext.FamilyMembers.Add(familyMember);

        // 7. Gắn tất cả các bé chưa thuộc nhóm gia đình của phụ huynh vào nhóm gia đình này
        var unassignedKids = await dbContext.Kids
            .Where(k => k.ParentId == currentUserId && k.FamilyGroupId == null)
            .ToListAsync();
        foreach (var uk in unassignedKids)
        {
            uk.FamilyGroupId = familyGroup.Id;
        }

        await dbContext.SaveChangesAsync();

        // 8. Gửi Gmail thông báo tự động cho người vừa được thêm
        if (!string.IsNullOrWhiteSpace(targetUser.Email))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await emailService.SendFamilyGroupAddedNotificationAsync(
                        targetUser.Email,
                        targetUser.FullName ?? targetUser.UserName ?? "Thành viên",
                        currentUserName,
                        familyGroup.Name,
                        familyMember.RoleInFamily
                    );
                }
                catch
                {
                    // Tránh ngắt kết nối
                }
            });
        }

        var roleMessage = isChild ? "tài khoản con (Bé)" : "thành viên cùng quản lý trẻ";

        return Ok(new
        {
            message = $"🎉 Đã thêm thành công tài khoản '{targetUser.UserName}' ({targetUser.Email}) vào Nhóm Gia Đình với vai trò {roleMessage}! Email thông báo đã được gửi qua Gmail.",
            familyGroupId = familyGroup.Id,
            member = new
            {
                id = familyMember.Id,
                userId = targetUser.Id,
                username = targetUser.UserName,
                email = targetUser.Email,
                fullName = targetUser.FullName ?? targetUser.UserName,
                avatarUrl = targetUser.AvatarUrl,
                roleInFamily = familyMember.RoleInFamily,
                joinedAt = familyMember.JoinedAt
            }
        });
    }

    /// <summary>
    /// Xóa một thành viên ra khỏi Nhóm Gia Đình (Chỉ chủ gia đình mới có quyền)
    /// </summary>
    [HttpDelete("members/{memberId:int}")]
    public async Task<IActionResult> RemoveMember(int memberId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập." });
        }

        var member = await dbContext.FamilyMembers
            .Include(m => m.FamilyGroup)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (member == null)
        {
            return NotFound(new { message = "Không tìm thấy thành viên trong nhóm gia đình." });
        }

        // Kiểm tra quyền: Phải là chủ gia đình hoặc chính người đó tự rời
        var isOwner = member.FamilyGroup?.OwnerId == currentUserId;
        var isSelf = member.UserId == currentUserId;

        if (!isOwner && !isSelf)
        {
            return StatusCode(403, new { message = "Bạn không có quyền xóa thành viên này khỏi nhóm gia đình." });
        }

        // Nếu xóa tài khoản con, gỡ liên kết FamilyGroupId trên hồ sơ Kid
        if (member.RoleInFamily == "Child")
        {
            var kid = await dbContext.Kids.FirstOrDefaultAsync(k => k.KidUserId == member.UserId && k.FamilyGroupId == member.FamilyGroupId);
            if (kid != null)
            {
                kid.FamilyGroupId = null;
            }
        }

        dbContext.FamilyMembers.Remove(member);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = $"Đã xóa thành viên '{member.User?.UserName ?? "Thành viên"}' khỏi nhóm gia đình thành công."
        });
    }

    /// <summary>
    /// Đổi tên Nhóm Gia Đình
    /// </summary>
    [HttpPut("rename")]
    public async Task<IActionResult> Rename([FromBody] RenameFamilyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Tên nhóm gia đình không được để trống." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập." });
        }

        var group = await GetOrCreateFamilyGroupAsync(currentUserId);
        if (group.OwnerId != currentUserId)
        {
            return StatusCode(403, new { message = "Chỉ chủ gia đình mới có quyền đổi tên nhóm." });
        }

        group.Name = dto.Name.Trim();
        group.Description = dto.Description?.Trim();
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Đổi tên nhóm gia đình thành công!",
            group = new { group.Id, group.Name, group.Description }
        });
    }

    // Helper: Tìm hoặc tạo mới FamilyGroup cho người dùng
    private async Task<FamilyGroup> GetOrCreateFamilyGroupAsync(string userId)
    {
        var group = await dbContext.FamilyGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.OwnerId == userId || g.Members.Any(m => m.UserId == userId));

        if (group != null) return group;

        var user = await userManager.FindByIdAsync(userId);
        var userName = user?.FullName ?? user?.UserName ?? "Phụ huynh";

        group = new FamilyGroup
        {
            Name = $"Gia đình của {userName}",
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.FamilyGroups.Add(group);
        await dbContext.SaveChangesAsync();

        // Thêm chính người tạo làm Owner trong bảng FamilyMembers
        dbContext.FamilyMembers.Add(new FamilyMember
        {
            FamilyGroupId = group.Id,
            UserId = userId,
            RoleInFamily = "Owner",
            JoinedAt = DateTime.UtcNow
        });

        // Gắn các bé hiện có của user vào nhóm gia đình
        var userKids = await dbContext.Kids.Where(k => k.ParentId == userId && k.FamilyGroupId == null).ToListAsync();
        foreach (var uk in userKids)
        {
            uk.FamilyGroupId = group.Id;
        }

        await dbContext.SaveChangesAsync();
        return group;
    }

    /// <summary>
    /// TÍNH NĂNG MỚI: Xin / Tạo mã QR mới (Thời hạn đúng 1 phút) để bé quét tham gia nhóm gia đình
    /// </summary>
    [HttpPost("generate-qr")]
    public async Task<IActionResult> GenerateFamilyQr([FromBody] GenerateQrRequest? request)
    {
        // 1. Xác định phụ huynh tạo mã (từ Claims hoặc fallback nếu chưa đăng nhập qua JWT)
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId) && !string.IsNullOrWhiteSpace(request?.UserId))
        {
            currentUserId = request.UserId;
        }

        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để tạo mã QR gia đình." });
        }

        var currentUser = await userManager.FindByIdAsync(currentUserId);
        var familyGroup = await GetOrCreateFamilyGroupAsync(currentUserId);

        // 2. Tạo Invite Token và ShortCode (6 số dự phòng)
        var inviteToken = Guid.NewGuid().ToString("N");
        var shortCode = Random.Shared.Next(100000, 999999).ToString();

        // 3. Thời điểm hết hạn CHÍNH XÁC 60 GIÂY (1 PHÚT)
        var nowUtc = DateTime.UtcNow;
        var expiresAt = nowUtc.AddSeconds(60);

        var invite = new FamilyQrInvite
        {
            FamilyGroupId = familyGroup.Id,
            CreatedByUserId = currentUserId,
            InviteToken = inviteToken,
            ShortCode = shortCode,
            CreatedAt = nowUtc,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        dbContext.FamilyQrInvites.Add(invite);
        await dbContext.SaveChangesAsync();

        // 4. Sinh URL hoặc payload để quét
        var scheme = Request?.Scheme ?? "http";
        var host = Request?.Host.Value ?? "localhost:5231";
        var joinUrl = $"{scheme}://{host}/join-family?token={inviteToken}";

        // 5. Sinh ảnh QR code đa tầng: Base64 PNG 32-bit & SVG Vector bằng QRCoder
        string qrCodeDataUrl = "";
        string qrCodeSvg = "";
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(joinUrl, QRCodeGenerator.ECCLevel.Q);

            // 5.1 PNG 32-bit RGBA (Đen tuyền trên nền trắng tinh khiết, có viền an toàn quiet zones)
            using var pngQrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = pngQrCode.GetGraphic(15, new byte[] { 0, 0, 0, 255 }, new byte[] { 255, 255, 255, 255 }, true);
            qrCodeDataUrl = "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);

            // 5.2 SVG Vector chuẩn (Hiển thị siêu nét trên mọi trình duyệt, không phụ thuộc định dạng)
            using var svgQrCode = new SvgQRCode(qrCodeData);
            qrCodeSvg = svgQrCode.GetGraphic(10, "#000000", "#ffffff", true);
        }
        catch
        {
            qrCodeDataUrl = "";
            qrCodeSvg = "";
        }

        return Ok(new
        {
            token = inviteToken,
            shortCode,
            durationSeconds = 60,
            expiresAt,
            qrCodeDataUrl,
            qrCodeSvg,
            joinUrl,
            familyGroupId = familyGroup.Id,
            familyGroupName = familyGroup.Name,
            ownerName = currentUser?.FullName ?? currentUser?.UserName ?? "Phụ huynh"
        });
    }

    /// <summary>
    /// Kiểm tra trạng thái mã QR (Thời gian còn lại trong 1 phút, đã được quét chưa)
    /// </summary>
    [HttpGet("qr-status/{token}")]
    public async Task<IActionResult> GetQrStatus(string token)
    {
        var invite = await dbContext.FamilyQrInvites
            .Include(q => q.FamilyGroup)
            .Include(q => q.UsedByUser)
            .FirstOrDefaultAsync(q => q.InviteToken == token || q.ShortCode == token);

        if (invite == null)
        {
            return NotFound(new { message = "Mã QR không tồn tại." });
        }

        var isExpired = DateTime.UtcNow > invite.ExpiresAt;
        var remainingSeconds = Math.Max(0, (int)(invite.ExpiresAt - DateTime.UtcNow).TotalSeconds);

        return Ok(new
        {
            token = invite.InviteToken,
            shortCode = invite.ShortCode,
            isExpired,
            remainingSeconds,
            isUsed = invite.IsUsed,
            usedAt = invite.UsedAt,
            usedByUserName = invite.UsedByUser?.FullName ?? invite.UsedByUser?.UserName,
            familyGroupName = invite.FamilyGroup?.Name
        });
    }

    /// <summary>
    /// Bé hoặc thiết bị con quét mã QR (hoặc nhập PIN 6 số) để tham gia nhóm gia đình
    /// </summary>
    [HttpPost("join-by-qr")]
    public async Task<IActionResult> JoinByQr([FromBody] JoinByQrRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) && string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "Vui lòng cung cấp mã QR (token) hoặc mã PIN 6 số." });
        }

        var tokenOrCode = (request.Token ?? request.Code)?.Trim();

        // 1. Tìm bản ghi QR invite
        var invite = await dbContext.FamilyQrInvites
            .Include(q => q.FamilyGroup)
            .FirstOrDefaultAsync(q => q.InviteToken == tokenOrCode || q.ShortCode == tokenOrCode);

        if (invite == null)
        {
            return NotFound(new { message = "Mã QR hoặc mã PIN không hợp lệ hoặc không tìm thấy." });
        }

        // 2. KIỂM TRA HIỆU LỰC 1 PHÚT
        if (DateTime.UtcNow > invite.ExpiresAt)
        {
            return BadRequest(new
            {
                message = "⚠️ Mã QR này đã HẾT HẠN sau 1 phút! Hãy yêu cầu phụ huynh tạo mã mới để quét lại nhé.",
                isExpired = true
            });
        }

        // 3. Kiểm tra xem mã đã dùng chưa
        if (invite.IsUsed)
        {
            return BadRequest(new
            {
                message = "Mã QR này đã được sử dụng rồi. Mỗi mã chỉ dùng được 1 lần.",
                isUsed = true
            });
        }

        // 4. Xác định tài khoản của Bé
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId) && !string.IsNullOrWhiteSpace(request.UserId))
        {
            currentUserId = request.UserId;
        }

        ApplicationUser? joiningUser = null;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            joiningUser = await userManager.FindByIdAsync(currentUserId);
        }
        else if (!string.IsNullOrWhiteSpace(request.Username))
        {
            joiningUser = await userManager.FindByNameAsync(request.Username.Trim()) ?? await userManager.FindByEmailAsync(request.Username.Trim());
        }

        // Nếu bé chưa đăng nhập hoặc chưa có tài khoản, tự động tạo tài khoản con cho bé
        if (joiningUser == null)
        {
            var kidUsername = !string.IsNullOrWhiteSpace(request.Username)
                ? request.Username.Trim().ToLowerInvariant()
                : "kid_" + Random.Shared.Next(1000, 9999);
            var kidFullName = !string.IsNullOrWhiteSpace(request.KidFullName)
                ? request.KidFullName.Trim()
                : "Bé " + kidUsername;

            joiningUser = new ApplicationUser
            {
                UserName = kidUsername,
                Email = $"{kidUsername}@kidcare.local",
                FullName = kidFullName,
                AvatarUrl = $"https://api.dicebear.com/7.x/bottts/svg?seed={Uri.EscapeDataString(kidUsername)}"
            };

            var pwd = !string.IsNullOrWhiteSpace(request.Password) ? request.Password : "Kid@123";
            var createResult = await userManager.CreateAsync(joiningUser, pwd);
            if (!createResult.Succeeded)
            {
                return BadRequest(new { message = "Không thể tạo tài khoản cho bé: " + string.Join(", ", createResult.Errors.Select(e => e.Description)) });
            }

            if (!await roleManager.RoleExistsAsync("Kid")) await roleManager.CreateAsync(new IdentityRole("Kid"));
            await userManager.AddToRoleAsync(joiningUser, "Kid");
        }

        // Đảm bảo user có role Kid
        if (!await roleManager.RoleExistsAsync("Kid")) await roleManager.CreateAsync(new IdentityRole("Kid"));
        if (!await userManager.IsInRoleAsync(joiningUser, "Kid")) await userManager.AddToRoleAsync(joiningUser, "Kid");

        // 5. Thêm bé vào FamilyMembers nếu chưa có
        var isAlreadyInGroup = await dbContext.FamilyMembers
            .AnyAsync(m => m.FamilyGroupId == invite.FamilyGroupId && m.UserId == joiningUser.Id);

        if (!isAlreadyInGroup)
        {
            var newMember = new FamilyMember
            {
                FamilyGroupId = invite.FamilyGroupId,
                UserId = joiningUser.Id,
                RoleInFamily = "Child",
                JoinedAt = DateTime.UtcNow
            };
            dbContext.FamilyMembers.Add(newMember);
        }

        // 6. Gán hoặc tạo hồ sơ Kid
        var existingKid = await dbContext.Kids.FirstOrDefaultAsync(k => k.KidUserId == joiningUser.Id);
        if (existingKid != null)
        {
            existingKid.FamilyGroupId = invite.FamilyGroupId;
            existingKid.ParentId ??= invite.CreatedByUserId;
            existingKid.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var newKid = new Kid
            {
                FullName = !string.IsNullOrWhiteSpace(joiningUser.FullName) ? joiningUser.FullName : joiningUser.UserName ?? "Bé yêu",
                AvatarUrl = joiningUser.AvatarUrl,
                ParentId = invite.CreatedByUserId,
                KidUserId = joiningUser.Id,
                FamilyGroupId = invite.FamilyGroupId,
                TotalPoints = 0,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Kids.Add(newKid);
        }

        // 7. Đánh dấu mã QR đã sử dụng
        invite.IsUsed = true;
        invite.UsedAt = DateTime.UtcNow;
        invite.UsedByUserId = joiningUser.Id;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"🎉 Chúc mừng bé '{joiningUser.FullName ?? joiningUser.UserName}' đã quét mã QR thành công và gia nhập Nhóm Gia Đình '{invite.FamilyGroup?.Name}'!",
            familyGroupId = invite.FamilyGroupId,
            familyGroupName = invite.FamilyGroup?.Name,
            user = new
            {
                joiningUser.Id,
                username = joiningUser.UserName,
                fullName = joiningUser.FullName ?? joiningUser.UserName,
                avatarUrl = joiningUser.AvatarUrl
            }
        });
    }
}

public class GenerateQrRequest
{
    public string? UserId { get; set; }
}

public class JoinByQrRequest
{
    public string? Token { get; set; }
    public string? Code { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? KidFullName { get; set; }
}

public record CheckEmailDto(
    string Email
);

public record AddFamilyMemberByEmailDto(
    string Email,
    string? RoleInFamily = "Child",
    string? KidFullName = null
);

public record RenameFamilyDto(
    string Name,
    string? Description = null
);
