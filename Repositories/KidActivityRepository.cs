using Giahuy0790.Data;
using Giahuy0790.Models;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Repositories;

/// <summary>
/// Triển khai Repository cho Nhật ký hoạt động & Nhiệm vụ của trẻ (KidActivity)
/// </summary>
public class KidActivityRepository(AppDbContext context) : Repository<KidActivity>(context), IKidActivityRepository
{
    public async Task<IEnumerable<KidActivity>> GetActivitiesByKidIdAsync(int kidId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.KidId == kidId)
            .OrderByDescending(a => a.ActivityTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<KidActivity>> GetActivitiesByDateAsync(int kidId, DateTime date)
    {
        var startOfDay = date.Date.ToUniversalTime();
        var endOfDay = startOfDay.AddDays(1);

        return await _dbSet
            .AsNoTracking()
            .Where(a => a.KidId == kidId && a.ActivityTime >= startOfDay && a.ActivityTime < endOfDay)
            .OrderBy(a => a.ActivityTime)
            .ToListAsync();
    }

    public async Task<bool> UpdateStatusAsync(int activityId, string status)
    {
        var activity = await _dbSet.Include(a => a.Kid).FirstOrDefaultAsync(a => a.Id == activityId);
        if (activity == null) return false;

        var prevStatus = activity.Status;
        activity.Status = status;

        // Nếu chuyển sang Completed và trước đó chưa hoàn thành, cộng điểm thưởng cho bé
        if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && 
            !prevStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) &&
            activity.Kid != null)
        {
            activity.Kid.TotalPoints += activity.RewardPoints;
            activity.Kid.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
