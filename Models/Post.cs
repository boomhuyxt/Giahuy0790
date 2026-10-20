using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Giahuy0790.Models;

/// <summary>
/// Model Bài đăng (Post - tương tự bài đăng Facebook)
/// </summary>
[Table("posts")]
public class Post
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("author_name")]
    [MaxLength(100)]
    public string? AuthorName { get; set; } = "Gia Huy";

    [Column("author_avatar")]
    public string? AuthorAvatar { get; set; }

    [Required(ErrorMessage = "Nội dung bài viết không được để trống")]
    [Column("content")]
    public string? Content { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("likes_count")]
    public int LikesCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
