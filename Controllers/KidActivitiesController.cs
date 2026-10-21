using Giahuy0790.Models;
using Giahuy0790.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Giahuy0790.Controllers;

/// <summary>
/// Controller API quản lý Nhật ký hoạt động, Lịch trình và Nhiệm vụ của trẻ (KidActivities)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class KidActivitiesController(IKidActivityRepository activityRepo, IKidRepository kidRepo) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách hoạt động & nhiệm vụ của một trẻ theo kidId
    /// </summary>
    [HttpGet("kid/{kidId:int}")]
    public async Task<IActionResult> GetByKidId(int kidId)
    {
        var activities = await activityRepo.GetActivitiesByKidIdAsync(kidId);
        return Ok(activities);
    }

    /// <summary>
    /// Lấy danh sách hoạt động trong một ngày cụ thể của trẻ
    /// </summary>
    [HttpGet("kid/{kidId:int}/by-date")]
    public async Task<IActionResult> GetByDate(int kidId, [FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var activities = await activityRepo.GetActivitiesByDateAsync(kidId, targetDate);
        return Ok(activities);
    }

    /// <summary>
    /// Thêm một hoạt động hoặc nhiệm vụ mới cho trẻ
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateActivityDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { message = "Tiêu đề hoạt động không được để trống." });
        }

        var kid = await kidRepo.GetByIdAsync(dto.KidId);
        if (kid == null)
        {
            return BadRequest(new { message = $"Không tìm thấy trẻ với ID = {dto.KidId}" });
        }

        var activity = new KidActivity
        {
            KidId = dto.KidId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "Sinh hoạt" : dto.Category.Trim(),
            ActivityTime = dto.ActivityTime ?? DateTime.UtcNow,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Pending" : dto.Status.Trim(),
            RewardPoints = dto.RewardPoints ?? 5,
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var created = await activityRepo.AddAsync(activity);
        return Ok(created);
    }

    /// <summary>
    /// Cập nhật trạng thái nhiệm vụ (Pending / Completed / Missed).
    /// Nếu chuyển sang 'Completed', hệ thống sẽ tự động cộng điểm thưởng cho bé!
    /// </summary>
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { message = "Trạng thái không được để trống." });
        }

        var success = await activityRepo.UpdateStatusAsync(id, dto.Status.Trim());
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy hoạt động với ID = {id}" });
        }

        return Ok(new
        {
            message = $"Cập nhật trạng thái sang '{dto.Status}' thành công!"
        });
    }

    /// <summary>
    /// Xóa hoạt động
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await activityRepo.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy hoạt động với ID = {id}" });
        }
        return Ok(new { message = "Đã xóa hoạt động thành công" });
    }
}

public record CreateActivityDto(
    int KidId,
    string Title,
    string? Description = null,
    string? Category = "Sinh hoạt",
    DateTime? ActivityTime = null,
    string? Status = "Pending",
    int? RewardPoints = 5,
    string? Notes = null
);

public record UpdateStatusDto(
    string Status
);
