using Microsoft.AspNetCore.Mvc;

namespace Giahuy0790.Controllers;

/// <summary>
/// Controller xử lý tải lên (upload) và lưu trữ hình ảnh trẻ em, ảnh đại diện vào thư mục wwwroot/uploads
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UploadsController(IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Upload hình ảnh hồ sơ bé và lưu vào wwwroot/uploads/kids
    /// </summary>
    [HttpPost("kid")]
    public async Task<IActionResult> UploadKidImage(IFormFile? file)
    {
        return await SaveFileAsync(file, "kids");
    }

    /// <summary>
    /// Upload ảnh đại diện tài khoản và lưu vào wwwroot/uploads/avatars
    /// </summary>
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile? file)
    {
        return await SaveFileAsync(file, "avatars");
    }

    private async Task<IActionResult> SaveFileAsync(IFormFile? file, string subFolder)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn một file ảnh hợp lệ để tải lên" });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { message = "Kích thước file ảnh không được vượt quá 10MB" });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Định dạng ảnh không được hỗ trợ. Chỉ chấp nhận: {string.Join(", ", AllowedExtensions)}" });
        }

        // Tạo đường dẫn thư mục lưu trữ
        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", subFolder);
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        // Tạo tên file độc nhất để tránh trùng lặp
        var safeFileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var fullPath = Path.Combine(uploadsDir, safeFileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Trả về URL tương đối để lưu vào database
        var relativeUrl = $"/uploads/{subFolder}/{safeFileName}";

        return Ok(new
        {
            message = "Tải ảnh lên thành công",
            url = relativeUrl,
            fileName = safeFileName,
            fileSize = file.Length
        });
    }
}
