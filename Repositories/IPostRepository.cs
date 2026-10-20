using Giahuy0790.Models;

namespace Giahuy0790.Repositories;

/// <summary>
/// Interface Repository dành riêng cho đối tượng Bài đăng Facebook (Post)
/// </summary>
public interface IPostRepository : IRepository<Post>
{
    /// <summary>
    /// Lấy danh sách bài đăng hiển thị trên bảng tin (sắp xếp giảm dần theo thời gian tạo)
    /// </summary>
    Task<IEnumerable<Post>> GetFeedAsync();

    /// <summary>
    /// Tăng lượt thích cho bài đăng và trả về tổng lượt thích mới
    /// </summary>
    Task<int?> LikePostAsync(int id);
}
