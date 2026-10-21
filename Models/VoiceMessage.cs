using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Giahuy0790.Models;

/// <summary>
/// Tin nhắn giọng nói / Bộ đàm đàm thoại trực tiếp giữa Phụ huynh và Bé (Voice Message / Family Walkie-Talkie)
/// </summary>
[Table("voice_messages")]
public class VoiceMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    // Id của người gửi (ApplicationUser.Id)
    [Column("sender_id")]
    [Required]
    [MaxLength(100)]
    public string SenderId { get; set; } = string.Empty;

    // Tên hiển thị người gửi (ví dụ: "Bố Gia Huy", "Mẹ", "Bé Bắp")
    [Column("sender_name")]
    [Required]
    [MaxLength(100)]
    public string SenderName { get; set; } = string.Empty;

    // Vai trò người gửi: "Parents" hoặc "Kid"
    [Column("sender_role")]
    [Required]
    [MaxLength(20)]
    public string SenderRole { get; set; } = "Parents";

    // Ảnh đại diện người gửi
    [Column("sender_avatar_url")]
    [MaxLength(500)]
    public string? SenderAvatarUrl { get; set; }

    // Bé liên quan đến cuộc trò chuyện giọng nói này
    [Column("kid_id")]
    [Required]
    public int KidId { get; set; }

    [ForeignKey(nameof(KidId))]
    [JsonIgnore]
    public Kid? Kid { get; set; }

    // Phụ huynh nhận/gửi
    [Column("parent_id")]
    [MaxLength(100)]
    public string? ParentId { get; set; }

    // Nhóm gia đình
    [Column("family_group_id")]
    public int? FamilyGroupId { get; set; }

    // Đường dẫn file âm thanh giọng nói (ví dụ: /uploads/voice/xxx.webm)
    [Column("audio_url")]
    [Required]
    [MaxLength(500)]
    public string AudioUrl { get; set; } = string.Empty;

    // Thời lượng file ghi âm (tính bằng giây, ví dụ: 5s, 18s)
    [Column("duration_seconds")]
    public int DurationSeconds { get; set; } = 0;

    // Dung lượng file (bytes)
    [Column("file_size_bytes")]
    public long FileSizeBytes { get; set; } = 0;

    // Trạng thái đã nghe / chưa nghe
    [Column("is_heard")]
    public bool IsHeard { get; set; } = false;

    // Thời điểm người nhận bấm nghe
    [Column("heard_at")]
    public DateTime? HeardAt { get; set; }

    // Thời điểm ghi âm và gửi
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
