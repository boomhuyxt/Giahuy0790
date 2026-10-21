using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
    [JsonIgnore]
    public ApplicationUser? Parent { get; set; }

    // Liên kết với tài khoản riêng của Trẻ (nếu trẻ tự đăng nhập bằng role Kid)
    [Column("kid_user_id")]
    public string? KidUserId { get; set; }

    [ForeignKey(nameof(KidUserId))]
    [JsonIgnore]
    public ApplicationUser? KidUser { get; set; }

    // Liên kết với Nhóm gia đình (Family Group)
    [Column("family_group_id")]
    public int? FamilyGroupId { get; set; }

    [ForeignKey(nameof(FamilyGroupId))]
    [JsonIgnore]
    public FamilyGroup? FamilyGroup { get; set; }

    [Column("total_points")]
    public int TotalPoints { get; set; } = 0; // Điểm tích lũy / Số sao khen thưởng khi hoàn thành nhiệm vụ

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Danh sách lịch sinh hoạt và hoạt động của trẻ
    public ICollection<KidActivity> Activities { get; set; } = new List<KidActivity>();

    // Danh sách các mục Web và App bị chặn của trẻ (Parental Control)
    public ICollection<BlockedItem> BlockedItems { get; set; } = new List<BlockedItem>();

    // Cấu hình Lịch trình & Giới hạn thời gian sử dụng thiết bị của trẻ (Screen Time)
    public KidScreenTime? ScreenTime { get; set; }

    // Lịch sử tọa độ GPS của trẻ (Location Tracking)
    public ICollection<KidLocation> Locations { get; set; } = new List<KidLocation>();

    // Danh sách các vùng an toàn (Geofences)
    public ICollection<KidGeofence> Geofences { get; set; } = new List<KidGeofence>();

    // ==========================================
    // Các trường trợ giúp Serialized cho API hiển thị Tài khoản con & Phụ huynh an toàn
    // ==========================================
    [NotMapped]
    public int BlockedItemsCount => BlockedItems?.Count(b => b.IsBlocked) ?? 0;

    [NotMapped]
    public string? ParentName => Parent?.FullName ?? Parent?.UserName;

    [NotMapped]
    public string? ParentEmail => Parent?.Email;

    [NotMapped]
    public bool HasKidAccount => !string.IsNullOrEmpty(KidUserId);

    [NotMapped]
    public string? KidUserName => KidUser?.UserName;

    [NotMapped]
    public string? KidUserEmail => KidUser?.Email;

    [NotMapped]
    public string? KidUserFullName => KidUser?.FullName;

    [NotMapped]
    public string? KidUserAvatar => KidUser?.AvatarUrl;

    [NotMapped]
    public object? KidAccount => KidUser == null ? null : new
    {
        id = KidUser.Id,
        username = KidUser.UserName,
        email = KidUser.Email,
        fullName = KidUser.FullName,
        avatarUrl = KidUser.AvatarUrl
    };

    [NotMapped]
    public object? ParentAccount => Parent == null ? null : new
    {
        id = Parent.Id,
        username = Parent.UserName,
        email = Parent.Email,
        fullName = Parent.FullName,
        avatarUrl = Parent.AvatarUrl
    };
}
