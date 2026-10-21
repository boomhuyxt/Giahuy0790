using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Giahuy0790.Services;

/// <summary>
/// Triển khai dịch vụ gửi Email thông qua giao thức SMTP sử dụng MailKit
/// </summary>
public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public async Task<bool> SendPasswordResetOtpAsync(string toEmail, string otpCode, string? recipientName = null)
    {
        var smtpSection = config.GetSection("SmtpSettings");
        var host = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? smtpSection["Server"] ?? "smtp.gmail.com";
        var portStr = Environment.GetEnvironmentVariable("SMTP_PORT") ?? smtpSection["Port"];
        var port = int.TryParse(portStr, out var p) ? p : 587;
        var senderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME") ?? smtpSection["SenderName"] ?? "Gia Huy POS";
        var senderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") ?? smtpSection["SenderEmail"] ?? "boomhuyxt@gmail.com";
        var username = (Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? smtpSection["Username"] ?? string.Empty).Trim();
        var password = (Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? smtpSection["Password"] ?? string.Empty).Replace(" ", "").Trim();
        var sslStr = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL") ?? smtpSection["EnableSsl"];
        var enableSsl = bool.TryParse(sslStr, out var ssl) && ssl;

        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Quý khách" : recipientName;

        // Nội dung HTML thông báo mã OTP đẹp mắt
        var htmlContent = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Mã xác thực đặt lại mật khẩu</title>
</head>
<body style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px;"">
    <div style=""max-width: 540px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08);"">
        <div style=""background: linear-gradient(135deg, #1E88E5 0%, #1565C0 100%); color: #ffffff; padding: 30px 20px; text-align: center;"">
            <h1 style=""margin: 0; font-size: 24px; font-weight: 700;"">🔐 Khôi phục mật khẩu</h1>
            <p style=""margin: 8px 0 0; opacity: 0.9; font-size: 14px;"">Hệ thống Xác thực Gia Huy POS</p>
        </div>
        <div style=""padding: 30px 24px; color: #333333; line-height: 1.6;"">
            <p style=""font-size: 16px; margin-top: 0;"">Xin chào <strong>{displayName}</strong>,</p>
            <p style=""font-size: 15px; color: #555555;"">
                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với địa chỉ email <strong>{toEmail}</strong>.
            </p>
            <p style=""font-size: 15px; color: #555555;"">
                Dưới đây là mã xác thực OTP của bạn:
            </p>
            <div style=""text-align: center; margin: 28px 0;"">
                <span style=""display: inline-block; font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #1E88E5; background-color: #E3F2FD; padding: 14px 28px; border-radius: 10px; border: 2px dashed #90CAF9;"">
                    {otpCode}
                </span>
            </div>
            <p style=""font-size: 14px; color: #777777; text-align: center;"">
                ⏳ Mã OTP này có hiệu lực trong vòng <strong>15 phút</strong>.
            </p>
            <hr style=""border: none; border-top: 1px solid #eeeeee; margin: 25px 0;"" />
            <p style=""font-size: 13px; color: #888888; margin-bottom: 0;"">
                ⚠️ <strong>Lưu ý bảo mật:</strong> Nếu bạn không gửi yêu cầu này, vui lòng bỏ qua email hoặc đổi mật khẩu tài khoản ngay để đảm bảo an toàn. Không chia sẻ mã này cho bất kỳ ai.
            </p>
        </div>
        <div style=""background-color: #fafafa; border-top: 1px solid #f0f0f0; padding: 16px; text-align: center; font-size: 12px; color: #999999;"">
            © {DateTime.UtcNow.Year} Gia Huy POS. Tất cả các quyền được bảo lưu.
        </div>
    </div>
</body>
</html>";

        // Nếu thông tin cấu hình SMTP chưa điền mật khẩu / tài khoản thật (chế độ DEV/TEST)
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("⚠️ [SMTP DEV MODE] Chưa cấu hình đầy đủ Username/Password SMTP trong appsettings.json.");
            logger.LogInformation("🔑 [MÃ OTP ĐẶT LẠI MẬT KHẨU]: Email = {Email} | OTP = {Otp}", toEmail, otpCode);
            return true;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(displayName, toEmail));
            message.Subject = $"[{otpCode}] Mã xác thực đặt lại mật khẩu - Gia Huy POS";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlContent,
                TextBody = $"Mã OTP đặt lại mật khẩu của bạn là: {otpCode}. Mã có hiệu lực trong vòng 15 phút."
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Xác định giao thức bảo mật
            var socketOptions = port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                _ => enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None
            };

            await client.ConnectAsync(host, port, socketOptions);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("✅ Gửi email OTP đặt lại mật khẩu thành công tới: {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Gửi email qua SMTP thất bại tới {Email}. Chi tiết: {Message}", toEmail, ex.Message);
            // Vẫn log mã OTP ra console để môi trường dev/test không bị nghẽn
            logger.LogWarning("🔑 [MÃ OTP DỰ PHÒNG DEV]: Email = {Email} | OTP = {Otp}", toEmail, otpCode);
            return false;
        }
    }
}
