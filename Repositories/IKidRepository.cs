using Giahuy0790.Models;

namespace Giahuy0790.Repositories;

/// <summary>
/// Interface Repository quản lý thông tin Trẻ em (Kid)
/// </summary>
public interface IKidRepository : IRepository<Kid>
{
    Task<IEnumerable<Kid>> GetKidsByParentAsync(string parentId);
    Task<Kid?> GetKidWithActivitiesAsync(int kidId);
    Task<IEnumerable<Kid>> SearchKidsAsync(string keyword);
    Task<bool> AddRewardPointsAsync(int kidId, int points);
}
