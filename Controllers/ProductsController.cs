using Giahuy0790.Models;
using Giahuy0790.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Giahuy0790.Controllers;

/// <summary>
/// API Quản lý Sản phẩm (Products): Thêm / Xóa / Sửa / Tìm theo ID / Tìm kiếm theo tên
/// Áp dụng Repository Pattern qua IProductRepository
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductRepository repository) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách tất cả sản phẩm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await repository.GetAllAsync();
        return Ok(products);
    }

    /// <summary>
    /// Tìm sản phẩm theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await repository.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Không tìm thấy sản phẩm với ID = {id}" });
        }

        return Ok(product);
    }

    /// <summary>
    /// Tìm kiếm sản phẩm theo từ khóa tên
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string? name)
    {
        var results = await repository.SearchByNameAsync(name ?? string.Empty);
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
            Name = dto.Name.Trim(),
            Price = dto.Price,
            Image = dto.Image?.Trim(),
            Description = dto.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var created = await repository.AddAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Cập nhật / Sửa thông tin sản phẩm
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
    {
        var product = await repository.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Không tìm thấy sản phẩm với ID = {id}" });
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Tên sản phẩm không được để trống" });
        }

        product.Name = dto.Name.Trim();
        product.Price = dto.Price;
        product.Image = dto.Image?.Trim();
        product.Description = dto.Description?.Trim();
        product.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(product);

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
        var deleted = await repository.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Không tìm thấy sản phẩm với ID = {id}" });
        }

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
