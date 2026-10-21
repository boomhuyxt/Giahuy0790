using Giahuy0790.Data;
using Giahuy0790.Models;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Repositories;

/// <summary>
/// Triển khai Repository cho Trẻ em (Kid) và quản lý liên kết tài khoản con
/// </summary>
public class KidRepository(AppDbContext context) : Repository<Kid>(context), IKidRepository
{
    public override async Task<IEnumerable<Kid>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(k => k.Parent)
            .Include(k => k.KidUser)
            .Include(k => k.Activities)
            .OrderByDescending(k => k.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Kid>> GetKidsByParentAsync(string parentId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(k => k.ParentId == parentId)
            .Include(k => k.Parent)
            .Include(k => k.KidUser)
            .Include(k => k.Activities)
            .OrderByDescending(k => k.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Kid>> GetKidsByKidUserAsync(string kidUserId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(k => k.KidUserId == kidUserId)
            .Include(k => k.Parent)
            .Include(k => k.KidUser)
            .Include(k => k.Activities)
            .OrderByDescending(k => k.Id)
            .ToListAsync();
    }

    public async Task<Kid?> GetKidWithActivitiesAsync(int kidId)
    {
        return await _dbSet
            .Include(k => k.Parent)
            .Include(k => k.KidUser)
            .Include(k => k.Activities.OrderByDescending(a => a.ActivityTime))
            .FirstOrDefaultAsync(k => k.Id == kidId);
    }

    public async Task<Kid?> GetKidWithDetailsAsync(int kidId)
    {
        return await _dbSet
            .Include(k => k.Parent)
            .Include(k => k.KidUser)
            .FirstOrDefaultAsync(k => k.Id == kidId);
    }

    public async Task<Kid?> FindByKidUserIdAsync(string kidUserId)
    {
        return await _dbSet
            .Include(k => k.Parent)
            .Include(k => k.KidUser)
            .FirstOrDefaultAsync(k => k.KidUserId == kidUserId);
    }

    public async Task<IEnumerable<Kid>> SearchKidsAsync(string keyword, string? parentId = null)
    {
        var query = _dbSet.AsNoTracking()
            .Include(k => k.Parent)
            .Include(k => k.KidUser)
            .Include(k => k.Activities)
            .AsQueryable();

        if (!string.IsNullOrEmpty(parentId))
        {
            query = query.Where(k => k.ParentId == parentId);
        }

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return await query.OrderByDescending(k => k.Id).ToListAsync();
        }

        var kw = keyword.Trim().ToLower();
        return await query
            .Where(k => (k.FullName != null && k.FullName.ToLower().Contains(kw)) ||
                        (k.Nickname != null && k.Nickname.ToLower().Contains(kw)) ||
                        (k.SchoolName != null && k.SchoolName.ToLower().Contains(kw)) ||
                        (k.KidUser != null && k.KidUser.UserName != null && k.KidUser.UserName.ToLower().Contains(kw)) ||
                        (k.KidUser != null && k.KidUser.Email != null && k.KidUser.Email.ToLower().Contains(kw)))
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
