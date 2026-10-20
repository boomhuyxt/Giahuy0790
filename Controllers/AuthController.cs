using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Giahuy0790.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Giahuy0790.Controllers;

/// <summary>
/// Controller xác thực người dùng (ASP.NET Core Identity & JWT Token): Đăng ký, Đăng nhập, Phân quyền Role
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration config) : ControllerBase
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

        // Phân quyền Role cho người dùng (mặc định là User nếu không chỉ định)
        var roleToAssign = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim();
        if (!await roleManager.RoleExistsAsync(roleToAssign))
        {
            await roleManager.CreateAsync(new IdentityRole(roleToAssign));
        }
        await userManager.AddToRoleAsync(user, roleToAssign);

        return Ok(new
        {
            message = "🎉 Đăng ký tài khoản thành công!",
            userId = user.Id,
            username = user.UserName,
            email = user.Email,
            role = roleToAssign
        });

        static string encode(string text) => Uri.EscapeDataString(text);
    }

    /// <summary>
    /// Đăng nhập tài khoản và nhận JWT Bearer Token chứa thông tin vai trò (Roles)
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

        // Lấy danh sách Roles của người dùng
        var userRoles = await userManager.GetRolesAsync(user);
        if (userRoles.Count == 0)
        {
            userRoles = ["User"];
        }

        // Tạo JWT Token có chứa Role claims
        var token = GenerateJwtToken(user, userRoles, out var expiresAt);

        return Ok(new
        {
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
        var secretKey = jwtSettings["Key"] ?? "Default_Secret_Key_For_Jwt_Authentication_Must_Be_Long_Enough_2026";
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
}

/// <summary>
/// DTO Đăng ký tài khoản (hỗ trợ chỉ định quyền Role: Admin, Staff, User)
/// </summary>
public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? FullName = null,
    string? Role = "User"
);

/// <summary>
/// DTO Đăng nhập
/// </summary>
public record LoginRequest(
    string UsernameOrEmail,
    string Password
);
