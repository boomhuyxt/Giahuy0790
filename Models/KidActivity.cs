using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Giahuy0790.Models;

/// <summary>
/// Nhật ký hoạt động, lịch sinh hoạt và nhiệm vụ rèn luyện của trẻ
/// </summary>
[Table("kid_activities")]
public class KidActivity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("kid_id")]
    [Required]
    public int KidId { get; set; }

    [ForeignKey(nameof(KidId))]
    [JsonIgnore]
    public Kid? Kid { get; set; }

    [Column("title")]
    [MaxLength(200)]
    [Required]
    public string Title { get; set; } = string.Empty; // Ví dụ: Học tiếng Anh, Ăn sáng đúng giờ, Uống sữa, Dọn phòng...

    [Column("description")]
    public string? Description { get; set; }

    [Column("category")]
    [MaxLength(50)]
    public string Category { get; set; } = "Sinh hoạt"; // Sinh hoạt, Học tập, Dinh dưỡng, Thể thao, Sức khỏe, Nhiệm vụ

    [Column("activity_time")]
    public DateTime ActivityTime { get; set; } = DateTime.UtcNow; // Thời gian diễn ra

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending (Chưa làm), Completed (Đã hoàn thành), Skipped (Bỏ qua)

    [Column("reward_points")]
    public int RewardPoints { get; set; } = 5; // Số điểm / Sao thưởng khi bé hoàn thành

    [Column("notes")]
    public string? Notes { get; set; } // Phụ huynh nhận xét hoặc đánh giá

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
