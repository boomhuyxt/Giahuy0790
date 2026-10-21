namespace Giahuy0790.Services;

/// <summary>
/// Dịch vụ gửi email thông báo và mã xác thực OTP qua giao thức SMTP
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi mã xác thực OTP khôi phục mật khẩu qua email
    /// </summary>
    /// <param name="toEmail">Địa chỉ email người nhận</param>
    /// <param name="otpCode">Mã OTP gồm 6 chữ số</param>
    /// <param name="recipientName">Tên người nhận (tùy chọn)</param>
    Task<bool> SendPasswordResetOtpAsync(string toEmail, string otpCode, string? recipientName = null);
}
