using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Giahuy0790.Models;

/// <summary>
/// Thực thể Quy tắc chặn Trang web hoặc Ứng dụng dành cho Trẻ em (Parental Control Blocklist)
/// Giúp phụ huynh quản lý danh sách cấm truy cập web độc hại, cờ bạc, 18+ và hạn chế dùng/tải ứng dụng, game gây nghiện
/// </summary>
[Table("blocked_items")]
public class BlockedItem
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

    /// <summary>
    /// Loại mục chặn: "Website" (Trang web) hoặc "App" (Ứng dụng)
    /// </summary>
    [Column("item_type")]
    [MaxLength(20)]
    [Required]
    public string ItemType { get; set; } = "Website"; // "Website" | "App"

    /// <summary>
    /// Mục tiêu chặn: Domain web (vd: tiktok.com, youtube.com, b52.club) hoặc Tên ứng dụng / Gói App (vd: Roblox, Free Fire, com.zhiliaoapp.musically)
    /// </summary>
    [Column("target")]
    [MaxLength(255)]
    [Required]
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Tên gợi nhớ hiển thị (vd: "Mạng xã hội TikTok", "Game Roblox", "Trang web cá cược")
    /// </summary>
    [Column("title")]
    [MaxLength(150)]
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Danh mục thể loại: "Mạng xã hội", "Trò chơi / Game", "Cờ bạc & Cá cược", "Nội dung độc hại / 18+", "Khác"
    /// </summary>
    [Column("category")]
    [MaxLength(50)]
    public string? Category { get; set; } = "Khác";

    /// <summary>
    /// Lý do chặn (Ghi chú của phụ huynh)
    /// </summary>
    [Column("reason")]
    [MaxLength(255)]
    public string? Reason { get; set; }

    /// <summary>
    /// Trạng thái kích hoạt chặn: true = Đang chặn, false = Tạm thời mở/cho phép
    /// </summary>
    [Column("is_blocked")]
    public bool IsBlocked { get; set; } = true;

    /// <summary>
    /// ID của phụ huynh tạo quy tắc này
    /// </summary>
    [Column("created_by_user_id")]
    public string? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    [JsonIgnore]
    public ApplicationUser? CreatedByUser { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
