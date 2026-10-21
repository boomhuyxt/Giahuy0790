using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Giahuy0790.Models;

/// <summary>
/// Mã QR tạm thời (hiệu lực đúng 1 phút) để bé hoặc thành viên quét tham gia nhóm gia đình
/// </summary>
[Table("family_qr_invites")]
public class FamilyQrInvite
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

    [Column("created_by_user_id")]
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(CreatedByUserId))]
    [JsonIgnore]
    public ApplicationUser? CreatedByUser { get; set; }

    /// <summary>
    /// Mã token ngẫu nhiên bảo mật dùng để quét QR
    /// </summary>
    [Column("invite_token")]
    [MaxLength(64)]
    [Required]
    public string InviteToken { get; set; } = string.Empty;

    /// <summary>
    /// Mã PIN 6 số ngắn gọn (dành cho trường hợp bé nhập tay nếu camera lỗi)
    /// </summary>
    [Column("short_code")]
    [MaxLength(10)]
    [Required]
    public string ShortCode { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm tạo
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời điểm hết hạn (chính xác 60 giây sau khi tạo)
    /// </summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Đã được sử dụng để gia nhập hay chưa
    /// </summary>
    [Column("is_used")]
    public bool IsUsed { get; set; } = false;

    [Column("used_by_user_id")]
    public string? UsedByUserId { get; set; }

    [ForeignKey(nameof(UsedByUserId))]
    [JsonIgnore]
    public ApplicationUser? UsedByUser { get; set; }

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    [NotMapped]
    public int RemainingSeconds => Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds);
}
