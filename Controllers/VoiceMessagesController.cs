using Giahuy0790.Data;
using Giahuy0790.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Giahuy0790.Controllers;

/// <summary>
/// API Quản lý đàm thoại giọng nói & Bộ đàm gia đình 8-Bit (Family Walkie-Talkie & Voice Intercom)
/// Cho phép Phụ huynh và Bé trò chuyện bằng giọng nói tức thì
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VoiceMessagesController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment env,
    ILogger<VoiceMessagesController> logger) : ControllerBase
{
    private static readonly string[] AllowedAudioExtensions = [".webm", ".ogg", ".wav", ".mp3", ".m4a", ".aac", ".bin"];
    private const long MaxAudioFileSize = 20 * 1024 * 1024; // 20MB

    private static DateTime ToVietnamTime(DateTime utc) => utc.AddHours(7);

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

        var isKid = User.IsInRole("Kid") || User.IsInRole("kid");
        int targetKidId;

        if (isKid)
        {
            var kid = await context.Kids.FirstOrDefaultAsync(k => k.KidUserId == currentUserId);
            if (kid == null)
            {
                return NotFound(new { message = "Tài khoản của bạn chưa được liên kết với hồ sơ trẻ." });
            }
            targetKidId = kid.Id;
        }
        else
        {
            if (!kidId.HasValue)
            {
                // Nếu phụ huynh không truyền kidId, lấy bé đầu tiên của phụ huynh
                var firstKid = await context.Kids.FirstOrDefaultAsync(k => k.ParentId == currentUserId);
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
    public async Task<IActionResult> SendVoiceMessage(
        [FromForm] IFormFile? audioFile,
        [FromForm] int? kidId,
        [FromForm] int? durationSeconds)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để gửi tin nhắn thoại." });
        }

        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest(new { message = "Không tìm thấy dữ liệu âm thanh giọng nói." });
        }

        if (audioFile.Length > MaxAudioFileSize)
        {
            return BadRequest(new { message = "Dung lượng file ghi âm quá lớn (tối đa 20MB)." });
        }

        var extension = Path.GetExtension(audioFile.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            // Mặc định trình duyệt MediaRecorder xuất audio/webm
            extension = ".webm";
        }
        else if (!AllowedAudioExtensions.Contains(extension))
        {
            extension = ".webm";
        }

        var currentUser = await userManager.FindByIdAsync(currentUserId);
        var isKid = User.IsInRole("Kid") || User.IsInRole("kid");

        int targetKidId;
        string? parentId;
        int? familyGroupId;
        string senderRole;
        string senderName;
        string? senderAvatarUrl;

        if (isKid)
        {
            var kid = await context.Kids.FirstOrDefaultAsync(k => k.KidUserId == currentUserId);
            if (kid == null)
            {
                return NotFound(new { message = "Tài khoản của bạn chưa được liên kết với hồ sơ trẻ." });
            }
            targetKidId = kid.Id;
            parentId = kid.ParentId;
            familyGroupId = kid.FamilyGroupId;
            senderRole = "Kid";
            senderName = kid.FullName;
            senderAvatarUrl = kid.AvatarUrl ?? currentUser?.AvatarUrl;
        }
        else
        {
            if (!kidId.HasValue)
            {
                var firstKid = await context.Kids.FirstOrDefaultAsync(k => k.ParentId == currentUserId);
                if (firstKid == null)
                {
                    return BadRequest(new { message = "Vui lòng chọn bé để đàm thoại giọng nói." });
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
            parentId = currentUserId;
            familyGroupId = targetKid.FamilyGroupId;
            senderRole = "Parents";
            senderName = currentUser?.FullName ?? currentUser?.UserName ?? "Phụ huynh";
            senderAvatarUrl = currentUser?.AvatarUrl;
        }

        // Lưu file âm thanh vào wwwroot/uploads/voice/
        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "voice");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var safeFileName = $"voice_{targetKidId}_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var fullPath = Path.Combine(uploadsDir, safeFileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await audioFile.CopyToAsync(stream);
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

        return Ok(new
        {
            message = "Đã gửi tin nhắn giọng nói thành công!",
            voiceMessage = new
            {
                id = voiceMessage.Id,
                senderId = voiceMessage.SenderId,
                senderName = voiceMessage.SenderName,
                senderRole = voiceMessage.SenderRole,
                senderAvatarUrl = voiceMessage.SenderAvatarUrl,
                isMe = true,
                audioUrl = voiceMessage.AudioUrl,
                durationSeconds = voiceMessage.DurationSeconds,
                fileSizeBytes = voiceMessage.FileSizeBytes,
                isHeard = voiceMessage.IsHeard,
                createdAt = voiceMessage.CreatedAt,
                createdAtVn = ToVietnamTime(voiceMessage.CreatedAt).ToString("HH:mm dd/MM/yyyy")
            }
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

        // Người gửi hoặc phụ huynh có quyền xóa
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (msg.SenderId != currentUserId && msg.ParentId != currentUserId && !isAdmin)
        {
            return Forbid();
        }

        // Xóa file trên disk nếu có
        try
        {
            var fileName = Path.GetFileName(msg.AudioUrl);
            var filePath = Path.Combine(env.WebRootPath, "uploads", "voice", fileName);
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

        var isKid = User.IsInRole("Kid") || User.IsInRole("kid");

        if (isKid)
        {
            var kid = await context.Kids.FirstOrDefaultAsync(k => k.KidUserId == currentUserId);
            if (kid == null) return Ok(new { unreadCount = 0 });

            // Bé chỉ nhận thông báo khi có tin từ phụ huynh chưa nghe
            var count = await context.VoiceMessages
                .Where(v => v.KidId == kid.Id && v.SenderRole != "Kid" && !v.IsHeard)
                .CountAsync();

            return Ok(new { unreadCount = count, kidId = kid.Id });
        }
        else
        {
            // Phụ huynh nhận thông báo khi bé gửi tin chưa nghe
            var parentKidIds = await context.Kids
                .Where(k => k.ParentId == currentUserId)
                .Select(k => k.Id)
                .ToListAsync();

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
