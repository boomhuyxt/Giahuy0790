using Microsoft.AspNetCore.Identity;

namespace Giahuy0790.Models;

/// <summary>
/// Thực thể người dùng kế thừa từ ASP.NET Core IdentityUser
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Họ và tên đầy đủ của người dùng
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Đường dẫn ảnh đại diện (avatar)
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Thời gian tạo tài khoản
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
