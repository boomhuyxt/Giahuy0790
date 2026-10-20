using Giahuy0790.Data;
using Giahuy0790.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Controllers;

/// <summary>
/// API Quản lý Sản phẩm (Products): Thêm / Xóa / Sửa / Tìm theo ID / Tìm kiếm theo tên
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách tất cả sản phẩm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await db.Products
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return Ok(products);
    }

    /// <summary>
    /// Tìm sản phẩm theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Không tìm thấy sản phẩm với ID = {id}" });
        }

        return Ok(product);
    }

    /// <summary>
    /// Tìm kiếm sản phẩm theo tên
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return await GetAll();
        }

        var keyword = name.Trim().ToLower();
        var results = await db.Products
            .AsNoTracking()
            .Where(p => p.Name != null && p.Name.ToLower().Contains(keyword))
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Thêm sản phẩm mới
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Tên sản phẩm không được để trống" });
        }

        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Image = dto.Image,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// Cập nhật / Sửa thông tin sản phẩm
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Không tìm thấy sản phẩm với ID = {id}" });
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Tên sản phẩm không được để trống" });
        }

        product.Name = dto.Name;
        product.Price = dto.Price;
        product.Image = dto.Image;
        product.Description = dto.Description;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật sản phẩm thành công",
            product
        });
    }

    /// <summary>
    /// Xóa sản phẩm theo ID
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Không tìm thấy sản phẩm với ID = {id}" });
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa sản phẩm ID = {id} thành công" });
    }
}

/// <summary>
/// DTO truyền dữ liệu khi Thêm / Sửa Sản phẩm
/// </summary>
public record ProductDto(
    string? Name,
    decimal Price,
    string? Image,
    string? Description
);
