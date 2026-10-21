using Giahuy0790.Data;
using Giahuy0790.Models;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Repositories;

/// <summary>
/// Triển khai Repository cho Sản phẩm (Product) kế thừa từ Generic Repository
/// </summary>
public class ProductRepository(AppDbContext context) : Repository<Product>(context), IProductRepository
{
    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return await GetAllAsync();
        }

        var keyword = name.Trim().ToLower();
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.Name != null && p.Name.ToLower().Contains(keyword))
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetRecentProductsAsync(int count = 50)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .Take(count)
            .ToListAsync();
    }
}
