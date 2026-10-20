using Giahuy0790.Models;

namespace Giahuy0790.Repositories;

/// <summary>
/// Interface Repository dành riêng cho đối tượng Sản phẩm (Product)
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Tìm kiếm danh sách sản phẩm theo từ khóa tên
    /// </summary>
    Task<IEnumerable<Product>> SearchByNameAsync(string name);

    /// <summary>
    /// Lấy danh sách sản phẩm mới nhất
    /// </summary>
    Task<IEnumerable<Product>> GetRecentProductsAsync(int count = 50);
}
