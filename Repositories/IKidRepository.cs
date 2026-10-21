using Giahuy0790.Models;

namespace Giahuy0790.Repositories;

/// <summary>
/// Interface Repository quản lý thông tin Trẻ em (Kid) và liên kết tài khoản con
/// </summary>
public interface IKidRepository : IRepository<Kid>
{
    Task<IEnumerable<Kid>> GetKidsByParentAsync(string parentId);
    Task<IEnumerable<Kid>> GetKidsByKidUserAsync(string kidUserId);
    Task<Kid?> GetKidWithActivitiesAsync(int kidId);
    Task<Kid?> GetKidWithDetailsAsync(int kidId);
    Task<Kid?> FindByKidUserIdAsync(string kidUserId);
    Task<IEnumerable<Kid>> SearchKidsAsync(string keyword, string? parentId = null);
    Task<bool> AddRewardPointsAsync(int kidId, int points);
}
