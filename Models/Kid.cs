using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Giahuy0790.Models;

/// <summary>
/// Thực thể Trẻ em (Kid) trong hệ thống quản lý trẻ
/// </summary>
[Table("kids")]
public class Kid
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("full_name")]
    [MaxLength(150)]
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Column("nickname")]
    [MaxLength(50)]
    public string? Nickname { get; set; } // Tên gọi thân mật ở nhà (ví dụ: Bé Bơ, Tin Tin...)

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [Column("gender")]
    [MaxLength(10)]
    public string? Gender { get; set; } // Nam, Nữ, Khác

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("school_name")]
    [MaxLength(200)]
    public string? SchoolName { get; set; } // Trường học / Lớp học

    [Column("health_notes")]
    public string? HealthNotes { get; set; } // Ghi chú sức khỏe: Dị ứng thực phẩm, tiền sử bệnh, nhóm máu

    [Column("interests")]
    public string? Interests { get; set; } // Sở thích / Năng khiếu

    // Liên kết với tài khoản Phụ huynh (Parent)
    [Column("parent_id")]
    public string? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public ApplicationUser? Parent { get; set; }

    // Liên kết với tài khoản riêng của Trẻ (nếu trẻ tự đăng nhập bằng role Kid)
    [Column("kid_user_id")]
    public string? KidUserId { get; set; }

    [ForeignKey(nameof(KidUserId))]
    public ApplicationUser? KidUser { get; set; }

    [Column("total_points")]
    public int TotalPoints { get; set; } = 0; // Điểm tích lũy / Số sao khen thưởng khi hoàn thành nhiệm vụ

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Danh sách lịch sinh hoạt và hoạt động của trẻ
    public ICollection<KidActivity> Activities { get; set; } = new List<KidActivity>();
}
