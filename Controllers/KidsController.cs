using System.Security.Claims;
using Giahuy0790.Data;
using Giahuy0790.Models;
using Giahuy0790.Repositories;
using Giahuy0790.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Controllers;

/// <summary>
/// Controller API quản lý Hồ sơ Trẻ em (Kids) & Quản lý Tài khoản con (Sub-accounts):
/// Thêm / Sửa / Xóa / Liên kết tài khoản con / Tạo mới tài khoản con / Đổi mật khẩu tài khoản con / Gửi danh sách qua Gmail
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class KidsController(
    IKidRepository kidRepo,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IEmailService emailService,
    AppDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách Trẻ em (Admin xem tất cả, Phụ huynh xem đúng các bé của mình hoặc gia đình mình, Trẻ xem hồ sơ chính mình)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isParent = User.IsInRole("Parents") || User.IsInRole("parents");
        var isKid = User.IsInRole("Kid") || User.IsInRole("kid");
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        // 1. Nếu là tài khoản con (Kid): chỉ xem hồ sơ của chính mình
        if (isKid && !string.IsNullOrEmpty(userId))
        {
            var myKidProfiles = await kidRepo.GetKidsByKidUserAsync(userId);
            return Ok(myKidProfiles);
        }

        // 2. Nếu là Phụ huynh (Parents): chỉ xem đúng các bé do mình quản lý hoặc trong nhóm gia đình của mình
        if (isParent && !string.IsNullOrEmpty(userId))
        {
            var myKids = await kidRepo.GetKidsByParentAsync(userId);
            return Ok(myKids);
        }

        // 3. Nếu là Admin: xem toàn bộ hệ thống
        if (isAdmin)
        {
            var allKids = await kidRepo.GetAllAsync();
            return Ok(allKids);
        }

        // 4. Nếu người dùng đã đăng nhập (bất kể role nào): chỉ xem các bé của chính người dùng đó
        if (!string.IsNullOrEmpty(userId))
        {
            var myKids = await kidRepo.GetKidsByParentAsync(userId);
            return Ok(myKids);
        }

        // 5. Khách vãng lai (Chưa đăng nhập): Tuyệt đối KHÔNG trả về danh sách trẻ của người khác!
        return Ok(Array.Empty<Kid>());
    }

    /// <summary>
    /// Lấy chi tiết hồ sơ một trẻ kèm lịch hoạt động và thông tin tài khoản con (có kiểm tra quyền phụ huynh)
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var kid = await kidRepo.GetKidWithActivitiesAsync(id);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && !string.IsNullOrEmpty(currentUserId))
        {
            var isOwner = kid.ParentId == currentUserId;
            var isKidSelf = kid.KidUserId == currentUserId;
            var isFamilyCoParent = kid.FamilyGroupId != null && await dbContext.FamilyMembers.AnyAsync(m => m.FamilyGroupId == kid.FamilyGroupId && m.UserId == currentUserId);

            if (!isOwner && !isKidSelf && !isFamilyCoParent)
            {
                return StatusCode(403, new { message = "Bạn không có quyền xem thông tin hồ sơ của trẻ này." });
            }
        }
        else if (!isAdmin && string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để xem thông tin trẻ." });
        }

        return Ok(kid);
    }

    /// <summary>
    /// Lấy hồ sơ của tài khoản con đang đăng nhập hiện tại
    /// </summary>
    [HttpGet("my-profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để xem hồ sơ." });
        }

        var kids = await kidRepo.GetKidsByKidUserAsync(userId);
        var kid = kids.FirstOrDefault();
        if (kid == null)
        {
            return NotFound(new { message = "Tài khoản của bạn chưa được liên kết với hồ sơ trẻ nào." });
        }

        return Ok(kid);
    }

    /// <summary>
    /// Tìm kiếm trẻ theo họ tên, tên gọi ở nhà, trường học hoặc tên tài khoản con
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? keyword)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isParent = User.IsInRole("Parents") || User.IsInRole("parents");

        string? parentFilter = isParent && !string.IsNullOrEmpty(userId) ? userId : null;
        var results = await kidRepo.SearchKidsAsync(keyword ?? string.Empty, parentFilter);
        return Ok(results);
    }

    /// <summary>
    /// Lấy danh sách các tài khoản người dùng đã đăng ký trong hệ thống (kèm Gmail) để Phụ huynh xem và đưa vào danh sách con
    /// </summary>
    [HttpGet("registered-users")]
    [HttpGet("search-subaccounts")]
    public async Task<IActionResult> GetRegisteredUsers([FromQuery] string? keyword = null)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(u => u.Id != currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(u => (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                                     (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                                     (u.FullName != null && u.FullName.ToLower().Contains(kw)));
        }

        var matchedUsers = await query
            .OrderByDescending(u => u.CreatedAt)
            .Take(30)
            .ToListAsync();

        var allKids = await kidRepo.GetAllAsync();
        var linkedMap = allKids
            .Where(k => !string.IsNullOrEmpty(k.KidUserId))
            .ToDictionary(k => k.KidUserId!, k => k);

        var results = matchedUsers.Select(u => new
        {
            id = u.Id,
            username = u.UserName,
            email = u.Email,
            fullName = u.FullName,
            avatarUrl = u.AvatarUrl,
            createdAt = u.CreatedAt,
            isAlreadyLinked = linkedMap.ContainsKey(u.Id),
            linkedKidName = linkedMap.TryGetValue(u.Id, out var linkedKid) ? linkedKid.FullName : null
        });

        return Ok(results);
    }

    /// <summary>
    /// Thêm mới hồ sơ trẻ em (Hỗ trợ tùy chọn tạo kèm hoặc liên kết tài khoản con ngay khi tạo)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateKidDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new { message = "Họ và tên của trẻ không được để trống." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập tài khoản Phụ huynh để thêm trẻ." });
        }

        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        var assignedParentId = (isAdmin && !string.IsNullOrEmpty(dto.ParentId)) ? dto.ParentId : currentUserId;

        // Tự động gắn vào nhóm gia đình của phụ huynh này nếu có
        var parentFamilyGroup = await dbContext.FamilyGroups
            .FirstOrDefaultAsync(g => g.OwnerId == currentUserId || g.Members.Any(m => m.UserId == currentUserId));

        var kid = new Kid
        {
            FullName = dto.FullName.Trim(),
            Nickname = dto.Nickname?.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender?.Trim() ?? "Nam",
            AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl)
                ? $"https://api.dicebear.com/7.x/bottts/svg?seed={Uri.EscapeDataString(dto.FullName.Trim())}"
                : dto.AvatarUrl.Trim(),
            SchoolName = dto.SchoolName?.Trim(),
            HealthNotes = dto.HealthNotes?.Trim(),
            Interests = dto.Interests?.Trim(),
            ParentId = assignedParentId,
            FamilyGroupId = parentFamilyGroup?.Id,
            TotalPoints = dto.InitialPoints ?? 0,
            CreatedAt = DateTime.UtcNow
        };

        // 1. Tùy chọn tạo nhanh tài khoản con mới cho bé
        if (dto.CreateSubAccount && !string.IsNullOrWhiteSpace(dto.SubAccountUsername) && !string.IsNullOrWhiteSpace(dto.SubAccountPassword))
        {
            var username = dto.SubAccountUsername.Trim();
            if (await userManager.FindByNameAsync(username) != null)
            {
                return BadRequest(new { message = $"Tên đăng nhập tài khoản con '{username}' đã được sử dụng. Vui lòng chọn tên khác." });
            }

            var email = !string.IsNullOrWhiteSpace(dto.SubAccountEmail)
                ? dto.SubAccountEmail.Trim()
                : $"{username.ToLower()}@kidcare.local";

            var existingEmail = await userManager.FindByEmailAsync(email);
            if (existingEmail != null)
            {
                email = $"{username.ToLower()}_{Guid.NewGuid().ToString("N")[..4]}@kidcare.local";
            }

            var childUser = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = kid.FullName,
                AvatarUrl = kid.AvatarUrl,
                CreatedAt = DateTime.UtcNow
            };

            var createRes = await userManager.CreateAsync(childUser, dto.SubAccountPassword);
            if (!createRes.Succeeded)
            {
                return BadRequest(new { message = "Không thể tạo tài khoản con", errors = createRes.Errors.Select(e => e.Description) });
            }

            if (!await roleManager.RoleExistsAsync("Kid"))
            {
                await roleManager.CreateAsync(new IdentityRole("Kid"));
            }
            await userManager.AddToRoleAsync(childUser, "Kid");

            kid.KidUserId = childUser.Id;
        }
        // 2. Hoặc tùy chọn liên kết tài khoản có sẵn
        else if (!string.IsNullOrWhiteSpace(dto.ExistingSubAccountUsernameOrEmail))
        {
            var identifier = dto.ExistingSubAccountUsernameOrEmail.Trim();
            var targetUser = await userManager.FindByNameAsync(identifier) ?? await userManager.FindByEmailAsync(identifier);
            if (targetUser != null)
            {
                var existingKid = await kidRepo.FindByKidUserIdAsync(targetUser.Id);
                if (existingKid == null && targetUser.Id != currentUserId)
                {
                    if (!await roleManager.RoleExistsAsync("Kid"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Kid"));
                    }
                    if (!await userManager.IsInRoleAsync(targetUser, "Kid"))
                    {
                        await userManager.AddToRoleAsync(targetUser, "Kid");
                    }
                    kid.KidUserId = targetUser.Id;
                }
            }
        }

        var created = await kidRepo.AddAsync(kid);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Cập nhật thông tin hồ sơ của trẻ
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateKidDto dto)
    {
        var kid = await kidRepo.GetByIdAsync(id);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy trẻ với ID = {id}" });
        }

        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new { message = "Họ và tên không được để trống." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để cập nhật hồ sơ." });
        }

        var isOwner = kid.ParentId == currentUserId;
        var isFamilyCoParent = kid.FamilyGroupId != null && await dbContext.FamilyMembers.AnyAsync(m => m.FamilyGroupId == kid.FamilyGroupId && m.UserId == currentUserId);

        if (!isAdmin && !isOwner && !isFamilyCoParent)
        {
            return StatusCode(403, new { message = "Bạn không có quyền chỉnh sửa hồ sơ trẻ này." });
        }

        kid.FullName = dto.FullName.Trim();
        kid.Nickname = dto.Nickname?.Trim();
        kid.DateOfBirth = dto.DateOfBirth;
        kid.Gender = dto.Gender?.Trim() ?? kid.Gender;
        kid.AvatarUrl = dto.AvatarUrl?.Trim() ?? kid.AvatarUrl;
        kid.SchoolName = dto.SchoolName?.Trim();
        kid.HealthNotes = dto.HealthNotes?.Trim();
        kid.Interests = dto.Interests?.Trim();
        kid.UpdatedAt = DateTime.UtcNow;

        await kidRepo.UpdateAsync(kid);

        return Ok(new
        {
            message = "Cập nhật hồ sơ trẻ thành công",
            kid
        });
    }

    /// <summary>
    /// Phụ huynh liên kết một tài khoản khác đã có sẵn trong hệ thống để làm tài khoản con của mình
    /// </summary>
    [HttpPost("{id:int}/link-account")]
    public async Task<IActionResult> LinkAccount(int id, [FromBody] LinkSubAccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UsernameOrEmail))
        {
            return BadRequest(new { message = "Vui lòng nhập Tên đăng nhập (Username) hoặc Email của tài khoản cần liên kết." });
        }

        var kid = await kidRepo.GetKidWithDetailsAsync(id);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && !string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(kid.ParentId) && kid.ParentId != currentUserId)
        {
            return StatusCode(403, new { message = "Bạn không có quyền quản lý hồ sơ trẻ này." });
        }

        var identifier = dto.UsernameOrEmail.Trim();
        var targetUser = await userManager.FindByNameAsync(identifier) ?? await userManager.FindByEmailAsync(identifier);
        if (targetUser == null)
        {
            return NotFound(new { message = $"Không tìm thấy người dùng nào với Username hoặc Email: '{identifier}'" });
        }

        if (!string.IsNullOrEmpty(currentUserId) && targetUser.Id == currentUserId)
        {
            return BadRequest(new { message = "Bạn không thể liên kết chính tài khoản của bạn làm tài khoản con." });
        }

        // Kiểm tra xem tài khoản này đã được gán cho hồ sơ trẻ nào khác chưa
        var existingKid = await kidRepo.FindByKidUserIdAsync(targetUser.Id);
        if (existingKid != null && existingKid.Id != id)
        {
            return BadRequest(new { message = $"Tài khoản '{targetUser.UserName}' hiện đã được liên kết với hồ sơ bé '{existingKid.FullName}'. Mỗi tài khoản con chỉ nên đại diện cho một bé." });
        }

        // Đảm bảo role Kid tồn tại và gán quyền Kid cho tài khoản con
        if (!await roleManager.RoleExistsAsync("Kid"))
        {
            await roleManager.CreateAsync(new IdentityRole("Kid"));
        }
        if (!await userManager.IsInRoleAsync(targetUser, "Kid"))
        {
            await userManager.AddToRoleAsync(targetUser, "Kid");
        }

        // Đảm bảo ParentId được gắn vào phụ huynh hiện tại
        if (string.IsNullOrEmpty(kid.ParentId) && !string.IsNullOrEmpty(currentUserId))
        {
            kid.ParentId = currentUserId;
        }

        kid.KidUserId = targetUser.Id;
        kid.UpdatedAt = DateTime.UtcNow;
        await kidRepo.UpdateAsync(kid);

        // Gửi Gmail thông báo tự động cho cả Phụ huynh và Bé
        _ = Task.Run(async () =>
        {
            try
            {
                var parentUser = !string.IsNullOrEmpty(currentUserId) ? await userManager.FindByIdAsync(currentUserId) : null;
                var parentName = parentUser?.FullName ?? parentUser?.UserName ?? "Phụ huynh";

                // 1. Gửi Gmail cho tài khoản con
                if (!string.IsNullOrWhiteSpace(targetUser.Email))
                {
                    await emailService.SendChildLinkedNotificationAsync(
                        targetUser.Email,
                        targetUser.FullName ?? targetUser.UserName ?? "Bé yêu",
                        parentName,
                        kid.FullName,
                        targetUser.UserName ?? string.Empty,
                        targetUser.Email,
                        isParent: false
                    );
                }

                // 2. Gửi Gmail cho Phụ huynh
                if (parentUser != null && !string.IsNullOrWhiteSpace(parentUser.Email))
                {
                    await emailService.SendChildLinkedNotificationAsync(
                        parentUser.Email,
                        parentName,
                        parentName,
                        kid.FullName,
                        targetUser.UserName ?? string.Empty,
                        targetUser.Email ?? string.Empty,
                        isParent: true
                    );
                }
            }
            catch
            {
                // Tránh lỗi ngắt background task
            }
        });

        return Ok(new
        {
            message = $"🎉 Đã liên kết thành công tài khoản '{targetUser.UserName}' làm tài khoản con cho bé {kid.FullName}! Email thông báo đã được gửi qua Gmail.",
            kidId = kid.Id,
            kidName = kid.FullName,
            childAccount = new
            {
                id = targetUser.Id,
                username = targetUser.UserName,
                email = targetUser.Email,
                fullName = targetUser.FullName
            }
        });
    }

    /// <summary>
    /// Phụ huynh tạo mới một tài khoản con trực tiếp cho bé (Cấp Tên đăng nhập và Mật khẩu để bé tự đăng nhập)
    /// </summary>
    [HttpPost("{id:int}/create-subaccount")]
    public async Task<IActionResult> CreateSubAccount(int id, [FromBody] CreateSubAccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Tên đăng nhập và mật khẩu không được để trống." });
        }

        if (dto.Password.Length < 6)
        {
            return BadRequest(new { message = "Mật khẩu cho bé phải có tối thiểu 6 ký tự." });
        }

        var kid = await kidRepo.GetKidWithDetailsAsync(id);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && !string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(kid.ParentId) && kid.ParentId != currentUserId)
        {
            return StatusCode(403, new { message = "Bạn không có quyền quản lý hồ sơ trẻ này." });
        }

        if (!string.IsNullOrEmpty(kid.KidUserId))
        {
            return BadRequest(new { message = "Hồ sơ bé này đã có tài khoản con liên kết rồi. Vui lòng hủy liên kết tài khoản cũ trước nếu bạn muốn tạo tài khoản mới." });
        }

        var username = dto.Username.Trim();
        var existingUser = await userManager.FindByNameAsync(username);
        if (existingUser != null)
        {
            return BadRequest(new { message = $"Tên đăng nhập '{username}' đã được sử dụng. Vui lòng chọn tên đăng nhập khác cho bé." });
        }

        var email = !string.IsNullOrWhiteSpace(dto.Email)
            ? dto.Email.Trim()
            : $"{username.ToLower()}@kidcare.local";

        var existingEmail = await userManager.FindByEmailAsync(email);
        if (existingEmail != null)
        {
            email = $"{username.ToLower()}_{Guid.NewGuid().ToString("N")[..4]}@kidcare.local";
        }

        var childUser = new ApplicationUser
        {
            UserName = username,
            Email = email,
            FullName = string.IsNullOrWhiteSpace(dto.FullName) ? kid.FullName : dto.FullName.Trim(),
            AvatarUrl = kid.AvatarUrl ?? $"https://api.dicebear.com/7.x/bottts/svg?seed={Uri.EscapeDataString(username)}",
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(childUser, dto.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { message = "Không thể tạo tài khoản con", errors = createResult.Errors.Select(e => e.Description) });
        }

        if (!await roleManager.RoleExistsAsync("Kid"))
        {
            await roleManager.CreateAsync(new IdentityRole("Kid"));
        }
        await userManager.AddToRoleAsync(childUser, "Kid");

        if (string.IsNullOrEmpty(kid.ParentId) && !string.IsNullOrEmpty(currentUserId))
        {
            kid.ParentId = currentUserId;
        }

        kid.KidUserId = childUser.Id;
        kid.UpdatedAt = DateTime.UtcNow;
        await kidRepo.UpdateAsync(kid);

        return Ok(new
        {
            message = $"🎉 Đã tạo mới và liên kết tài khoản con '{username}' thành công cho bé {kid.FullName}!",
            kidId = kid.Id,
            kidName = kid.FullName,
            childAccount = new
            {
                id = childUser.Id,
                username = childUser.UserName,
                email = childUser.Email,
                fullName = childUser.FullName
            }
        });
    }

    /// <summary>
    /// Hủy liên kết tài khoản con khỏi hồ sơ bé
    /// </summary>
    [HttpPost("{id:int}/unlink-account")]
    public async Task<IActionResult> UnlinkAccount(int id)
    {
        var kid = await kidRepo.GetKidWithDetailsAsync(id);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && !string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(kid.ParentId) && kid.ParentId != currentUserId)
        {
            return StatusCode(403, new { message = "Bạn không có quyền quản lý hồ sơ trẻ này." });
        }

        if (string.IsNullOrEmpty(kid.KidUserId))
        {
            return BadRequest(new { message = "Hồ sơ bé này hiện chưa liên kết tài khoản con nào." });
        }

        kid.KidUserId = null;
        kid.UpdatedAt = DateTime.UtcNow;
        await kidRepo.UpdateAsync(kid);

        return Ok(new
        {
            message = $"Đã hủy liên kết tài khoản con khỏi hồ sơ bé {kid.FullName} thành công.",
            kidId = kid.Id
        });
    }

    /// <summary>
    /// Phụ huynh hỗ trợ đổi mật khẩu mới cho tài khoản con của bé khi bé quên mật khẩu
    /// </summary>
    [HttpPost("{id:int}/reset-subaccount-password")]
    public async Task<IActionResult> ResetSubAccountPassword(int id, [FromBody] ResetSubAccountPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "Mật khẩu mới phải có tối thiểu 6 ký tự." });
        }

        var kid = await kidRepo.GetKidWithDetailsAsync(id);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && !string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(kid.ParentId) && kid.ParentId != currentUserId)
        {
            return StatusCode(403, new { message = "Bạn không có quyền quản lý hồ sơ trẻ này." });
        }

        if (string.IsNullOrEmpty(kid.KidUserId))
        {
            return BadRequest(new { message = "Hồ sơ bé chưa có tài khoản con để đổi mật khẩu." });
        }

        var childUser = await userManager.FindByIdAsync(kid.KidUserId);
        if (childUser == null)
        {
            return NotFound(new { message = "Không tìm thấy dữ liệu tài khoản con trong hệ thống." });
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(childUser);
        var resetResult = await userManager.ResetPasswordAsync(childUser, resetToken, dto.NewPassword);
        if (!resetResult.Succeeded)
        {
            return BadRequest(new { message = "Đổi mật khẩu thất bại", errors = resetResult.Errors.Select(e => e.Description) });
        }

        return Ok(new
        {
            message = $"🔑 Đã đổi mật khẩu cho tài khoản con '{childUser.UserName}' của bé {kid.FullName} thành công!"
        });
    }

    /// <summary>
    /// Thưởng điểm / Sao khen ngợi cho trẻ khi làm việc tốt
    /// </summary>
    [HttpPost("{id:int}/points")]
    public async Task<IActionResult> RewardPoints(int id, [FromBody] RewardPointsDto dto)
    {
        var success = await kidRepo.AddRewardPointsAsync(id, dto.Points);
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }

        return Ok(new
        {
            message = $"🎉 Đã thưởng thành công {dto.Points} điểm/sao khen ngợi cho bé!"
        });
    }

    /// <summary>
    /// Xóa hồ sơ trẻ
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var kid = await kidRepo.GetByIdAsync(id);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để xóa hồ sơ." });
        }

        var isOwner = kid.ParentId == currentUserId;
        if (!isAdmin && !isOwner)
        {
            return StatusCode(403, new { message = "Chỉ phụ huynh sở hữu hồ sơ mới có quyền xóa bé này." });
        }

        var success = await kidRepo.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }
        return Ok(new { message = "Đã xóa hồ sơ trẻ thành công" });
    }

    /// <summary>
    /// Gửi danh sách toàn bộ các bé và tài khoản con về Gmail của Phụ huynh đang đăng nhập
    /// </summary>
    [HttpPost("send-summary-email")]
    public async Task<IActionResult> SendSummaryEmail()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để nhận báo cáo qua Gmail." });
        }

        var parentUser = await userManager.FindByIdAsync(currentUserId);
        if (parentUser == null || string.IsNullOrWhiteSpace(parentUser.Email))
        {
            return BadRequest(new { message = "Tài khoản của bạn chưa có địa chỉ Gmail hợp lệ để nhận email." });
        }

        var myKids = (await kidRepo.GetKidsByParentAsync(currentUserId)).ToList();
        if (myKids.Count == 0)
        {
            return BadRequest(new { message = "Bạn hiện chưa có bé nào trong danh sách con để gửi báo cáo." });
        }

        var summaryItems = myKids.Select(k => new KidSummaryItem(
            k.Id,
            k.FullName,
            k.Nickname,
            k.SchoolName,
            k.TotalPoints,
            k.HasKidAccount,
            k.KidUserName,
            k.KidUserEmail
        ));

        var parentName = string.IsNullOrWhiteSpace(parentUser.FullName) ? parentUser.UserName : parentUser.FullName;
        var sent = await emailService.SendKidsSummaryEmailAsync(parentUser.Email, parentName ?? "Quý Phụ Huynh", summaryItems);

        if (!sent)
        {
            return StatusCode(500, new { message = "Không thể gửi email qua máy chủ SMTP Gmail. Vui lòng kiểm tra lại cấu hình." });
        }

        return Ok(new
        {
            message = $"🎉 Đã gửi danh sách tổng hợp {myKids.Count} bé về Gmail: {parentUser.Email} thành công! Hãy kiểm tra hộp thư của bạn."
        });
    }

    /// <summary>
    /// Gửi thư mời qua Gmail đến người dùng hoặc bé để kết nối vào danh sách con
    /// </summary>
    [HttpPost("invite-by-email")]
    public async Task<IActionResult> InviteByEmail([FromBody] InviteChildEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TargetEmail))
        {
            return BadRequest(new { message = "Vui lòng cung cấp địa chỉ Gmail của người nhận." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var parentUser = !string.IsNullOrEmpty(currentUserId) ? await userManager.FindByIdAsync(currentUserId) : null;
        var parentName = parentUser?.FullName ?? parentUser?.UserName ?? "Phụ huynh";

        var kidName = "bé yêu";
        if (dto.KidId.HasValue)
        {
            var kid = await kidRepo.GetByIdAsync(dto.KidId.Value);
            if (kid != null) kidName = kid.FullName;
        }

        var sent = await emailService.SendInviteChildEmailAsync(dto.TargetEmail.Trim(), parentName, kidName);
        if (!sent)
        {
            return StatusCode(500, new { message = "Không thể gửi email mời qua Gmail. Vui lòng thử lại sau." });
        }

        return Ok(new
        {
            message = $"💌 Đã gửi lời mời tham gia qua Gmail đến: {dto.TargetEmail.Trim()} thành công!"
        });
    }
}

public record InviteChildEmailDto(
    string TargetEmail,
    int? KidId = null
);

public record CreateKidDto(
    string FullName,
    string? Nickname = null,
    DateTime? DateOfBirth = null,
    string? Gender = "Nam",
    string? AvatarUrl = null,
    string? SchoolName = null,
    string? HealthNotes = null,
    string? Interests = null,
    string? ParentId = null,
    int? InitialPoints = 0,
    bool CreateSubAccount = false,
    string? SubAccountUsername = null,
    string? SubAccountPassword = null,
    string? SubAccountEmail = null,
    string? ExistingSubAccountUsernameOrEmail = null
);

public record UpdateKidDto(
    string FullName,
    string? Nickname = null,
    DateTime? DateOfBirth = null,
    string? Gender = null,
    string? AvatarUrl = null,
    string? SchoolName = null,
    string? HealthNotes = null,
    string? Interests = null
);

public record LinkSubAccountDto(
    string UsernameOrEmail
);

public record CreateSubAccountDto(
    string Username,
    string Password,
    string? Email = null,
    string? FullName = null
);

public record ResetSubAccountPasswordDto(
    string NewPassword
);

public record RewardPointsDto(
    int Points
);
