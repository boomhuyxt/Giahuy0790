using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Giahuy0790.Models;

/// <summary>
/// Model Sản phẩm (Product) theo yêu cầu bài tập
/// </summary>
[Table("products")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [MaxLength(255)]
    public string? Name { get; set; }

    [Column("price", TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column("image")]
    public string? Image { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    // Các thuộc tính bổ sung quản lý thời gian
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
