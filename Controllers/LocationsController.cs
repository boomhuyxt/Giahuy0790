using Giahuy0790.Data;
using Giahuy0790.Hubs;
using Giahuy0790.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Controllers;

[ApiController]
[Route("api/kids/{kidId:int}/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<LocationHub> _hubContext;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        AppDbContext context,
        IHubContext<LocationHub> hubContext,
        ILogger<LocationsController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Gửi tọa độ vị trí GPS hiện tại từ thiết bị của trẻ hoặc giả lập
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RecordLocation(int kidId, [FromBody] RecordLocationRequest request)
    {
        var kid = await _context.Kids
            .Include(k => k.Geofences)
            .FirstOrDefaultAsync(k => k.Id == kidId);

        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ trẻ ID: {kidId}" });
        }

        var location = new KidLocation
        {
            KidId = kidId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Accuracy = request.Accuracy,
            Speed = request.Speed,
            Altitude = request.Altitude,
            BatteryLevel = request.BatteryLevel,
            IsCharging = request.IsCharging,
            Address = request.Address,
            RecordedAt = request.RecordedAt ?? DateTime.UtcNow
        };

        _context.KidLocations.Add(location);
        await _context.SaveChangesAsync();

        // Kiểm tra vi phạm Vùng an toàn (Geofencing)
        var alerts = new List<GeofenceAlertDto>();
        foreach (var fence in kid.Geofences)
        {
            var distance = CalculateDistanceMeters(location.Latitude, location.Longitude, fence.Latitude, fence.Longitude);
            var isInside = distance <= fence.RadiusMeters;

            if (!isInside && fence.AlertOnExit)
            {
                alerts.Add(new GeofenceAlertDto(
                    fence.Id,
                    fence.Name,
                    "EXIT",
                    $"⚠️ Bé {kid.FullName} đã ra khỏi vùng an toàn \"{fence.Name}\" (Khoảng cách: {Math.Round(distance)}m, Bán kính: {fence.RadiusMeters}m)",
                    DateTime.UtcNow
                ));
            }
            else if (isInside && fence.AlertOnEnter)
            {
                alerts.Add(new GeofenceAlertDto(
                    fence.Id,
                    fence.Name,
                    "ENTER",
                    $"✅ Bé {kid.FullName} đã đến vùng an toàn \"{fence.Name}\"",
                    DateTime.UtcNow
                ));
            }
        }

        // Bắn tín hiệu Real-time qua SignalR tới phụ huynh đang mở ứng dụng / web
        var liveUpdate = new
        {
            kidId = location.KidId,
            kidName = kid.FullName,
            latitude = location.Latitude,
            longitude = location.Longitude,
            accuracy = location.Accuracy,
            speed = location.Speed,
            altitude = location.Altitude,
            batteryLevel = location.BatteryLevel,
            isCharging = location.IsCharging,
            address = location.Address,
            recordedAt = location.RecordedAt,
            alerts
        };

        await _hubContext.Clients.Group($"kid_{kidId}").SendAsync("ReceiveLocationUpdate", liveUpdate);

        return Ok(new
        {
            message = "Đã lưu tọa độ và phát sóng thời gian thực",
            data = liveUpdate
        });
    }

    /// <summary>
    /// Lấy vị trí mới nhất đã ghi nhận của trẻ
    /// </summary>
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestLocation(int kidId)
    {
        var latest = await _context.KidLocations
            .Where(l => l.KidId == kidId)
            .OrderByDescending(l => l.RecordedAt)
            .FirstOrDefaultAsync();

        if (latest == null)
        {
            return Ok(new { hasLocation = false, message = "Chưa có dữ liệu vị trí nào cho bé" });
        }

        return Ok(new
        {
            hasLocation = true,
            id = latest.Id,
            kidId = latest.KidId,
            latitude = latest.Latitude,
            longitude = latest.Longitude,
            accuracy = latest.Accuracy,
            speed = latest.Speed,
            altitude = latest.Altitude,
            batteryLevel = latest.BatteryLevel,
            isCharging = latest.IsCharging,
            address = latest.Address,
            recordedAt = latest.RecordedAt
        });
    }

    /// <summary>
    /// Lấy lịch sử lộ trình di chuyển của trẻ theo ngày (mặc định hôm nay)
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetLocationHistory(int kidId, [FromQuery] DateTime? date)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var nextDate = targetDate.AddDays(1);

        var points = await _context.KidLocations
            .Where(l => l.KidId == kidId && l.RecordedAt >= targetDate && l.RecordedAt < nextDate)
            .OrderBy(l => l.RecordedAt)
            .Select(l => new
            {
                l.Id,
                l.Latitude,
                l.Longitude,
                l.Speed,
                l.Accuracy,
                l.BatteryLevel,
                l.Address,
                l.RecordedAt
            })
            .ToListAsync();

        return Ok(new
        {
            kidId,
            date = targetDate.ToString("yyyy-MM-dd"),
            totalPoints = points.Count,
            points
        });
    }

    /// <summary>
    /// Lấy danh sách vùng an toàn của trẻ
    /// </summary>
    [HttpGet("geofences")]
    public async Task<IActionResult> GetGeofences(int kidId)
    {
        var fences = await _context.KidGeofences
            .Where(g => g.KidId == kidId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return Ok(fences);
    }

    /// <summary>
    /// Thiết lập vùng an toàn mới cho trẻ
    /// </summary>
    [HttpPost("geofences")]
    public async Task<IActionResult> CreateGeofence(int kidId, [FromBody] CreateGeofenceRequest request)
    {
        var kid = await _context.Kids.FindAsync(kidId);
        if (kid == null) return NotFound(new { message = "Không tìm thấy hồ sơ trẻ" });

        var fence = new KidGeofence
        {
            KidId = kidId,
            Name = request.Name.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMeters = request.RadiusMeters <= 0 ? 200 : request.RadiusMeters,
            AlertOnExit = request.AlertOnExit,
            AlertOnEnter = request.AlertOnEnter,
            CreatedAt = DateTime.UtcNow
        };

        _context.KidGeofences.Add(fence);
        await _context.SaveChangesAsync();

        return Created($"/api/kids/{kidId}/locations/geofences/{fence.Id}", fence);
    }

    /// <summary>
    /// Xóa vùng an toàn
    /// </summary>
    [HttpDelete("geofences/{geofenceId:int}")]
    public async Task<IActionResult> DeleteGeofence(int kidId, int geofenceId)
    {
        var fence = await _context.KidGeofences.FirstOrDefaultAsync(g => g.Id == geofenceId && g.KidId == kidId);
        if (fence == null) return NotFound(new { message = "Không tìm thấy vùng an toàn" });

        _context.KidGeofences.Remove(fence);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa vùng an toàn thành công" });
    }

    /// <summary>
    /// Chạy mô phỏng di chuyển GPS thực tế cho bé để kiểm thử tức thì
    /// </summary>
    [HttpPost("simulate")]
    public async Task<IActionResult> SimulateMovement(int kidId, [FromQuery] int step = 0)
    {
        // Danh sách tọa độ mô phỏng hành trình từ Trường học về Nhà (khu vực Hoàn Kiếm, Hà Nội)
        var route = new[]
        {
            new { Lat = 21.028511, Lng = 105.854167, Speed = 0.0, Battery = 95, Addr = "Trường Tiểu Học Kim Đồng, Hà Nội" },
            new { Lat = 21.029240, Lng = 105.853200, Speed = 15.2, Battery = 94, Addr = "Đoạn đường Tràng Thi, Hà Nội" },
            new { Lat = 21.030500, Lng = 105.851800, Speed = 22.5, Battery = 93, Addr = "Giao lộ Quang Trung - Tràng Thi" },
            new { Lat = 21.031400, Lng = 105.850900, Speed = 18.0, Battery = 92, Addr = "Đoạn đường Quán Sứ, Hà Nội" },
            new { Lat = 21.032100, Lng = 105.849900, Speed = 0.0, Battery = 92, Addr = "Nhà riêng, Phố Lý Thường Kiệt, Hà Nội" }
        };

        var currentStep = Math.Clamp(step, 0, route.Length - 1);
        var point = route[currentStep];

        return await RecordLocation(kidId, new RecordLocationRequest(
            point.Lat,
            point.Lng,
            10.0,
            point.Speed,
            15.0,
            point.Battery,
            false,
            point.Addr,
            DateTime.UtcNow
        ));
    }

    /// <summary>
    /// Công thức Haversine tính khoảng cách đại vòng cung giữa 2 tọa độ GPS (mét)
    /// </summary>
    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371e3; // Bán kính trái đất xấp xỉ 6,371 km
        var phi1 = lat1 * Math.PI / 180;
        var phi2 = lat2 * Math.PI / 180;
        var deltaPhi = (lat2 - lat1) * Math.PI / 180;
        var deltaLambda = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}

public record RecordLocationRequest(
    double Latitude,
    double Longitude,
    double? Accuracy = null,
    double? Speed = null,
    double? Altitude = null,
    int? BatteryLevel = null,
    bool? IsCharging = null,
    string? Address = null,
    DateTime? RecordedAt = null
);

public record CreateGeofenceRequest(
    string Name,
    double Latitude,
    double Longitude,
    double RadiusMeters = 200,
    bool AlertOnExit = true,
    bool AlertOnEnter = false
);

public record GeofenceAlertDto(
    int GeofenceId,
    string GeofenceName,
    string Type,
    string Message,
    DateTime Timestamp
);
