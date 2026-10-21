using Giahuy0790.Data;
using Giahuy0790.Models;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Repositories;

/// <summary>
/// Triển khai Repository cho Trẻ em (Kid)
/// </summary>
public class KidRepository(AppDbContext context) : Repository<Kid>(context), IKidRepository
{
    public override async Task<IEnumerable<Kid>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(k => k.Parent)
            .Include(k => k.Activities)
            .OrderByDescending(k => k.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Kid>> GetKidsByParentAsync(string parentId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(k => k.ParentId == parentId)
            .Include(k => k.Activities)
            .OrderByDescending(k => k.Id)
            .ToListAsync();
    }

    public async Task<Kid?> GetKidWithActivitiesAsync(int kidId)
    {
        return await _dbSet
            .Include(k => k.Parent)
            .Include(k => k.Activities.OrderByDescending(a => a.ActivityTime))
            .FirstOrDefaultAsync(k => k.Id == kidId);
    }

    public async Task<IEnumerable<Kid>> SearchKidsAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return await GetAllAsync();
        }

        var kw = keyword.Trim().ToLower();
        return await _dbSet
            .AsNoTracking()
            .Include(k => k.Parent)
            .Where(k => (k.FullName != null && k.FullName.ToLower().Contains(kw)) ||
                        (k.Nickname != null && k.Nickname.ToLower().Contains(kw)) ||
                        (k.SchoolName != null && k.SchoolName.ToLower().Contains(kw)))
            .OrderByDescending(k => k.Id)
            .ToListAsync();
    }

    public async Task<bool> AddRewardPointsAsync(int kidId, int points)
    {
        var kid = await _dbSet.FindAsync(kidId);
        if (kid == null) return false;

        kid.TotalPoints += points;
        kid.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
