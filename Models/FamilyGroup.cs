using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Giahuy0790.Models;

/// <summary>
/// Thực thể Nhóm gia đình (Family Group) kết nối các phụ huynh và trẻ em cùng gia đình
/// </summary>
[Table("family_groups")]
public class FamilyGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [MaxLength(150)]
    [Required]
    public string Name { get; set; } = "Gia đình yêu thương";

    [Column("description")]
    public string? Description { get; set; }

    // Người tạo / Trưởng nhóm gia đình
    [Column("owner_id")]
    [Required]
    public string OwnerId { get; set; } = string.Empty;

    [ForeignKey(nameof(OwnerId))]
    [JsonIgnore]
    public ApplicationUser? Owner { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Danh sách thành viên trong nhóm gia đình (Bố, Mẹ, Con, Người giám hộ)
    public ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();

    // Danh sách các bé thuộc nhóm gia đình
    public ICollection<Kid> Kids { get; set; } = new List<Kid>();
}
