using Giahuy0790.Models;
using Giahuy0790.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Giahuy0790.Controllers;

/// <summary>
/// API Bài đăng (tương tự bài đăng Facebook): Thêm / Xóa / Sửa / Lấy danh sách / Thả tim
/// Áp dụng Repository Pattern qua IPostRepository
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PostsController(IPostRepository repository) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách tất cả bài đăng (mới nhất lên đầu)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var posts = await repository.GetFeedAsync();
        return Ok(posts);
    }

    /// <summary>
    /// Lấy chi tiết bài đăng theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await repository.GetByIdAsync(id);
        if (post == null)
        {
            return NotFound(new { message = $"Không tìm thấy bài đăng với ID = {id}" });
        }

        return Ok(post);
    }

    /// <summary>
    /// Thêm bài đăng mới (Chức năng Thêm: 1đ)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest(new { message = "Nội dung bài viết không được để trống" });
        }

        var post = new Post
        {
            AuthorName = string.IsNullOrWhiteSpace(dto.AuthorName) ? "Gia Huy" : dto.AuthorName.Trim(),
            AuthorAvatar = string.IsNullOrWhiteSpace(dto.AuthorAvatar) 
                ? "https://api.dicebear.com/7.x/bottts/svg?seed=GiaHuy" 
                : dto.AuthorAvatar.Trim(),
            Content = dto.Content.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
            LikesCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        var created = await repository.AddAsync(post);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Sửa thông tin bài đăng (Chức năng Sửa: 1đ)
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostDto dto)
    {
        var post = await repository.GetByIdAsync(id);
        if (post == null)
        {
            return NotFound(new { message = $"Không tìm thấy bài đăng với ID = {id}" });
        }

        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest(new { message = "Nội dung bài viết không được để trống" });
        }

        post.Content = dto.Content.Trim();
        post.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        if (!string.IsNullOrWhiteSpace(dto.AuthorName))
        {
            post.AuthorName = dto.AuthorName.Trim();
        }
        post.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(post);

        return Ok(new
        {
            message = "Cập nhật bài viết thành công",
            post
        });
    }

    /// <summary>
    /// Xóa bài đăng theo ID (Chức năng Xóa: 1đ)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await repository.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Không tìm thấy bài đăng với ID = {id}" });
        }

        return Ok(new { message = $"Đã xóa bài đăng ID = {id} thành công" });
    }

    /// <summary>
    /// Tương tác Thích (Like) bài đăng
    /// </summary>
    [HttpPost("{id:int}/like")]
    public async Task<IActionResult> LikePost(int id)
    {
        var newLikesCount = await repository.LikePostAsync(id);
        if (newLikesCount == null)
        {
            return NotFound(new { message = $"Không tìm thấy bài đăng với ID = {id}" });
        }

        return Ok(new
        {
            message = "Đã thích bài viết",
            likesCount = newLikesCount.Value
        });
    }
}

public record CreatePostDto(
    string? AuthorName,
    string? AuthorAvatar,
    string Content,
    string? ImageUrl
);

public record UpdatePostDto(
    string? AuthorName,
    string Content,
    string? ImageUrl
);
