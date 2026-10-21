using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Giahuy0790.Models;

/// <summary>
/// Cấu hình Lịch trình & Giới hạn thời gian sử dụng thiết bị (Screen Time & Device Schedule) của Trẻ
/// </summary>
[Table("kid_screen_times")]
public class KidScreenTime
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

    [Column("device_name")]
    [MaxLength(100)]
    public string DeviceName { get; set; } = "Máy tính / Điện thoại của bé";

    // ==========================================
    // 1. GIỚI HẠN THỜI GIAN SỬ DỤNG MỖI NGÀY (DAILY QUOTA)
    // ==========================================
    [Column("daily_limit_minutes")]
    public int DailyLimitMinutes { get; set; } = 120; // Ngày thường (Thứ 2 - Thứ 6): mặc định 120 phút (2 giờ)

    [Column("weekend_limit_minutes")]
    public int WeekendLimitMinutes { get; set; } = 180; // Cuối tuần (Thứ 7 - CN): mặc định 180 phút (3 giờ)

    [Column("is_daily_limit_enabled")]
    public bool IsDailyLimitEnabled { get; set; } = true;

    // ==========================================
    // 2. GIỜ ĐI NGỦ / GIỚI NGHIÊM BAN ĐÊM (BEDTIME / DOWNTIME)
    // ==========================================
    [Column("is_bedtime_enabled")]
    public bool IsBedtimeEnabled { get; set; } = true;

    [Column("bedtime_start")]
    [MaxLength(10)]
    public string BedtimeStart { get; set; } = "21:30"; // Giờ bắt đầu khóa máy ban đêm (21h30)

    [Column("bedtime_end")]
    [MaxLength(10)]
    public string BedtimeEnd { get; set; } = "06:30"; // Giờ kết thúc khóa máy vào buổi sáng (06h30)

    // ==========================================
    // 3. KHUNG GIỜ ĐƯỢC PHÉP SỬ DỤNG TRONG NGÀY (ALLOWED TIME WINDOW)
    // ==========================================
    [Column("is_allowed_window_enabled")]
    public bool IsAllowedWindowEnabled { get; set; } = false; // Mặc định tắt, chỉ bật khi muốn giới hạn khung giờ nghiêm ngặt

    [Column("allowed_start_time")]
    [MaxLength(10)]
    public string AllowedStartTime { get; set; } = "07:00"; // Giờ sớm nhất được mở máy

    [Column("allowed_end_time")]
    [MaxLength(10)]
    public string AllowedEndTime { get; set; } = "21:00"; // Giờ muộn nhất trước khi tự động khóa

    // ==========================================
    // 4. KHÓA TỨC THÌ TỪ XA CỦA PHỤ HUYNH (REMOTE INSTANT LOCK)
    // ==========================================
    [Column("is_locked_now")]
    public bool IsLockedNow { get; set; } = false; // Phụ huynh bật = Khóa máy bé ngay lập tức

    [Column("lock_reason")]
    [MaxLength(255)]
    public string? LockReason { get; set; }

    [Column("locked_at")]
    public DateTime? LockedAt { get; set; }

    // ==========================================
    // 5. THƯỞNG THÊM GIỜ HÔM NAY (BONUS TIME TODAY)
    // ==========================================
    [Column("bonus_minutes_today")]
    public int BonusMinutesToday { get; set; } = 0; // Phụ huynh cộng thêm 15p, 30p, 60p

    [Column("bonus_date")]
    [MaxLength(20)]
    public string? BonusDate { get; set; } // Ngày áp dụng (YYYY-MM-DD)

    // ==========================================
    // 6. THEO DÕI THỜI LƯỢNG ĐÃ DÙNG THỰC TẾ (USAGE TRACKING)
    // ==========================================
    [Column("used_minutes_today")]
    public int UsedMinutesToday { get; set; } = 0; // Số phút máy bé đã chạy hôm nay

    [Column("last_usage_date")]
    [MaxLength(20)]
    public string? LastUsageDate { get; set; } // Ngày gần nhất ghi nhận (YYYY-MM-DD), reset về 0 khi qua ngày mới

    [Column("last_heartbeat_at")]
    public DateTime? LastHeartbeatAt { get; set; } // Thời điểm thiết bị bé ping gần nhất

    // ==========================================
    // 7. THÔNG ĐIỆP HIỂN THỊ TRÊN MÀN HÌNH KHÓA CỦA BÉ
    // ==========================================
    [Column("lock_message")]
    [MaxLength(500)]
    public string? LockMessage { get; set; } = "Đã hết giờ sử dụng máy hôm nay hoặc đang trong giờ nghỉ ngơi. Bé hãy nghỉ mắt và vận động nhé!";

    [Column("schedule_json")]
    public string? ScheduleJson { get; set; } // Cấu hình chi tiết Thứ 2 -> CN nếu muốn

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // ==========================================
    // CÁC THUỘC TÍNH TÍNH TOÁN REALTIME (NOT MAPPED)
    // ==========================================
    [NotMapped]
    public bool IsOnline => LastHeartbeatAt.HasValue && LastHeartbeatAt.Value > DateTime.UtcNow.AddMinutes(-3);
}
