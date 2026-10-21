using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Giahuy0790.Models;

/// <summary>
/// Lưu trữ lịch sử tọa độ GPS của trẻ (Live Location & Route History)
/// </summary>
[Table("kid_locations")]
public class KidLocation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("kid_id")]
    [Required]
    public int KidId { get; set; }

    [ForeignKey(nameof(KidId))]
    [JsonIgnore]
    public Kid? Kid { get; set; }

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("accuracy")]
    public double? Accuracy { get; set; } // Sai số định vị GPS (mét)

    [Column("speed")]
    public double? Speed { get; set; } // Tốc độ di chuyển (km/h)

    [Column("altitude")]
    public double? Altitude { get; set; } // Độ cao so với mực nước biển (mét)

    [Column("battery_level")]
    public int? BatteryLevel { get; set; } // Phần trăm pin của thiết bị trẻ (0-100)

    [Column("is_charging")]
    public bool? IsCharging { get; set; } // Thiết bị có đang cắm sạc không

    [Column("address")]
    [MaxLength(255)]
    public string? Address { get; set; } // Tên địa điểm hoặc tên đường

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
