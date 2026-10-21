using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Giahuy0790.Models;

/// <summary>
/// Vùng an toàn (Geofence) cho trẻ: Nhà riêng, Trường học, Nhà ông bà, Công viên...
/// </summary>
[Table("kid_geofences")]
public class KidGeofence
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

    [Column("name")]
    [MaxLength(100)]
    [Required]
    public string Name { get; set; } = string.Empty; // Tên vùng: Trường học, Nhà...

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("radius_meters")]
    public double RadiusMeters { get; set; } = 200; // Bán kính an toàn (mét), mặc định 200m

    [Column("alert_on_exit")]
    public bool AlertOnExit { get; set; } = true; // Cảnh báo khi bé rời khỏi vùng này

    [Column("alert_on_enter")]
    public bool AlertOnEnter { get; set; } = false; // Cảnh báo khi bé đến vùng này

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
