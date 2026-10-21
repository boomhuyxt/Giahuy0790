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

    /// <summary>
    /// Mã OTP xác thực đặt lại mật khẩu (6 chữ số)
    /// </summary>
    public string? PasswordResetOtp { get; set; }

    /// <summary>
    /// Thời gian hết hạn của mã OTP đặt lại mật khẩu (UTC)
    /// </summary>
    public DateTime? PasswordResetOtpExpiry { get; set; }
}
