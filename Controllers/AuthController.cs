using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Giahuy0790.Data;
using Giahuy0790.Models;
using Giahuy0790.Repositories;
using Giahuy0790.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Giahuy0790.Controllers;

/// <summary>
/// Controller xác thực người dùng (ASP.NET Core Identity & JWT Token): Đăng ký, Đăng nhập, Quên mật khẩu qua SMTP, Phân quyền Role
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration config,
    IEmailService emailService,
    IKidRepository kidRepo) : ControllerBase
{
    /// <summary>
    /// Đăng ký tài khoản người dùng mới (có gán quyền Role)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Kiểm tra xem username hoặc email đã tồn tại chưa
        var existingByUsername = await userManager.FindByNameAsync(request.Username);
        if (existingByUsername != null)
        {
            return BadRequest(new { message = "Tên đăng nhập (Username) này đã được sử dụng." });
        }

        var existingByEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingByEmail != null)
        {
            return BadRequest(new { message = "Địa chỉ Email này đã được đăng ký tài khoản khác." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Username.Trim(),
            Email = request.Email.Trim(),
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Username.Trim() : request.FullName.Trim(),
            AvatarUrl = $"https://api.dicebear.com/7.x/bottts/svg?seed={encode(request.Username)}",
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Đăng ký không thành công", errors });
        }

        // Phân quyền Role: 3 nhóm quyền chính là Admin, Parents, Kid (mặc định là Parents)
        var requestedRole = string.IsNullOrWhiteSpace(request.Role) ? "Parents" : request.Role.Trim();
        var roleToAssign = requestedRole.ToLower() switch
        {
            "admin" => "Admin",
            "kid" => "Kid",
            "parents" or "parent" => "Parents",
            _ => "Parents"
        };

        if (!await roleManager.RoleExistsAsync(roleToAssign))
        {
            await roleManager.CreateAsync(new IdentityRole(roleToAssign));
        }
        await userManager.AddToRoleAsync(user, roleToAssign);

        // Tự động kết nối và gửi Gmail cho Phụ huynh nếu có cung cấp ParentEmail
        var parentEmail = request.ParentEmail?.Trim();
        var autoLinked = false;
        string? linkedParentName = null;

        if (!string.IsNullOrWhiteSpace(parentEmail))
        {
            var parentUser = await userManager.FindByEmailAsync(parentEmail)
                             ?? await userManager.FindByNameAsync(parentEmail);
            if (parentUser != null)
            {
                linkedParentName = parentUser.FullName ?? parentUser.UserName;
                var kid = new Kid
                {
                    FullName = user.FullName ?? user.UserName ?? "Bé yêu",
                    AvatarUrl = user.AvatarUrl,
                    ParentId = parentUser.Id,
                    KidUserId = user.Id,
                    TotalPoints = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await kidRepo.AddAsync(kid);
                autoLinked = true;
            }

            // Gửi Gmail thông báo qua SMTP cho Phụ huynh
            var targetParentEmail = parentUser?.Email ?? parentEmail;
            var targetParentName = linkedParentName ?? "Quý Phụ Huynh";
            _ = Task.Run(async () =>
            {
                try
                {
                    await emailService.SendChildRegisteredNotificationAsync(
                        targetParentEmail,
                        targetParentName,
                        user.UserName ?? string.Empty,
                        user.Email ?? string.Empty,
                        user.FullName ?? user.UserName ?? string.Empty,
                        autoLinked
                    );
                }
                catch
                {
                    // Tránh uncaught background exception
                }
            });
        }

        return Ok(new
        {
            message = autoLinked
                ? $"🎉 Đăng ký thành công! Tài khoản đã được tự động đưa vào danh sách con của phụ huynh {linkedParentName} và gửi email thông báo."
                : (!string.IsNullOrWhiteSpace(parentEmail)
                    ? $"🎉 Đăng ký thành công! Hệ thống đã gửi email thông báo tới Gmail phụ huynh: {parentEmail} để đưa vào danh sách con."
                    : "🎉 Đăng ký tài khoản thành công!"),
            userId = user.Id,
            username = user.UserName,
            email = user.Email,
            role = roleToAssign,
            autoLinked
        });

        static string encode(string text) => Uri.EscapeDataString(text);
    }

    /// <summary>
    /// Đăng nhập tài khoản: Bước 1 xác thực thông tin và gửi OTP qua Email (nếu có); Bước 2 (hoặc kèm OTP) cấp JWT Token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Cho phép đăng nhập bằng cả Username hoặc Email
        var identifier = request.UsernameOrEmail.Trim();
        var user = await userManager.FindByNameAsync(identifier) 
                   ?? await userManager.FindByEmailAsync(identifier);

        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Tên đăng nhập/Email hoặc mật khẩu không chính xác." });
        }

        // Nếu tài khoản không có email (ví dụ tài khoản độc lập tạo nội bộ), cho phép đăng nhập trực tiếp
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return await CompleteLoginSuccessAsync(user);
        }

        // Nếu client cung cấp sẵn mã OTP (xác thực trực tiếp từ màn hình hoặc app mobile)
        if (!string.IsNullOrWhiteSpace(request.Otp))
        {
            return await VerifyAndCompleteLoginAsync(user, request.Otp.Trim());
        }

        // BƯỚC 1: Sinh mã OTP 6 chữ số và gửi qua Email
        var otpCode = Random.Shared.Next(100000, 1000000).ToString();
        user.LoginOtp = otpCode;
        user.LoginOtpExpiry = DateTime.UtcNow.AddMinutes(5);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return StatusCode(500, new { message = "Không thể khởi tạo mã OTP xác thực. Vui lòng thử lại." });
        }

        // Gửi email OTP ngầm không chặn luồng chính
        _ = Task.Run(async () =>
        {
            try
            {
                await emailService.SendLoginOtpAsync(
                    user.Email!,
                    otpCode,
                    string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName
                );
            }
            catch
            {
                // Tránh crash unhandled exception
            }
        });

        return Ok(new
        {
            requiresOtp = true,
            message = "🔐 Mã OTP xác thực đăng nhập đã được gửi tới email của bạn. Vui lòng nhập mã để hoàn tất.",
            email = MaskEmail(user.Email),
            username = user.UserName
        });
    }

    /// <summary>
    /// Xác thực mã OTP đăng nhập và nhận JWT Bearer Token
    /// </summary>
    [HttpPost("verify-login-otp")]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Otp))
        {
            return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin tài khoản và mã OTP." });
        }

        var identifier = request.UsernameOrEmail.Trim();
        var user = await userManager.FindByNameAsync(identifier) 
                   ?? await userManager.FindByEmailAsync(identifier);

        if (user == null)
        {
            return Unauthorized(new { message = "Không tìm thấy thông tin tài khoản." });
        }

        return await VerifyAndCompleteLoginAsync(user, request.Otp.Trim());
    }

    /// <summary>
    /// Gửi lại mã OTP xác thực đăng nhập
    /// </summary>
    [HttpPost("resend-login-otp")]
    public async Task<IActionResult> ResendLoginOtp([FromBody] ResendLoginOtpRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.UsernameOrEmail))
        {
            return BadRequest(new { message = "Vui lòng cung cấp tên đăng nhập hoặc email." });
        }

        var identifier = request.UsernameOrEmail.Trim();
        var user = await userManager.FindByNameAsync(identifier) 
                   ?? await userManager.FindByEmailAsync(identifier);

        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest(new { message = "Tài khoản không tồn tại hoặc chưa đăng ký email để nhận mã OTP." });
        }

        // Sinh mã OTP mới (5 phút hiệu lực)
        var otpCode = Random.Shared.Next(100000, 1000000).ToString();
        user.LoginOtp = otpCode;
        user.LoginOtpExpiry = DateTime.UtcNow.AddMinutes(5);
        await userManager.UpdateAsync(user);

        _ = Task.Run(async () =>
        {
            try
            {
                await emailService.SendLoginOtpAsync(
                    user.Email!,
                    otpCode,
                    string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName
                );
            }
            catch { }
        });

        return Ok(new
        {
            message = "Mã xác thực OTP mới đã được gửi tới email của bạn.",
            email = MaskEmail(user.Email)
        });
    }

    private async Task<IActionResult> VerifyAndCompleteLoginAsync(ApplicationUser user, string inputOtp)
    {
        if (string.IsNullOrWhiteSpace(user.LoginOtp) ||
            user.LoginOtp != inputOtp ||
            user.LoginOtpExpiry == null ||
            user.LoginOtpExpiry < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Mã xác thực OTP không chính xác hoặc đã hết thời gian hiệu lực (5 phút)." });
        }

        // Xóa mã OTP sau khi sử dụng thành công để chống tấn công lặp lại (Replay attack)
        user.LoginOtp = null;
        user.LoginOtpExpiry = null;
        await userManager.UpdateAsync(user);

        return await CompleteLoginSuccessAsync(user);
    }

    private async Task<IActionResult> CompleteLoginSuccessAsync(ApplicationUser user)
    {
        // Lấy danh sách Roles của người dùng
        var userRoles = await userManager.GetRolesAsync(user);
        if (userRoles.Count == 0)
        {
            userRoles = ["Parents"];
        }

        // Tạo JWT Token có chứa Role claims
        var token = GenerateJwtToken(user, userRoles, out var expiresAt);

        return Ok(new
        {
            requiresOtp = false,
            message = "✅ Đăng nhập thành công!",
            token,
            tokenType = "Bearer",
            expiresAt,
            user = new
            {
                id = user.Id,
                username = user.UserName,
                email = user.Email,
                fullName = user.FullName,
                avatarUrl = user.AvatarUrl,
                roles = userRoles
            }
        });
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        var name = parts[0];
        var domain = parts[1];
        if (name.Length <= 2)
        {
            return $"{name}***@{domain}";
        }
        return $"{name[..2]}***{name[^1..]}@{domain}";
    }

    /// <summary>
    /// Lấy thông tin tài khoản đang đăng nhập (Yêu cầu JWT Bearer Token)
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Không xác định được phiên đăng nhập." });
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new
        {
            id = user.Id,
            username = user.UserName,
            email = user.Email,
            fullName = user.FullName,
            avatarUrl = user.AvatarUrl,
            roles,
            createdAt = user.CreatedAt
        });
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles, out DateTime expiresAt)
    {
        var jwtSettings = config.GetSection("Jwt");
        var rawKey = config["JWT_SECRET_KEY"];
        var settingKey = jwtSettings["Key"];
        var secretKey = !string.IsNullOrWhiteSpace(rawKey)
            ? rawKey
            : (!string.IsNullOrWhiteSpace(settingKey)
                ? settingKey
                : "Giahuy0790_Super_Secret_Key_For_Jwt_Authentication_2026!#@$");

        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
        {
            secretKey = "Giahuy0790_Super_Secret_Key_For_Jwt_Authentication_2026!#@$";
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var durationMinutes = double.TryParse(jwtSettings["DurationInMinutes"], out var minutes) ? minutes : 1440;
        expiresAt = DateTime.UtcNow.AddMinutes(durationMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("FullName", user.FullName ?? user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Gắn các Role vào Claim để JWT_decode phía Mobile (Flutter) đọc được trực tiếp
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }

    /// <summary>
    /// Quên mật khẩu: Gửi mã OTP xác thực 6 chữ số qua Email bằng giao thức SMTP
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Vui lòng cung cấp địa chỉ email hợp lệ." });
        }

        var identifier = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(identifier)
                   ?? await userManager.FindByNameAsync(identifier);

        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            // Phản hồi chung để đảm bảo bảo mật không lộ thông tin tài khoản
            return Ok(new
            {
                message = "Nếu thông tin tài khoản tồn tại trong hệ thống, mã xác thực OTP đã được gửi đến email liên kết. Vui lòng kiểm tra hộp thư."
            });
        }

        // Tạo mã OTP ngẫu nhiên gồm 6 chữ số (100000 - 999999)
        var otpCode = Random.Shared.Next(100000, 1000000).ToString();
        user.PasswordResetOtp = otpCode;
        user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(15);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return StatusCode(500, new { message = "Không thể tạo mã xác thực. Vui lòng thử lại sau." });
        }

        // Gửi email chứa mã OTP qua dịch vụ SMTP
        await emailService.SendPasswordResetOtpAsync(
            user.Email!,
            otpCode,
            string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName
        );

        return Ok(new
        {
            message = "Mã xác thực OTP đã được gửi đến email của bạn. Mã có hiệu lực trong vòng 15 phút.",
            email = user.Email
        });
    }

    /// <summary>
    /// Xác thực mã OTP đặt lại mật khẩu trước khi cho phép nhập mật khẩu mới
    /// </summary>
    [HttpPost("verify-reset-otp")]
    public async Task<IActionResult> VerifyResetOtp([FromBody] VerifyResetOtpRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
        {
            return BadRequest(new { message = "Email và mã OTP không được để trống." });
        }

        var identifier = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(identifier)
                   ?? await userManager.FindByNameAsync(identifier);
        if (user == null ||
            string.IsNullOrWhiteSpace(user.PasswordResetOtp) ||
            user.PasswordResetOtp != request.Otp.Trim() ||
            user.PasswordResetOtpExpiry == null ||
            user.PasswordResetOtpExpiry < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Mã xác thực OTP không chính xác hoặc đã hết hạn." });
        }

        return Ok(new { message = "Mã xác thực OTP hợp lệ. Bạn có thể tiến hành đổi mật khẩu mới." });
    }

    /// <summary>
    /// Đặt lại mật khẩu mới sau khi xác thực mã OTP
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Otp) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Vui lòng nhập đầy đủ Email, mã OTP và mật khẩu mới." });
        }

        if (!string.IsNullOrWhiteSpace(request.ConfirmPassword) && request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Mật khẩu xác nhận không khớp với mật khẩu mới." });
        }

        var identifier = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(identifier)
                   ?? await userManager.FindByNameAsync(identifier);
        if (user == null ||
            string.IsNullOrWhiteSpace(user.PasswordResetOtp) ||
            user.PasswordResetOtp != request.Otp.Trim() ||
            user.PasswordResetOtpExpiry == null ||
            user.PasswordResetOtpExpiry < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Mã xác thực OTP không chính xác hoặc đã hết thời gian hiệu lực." });
        }

        // Tạo Identity Reset Token an toàn để đổi mật khẩu qua Identity
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            var errors = resetResult.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Đổi mật khẩu thất bại", errors });
        }

        // Xóa mã OTP sau khi sử dụng thành công để tránh tấn công Replay
        user.PasswordResetOtp = null;
        user.PasswordResetOtpExpiry = null;
        await userManager.UpdateAsync(user);

        return Ok(new
        {
            message = "🎉 Đặt lại mật khẩu thành công! Bạn có thể sử dụng mật khẩu mới để đăng nhập."
        });
    }

    /// <summary>
    /// Xóa tài khoản người dùng (chỉ Quản trị viên mới có quyền)
    /// </summary>
    [Authorize(Roles = "Admin,admin")]
    [HttpDelete("users/{username}")]
    public async Task<IActionResult> DeleteUser(string username)
    {
        var targetUser = await userManager.FindByNameAsync(username);
        if (targetUser == null)
        {
            return NotFound(new { message = $"Không tìm thấy tài khoản '{username}'." });
        }

        var res = await userManager.DeleteAsync(targetUser);
        if (!res.Succeeded)
        {
            return BadRequest(new { message = "Không thể xóa tài khoản", errors = res.Errors.Select(e => e.Description) });
        }

        return Ok(new { message = $"Đã xóa tài khoản '{username}' thành công." });
    }
}

/// <summary>
/// DTO Đăng ký tài khoản (hỗ trợ chỉ định quyền Role: Admin, Parents, Kid & Gmail Phụ huynh)
/// </summary>
public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? FullName = null,
    string? Role = "Parents",
    string? ParentEmail = null
);

/// <summary>
/// DTO Đăng nhập (hỗ trợ nhập mã OTP xác thực)
/// </summary>
public record LoginRequest(
    string UsernameOrEmail,
    string Password,
    string? Otp = null
);

/// <summary>
/// DTO Xác thực mã OTP khi đăng nhập
/// </summary>
public record VerifyLoginOtpRequest(
    string UsernameOrEmail,
    string Otp
);

/// <summary>
/// DTO Gửi lại mã OTP xác thực đăng nhập
/// </summary>
public record ResendLoginOtpRequest(
    string UsernameOrEmail
);

/// <summary>
/// DTO Yêu cầu gửi mã OTP quên mật khẩu
/// </summary>
public record ForgotPasswordRequest(
    string Email
);

/// <summary>
/// DTO Xác thực mã OTP
/// </summary>
public record VerifyResetOtpRequest(
    string Email,
    string Otp
);

/// <summary>
/// DTO Đặt lại mật khẩu mới
/// </summary>
public record ResetPasswordRequest(
    string Email,
    string Otp,
    string NewPassword,
    string? ConfirmPassword = null
);
