using System.Security.Claims;
using Giahuy0790.Models;
using Giahuy0790.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Giahuy0790.Controllers;

/// <summary>
/// Controller API quản lý Hồ sơ Trẻ em (Kids): Thêm / Xóa / Sửa / Tra cứu theo Phụ huynh & Quyền hạn
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class KidsController(IKidRepository kidRepo) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách Trẻ em (Admin xem tất cả, Phụ huynh xem con em mình, Trẻ xem hồ sơ chính mình)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isParent = User.IsInRole("Parents") || User.IsInRole("parents");
        var isKid = User.IsInRole("Kid") || User.IsInRole("kid");
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        if (isParent && !string.IsNullOrEmpty(userId))
        {
            var myKids = await kidRepo.GetKidsByParentAsync(userId);
            return Ok(myKids);
        }

        // Mặc định hoặc Admin xem toàn bộ
        var kids = await kidRepo.GetAllAsync();
        return Ok(kids);
    }

    /// <summary>
    /// Lấy chi tiết hồ sơ một trẻ kèm lịch hoạt động
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var kid = await kidRepo.GetKidWithActivitiesAsync(id);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }
        return Ok(kid);
    }

    /// <summary>
    /// Tìm kiếm trẻ theo họ tên, tên gọi ở nhà, hoặc trường học
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? keyword)
    {
        var results = await kidRepo.SearchKidsAsync(keyword ?? string.Empty);
        return Ok(results);
    }

    /// <summary>
    /// Thêm mới hồ sơ trẻ em
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateKidDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new { message = "Họ và tên của trẻ không được để trống." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var kid = new Kid
        {
            FullName = dto.FullName.Trim(),
            Nickname = dto.Nickname?.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender?.Trim() ?? "Nam",
            AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl)
                ? $"https://api.dicebear.com/7.x/bottts/svg?seed={Uri.EscapeDataString(dto.FullName.Trim())}"
                : dto.AvatarUrl.Trim(),
            SchoolName = dto.SchoolName?.Trim(),
            HealthNotes = dto.HealthNotes?.Trim(),
            Interests = dto.Interests?.Trim(),
            ParentId = !string.IsNullOrEmpty(dto.ParentId) ? dto.ParentId : currentUserId,
            TotalPoints = dto.InitialPoints ?? 0,
            CreatedAt = DateTime.UtcNow
        };

        var created = await kidRepo.AddAsync(kid);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Cập nhật thông tin hồ sơ của trẻ
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateKidDto dto)
    {
        var kid = await kidRepo.GetByIdAsync(id);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy trẻ với ID = {id}" });
        }

        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new { message = "Họ và tên không được để trống." });
        }

        kid.FullName = dto.FullName.Trim();
        kid.Nickname = dto.Nickname?.Trim();
        kid.DateOfBirth = dto.DateOfBirth;
        kid.Gender = dto.Gender?.Trim() ?? kid.Gender;
        kid.AvatarUrl = dto.AvatarUrl?.Trim() ?? kid.AvatarUrl;
        kid.SchoolName = dto.SchoolName?.Trim();
        kid.HealthNotes = dto.HealthNotes?.Trim();
        kid.Interests = dto.Interests?.Trim();
        kid.UpdatedAt = DateTime.UtcNow;

        await kidRepo.UpdateAsync(kid);

        return Ok(new
        {
            message = "Cập nhật hồ sơ trẻ thành công",
            kid
        });
    }

    /// <summary>
    /// Thưởng điểm / Sao khen ngợi cho trẻ khi làm việc tốt
    /// </summary>
    [HttpPost("{id:int}/points")]
    public async Task<IActionResult> RewardPoints(int id, [FromBody] RewardPointsDto dto)
    {
        var success = await kidRepo.AddRewardPointsAsync(id, dto.Points);
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }

        return Ok(new
        {
            message = $"🎉 Đã thưởng thành công {dto.Points} điểm/sao khen ngợi cho bé!"
        });
    }

    /// <summary>
    /// Xóa hồ sơ trẻ
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await kidRepo.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ với ID = {id}" });
        }
        return Ok(new { message = "Đã xóa hồ sơ trẻ thành công" });
    }
}

public record CreateKidDto(
    string FullName,
    string? Nickname = null,
    DateTime? DateOfBirth = null,
    string? Gender = "Nam",
    string? AvatarUrl = null,
    string? SchoolName = null,
    string? HealthNotes = null,
    string? Interests = null,
    string? ParentId = null,
    int? InitialPoints = 0
);

public record UpdateKidDto(
    string FullName,
    string? Nickname = null,
    DateTime? DateOfBirth = null,
    string? Gender = null,
    string? AvatarUrl = null,
    string? SchoolName = null,
    string? HealthNotes = null,
    string? Interests = null
);

public record RewardPointsDto(
    int Points
);
