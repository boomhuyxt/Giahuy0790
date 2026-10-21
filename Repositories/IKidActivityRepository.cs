using Giahuy0790.Models;

namespace Giahuy0790.Repositories;

/// <summary>
/// Interface Repository quản lý Hoạt động & Nhiệm vụ của trẻ (KidActivity)
/// </summary>
public interface IKidActivityRepository : IRepository<KidActivity>
{
    Task<IEnumerable<KidActivity>> GetActivitiesByKidIdAsync(int kidId);
    Task<IEnumerable<KidActivity>> GetActivitiesByDateAsync(int kidId, DateTime date);
    Task<bool> UpdateStatusAsync(int activityId, string status);
}
