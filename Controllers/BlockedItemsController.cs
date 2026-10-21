using System.Security.Claims;
using Giahuy0790.Data;
using Giahuy0790.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Controllers;

/// <summary>
/// Controller API quản lý Danh sách Chặn Trang Web & Ứng Dụng dành cho Trẻ (Parental Control Blocklist)
/// Hỗ trợ phụ huynh ngăn chặn trẻ truy cập các trang web độc hại, cờ bạc, 18+ và hạn chế dùng/tải ứng dụng gây nghiện
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BlockedItemsController(
    AppDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Lấy toàn bộ danh sách các bé và các quy tắc chặn của phụ huynh để hiển thị trên trang Quản lý Chặn tập trung
    /// </summary>
    [HttpGet("my-kids-overview")]
    public async Task<IActionResult> GetMyKidsOverview()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        List<Kid> myKids;
        if (isAdmin)
        {
            myKids = await dbContext.Kids.AsNoTracking().OrderBy(k => k.FullName).ToListAsync();
        }
        else if (!string.IsNullOrEmpty(currentUserId))
        {
            myKids = await GetMyKidsListAsync(currentUserId);
        }
        else
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập." });
        }

        var kidIds = myKids.Select(k => k.Id).ToList();

        var blockedItems = await dbContext.BlockedItems
            .AsNoTracking()
            .Where(b => kidIds.Contains(b.KidId))
            .Include(b => b.Kid)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.KidId,
                kidFullName = b.Kid != null ? b.Kid.FullName : "Bé",
                b.ItemType,
                b.Target,
                b.Title,
                b.Category,
                b.Reason,
                b.IsBlocked,
                b.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            kids = myKids.Select(k => new
            {
                k.Id,
                k.FullName,
                k.Nickname,
                k.AvatarUrl,
                blockedCount = blockedItems.Count(b => b.KidId == k.Id && b.IsBlocked)
            }),
            totalBlocked = blockedItems.Count(b => b.IsBlocked),
            items = blockedItems
        });
    }

    /// <summary>
    /// Thêm chặn một link (Web / App) cho TẤT CẢ các bé trong gia đình cùng một lúc
    /// </summary>
    [HttpPost("add-to-all")]
    public async Task<IActionResult> AddToAllKids([FromBody] CreateBlockedItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Target))
        {
            return BadRequest(new { message = "Vui lòng nhập đường link hoặc tên ứng dụng cần chặn." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập." });
        }

        var myKids = await GetMyKidsListAsync(currentUserId);
        if (myKids.Count == 0)
        {
            return BadRequest(new { message = "Bạn chưa có hồ sơ bé nào để thiết lập chặn." });
        }

        var (normalizedType, normalizedTarget, finalTitle, finalCategory) = ParseInputLinkOrTarget(
            dto.Target,
            dto.ItemType,
            dto.Title,
            dto.Category);

        int addedCount = 0;
        int reactivatedCount = 0;

        foreach (var kid in myKids)
        {
            var existing = await dbContext.BlockedItems
                .FirstOrDefaultAsync(b => b.KidId == kid.Id && b.Target.ToLower() == normalizedTarget.ToLower());

            if (existing != null)
            {
                if (!existing.IsBlocked)
                {
                    existing.IsBlocked = true;
                    existing.UpdatedAt = DateTime.UtcNow;
                    reactivatedCount++;
                }
            }
            else
            {
                dbContext.BlockedItems.Add(new BlockedItem
                {
                    KidId = kid.Id,
                    ItemType = normalizedType,
                    Target = normalizedTarget,
                    Title = finalTitle,
                    Category = finalCategory,
                    Reason = dto.Reason?.Trim(),
                    IsBlocked = true,
                    CreatedByUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                });
                addedCount++;
            }
        }

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = $"Đã áp dụng chặn '{finalTitle}' cho toàn bộ {myKids.Count} bé trong gia đình (Thêm mới {addedCount}, kích hoạt lại {reactivatedCount}).",
            addedCount,
            reactivatedCount
        });
    }

    /// <summary>
    /// Xóa nhiều mục cùng lúc
    /// </summary>
    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteDto dto)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn các mục cần xóa." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        var items = await dbContext.BlockedItems
            .Include(b => b.Kid)
            .Where(b => dto.Ids.Contains(b.Id))
            .ToListAsync();

        int deletedCount = 0;
        foreach (var item in items)
        {
            if (isAdmin || (item.Kid != null && await CanManageKidAsync(item.Kid, currentUserId!)))
            {
                dbContext.BlockedItems.Remove(item);
                deletedCount++;
            }
        }

        await dbContext.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa thành công {deletedCount} mục chặn.", deletedCount });
    }

    /// <summary>
    /// Lấy danh sách các trang web và ứng dụng bị chặn của một bé
    /// </summary>
    [HttpGet("/api/kids/{kidId:int}/blocked-items")]
    public async Task<IActionResult> GetBlockedItems(int kidId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        var isKid = User.IsInRole("Kid") || User.IsInRole("kid");

        var kid = await dbContext.Kids.AsNoTracking().FirstOrDefaultAsync(k => k.Id == kidId);
        if (kid == null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ của bé." });
        }

        // Kiểm tra quyền
        if (!isAdmin)
        {
            if (isKid)
            {
                if (kid.KidUserId != currentUserId)
                {
                    return Forbid();
                }
            }
            else if (!string.IsNullOrEmpty(currentUserId))
            {
                var canManage = await CanManageKidAsync(kid, currentUserId);
                if (!canManage)
                {
                    return Forbid();
                }
            }
        }

        var items = await dbContext.BlockedItems
            .AsNoTracking()
            .Where(b => b.KidId == kidId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.KidId,
                b.ItemType,
                b.Target,
                b.Title,
                b.Category,
                b.Reason,
                b.IsBlocked,
                b.CreatedAt,
                b.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            kidId = kid.Id,
            kidFullName = kid.FullName,
            totalCount = items.Count,
            activeBlockedCount = items.Count(x => x.IsBlocked),
            items
        });
    }

    /// <summary>
    /// Thêm một mục (Web hoặc App) vào danh sách chặn của bé (Hỗ trợ tự động nhận diện từ Link Web, Link CH Play, App Store...)
    /// </summary>
    [HttpPost("/api/kids/{kidId:int}/blocked-items")]
    public async Task<IActionResult> AddBlockedItem(int kidId, [FromBody] CreateBlockedItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Target))
        {
            return BadRequest(new { message = "Vui lòng nhập đường link hoặc tên ứng dụng cần chặn." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        var kid = await dbContext.Kids.FirstOrDefaultAsync(k => k.Id == kidId);
        if (kid == null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ của bé." });
        }

        if (!isAdmin && !string.IsNullOrEmpty(currentUserId))
        {
            var canManage = await CanManageKidAsync(kid, currentUserId);
            if (!canManage)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền quản lý hồ sơ bé này." });
            }
        }

        // Tự động phân tích và chuẩn hóa đường link / tên mục tiêu
        var (normalizedType, normalizedTarget, finalTitle, finalCategory) = ParseInputLinkOrTarget(
            dto.Target,
            dto.ItemType,
            dto.Title,
            dto.Category);

        // Kiểm tra xem mục này đã có trong danh sách chặn của bé chưa
        var existing = await dbContext.BlockedItems
            .FirstOrDefaultAsync(b => b.KidId == kidId && b.Target.ToLower() == normalizedTarget.ToLower());

        if (existing != null)
        {
            if (!existing.IsBlocked)
            {
                existing.IsBlocked = true;
                existing.Title = !string.IsNullOrWhiteSpace(dto.Title) ? dto.Title.Trim() : existing.Title;
                existing.Category = !string.IsNullOrWhiteSpace(dto.Category) ? dto.Category.Trim() : existing.Category;
                existing.Reason = dto.Reason?.Trim() ?? existing.Reason;
                existing.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                return Ok(new { message = $"Đã kích hoạt lại trạng thái chặn cho '{existing.Title}' ({existing.Target}).", item = existing });
            }

            return BadRequest(new { message = $"Mục '{existing.Title}' ({existing.Target}) đã có trong danh sách chặn của bé." });
        }

        var blockedItem = new BlockedItem
        {
            KidId = kidId,
            ItemType = normalizedType,
            Target = normalizedTarget,
            Title = finalTitle,
            Category = finalCategory,
            Reason = dto.Reason?.Trim(),
            IsBlocked = true,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.BlockedItems.Add(blockedItem);
        await dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = $"Đã thêm thành công '{blockedItem.Title}' vào danh sách chặn của bé {kid.FullName}.",
            item = new
            {
                blockedItem.Id,
                blockedItem.KidId,
                blockedItem.ItemType,
                blockedItem.Target,
                blockedItem.Title,
                blockedItem.Category,
                blockedItem.Reason,
                blockedItem.IsBlocked,
                blockedItem.CreatedAt
            }
        });
    }

    /// <summary>
    /// Thêm danh sách nhiều mục cùng lúc (Dành cho chọn nhanh từ Presets)
    /// </summary>
    [HttpPost("/api/kids/{kidId:int}/blocked-items/batch")]
    public async Task<IActionResult> AddBatchBlockedItems(int kidId, [FromBody] List<CreateBlockedItemDto> dtoList)
    {
        if (dtoList == null || dtoList.Count == 0)
        {
            return BadRequest(new { message = "Danh sách mục cần chặn không được để trống." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        var kid = await dbContext.Kids.FirstOrDefaultAsync(k => k.Id == kidId);
        if (kid == null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ của bé." });
        }

        if (!isAdmin && !string.IsNullOrEmpty(currentUserId))
        {
            var canManage = await CanManageKidAsync(kid, currentUserId);
            if (!canManage)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền quản lý hồ sơ bé này." });
            }
        }

        int addedCount = 0;
        int reactivatedCount = 0;

        foreach (var dto in dtoList)
        {
            if (string.IsNullOrWhiteSpace(dto.Target)) continue;

            var (normalizedType, normalizedTarget, finalTitle, finalCategory) = ParseInputLinkOrTarget(
                dto.Target,
                dto.ItemType,
                dto.Title,
                dto.Category);

            var existing = await dbContext.BlockedItems
                .FirstOrDefaultAsync(b => b.KidId == kidId && b.Target.ToLower() == normalizedTarget.ToLower());

            if (existing != null)
            {
                if (!existing.IsBlocked)
                {
                    existing.IsBlocked = true;
                    existing.UpdatedAt = DateTime.UtcNow;
                    reactivatedCount++;
                }
            }
            else
            {
                dbContext.BlockedItems.Add(new BlockedItem
                {
                    KidId = kidId,
                    ItemType = normalizedType,
                    Target = normalizedTarget,
                    Title = finalTitle,
                    Category = finalCategory,
                    Reason = dto.Reason?.Trim(),
                    IsBlocked = true,
                    CreatedByUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                });
                addedCount++;
            }
        }

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = $"Đã cập nhật danh sách chặn: thêm mới {addedCount} mục, kích hoạt lại {reactivatedCount} mục.",
            addedCount,
            reactivatedCount
        });
    }

    /// <summary>
    /// Bật / Tắt trạng thái chặn (Toggle IsBlocked)
    /// </summary>
    [HttpPut("{id:int}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        var item = await dbContext.BlockedItems.Include(b => b.Kid).FirstOrDefaultAsync(b => b.Id == id);
        if (item == null)
        {
            return NotFound(new { message = "Không tìm thấy quy tắc chặn." });
        }

        if (!isAdmin && !string.IsNullOrEmpty(currentUserId) && item.Kid != null)
        {
            var canManage = await CanManageKidAsync(item.Kid, currentUserId);
            if (!canManage)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền quản lý hồ sơ bé này." });
            }
        }

        item.IsBlocked = !item.IsBlocked;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var statusText = item.IsBlocked ? "Đang chặn ⛔" : "Tạm mở cho phép ✅";
        return Ok(new
        {
            message = $"Đã cập nhật trạng thái mục '{item.Title}': {statusText}",
            isBlocked = item.IsBlocked
        });
    }

    /// <summary>
    /// Xóa hoàn toàn một mục khỏi danh sách chặn
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        var item = await dbContext.BlockedItems.Include(b => b.Kid).FirstOrDefaultAsync(b => b.Id == id);
        if (item == null)
        {
            return NotFound(new { message = "Không tìm thấy quy tắc chặn cần xóa." });
        }

        if (!isAdmin && !string.IsNullOrEmpty(currentUserId) && item.Kid != null)
        {
            var canManage = await CanManageKidAsync(item.Kid, currentUserId);
            if (!canManage)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền quản lý hồ sơ bé này." });
            }
        }

        dbContext.BlockedItems.Remove(item);
        await dbContext.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa mục '{item.Title}' ({item.Target}) khỏi danh sách chặn thành công." });
    }

    /// <summary>
    /// Danh sách các trang web và ứng dụng gợi ý phổ biến (Presets) để phụ huynh thêm chặn 1 chạm
    /// </summary>
    [HttpGet("presets")]
    public IActionResult GetPresets()
    {
        var presets = new List<object>
        {
            // === Ứng dụng phổ biến (Apps) ===
            new { itemType = "App", title = "TikTok", target = "com.zhiliaoapp.musically", category = "Mạng xã hội", icon = "📱", defaultReason = "Dễ gây nghiện, tốn nhiều thời gian học tập", popular = true },
            new { itemType = "App", title = "Roblox", target = "com.roblox.client", category = "Trò chơi / Game", icon = "🎮", defaultReason = "Game trực tuyến có yếu tố nạp tiền và giao tiếp người lạ", popular = true },
            new { itemType = "App", title = "Free Fire", target = "com.dts.freefireth", category = "Trò chơi / Game", icon = "🎮", defaultReason = "Game bắn súng bạo lực không phù hợp lứa tuổi", popular = true },
            new { itemType = "App", title = "Liên Quân Mobile", target = "com.garena.game.kgvn", category = "Trò chơi / Game", icon = "🎮", defaultReason = "Game MOBA chơi quá giờ gây ảnh hưởng sinh hoạt", popular = true },
            new { itemType = "App", title = "Facebook", target = "com.facebook.katana", category = "Mạng xã hội", icon = "📱", defaultReason = "Hạn chế thời gian lướt mạng xã hội", popular = false },
            new { itemType = "App", title = "Instagram", target = "com.instagram.android", category = "Mạng xã hội", icon = "📱", defaultReason = "Hạn chế tiếp xúc nội dung người lớn trên mạng xã hội", popular = false },
            new { itemType = "App", title = "Bigo Live", target = "sg.bigo.live", category = "Nội dung độc hại / 18+", icon = "⚠️", defaultReason = "Nội dung livestream người lớn và trò chuyện nhạy cảm", popular = true },
            new { itemType = "App", title = "Discord", target = "com.discord", category = "Mạng xã hội", icon = "💬", defaultReason = "Nhóm chat người lạ không được kiểm duyệt", popular = false },

            // === Trang web phổ biến (Websites) ===
            new { itemType = "Website", title = "TikTok Web", target = "tiktok.com", category = "Mạng xã hội", icon = "🌐", defaultReason = "Chặn xem video TikTok trên trình duyệt", popular = true },
            new { itemType = "Website", title = "YouTube Shorts", target = "youtube.com/shorts", category = "Mạng xã hội", icon = "🌐", defaultReason = "Chặn lướt video ngắn YouTube gây nghiện", popular = true },
            new { itemType = "Website", title = "Web Cờ Bạc B52", target = "b52.club", category = "Cờ bạc & Cá cược", icon = "🚫", defaultReason = "Cờ bạc trực tuyến, game bài đổi thưởng bất hợp pháp", popular = true },
            new { itemType = "Website", title = "Web Game Bài Go88", target = "go88.com", category = "Cờ bạc & Cá cược", icon = "🚫", defaultReason = "Trang web cờ bạc và cá cược online", popular = true },
            new { itemType = "Website", title = "Web Cá Cược KuBet", target = "kubet.com", category = "Cờ bạc & Cá cược", icon = "🚫", defaultReason = "Trang web cá cược thể thao và cờ bạc", popular = false },
            new { itemType = "Website", title = "Trang Web 18+ (Pornhub)", target = "pornhub.com", category = "Nội dung độc hại / 18+", icon = "🔞", defaultReason = "Nội dung người lớn đồi trụy nguy hiểm cho trẻ", popular = true },
            new { itemType = "Website", title = "Trang Web 18+ (Xvideos)", target = "xvideos.com", category = "Nội dung độc hại / 18+", icon = "🔞", defaultReason = "Nội dung người lớn 18+", popular = true }
        };

        return Ok(presets);
    }

    /// <summary>
    /// API kiểm tra xem một trang web hoặc ứng dụng có đang bị chặn đối với bé hay không
    /// (Sử dụng cho Extension trình duyệt hoặc Agent trên thiết bị của bé)
    /// </summary>
    [HttpGet("check")]
    public async Task<IActionResult> CheckTargetIsBlocked([FromQuery] int kidId, [FromQuery] string target)
    {
        if (kidId <= 0 || string.IsNullOrWhiteSpace(target))
        {
            return BadRequest(new { message = "Vui lòng cung cấp kidId và target cần kiểm tra." });
        }

        var normalizedTarget = target.Trim().ToLower()
            .Replace("https://", "")
            .Replace("http://", "")
            .TrimEnd('/');

        var matchedRule = await dbContext.BlockedItems
            .AsNoTracking()
            .Where(b => b.KidId == kidId && b.IsBlocked)
            .FirstOrDefaultAsync(b => b.Target.ToLower() == normalizedTarget ||
                                      normalizedTarget.Contains(b.Target.ToLower()) ||
                                      b.Target.ToLower().Contains(normalizedTarget));

        if (matchedRule != null)
        {
            return Ok(new
            {
                isBlocked = true,
                target,
                ruleTitle = matchedRule.Title,
                category = matchedRule.Category,
                reason = matchedRule.Reason ?? "Bị giới hạn bởi phụ huynh",
                message = $"⛔ Truy cập bị chặn: '{matchedRule.Title}' đã bị phụ huynh thiết lập hạn chế để bảo vệ bạn."
            });
        }

        return Ok(new
        {
            isBlocked = false,
            target,
            message = "Nội dung được phép truy cập."
        });
    }

    // ==========================================
    // PHƯƠNG THỨC HỖ TRỢ NỘI BỘ
    // ==========================================
    private async Task<bool> CanManageKidAsync(Kid kid, string userId)
    {
        if (kid.ParentId == userId) return true;

        if (kid.FamilyGroupId.HasValue)
        {
            var isMember = await dbContext.FamilyMembers
                .AnyAsync(m => m.FamilyGroupId == kid.FamilyGroupId.Value && m.UserId == userId);
            if (isMember) return true;

            var isOwner = await dbContext.FamilyGroups
                .AnyAsync(g => g.Id == kid.FamilyGroupId.Value && g.OwnerId == userId);
            if (isOwner) return true;
        }

        return false;
    }

    private async Task<List<Kid>> GetMyKidsListAsync(string userId)
    {
        var memberGroupIds = await dbContext.FamilyMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.FamilyGroupId)
            .ToListAsync();

        var ownerGroupIds = await dbContext.FamilyGroups
            .Where(g => g.OwnerId == userId)
            .Select(g => g.Id)
            .ToListAsync();

        var allFamilyGroupIds = memberGroupIds.Union(ownerGroupIds).Distinct().ToList();

        return await dbContext.Kids
            .AsNoTracking()
            .Where(k => k.ParentId == userId || (k.FamilyGroupId != null && allFamilyGroupIds.Contains(k.FamilyGroupId.Value)))
            .OrderBy(k => k.FullName)
            .ToListAsync();
    }

    private static (string ItemType, string Target, string Title, string Category) ParseInputLinkOrTarget(
        string rawInput,
        string? specifiedType,
        string? specifiedTitle,
        string? specifiedCategory)
    {
        var input = rawInput.Trim();
        string detectedType = "Website";
        string target = input;
        string title = specifiedTitle?.Trim() ?? string.Empty;
        string category = specifiedCategory?.Trim() ?? string.Empty;

        // 1. Nhận diện link Google Play Store
        // vd: https://play.google.com/store/apps/details?id=com.roblox.client&hl=vi
        if (input.Contains("play.google.com/store/apps/details", StringComparison.OrdinalIgnoreCase))
        {
            detectedType = "App";
            try
            {
                var uri = new Uri(input.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? input : "https://" + input);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var packageId = query["id"];
                if (!string.IsNullOrEmpty(packageId))
                {
                    target = packageId.Trim();
                    if (string.IsNullOrEmpty(title))
                    {
                        var parts = target.Split('.');
                        var candidate = parts[^1];
                        title = char.ToUpper(candidate[0]) + candidate[1..];
                    }
                }
            }
            catch
            {
                // fallback
            }
        }
        // 2. Nhận diện link Apple App Store
        // vd: https://apps.apple.com/us/app/roblox/id431946152
        else if (input.Contains("apps.apple.com", StringComparison.OrdinalIgnoreCase) && input.Contains("/app/", StringComparison.OrdinalIgnoreCase))
        {
            detectedType = "App";
            try
            {
                var uri = new Uri(input.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? input : "https://" + input);
                var segments = uri.Segments;
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    if (segments[i].Trim('/').Equals("app", StringComparison.OrdinalIgnoreCase))
                    {
                        var appSlug = segments[i + 1].Trim('/');
                        target = appSlug;
                        if (string.IsNullOrEmpty(title))
                        {
                            title = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(appSlug.Replace('-', ' '));
                        }
                        break;
                    }
                }
            }
            catch
            {
                // fallback
            }
        }
        // 3. Nhận diện package name dạng com.xxx.yyy
        else if (System.Text.RegularExpressions.Regex.IsMatch(input, @"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$"))
        {
            detectedType = "App";
            target = input;
            if (string.IsNullOrEmpty(title))
            {
                var parts = target.Split('.');
                var last = parts[^1];
                title = char.ToUpper(last[0]) + last[1..];
            }
        }
        // 4. Nếu là Website / Đường dẫn URL
        else if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                 input.Contains(".com") || input.Contains(".vn") || input.Contains(".net") ||
                 input.Contains(".org") || input.Contains(".club") || input.Contains(".io") ||
                 input.Contains(".xyz") || input.Contains(".tv") || input.Contains(".app"))
        {
            detectedType = "Website";
            try
            {
                var fullUrl = input.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? input : "https://" + input;
                var uri = new Uri(fullUrl);
                var host = uri.Host.ToLower().Replace("www.", "").Trim();

                var path = uri.AbsolutePath.TrimEnd('/');
                if (path.StartsWith("/shorts", StringComparison.OrdinalIgnoreCase))
                {
                    target = $"{host}/shorts";
                }
                else
                {
                    target = host;
                }

                if (string.IsNullOrEmpty(title))
                {
                    var hostParts = host.Split('.');
                    var mainDomain = hostParts.Length >= 2 ? hostParts[0] : host;
                    title = char.ToUpper(mainDomain[0]) + mainDomain[1..];
                }
            }
            catch
            {
                target = input.ToLower().Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');
                if (string.IsNullOrEmpty(title)) title = target;
            }
        }
        else
        {
            // Nhập tên app hoặc từ khóa trực tiếp (vd: Roblox, Liên Quân)
            if (specifiedType?.Equals("Website", StringComparison.OrdinalIgnoreCase) == true)
            {
                detectedType = "Website";
                target = input.ToLower().Replace("www.", "");
            }
            else
            {
                detectedType = "App";
                target = input;
            }

            if (string.IsNullOrEmpty(title))
            {
                title = input;
            }
        }

        // Tôn trọng type người dùng chọn nếu có chỉ định rõ ràng
        if (!string.IsNullOrWhiteSpace(specifiedType))
        {
            detectedType = specifiedType.Trim().Equals("App", StringComparison.OrdinalIgnoreCase) ? "App" : "Website";
        }

        if (string.IsNullOrEmpty(title))
        {
            title = target;
        }

        // Tự động phân loại category nếu chưa chọn
        if (string.IsNullOrWhiteSpace(category) || category == "Khác")
        {
            var lower = (target + " " + title).ToLower();
            if (lower.Contains("tiktok") || lower.Contains("facebook") || lower.Contains("instagram") ||
                lower.Contains("threads") || lower.Contains("discord") || lower.Contains("telegram") ||
                lower.Contains("twitter") || lower.Contains("x.com") || lower.Contains("zalo") || lower.Contains("shorts"))
            {
                category = "Mạng xã hội";
            }
            else if (lower.Contains("roblox") || lower.Contains("fire") || lower.Contains("garena") ||
                     lower.Contains("game") || lower.Contains("lienquan") || lower.Contains("pubg") ||
                     lower.Contains("minecraft") || lower.Contains("play"))
            {
                category = "Trò chơi / Game";
            }
            else if (lower.Contains("b52") || lower.Contains("go88") || lower.Contains("bet") ||
                     lower.Contains("kubet") || lower.Contains("casino") || lower.Contains("789") ||
                     lower.Contains("sunwin") || lower.Contains("taixiu") || lower.Contains("bai"))
            {
                category = "Cờ bạc & Cá cược";
            }
            else if (lower.Contains("porn") || lower.Contains("xvideo") || lower.Contains("sex") ||
                     lower.Contains("18+") || lower.Contains("hentai") || lower.Contains("jav"))
            {
                category = "Nội dung độc hại / 18+";
            }
            else
            {
                category = "Khác";
            }
        }

        return (detectedType, target, title, category);
    }
}

public class CreateBlockedItemDto
{
    public string? ItemType { get; set; }
    public string Target { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Category { get; set; }
    public string? Reason { get; set; }
}

public class BatchDeleteDto
{
    public List<int> Ids { get; set; } = new();
}
