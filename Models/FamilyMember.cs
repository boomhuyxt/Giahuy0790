using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Giahuy0790.Models;

/// <summary>
/// Thực thể Thành viên trong Nhóm gia đình
/// </summary>
[Table("family_members")]
public class FamilyMember
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("family_group_id")]
    [Required]
    public int FamilyGroupId { get; set; }

    [ForeignKey(nameof(FamilyGroupId))]
    [JsonIgnore]
    public FamilyGroup? FamilyGroup { get; set; }

    [Column("user_id")]
    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Vai trò trong nhóm gia đình:
    /// "Owner" (Chủ gia đình), "Parent" (Phụ huynh đồng quản lý), "Guardian" (Người giám hộ), "Child" (Con)
    /// </summary>
    [Column("role_in_family")]
    [MaxLength(50)]
    public string RoleInFamily { get; set; } = "Parent";

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Các thuộc tính tiện ích cho JSON response
    [NotMapped]
    public string? UserName => User?.UserName;

    [NotMapped]
    public string? Email => User?.Email;

    [NotMapped]
    public string? FullName => User?.FullName ?? User?.UserName;

    [NotMapped]
    public string? AvatarUrl => User?.AvatarUrl;
}
