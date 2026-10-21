using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Giahuy0790.Services;

/// <summary>
/// Triển khai dịch vụ gửi Email thông qua giao thức SMTP (Gmail) sử dụng MailKit
/// </summary>
public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    private (string host, int port, string senderName, string senderEmail, string username, string password, bool enableSsl) GetSmtpConfig()
    {
        var smtpSection = config.GetSection("SmtpSettings");
        var host = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? smtpSection["Server"] ?? "smtp.gmail.com";
        var portStr = Environment.GetEnvironmentVariable("SMTP_PORT") ?? smtpSection["Port"];
        var port = int.TryParse(portStr, out var p) ? p : 587;
        var senderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME") ?? smtpSection["SenderName"] ?? "KidCare - Quản Lý Trẻ Em";
        var senderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") ?? smtpSection["SenderEmail"] ?? "boomhuyxt@gmail.com";
        var username = (Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? smtpSection["Username"] ?? string.Empty).Trim();
        var password = (Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? smtpSection["Password"] ?? string.Empty).Replace(" ", "").Trim();
        var sslStr = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL") ?? smtpSection["EnableSsl"];
        var enableSsl = bool.TryParse(sslStr, out var ssl) && ssl;

        return (host, port, senderName, senderEmail, username, password, enableSsl);
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string textContent, string? recipientName = null)
    {
        var (host, port, senderName, senderEmail, username, password, enableSsl) = GetSmtpConfig();
        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Quý khách" : recipientName;

        // Nếu thông tin cấu hình SMTP chưa điền mật khẩu / tài khoản thật (chế độ DEV/TEST)
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("⚠️ [SMTP DEV MODE] Chưa cấu hình đầy đủ Username/Password SMTP trong appsettings.json.");
            logger.LogInformation("✉️ [EMAIL DEV PREVIEW]: To = {Email} | Subject = {Subject}", toEmail, subject);
            return true;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(displayName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlContent,
                TextBody = textContent
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

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

            logger.LogInformation("✅ Gửi email thành công tới: {Email} | Tiêu đề: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Gửi email qua SMTP thất bại tới {Email}. Chi tiết: {Message}", toEmail, ex.Message);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetOtpAsync(string toEmail, string otpCode, string? recipientName = null)
    {
        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Quý khách" : recipientName;
        var htmlContent = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Mã xác thực đặt lại mật khẩu</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px;"">
    <div style=""max-width: 540px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08);"">
        <div style=""background: linear-gradient(135deg, #4f46e5 0%, #06b6d4 100%); color: #ffffff; padding: 30px 20px; text-align: center;"">
            <h1 style=""margin: 0; font-size: 24px; font-weight: 700;"">🔐 Khôi phục mật khẩu</h1>
            <p style=""margin: 8px 0 0; opacity: 0.9; font-size: 14px;"">Hệ thống KidCare - Quản Lý Trẻ Em</p>
        </div>
        <div style=""padding: 30px 24px; color: #333333; line-height: 1.6;"">
            <p style=""font-size: 16px; margin-top: 0;"">Xin chào <strong>{displayName}</strong>,</p>
            <p style=""font-size: 15px; color: #555555;"">
                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với địa chỉ Gmail <strong>{toEmail}</strong>.
            </p>
            <p style=""font-size: 15px; color: #555555;"">Dưới đây là mã xác thực OTP của bạn:</p>
            <div style=""text-align: center; margin: 28px 0;"">
                <span style=""display: inline-block; font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #4f46e5; background-color: #eef2ff; padding: 14px 28px; border-radius: 10px; border: 2px dashed #a5b4fc;"">
                    {otpCode}
                </span>
            </div>
            <p style=""font-size: 14px; color: #777777; text-align: center;"">
                ⏳ Mã OTP này có hiệu lực trong vòng <strong>15 phút</strong>.
            </p>
        </div>
        <div style=""background-color: #fafafa; border-top: 1px solid #f0f0f0; padding: 16px; text-align: center; font-size: 12px; color: #999999;"">
            © {DateTime.UtcNow.Year} KidCare. Tất cả các quyền được bảo lưu.
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(
            toEmail,
            $"[{otpCode}] Mã xác thực đặt lại mật khẩu - KidCare",
            htmlContent,
            $"Mã OTP đặt lại mật khẩu của bạn là: {otpCode}. Mã có hiệu lực trong vòng 15 phút.",
            displayName
        );
    }

    public async Task<bool> SendLoginOtpAsync(string toEmail, string otpCode, string? recipientName = null)
    {
        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Quý khách" : recipientName;
        var htmlContent = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Mã OTP xác thực đăng nhập</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px;"">
    <div style=""max-width: 540px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08);"">
        <div style=""background: linear-gradient(135deg, #2563eb 0%, #3b82f6 100%); color: #ffffff; padding: 30px 20px; text-align: center;"">
            <h1 style=""margin: 0; font-size: 24px; font-weight: 700;"">🔐 Xác thực đăng nhập (OTP)</h1>
            <p style=""margin: 8px 0 0; opacity: 0.9; font-size: 14px;"">Hệ thống KidCare - Quản Lý An Toàn Trẻ Em</p>
        </div>
        <div style=""padding: 30px 24px; color: #333333; line-height: 1.6;"">
            <p style=""font-size: 16px; margin-top: 0;"">Xin chào <strong>{displayName}</strong>,</p>
            <p style=""font-size: 15px; color: #555555;"">
                Hệ thống nhận được yêu cầu đăng nhập vào tài khoản của bạn trên <strong>KidCare</strong>.
            </p>
            <p style=""font-size: 15px; color: #555555;"">Vui lòng sử dụng mã OTP gồm 6 chữ số dưới đây để xác thực và hoàn tất đăng nhập:</p>
            <div style=""text-align: center; margin: 28px 0;"">
                <span style=""display: inline-block; font-size: 34px; font-weight: 800; letter-spacing: 8px; color: #2563eb; background-color: #eff6ff; padding: 14px 28px; border-radius: 10px; border: 2px dashed #93c5fd;"">
                    {otpCode}
                </span>
            </div>
            <p style=""font-size: 14px; color: #dc2626; text-align: center; font-weight: 600;"">
                ⏳ Mã OTP này chỉ có hiệu lực trong vòng <strong>5 phút</strong>.
            </p>
            <div style=""background-color: #fef2f2; border: 1px solid #fee2e2; border-radius: 8px; padding: 12px 16px; margin-top: 20px; font-size: 13px; color: #991b1b;"">
                ⚠️ <strong>Cảnh báo bảo mật:</strong> Tuyệt đối không chia sẻ mã này cho bất kỳ ai. Nếu bạn không thực hiện đăng nhập này, hãy đổi mật khẩu tài khoản ngay để bảo vệ dữ liệu.
            </div>
        </div>
        <div style=""background-color: #fafafa; border-top: 1px solid #f0f0f0; padding: 16px; text-align: center; font-size: 12px; color: #999999;"">
            © {DateTime.UtcNow.Year} KidCare. Tất cả các quyền được bảo lưu.
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(
            toEmail,
            $"[{otpCode}] Mã OTP xác thực đăng nhập - KidCare",
            htmlContent,
            $"Mã OTP xác thực đăng nhập của bạn là: {otpCode}. Mã có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.",
            displayName
        );
    }

    public async Task<bool> SendChildRegisteredNotificationAsync(
        string parentEmail,
        string parentName,
        string childUsername,
        string childEmail,
        string childFullName,
        bool autoLinked)
    {
        var statusBadge = autoLinked
            ? "<div style=\"background-color: #ecfdf5; border: 1px solid #a7f3d0; color: #065f46; padding: 12px 16px; border-radius: 8px; font-weight: 600; margin-bottom: 20px;\">✅ Hệ thống đã tự động đưa bé vào Danh sách con của bạn trên KidCare!</div>"
            : "<div style=\"background-color: #fffbeb; border: 1px solid #fde68a; color: #92400e; padding: 12px 16px; border-radius: 8px; font-weight: 600; margin-bottom: 20px;\">💡 Bạn hãy đăng nhập KidCare và chọn liên kết tài khoản này vào danh sách con để dễ dàng quản lý!</div>";

        var htmlContent = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Thông báo tài khoản con vừa đăng ký</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px;"">
    <div style=""max-width: 560px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.08);"">
        <div style=""background: linear-gradient(135deg, #4f46e5 0%, #06b6d4 100%); color: #ffffff; padding: 32px 24px; text-align: center;"">
            <div style=""font-size: 40px; margin-bottom: 8px;"">🧒🎈</div>
            <h1 style=""margin: 0; font-size: 22px; font-weight: 700;"">Tài khoản con vừa đăng ký thành công!</h1>
            <p style=""margin: 6px 0 0; opacity: 0.9; font-size: 14px;"">KidCare - Đồng hành cùng cha mẹ & bé yêu</p>
        </div>
        <div style=""padding: 30px 24px; color: #334155; line-height: 1.6;"">
            <p style=""font-size: 16px; margin-top: 0;"">Kính gửi <strong>{parentName}</strong>,</p>
            <p style=""font-size: 15px; color: #475569;"">
                Một tài khoản người dùng của bé đã vừa hoàn tất đăng ký trên hệ thống <strong>KidCare</strong> và sử dụng Gmail của bạn làm thông tin Phụ huynh:
            </p>

            <div style=""background-color: #f1f5f9; border-radius: 10px; padding: 18px 20px; margin: 20px 0;"">
                <table style=""width: 100%; border-collapse: collapse; font-size: 14px;"">
                    <tr>
                        <td style=""color: #64748b; padding: 6px 0; width: 140px;"">Họ và tên của bé:</td>
                        <td style=""font-weight: 700; color: #1e293b;"">{childFullName}</td>
                    </tr>
                    <tr>
                        <td style=""color: #64748b; padding: 6px 0;"">Tên đăng nhập (User):</td>
                        <td style=""font-weight: 700; color: #4f46e5;"">@{childUsername}</td>
                    </tr>
                    <tr>
                        <td style=""color: #64748b; padding: 6px 0;"">Gmail của bé:</td>
                        <td style=""color: #334155;"">{childEmail}</td>
                    </tr>
                    <tr>
                        <td style=""color: #64748b; padding: 6px 0;"">Thời gian đăng ký:</td>
                        <td style=""color: #334155;"">{DateTime.UtcNow.AddHours(7):HH:mm dd/MM/yyyy}</td>
                    </tr>
                </table>
            </div>

            {statusBadge}

            <p style=""font-size: 14px; color: #64748b;"">
                Sau khi kết nối, bạn có thể thiết lập thời gian biểu, giao nhiệm vụ học tập, rèn luyện và tặng điểm sao ⭐ để khen thưởng bé mỗi ngày.
            </p>

            <div style=""text-align: center; margin: 30px 0 10px;"">
                <a href=""http://localhost:5246/kids"" style=""display: inline-block; background-color: #4f46e5; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 15px; padding: 12px 28px; border-radius: 30px; box-shadow: 0 4px 10px rgba(79, 70, 229, 0.3);"">
                    👉 Xem Danh Sách Con Trên KidCare
                </a>
            </div>
        </div>
        <div style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 16px; text-align: center; font-size: 12px; color: #94a3b8;"">
            © {DateTime.UtcNow.Year} KidCare. Email này được gửi tự động đến Gmail của bạn.
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(
            parentEmail,
            $"🧒 [KidCare] Bé {childFullName} (@{childUsername}) vừa đăng ký - Đưa vào danh sách con",
            htmlContent,
            $"Tài khoản con của bé {childFullName} (@{childUsername}, Gmail: {childEmail}) vừa đăng ký thành công trên KidCare.",
            parentName
        );
    }

    public async Task<bool> SendChildLinkedNotificationAsync(
        string toEmail,
        string recipientName,
        string parentName,
        string childName,
        string childUsername,
        string childEmail,
        bool isParent)
    {
        var subject = isParent
            ? $"✅ [KidCare] Đã thêm tài khoản con @{childUsername} vào danh sách quản lý của bạn"
            : $"👨‍👩‍👧 [KidCare] Bạn đã được thêm vào danh sách con của Phụ huynh {parentName}";

        var greeting = isParent
            ? $"Xin chào <strong>{recipientName}</strong>,"
            : $"Xin chào bé <strong>{childName}</strong> (@{childUsername}),";

        var messageBody = isParent
            ? $"Bạn vừa liên kết thành công tài khoản <strong>@{childUsername}</strong> ({childEmail}) làm tài khoản con cho bé <strong>{childName}</strong> trên KidCare. Giờ đây bé có thể tự đăng nhập để làm nhiệm vụ và nhận sao thưởng từ bạn."
            : $"Phụ huynh <strong>{parentName}</strong> đã đưa tài khoản của bạn vào danh sách con để đồng hành, theo dõi lịch học tập và giao nhiệm vụ nhận sao thưởng ⭐ hàng ngày.";

        var htmlContent = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>{subject}</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px;"">
    <div style=""max-width: 560px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.08);"">
        <div style=""background: linear-gradient(135deg, #059669 0%, #10b981 100%); color: #ffffff; padding: 28px 24px; text-align: center;"">
            <h1 style=""margin: 0; font-size: 22px; font-weight: 700;"">🎉 Kết nối tài khoản con thành công!</h1>
            <p style=""margin: 6px 0 0; opacity: 0.9; font-size: 14px;"">KidCare - Hệ Thống Quản Lý Trẻ Em</p>
        </div>
        <div style=""padding: 28px 24px; color: #334155; line-height: 1.6;"">
            <p style=""font-size: 15px; margin-top: 0;"">{greeting}</p>
            <p style=""font-size: 15px; color: #475569;"">{messageBody}</p>

            <div style=""background-color: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 10px; padding: 16px 20px; margin: 20px 0;"">
                <div style=""font-weight: 700; color: #166534; margin-bottom: 8px;"">📋 Thông tin kết nối:</div>
                <div style=""font-size: 14px; color: #15803d;"">
                    • Phụ huynh: <strong>{parentName}</strong><br/>
                    • Bé yêu: <strong>{childName}</strong><br/>
                    • Tài khoản con: <strong>@{childUsername}</strong> ({childEmail})
                </div>
            </div>

            <div style=""text-align: center; margin: 26px 0 10px;"">
                <a href=""http://localhost:5246/kids"" style=""display: inline-block; background-color: #059669; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 15px; padding: 12px 28px; border-radius: 30px;"">
                    🚀 Truy cập KidCare Ngay
                </a>
            </div>
        </div>
        <div style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 14px; text-align: center; font-size: 12px; color: #94a3b8;"">
            © {DateTime.UtcNow.Year} KidCare.
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, htmlContent, messageBody, recipientName);
    }

    public async Task<bool> SendKidsSummaryEmailAsync(
        string parentEmail,
        string parentName,
        IEnumerable<KidSummaryItem> kids)
    {
        var kidsList = kids.ToList();
        var rowsHtml = string.Empty;

        foreach (var k in kidsList)
        {
            var accountInfo = k.HasAccount
                ? $"<span style=\"color: #059669; font-weight: 600;\">@{k.ChildUsername}</span><br/><span style=\"color: #64748b; font-size: 12px;\">{k.ChildEmail}</span>"
                : "<span style=\"color: #d97706; font-size: 12px;\">⚠️ Chưa có tài khoản con</span>";

            rowsHtml += $@"
            <tr style=""border-bottom: 1px solid #e2e8f0;"">
                <td style=""padding: 12px 8px;"">
                    <strong style=""color: #1e293b;"">{k.FullName}</strong>
                    {(string.IsNullOrEmpty(k.Nickname) ? "" : $"<span style=\"color: #64748b; font-size: 12px;\"> ({k.Nickname})</span>")}
                    <div style=""color: #64748b; font-size: 12px;"">{k.SchoolName ?? "Chưa cập nhật trường"}</div>
                </td>
                <td style=""padding: 12px 8px; text-align: center;"">
                    <strong style=""color: #d97706; font-size: 15px;"">⭐ {k.TotalPoints}</strong>
                </td>
                <td style=""padding: 12px 8px;"">
                    {accountInfo}
                </td>
            </tr>";
        }

        var htmlContent = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Danh sách các bé & Tài khoản con của bạn</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.08);"">
        <div style=""background: linear-gradient(135deg, #4f46e5 0%, #06b6d4 100%); color: #ffffff; padding: 28px 24px; text-align: center;"">
            <h1 style=""margin: 0; font-size: 22px; font-weight: 700;"">📋 Danh sách con & Tài khoản quản lý</h1>
            <p style=""margin: 6px 0 0; opacity: 0.9; font-size: 14px;"">Hệ thống KidCare - Báo cáo gửi về Gmail</p>
        </div>
        <div style=""padding: 26px 22px; color: #334155; line-height: 1.6;"">
            <p style=""font-size: 15px; margin-top: 0;"">Xin chào <strong>{parentName}</strong>,</p>
            <p style=""font-size: 14px; color: #475569;"">
                Dưới đây là danh sách các bé và trạng thái tài khoản con hiện đang nằm trong danh sách quản lý của bạn trên KidCare:
            </p>

            <div style=""border: 1px solid #e2e8f0; border-radius: 10px; overflow: hidden; margin: 20px 0;"">
                <table style=""width: 100%; border-collapse: collapse; font-size: 13px;"">
                    <thead style=""background-color: #f1f5f9; color: #475569; text-align: left;"">
                        <tr>
                            <th style=""padding: 10px 8px;"">Bé yêu</th>
                            <th style=""padding: 10px 8px; text-align: center;"">Sao tích lũy</th>
                            <th style=""padding: 10px 8px;"">Tài khoản con</th>
                        </tr>
                    </thead>
                    <tbody>
                        {rowsHtml}
                    </tbody>
                </table>
            </div>

            <div style=""background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 12px 16px; border-radius: 4px; font-size: 13px; color: #1e40af; margin-bottom: 20px;"">
                💡 <strong>Mẹo:</strong> Để bé tự theo dõi lịch học và rèn luyện, hãy cấp Tên đăng nhập và Mật khẩu tài khoản con cho bé đăng nhập trên ứng dụng!
            </div>

            <div style=""text-align: center; margin: 24px 0 10px;"">
                <a href=""http://localhost:5246/kids"" style=""display: inline-block; background-color: #4f46e5; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 14px; padding: 12px 28px; border-radius: 30px;"">
                    👉 Quản Lý Trực Tiếp Trên KidCare
                </a>
            </div>
        </div>
        <div style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 14px; text-align: center; font-size: 12px; color: #94a3b8;"">
            © {DateTime.UtcNow.Year} KidCare.
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(
            parentEmail,
            $"📋 [KidCare] Báo cáo danh sách các bé & tài khoản con của bạn ({kidsList.Count} bé)",
            htmlContent,
            $"Danh sách quản lý con của bạn gồm {kidsList.Count} bé trên KidCare.",
            parentName
        );
    }

    public async Task<bool> SendInviteChildEmailAsync(
        string targetEmail,
        string parentName,
        string kidName)
    {
        var htmlContent = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Lời mời tham gia KidCare</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px;"">
    <div style=""max-width: 560px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.08);"">
        <div style=""background: linear-gradient(135deg, #4f46e5 0%, #06b6d4 100%); color: #ffffff; padding: 28px 24px; text-align: center;"">
            <h1 style=""margin: 0; font-size: 22px; font-weight: 700;"">💌 Lời mời tham gia KidCare</h1>
            <p style=""margin: 6px 0 0; opacity: 0.9; font-size: 14px;"">Hệ thống Quản lý và Đồng hành cùng bé</p>
        </div>
        <div style=""padding: 28px 24px; color: #334155; line-height: 1.6;"">
            <p style=""font-size: 15px; margin-top: 0;"">Xin chào,</p>
            <p style=""font-size: 15px; color: #475569;"">
                Phụ huynh <strong>{parentName}</strong> đã gửi lời mời bạn tham gia <strong>KidCare</strong> để kết nối vào hồ sơ của bé <strong>{kidName}</strong>.
            </p>
            <p style=""font-size: 14px; color: #64748b;"">
                Với KidCare, bạn có thể dễ dàng theo dõi thời khóa biểu, nhật ký hoạt động, thực hiện nhiệm vụ và tích lũy thật nhiều điểm sao ⭐ khen thưởng!
            </p>

            <div style=""text-align: center; margin: 28px 0 10px;"">
                <a href=""http://localhost:5246"" style=""display: inline-block; background-color: #4f46e5; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 15px; padding: 12px 28px; border-radius: 30px;"">
                    🎉 Đăng Ký & Tham Gia Ngay
                </a>
            </div>
        </div>
        <div style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 14px; text-align: center; font-size: 12px; color: #94a3b8;"">
            © {DateTime.UtcNow.Year} KidCare.
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(
            targetEmail,
            $"💌 [KidCare] Lời mời tham gia kết nối vào hồ sơ bé {kidName} từ {parentName}",
            htmlContent,
            $"Phụ huynh {parentName} mời bạn tham gia KidCare để kết nối hồ sơ bé {kidName}."
        );
    }

    public async Task<bool> SendFamilyGroupAddedNotificationAsync(
        string toEmail,
        string recipientName,
        string parentName,
        string groupName,
        string roleInFamily)
    {
        var roleLabel = roleInFamily switch
        {
            "Child" => "Tài khoản Con (Bé)",
            "Parent" => "Phụ huynh đồng quản lý",
            "Guardian" => "Người giám hộ",
            _ => "Thành viên gia đình"
        };

        var roleDescription = roleInFamily switch
        {
            "Child" => "Bạn có thể tự đăng nhập để theo dõi lịch trình, làm nhiệm vụ và nhận điểm sao thưởng ⭐ từ cha mẹ.",
            _ => "Bạn có thể cùng xem danh sách các bé, theo dõi sức khỏe, lịch sinh hoạt và cùng khen thưởng sao cho các bé trong gia đình."
        };

        var htmlContent = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Bạn đã được thêm vào Nhóm Gia Đình</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px;"">
    <div style=""max-width: 560px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.08);"">
        <div style=""background: linear-gradient(135deg, #4f46e5 0%, #06b6d4 100%); color: #ffffff; padding: 28px 24px; text-align: center;"">
            <h1 style=""margin: 0; font-size: 22px; font-weight: 700;"">👨‍👩‍👧‍👦 Chào mừng bạn đến với Nhóm Gia Đình!</h1>
            <p style=""margin: 6px 0 0; opacity: 0.9; font-size: 14px;"">KidCare - Quản Lý & Chăm Sóc Trẻ Em</p>
        </div>
        <div style=""padding: 28px 24px; color: #334155; line-height: 1.6;"">
            <p style=""font-size: 15px; margin-top: 0;"">Xin chào <strong>{recipientName}</strong>,</p>
            <p style=""font-size: 15px; color: #475569;"">
                Phụ huynh <strong>{parentName}</strong> đã thêm tài khoản Gmail <strong>{toEmail}</strong> của bạn vào <strong>{groupName}</strong> để cùng đồng hành và quản lý trẻ.
            </p>

            <div style=""background-color: #eef2ff; border: 1px solid #c7d2fe; border-radius: 10px; padding: 16px 20px; margin: 20px 0;"">
                <div style=""font-weight: 700; color: #3730a3; margin-bottom: 6px;"">🎯 Vai trò của bạn: {roleLabel}</div>
                <div style=""font-size: 14px; color: #4338ca;"">
                    {roleDescription}
                </div>
            </div>

            <div style=""text-align: center; margin: 28px 0 10px;"">
                <a href=""http://localhost:5246/kids"" style=""display: inline-block; background-color: #4f46e5; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 15px; padding: 12px 28px; border-radius: 30px; box-shadow: 0 4px 10px rgba(79, 70, 229, 0.3);"">
                    👉 Truy Cập Gia Đình Trên KidCare
                </a>
            </div>
        </div>
        <div style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 14px; text-align: center; font-size: 12px; color: #94a3b8;"">
            © {DateTime.UtcNow.Year} KidCare.
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(
            toEmail,
            $"👨‍👩‍👧‍👦 [KidCare] Bạn đã được thêm vào {groupName} để cùng quản lý trẻ",
            htmlContent,
            $"Bạn đã được thêm vào {groupName} của {parentName} trên KidCare với vai trò {roleLabel}."
        );
    }
}
