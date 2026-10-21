namespace Giahuy0790.Services;

/// <summary>
/// Dịch vụ gửi email thông báo và mã xác thực OTP qua giao thức SMTP (Gmail)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi mã xác thực OTP khôi phục mật khẩu qua email
    /// </summary>
    Task<bool> SendPasswordResetOtpAsync(string toEmail, string otpCode, string? recipientName = null);

    /// <summary>
    /// Gửi mã xác thực OTP bảo mật khi đăng nhập qua email (Xác thực 2 bước 2FA)
    /// </summary>
    Task<bool> SendLoginOtpAsync(string toEmail, string otpCode, string? recipientName = null);

    /// <summary>
    /// Gửi email thông báo cho Phụ huynh khi tài khoản con vừa đăng ký
    /// </summary>
    Task<bool> SendChildRegisteredNotificationAsync(
        string parentEmail,
        string parentName,
        string childUsername,
        string childEmail,
        string childFullName,
        bool autoLinked);

    /// <summary>
    /// Gửi email thông báo khi tài khoản con được liên kết vào danh sách của Phụ huynh
    /// </summary>
    Task<bool> SendChildLinkedNotificationAsync(
        string toEmail,
        string recipientName,
        string parentName,
        string childName,
        string childUsername,
        string childEmail,
        bool isParent);

    /// <summary>
    /// Gửi danh sách tổng hợp tất cả các con và tài khoản con về Gmail của Phụ huynh
    /// </summary>
    Task<bool> SendKidsSummaryEmailAsync(
        string parentEmail,
        string parentName,
        IEnumerable<KidSummaryItem> kids);

    /// <summary>
    /// Gửi lời mời tham gia kết nối vào danh sách con đến một địa chỉ Gmail
    /// </summary>
    Task<bool> SendInviteChildEmailAsync(
        string targetEmail,
        string parentName,
        string kidName);

    /// <summary>
    /// Gửi thông báo qua Gmail khi một tài khoản được thêm vào Nhóm Gia Đình để quản lý trẻ
    /// </summary>
    Task<bool> SendFamilyGroupAddedNotificationAsync(
        string toEmail,
        string recipientName,
        string parentName,
        string groupName,
        string roleInFamily);
}

/// <summary>
/// DTO chứa thông tin tóm tắt hồ sơ bé để hiển thị trong email danh sách con
/// </summary>
public record KidSummaryItem(
    int Id,
    string FullName,
    string? Nickname,
    string? SchoolName,
    int TotalPoints,
    bool HasAccount,
    string? ChildUsername,
    string? ChildEmail
);
