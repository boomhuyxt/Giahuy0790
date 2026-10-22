using Giahuy0790.Data;
using Giahuy0790.Hubs;
using Giahuy0790.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Giahuy0790.Controllers;

/// <summary>
/// API Quản lý đàm thoại giọng nói & Bộ đàm gia đình (Family Walkie-Talkie & Voice Intercom)
/// Cho phép Phụ huynh và Bé trò chuyện bằng giọng nói tức thì theo chuẩn Apple Human Interface
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VoiceMessagesController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment env,
    ILogger<VoiceMessagesController> logger,
    IHubContext<LocationHub> hubContext) : ControllerBase
{
    private static readonly string[] AllowedAudioExtensions = [".webm", ".ogg", ".wav", ".mp3", ".m4a", ".mp4", ".aac", ".bin"];
    private const long MaxAudioFileSize = 20 * 1024 * 1024; // 20MB

    private static DateTime ToVietnamTime(DateTime utc) => utc.AddHours(7);

    private string GetUploadsDirectory()
    {
        var webRoot = !string.IsNullOrEmpty(env.WebRootPath)
            ? env.WebRootPath
            : Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "voice");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }
        return uploadsDir;
    }

    /// <summary>
    /// Lấy danh sách tin nhắn giọng nói giữa Phụ huynh và Bé
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVoiceMessages([FromQuery] int? kidId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để nghe tin nhắn thoại." });
        }

        var isKidRole = User.IsInRole("Kid") || User.IsInRole("kid");
        var kidProfile = await context.Kids.FirstOrDefaultAsync(k => k.KidUserId == currentUserId);
        var isKid = isKidRole || kidProfile != null;

        int targetKidId;

        if (isKid)
        {
            if (kidProfile == null)
            {
                return NotFound(new { message = "Tài khoản của bạn chưa được liên kết với hồ sơ trẻ." });
            }
            targetKidId = kidProfile.Id;
        }
        else
        {
            if (!kidId.HasValue)
            {
                // Ưu tiên bé của phụ huynh hiện tại
                var firstKid = await context.Kids.FirstOrDefaultAsync(k => k.ParentId == currentUserId);
                if (firstKid == null && (User.IsInRole("Admin") || User.IsInRole("admin")))
                {
                    firstKid = await context.Kids.FirstOrDefaultAsync();
                }
                if (firstKid == null)
                {
                    firstKid = await (from k in context.Kids
                                      join m in context.FamilyMembers on k.FamilyGroupId equals m.FamilyGroupId
                                      where m.UserId == currentUserId
                                      select k).FirstOrDefaultAsync();
                }

                if (firstKid == null)
                {
                    return Ok(new List<object>());
                }
                targetKidId = firstKid.Id;
            }
            else
            {
                targetKidId = kidId.Value;
            }

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
            var hasAccess = isAdmin || await context.Kids.AnyAsync(k =>
                k.Id == targetKidId && (k.ParentId == currentUserId ||
                (k.FamilyGroupId != null && context.FamilyMembers.Any(m => m.FamilyGroupId == k.FamilyGroupId && m.UserId == currentUserId))));

            if (!hasAccess)
            {
                var kidExists = await context.Kids.AnyAsync(k => k.Id == targetKidId);
                if (!kidExists) return NotFound(new { message = "Không tìm thấy hồ sơ bé tương ứng." });
            }
        }

        var messages = await context.VoiceMessages
            .Where(v => v.KidId == targetKidId)
            .OrderBy(v => v.CreatedAt)
            .Take(100)
            .ToListAsync();

        var result = messages.Select(m => new
        {
            id = m.Id,
            senderId = m.SenderId,
            senderName = m.SenderName,
            senderRole = m.SenderRole,
            senderAvatarUrl = m.SenderAvatarUrl ?? (m.SenderRole == "Kid" ? "https://api.dicebear.com/7.x/bottts/svg?seed=Kid" : "https://api.dicebear.com/7.x/bottts/svg?seed=Parent"),
            isMe = m.SenderId == currentUserId,
            audioUrl = m.AudioUrl,
            durationSeconds = m.DurationSeconds,
            fileSizeBytes = m.FileSizeBytes,
            isHeard = m.IsHeard,
            heardAt = m.HeardAt,
            createdAt = m.CreatedAt,
            createdAtVn = ToVietnamTime(m.CreatedAt).ToString("HH:mm dd/MM/yyyy")
        });

        return Ok(result);
    }

    /// <summary>
    /// Gửi tin nhắn giọng nói mới (Upload file âm thanh thu từ microphone trình duyệt)
    /// </summary>
    [HttpPost("send")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SendVoiceMessage([FromForm] SendVoiceMessageRequest request)
    {
        var audioFile = request.AudioFile;
        var kidId = request.KidId;
        var durationSeconds = request.DurationSeconds;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để gửi tin nhắn thoại." });
        }

        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest(new { message = "Không tìm thấy dữ liệu âm thanh giọng nói. Vui lòng bấm giữ nút nói và thử lại." });
        }

        if (audioFile.Length > MaxAudioFileSize)
        {
            return BadRequest(new { message = "Dung lượng file ghi âm quá lớn (tối đa 20MB)." });
        }

        var rawExt = Path.GetExtension(audioFile.FileName)?.ToLowerInvariant();
        var extension = !string.IsNullOrEmpty(rawExt) && AllowedAudioExtensions.Contains(rawExt)
            ? rawExt
            : (audioFile.ContentType.Contains("mp4") ? ".mp4" : ".webm");

        var currentUser = await userManager.FindByIdAsync(currentUserId);
        var isKidRole = User.IsInRole("Kid") || User.IsInRole("kid");
        var kidProfile = await context.Kids.FirstOrDefaultAsync(k => k.KidUserId == currentUserId);
        var isKid = isKidRole || kidProfile != null;

        int targetKidId;
        string? parentId;
        int? familyGroupId;
        string senderRole;
        string senderName;
        string? senderAvatarUrl;

        if (isKid)
        {
            if (kidProfile == null)
            {
                return NotFound(new { message = "Tài khoản của bạn chưa được liên kết với hồ sơ trẻ." });
            }
            targetKidId = kidProfile.Id;
            parentId = kidProfile.ParentId;
            familyGroupId = kidProfile.FamilyGroupId;
            senderRole = "Kid";
            senderName = !string.IsNullOrWhiteSpace(kidProfile.FullName) ? kidProfile.FullName : (currentUser?.FullName ?? currentUser?.UserName ?? "Bé yêu");
            senderAvatarUrl = kidProfile.AvatarUrl ?? currentUser?.AvatarUrl;
        }
        else
        {
            if (!kidId.HasValue)
            {
                var firstKid = await context.Kids.FirstOrDefaultAsync(k => k.ParentId == currentUserId);
                if (firstKid == null && (User.IsInRole("Admin") || User.IsInRole("admin")))
                {
                    firstKid = await context.Kids.FirstOrDefaultAsync();
                }
                if (firstKid == null)
                {
                    firstKid = await (from k in context.Kids
                                      join m in context.FamilyMembers on k.FamilyGroupId equals m.FamilyGroupId
                                      where m.UserId == currentUserId
                                      select k).FirstOrDefaultAsync();
                }

                if (firstKid == null)
                {
                    return BadRequest(new { message = "Vui lòng chọn hoặc thêm hồ sơ bé trước khi gửi tin nhắn thoại." });
                }
                targetKidId = firstKid.Id;
            }
            else
            {
                targetKidId = kidId.Value;
            }

            var targetKid = await context.Kids.FindAsync(targetKidId);
            if (targetKid == null)
            {
                return NotFound(new { message = "Không tìm thấy hồ sơ bé tương ứng." });
            }

            targetKidId = targetKid.Id;
            parentId = targetKid.ParentId ?? currentUserId;
            familyGroupId = targetKid.FamilyGroupId;
            senderRole = User.IsInRole("Admin") || User.IsInRole("admin") ? "Admin" : "Parents";
            senderName = currentUser?.FullName ?? currentUser?.UserName ?? "Phụ huynh";
            senderAvatarUrl = currentUser?.AvatarUrl;
        }

        // Lưu file âm thanh an toàn vào wwwroot/uploads/voice/
        var safeFileName = $"voice_{targetKidId}_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        try
        {
            var uploadsDir = GetUploadsDirectory();
            var fullPath = Path.Combine(uploadsDir, safeFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Lỗi phân quyền khi ghi file âm thanh vào wwwroot/uploads/voice trên Linux.");
            return StatusCode(500, new { message = "Lỗi phân quyền thư mục trên máy chủ Linux. Vui lòng cấp quyền ghi trên server bằng lệnh: chmod -R 777 wwwroot/uploads" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi lưu file ghi âm: {Msg}", ex.Message);
            return StatusCode(500, new { message = $"Không thể lưu file âm thanh trên máy chủ: {ex.Message}" });
        }

        var relativeAudioUrl = $"/uploads/voice/{safeFileName}";

        var voiceMessage = new VoiceMessage
        {
            SenderId = currentUserId,
            SenderName = senderName,
            SenderRole = senderRole,
            SenderAvatarUrl = senderAvatarUrl,
            KidId = targetKidId,
            ParentId = parentId,
            FamilyGroupId = familyGroupId,
            AudioUrl = relativeAudioUrl,
            DurationSeconds = Math.Max(1, durationSeconds ?? 1),
            FileSizeBytes = audioFile.Length,
            IsHeard = false,
            CreatedAt = DateTime.UtcNow
        };

        context.VoiceMessages.Add(voiceMessage);
        await context.SaveChangesAsync();

        logger.LogInformation("Gửi tin nhắn giọng nói mới ID={Id} từ {Sender} (Role={Role}) cho bé ID={KidId}",
            voiceMessage.Id, senderName, senderRole, targetKidId);

        var payload = new
        {
            id = voiceMessage.Id,
            senderId = voiceMessage.SenderId,
            senderName = voiceMessage.SenderName,
            senderRole = voiceMessage.SenderRole,
            senderAvatarUrl = voiceMessage.SenderAvatarUrl ?? (voiceMessage.SenderRole == "Kid" ? "https://api.dicebear.com/7.x/bottts/svg?seed=Kid" : "https://api.dicebear.com/7.x/bottts/svg?seed=Parent"),
            isMe = true,
            audioUrl = voiceMessage.AudioUrl,
            durationSeconds = voiceMessage.DurationSeconds,
            fileSizeBytes = voiceMessage.FileSizeBytes,
            isHeard = voiceMessage.IsHeard,
            createdAt = voiceMessage.CreatedAt,
            createdAtVn = ToVietnamTime(voiceMessage.CreatedAt).ToString("HH:mm dd/MM/yyyy")
        };

        // Bắn SignalR Real-Time cho các client đang mở kết nối
        try
        {
            await hubContext.Clients.Group($"kid_{targetKidId}").SendAsync("ReceiveVoiceMessage", payload);
            await hubContext.Clients.All.SendAsync("NewVoiceNotification", new { kidId = targetKidId, senderId = currentUserId });
        }
        catch (Exception hubEx)
        {
            logger.LogWarning("Không thể gửi thông báo SignalR: {Msg}", hubEx.Message);
        }

        return Ok(new
        {
            message = "Đã gửi tin nhắn giọng nói thành công!",
            voiceMessage = payload
        });
    }

    /// <summary>
    /// Đánh dấu tin nhắn thoại đã được nghe
    /// </summary>
    [HttpPut("{id:int}/mark-heard")]
    public async Task<IActionResult> MarkHeard(int id)
    {
        var msg = await context.VoiceMessages.FindAsync(id);
        if (msg == null) return NotFound(new { message = "Không tìm thấy tin nhắn thoại." });

        if (!msg.IsHeard)
        {
            msg.IsHeard = true;
            msg.HeardAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return Ok(new { message = "Đã đánh dấu đã nghe." });
    }

    /// <summary>
    /// Xóa tin nhắn giọng nói
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteVoiceMessage(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

        var msg = await context.VoiceMessages.FindAsync(id);
        if (msg == null) return NotFound(new { message = "Không tìm thấy tin nhắn thoại." });

        // Người gửi, phụ huynh hoặc admin có quyền xóa
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (msg.SenderId != currentUserId && msg.ParentId != currentUserId && !isAdmin)
        {
            return Forbid();
        }

        // Xóa file trên disk nếu có
        try
        {
            var fileName = Path.GetFileName(msg.AudioUrl);
            var uploadsDir = GetUploadsDirectory();
            var filePath = Path.Combine(uploadsDir, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Không thể xóa file âm thanh {Url}: {Err}", msg.AudioUrl, ex.Message);
        }

        context.VoiceMessages.Remove(msg);
        await context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa tin nhắn giọng nói thành công." });
    }

    /// <summary>
    /// Lấy số lượng tin nhắn thoại mới chưa nghe cho người dùng hiện tại
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount([FromQuery] int? kidId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Ok(new { unreadCount = 0 });

        var isKidRole = User.IsInRole("Kid") || User.IsInRole("kid");
        var kidProfile = await context.Kids.FirstOrDefaultAsync(k => k.KidUserId == currentUserId);
        var isKid = isKidRole || kidProfile != null;

        if (isKid)
        {
            var kidIdVal = kidProfile?.Id;
            if (!kidIdVal.HasValue) return Ok(new { unreadCount = 0 });

            // Bé nhận thông báo khi có tin từ phụ huynh chưa nghe
            var count = await context.VoiceMessages
                .Where(v => v.KidId == kidIdVal.Value && v.SenderRole != "Kid" && !v.IsHeard)
                .CountAsync();

            return Ok(new { unreadCount = count, kidId = kidIdVal.Value });
        }
        else
        {
            // Phụ huynh nhận thông báo khi bé gửi tin chưa nghe
            var parentKidIds = await context.Kids
                .Where(k => k.ParentId == currentUserId)
                .Select(k => k.Id)
                .ToListAsync();

            if (parentKidIds.Count == 0 && (User.IsInRole("Admin") || User.IsInRole("admin")))
            {
                parentKidIds = await context.Kids.Select(k => k.Id).ToListAsync();
            }

            var query = context.VoiceMessages
                .Where(v => parentKidIds.Contains(v.KidId) && v.SenderRole == "Kid" && !v.IsHeard);

            if (kidId.HasValue)
            {
                query = query.Where(v => v.KidId == kidId.Value);
            }

            var count = await query.CountAsync();
            return Ok(new { unreadCount = count });
        }
    }
}

public class SendVoiceMessageRequest
{
    public IFormFile? AudioFile { get; set; }
    public int? KidId { get; set; }
    public int? DurationSeconds { get; set; }
}
