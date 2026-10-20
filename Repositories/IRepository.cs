using System.Linq.Expressions;

namespace Giahuy0790.Repositories;

/// <summary>
/// Interface Generic Repository cung cấp các thao tác CRUD cơ bản
/// </summary>
/// <typeparam name="T">Kiểu thực thể (Entity)</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Lấy toàn bộ danh sách thực thể (chỉ đọc AsNoTracking)
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Lấy thực thể theo khóa chính Id
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Tìm kiếm thực thể theo biểu thức điều kiện (Predicate)
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Thêm mới một thực thể vào Database
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Cập nhật thông tin thực thể
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Xóa thực thể theo Id
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Kiểm tra thực thể có tồn tại theo Id không
    /// </summary>
    Task<bool> ExistsAsync(int id);
}
