using Giahuy0790.Data;
using Giahuy0790.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Giahuy0790.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScreenTimeController(AppDbContext context, ILogger<ScreenTimeController> logger) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<ScreenTimeController> _logger = logger;

    // Giờ Việt Nam chuẩn (UTC+7)
    private static DateTime GetVietnamTime() => DateTime.UtcNow.AddHours(7);

    /// <summary>
    /// Lấy trạng thái thời gian thực sử dụng thiết bị cho tài khoản con (Kid) đang đăng nhập hiện tại
    /// </summary>
    [HttpGet("my-status")]
    public async Task<IActionResult> GetMyStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để kiểm tra trạng thái." });
        }

        var kid = await _context.Kids.FirstOrDefaultAsync(k => k.KidUserId == userId);
        if (kid == null)
        {
            return NotFound(new { message = "Tài khoản của bạn chưa được liên kết với hồ sơ trẻ nào." });
        }

        var screenTime = await GetOrInitScreenTimeAsync(kid.Id);
        var status = CalculateRealtimeStatus(screenTime);

        return Ok(new
        {
            kidId = kid.Id,
            fullName = kid.FullName,
            nickname = kid.Nickname,
            avatarUrl = kid.AvatarUrl,
            status,
            settings = screenTime
        });
    }

    /// <summary>
    /// Gửi tín hiệu Heartbeat từ phiên trình duyệt / thiết bị của tài khoản con đang đăng nhập
    /// Tự động cập nhật thời gian dùng và kiểm tra xem có bị khóa máy hay không
    /// </summary>
    [HttpPost("my-heartbeat")]
    public async Task<IActionResult> MyHeartbeat([FromBody] HeartbeatRequest? request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để gửi tín hiệu máy bé." });
        }

        var kid = await _context.Kids.FirstOrDefaultAsync(k => k.KidUserId == userId);
        if (kid == null)
        {
            return NotFound(new { message = "Tài khoản của bạn chưa được liên kết với hồ sơ trẻ nào." });
        }

        return await Heartbeat(kid.Id, request);
    }

    /// <summary>
    /// Lấy cấu hình và trạng thái thời gian thực sử dụng thiết bị của bé
    /// </summary>
    [HttpGet("{kidId:int}")]
    public async Task<IActionResult> GetScreenTime(int kidId)
    {
        var kid = await _context.Kids.FindAsync(kidId);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy bé có ID = {kidId}" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isParent = User.IsInRole("Parents") || User.IsInRole("parents");
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!string.IsNullOrEmpty(currentUserId) && !isAdmin)
        {
            var isKid = User.IsInRole("Kid") || User.IsInRole("kid");
            if (isKid && kid.KidUserId != currentUserId)
            {
                return Forbid();
            }

            if (isParent && kid.ParentId != null && kid.ParentId != currentUserId)
            {
                var inSameFamily = kid.FamilyGroupId != null && await _context.FamilyMembers.AnyAsync(fm => fm.UserId == currentUserId && fm.FamilyGroupId == kid.FamilyGroupId);
                if (!inSameFamily)
                {
                    return Forbid();
                }
            }
        }

        var screenTime = await GetOrInitScreenTimeAsync(kidId);
        var status = CalculateRealtimeStatus(screenTime);

        return Ok(new
        {
            kid = new
            {
                id = kid.Id,
                fullName = kid.FullName,
                nickname = kid.Nickname,
                avatarUrl = kid.AvatarUrl
            },
            settings = screenTime,
            status
        });
    }

    /// <summary>
    /// Cập nhật cài đặt lịch & giờ giới hạn dùng máy
    /// </summary>
    [HttpPut("{kidId:int}")]
    public async Task<IActionResult> UpdateSettings(int kidId, [FromBody] UpdateScreenTimeRequest request)
    {
        var kid = await _context.Kids.FindAsync(kidId);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy bé có ID = {kidId}" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!string.IsNullOrEmpty(currentUserId) && !isAdmin)
        {
            if (kid.ParentId != null && kid.ParentId != currentUserId)
            {
                var inSameFamily = kid.FamilyGroupId != null && await _context.FamilyMembers.AnyAsync(fm => fm.UserId == currentUserId && fm.FamilyGroupId == kid.FamilyGroupId);
                if (!inSameFamily)
                {
                    return Forbid();
                }
            }
        }

        var screenTime = await GetOrInitScreenTimeAsync(kidId);

        screenTime.DeviceName = string.IsNullOrWhiteSpace(request.DeviceName) ? screenTime.DeviceName : request.DeviceName.Trim();
        screenTime.DailyLimitMinutes = Math.Max(0, request.DailyLimitMinutes);
        screenTime.WeekendLimitMinutes = Math.Max(0, request.WeekendLimitMinutes);
        screenTime.IsDailyLimitEnabled = request.IsDailyLimitEnabled;

        screenTime.IsBedtimeEnabled = request.IsBedtimeEnabled;
        if (!string.IsNullOrWhiteSpace(request.BedtimeStart)) screenTime.BedtimeStart = request.BedtimeStart.Trim();
        if (!string.IsNullOrWhiteSpace(request.BedtimeEnd)) screenTime.BedtimeEnd = request.BedtimeEnd.Trim();

        screenTime.IsAllowedWindowEnabled = request.IsAllowedWindowEnabled;
        if (!string.IsNullOrWhiteSpace(request.AllowedStartTime)) screenTime.AllowedStartTime = request.AllowedStartTime.Trim();
        if (!string.IsNullOrWhiteSpace(request.AllowedEndTime)) screenTime.AllowedEndTime = request.AllowedEndTime.Trim();

        if (!string.IsNullOrWhiteSpace(request.LockMessage)) screenTime.LockMessage = request.LockMessage.Trim();

        screenTime.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        var status = CalculateRealtimeStatus(screenTime);

        return Ok(new
        {
            message = "Đã lưu cài đặt lịch và giờ dùng máy thành công!",
            settings = screenTime,
            status
        });
    }

    /// <summary>
    /// Khóa tức thì hoặc mở khóa máy bé từ xa
    /// </summary>
    [HttpPost("{kidId:int}/quick-lock")]
    public async Task<IActionResult> QuickLock(int kidId, [FromBody] QuickLockRequest request)
    {
        var kid = await _context.Kids.FindAsync(kidId);
        if (kid == null)
        {
            return NotFound(new { message = $"Không tìm thấy bé có ID = {kidId}" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!string.IsNullOrEmpty(currentUserId) && !isAdmin)
        {
            if (kid.ParentId != null && kid.ParentId != currentUserId)
            {
                var inSameFamily = kid.FamilyGroupId != null && await _context.FamilyMembers.AnyAsync(fm => fm.UserId == currentUserId && fm.FamilyGroupId == kid.FamilyGroupId);
                if (!inSameFamily)
                {
                    return Forbid();
                }
            }
        }

        var screenTime = await GetOrInitScreenTimeAsync(kidId);

        screenTime.IsLockedNow = request.IsLocked;
        screenTime.LockReason = request.IsLocked
            ? (string.IsNullOrWhiteSpace(request.Reason) ? "Phụ huynh đã tạm khóa máy từ xa" : request.Reason.Trim())
            : null;
        screenTime.LockedAt = request.IsLocked ? DateTime.UtcNow : null;
        screenTime.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        var status = CalculateRealtimeStatus(screenTime);

        return Ok(new
        {
            message = request.IsLocked ? "Đã khóa máy của bé thành công!" : "Đã mở khóa máy của bé!",
            isLocked = screenTime.IsLockedNow,
            status
        });
    }

    /// <summary>
    /// Thưởng thêm phút sử dụng trong ngày (ví dụ +15p, +30p, +60p)
    /// </summary>
    [HttpPost("{kidId:int}/add-bonus")]
    public async Task<IActionResult> AddBonus(int kidId, [FromBody] AddBonusRequest request)
    {
        var screenTime = await GetOrInitScreenTimeAsync(kidId);
        var nowVn = GetVietnamTime();
        var todayStr = nowVn.ToString("yyyy-MM-dd");

        // Đảm bảo cùng ngày hôm nay
        if (screenTime.BonusDate != todayStr)
        {
            screenTime.BonusMinutesToday = 0;
            screenTime.BonusDate = todayStr;
        }

        screenTime.BonusMinutesToday += Math.Max(0, request.Minutes);
        screenTime.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        var status = CalculateRealtimeStatus(screenTime);

        return Ok(new
        {
            message = $"Đã cộng thêm {request.Minutes} phút sử dụng hôm nay cho bé!",
            bonusMinutesToday = screenTime.BonusMinutesToday,
            status
        });
    }

    /// <summary>
    /// Đặt lại thời gian sử dụng hôm nay (reset UsedMinutesToday = 0)
    /// </summary>
    [HttpPost("{kidId:int}/reset-usage")]
    public async Task<IActionResult> ResetUsage(int kidId)
    {
        var screenTime = await GetOrInitScreenTimeAsync(kidId);
        var nowVn = GetVietnamTime();
        var todayStr = nowVn.ToString("yyyy-MM-dd");

        screenTime.UsedMinutesToday = 0;
        screenTime.LastUsageDate = todayStr;
        screenTime.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        var status = CalculateRealtimeStatus(screenTime);

        return Ok(new
        {
            message = "Đã đặt lại thời gian sử dụng hôm nay về 0 phút!",
            status
        });
    }

    /// <summary>
    /// API Heartbeat gửi từ thiết bị máy bé (hoặc trình mô phỏng máy bé)
    /// Tự động cộng dồn thời gian sử dụng và trả về trạng thái khóa/mở ngay lập tức
    /// </summary>
    [HttpPost("{kidId:int}/heartbeat")]
    public async Task<IActionResult> Heartbeat(int kidId, [FromBody] HeartbeatRequest? request)
    {
        var screenTime = await GetOrInitScreenTimeAsync(kidId);
        var nowVn = GetVietnamTime();
        var todayStr = nowVn.ToString("yyyy-MM-dd");

        // Tự động reset sang ngày mới nếu qua 0h
        if (screenTime.LastUsageDate != todayStr)
        {
            screenTime.UsedMinutesToday = 0;
            screenTime.LastUsageDate = todayStr;
        }

        if (screenTime.BonusDate != todayStr)
        {
            screenTime.BonusMinutesToday = 0;
            screenTime.BonusDate = todayStr;
        }

        screenTime.LastHeartbeatAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request?.DeviceName))
        {
            screenTime.DeviceName = request.DeviceName.Trim();
        }

        // Tính toán trạng thái trước khi cộng phút
        var statusBefore = CalculateRealtimeStatus(screenTime);

        // Nếu máy không bị khóa thì cộng dồn số phút hoạt động
        int increment = request?.IncrementMinutes ?? 1;
        if (!statusBefore.IsLocked && increment > 0)
        {
            screenTime.UsedMinutesToday += increment;
        }

        screenTime.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var statusAfter = CalculateRealtimeStatus(screenTime);

        return Ok(new
        {
            isLocked = statusAfter.IsLocked,
            reason = statusAfter.LockReason,
            remainingMinutes = statusAfter.RemainingMinutesToday,
            usedMinutesToday = screenTime.UsedMinutesToday,
            effectiveLimitToday = statusAfter.EffectiveLimitToday,
            lockMessage = screenTime.LockMessage,
            serverTime = nowVn.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }

    /// <summary>
    /// Kiểm tra trạng thái nhanh cho máy của bé
    /// </summary>
    [HttpGet("{kidId:int}/status")]
    public async Task<IActionResult> GetStatus(int kidId)
    {
        var screenTime = await GetOrInitScreenTimeAsync(kidId);
        var status = CalculateRealtimeStatus(screenTime);
        return Ok(status);
    }

    /// <summary>
    /// Tổng hợp thông tin Screen Time của tất cả các bé
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var kids = await _context.Kids
            .Include(k => k.ScreenTime)
            .OrderBy(k => k.Id)
            .ToListAsync();

        var list = new List<object>();
        foreach (var k in kids)
        {
            var st = k.ScreenTime ?? await GetOrInitScreenTimeAsync(k.Id);
            var status = CalculateRealtimeStatus(st);
            list.Add(new
            {
                kidId = k.Id,
                fullName = k.FullName,
                nickname = k.Nickname,
                avatarUrl = k.AvatarUrl,
                deviceName = st.DeviceName,
                isLocked = status.IsLocked,
                lockReason = status.LockReason,
                usedMinutesToday = st.UsedMinutesToday,
                remainingMinutesToday = status.RemainingMinutesToday,
                effectiveLimitToday = status.EffectiveLimitToday,
                isOnline = st.IsOnline
            });
        }

        return Ok(list);
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ NỘI BỘ (HELPERS)
    // ==========================================

    private async Task<KidScreenTime> GetOrInitScreenTimeAsync(int kidId)
    {
        var record = await _context.KidScreenTimes.FirstOrDefaultAsync(s => s.KidId == kidId);
        if (record == null)
        {
            record = new KidScreenTime
            {
                KidId = kidId,
                DeviceName = "Máy tính / Điện thoại của bé",
                DailyLimitMinutes = 120,
                WeekendLimitMinutes = 180,
                IsDailyLimitEnabled = true,
                IsBedtimeEnabled = true,
                BedtimeStart = "21:30",
                BedtimeEnd = "06:30",
                IsAllowedWindowEnabled = false,
                AllowedStartTime = "07:00",
                AllowedEndTime = "21:00",
                IsLockedNow = false,
                UsedMinutesToday = 0,
                BonusMinutesToday = 0,
                LockMessage = "Đã hết giờ sử dụng máy hôm nay hoặc đang trong giờ nghỉ ngơi. Bé hãy nghỉ ngơi và đọc sách nhé!",
                CreatedAt = DateTime.UtcNow
            };
            _context.KidScreenTimes.Add(record);
            await _context.SaveChangesAsync();
        }
        return record;
    }

    private static ScreenTimeRealtimeStatus CalculateRealtimeStatus(KidScreenTime st)
    {
        var nowVn = GetVietnamTime();
        var todayStr = nowVn.ToString("yyyy-MM-dd");
        var nowTime = nowVn.TimeOfDay;

        bool isWeekend = nowVn.DayOfWeek == DayOfWeek.Saturday || nowVn.DayOfWeek == DayOfWeek.Sunday;
        int baseLimit = isWeekend ? st.WeekendLimitMinutes : st.DailyLimitMinutes;

        int bonus = (st.BonusDate == todayStr) ? st.BonusMinutesToday : 0;
        int used = (st.LastUsageDate == todayStr) ? st.UsedMinutesToday : 0;
        int effectiveLimit = baseLimit + bonus;
        int remaining = Math.Max(0, effectiveLimit - used);

        // 1. Kiểm tra Giờ đi ngủ (Bedtime / Downtime)
        bool isInBedtime = false;
        if (st.IsBedtimeEnabled && TimeSpan.TryParse(st.BedtimeStart, out var bedStart) && TimeSpan.TryParse(st.BedtimeEnd, out var bedEnd))
        {
            if (bedStart <= bedEnd)
            {
                isInBedtime = nowTime >= bedStart && nowTime < bedEnd;
            }
            else
            {
                // Khung giờ vắt qua nửa đêm (ví dụ: 21:30 -> 06:30)
                isInBedtime = nowTime >= bedStart || nowTime < bedEnd;
            }
        }

        // 2. Kiểm tra Khung giờ cho phép ban ngày (Allowed Window)
        bool isOutsideWindow = false;
        if (st.IsAllowedWindowEnabled && TimeSpan.TryParse(st.AllowedStartTime, out var winStart) && TimeSpan.TryParse(st.AllowedEndTime, out var winEnd))
        {
            if (winStart <= winEnd)
            {
                isOutsideWindow = nowTime < winStart || nowTime >= winEnd;
            }
            else
            {
                isOutsideWindow = nowTime < winStart && nowTime >= winEnd;
            }
        }

        // 3. Kiểm tra Giới hạn thời lượng hôm nay (Daily limit)
        bool isLimitReached = st.IsDailyLimitEnabled && (used >= effectiveLimit);

        // Tổng hợp trạng thái Khóa / Cho phép
        bool isLocked = false;
        string? reason = null;

        if (st.IsLockedNow)
        {
            isLocked = true;
            reason = string.IsNullOrWhiteSpace(st.LockReason) ? "Phụ huynh đã tạm khóa máy từ xa" : st.LockReason;
        }
        else if (isInBedtime)
        {
            isLocked = true;
            reason = $"Đang trong giờ giới nghiêm / đi ngủ ({st.BedtimeStart} - {st.BedtimeEnd})";
        }
        else if (isOutsideWindow)
        {
            isLocked = true;
            reason = $"Ngoài khung giờ được phép sử dụng máy ({st.AllowedStartTime} - {st.AllowedEndTime})";
        }
        else if (isLimitReached)
        {
            isLocked = true;
            reason = $"Bé đã dùng hết thời lượng {effectiveLimit} phút được phép hôm nay";
        }

        return new ScreenTimeRealtimeStatus
        {
            IsLocked = isLocked,
            LockReason = reason,
            UsedMinutesToday = used,
            RemainingMinutesToday = remaining,
            EffectiveLimitToday = effectiveLimit,
            BaseLimitToday = baseLimit,
            BonusMinutesToday = bonus,
            IsWeekend = isWeekend,
            IsInBedtime = isInBedtime,
            IsOutsideWindow = isOutsideWindow,
            IsLimitReached = isLimitReached,
            IsOnline = st.IsOnline,
            LastHeartbeatAt = st.LastHeartbeatAt,
            LockMessage = st.LockMessage,
            CurrentTimeVn = nowVn.ToString("HH:mm:ss dd/MM/yyyy")
        };
    }
}

public class UpdateScreenTimeRequest
{
    public string? DeviceName { get; set; }
    public int DailyLimitMinutes { get; set; } = 120;
    public int WeekendLimitMinutes { get; set; } = 180;
    public bool IsDailyLimitEnabled { get; set; } = true;

    public bool IsBedtimeEnabled { get; set; } = true;
    public string? BedtimeStart { get; set; } = "21:30";
    public string? BedtimeEnd { get; set; } = "06:30";

    public bool IsAllowedWindowEnabled { get; set; } = false;
    public string? AllowedStartTime { get; set; } = "07:00";
    public string? AllowedEndTime { get; set; } = "21:00";

    public string? LockMessage { get; set; }
}

public class QuickLockRequest
{
    public bool IsLocked { get; set; }
    public string? Reason { get; set; }
}

public class AddBonusRequest
{
    public int Minutes { get; set; } = 15;
}

public class HeartbeatRequest
{
    public string? DeviceName { get; set; }
    public int IncrementMinutes { get; set; } = 1;
}

public class ScreenTimeRealtimeStatus
{
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public int UsedMinutesToday { get; set; }
    public int RemainingMinutesToday { get; set; }
    public int EffectiveLimitToday { get; set; }
    public int BaseLimitToday { get; set; }
    public int BonusMinutesToday { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsInBedtime { get; set; }
    public bool IsOutsideWindow { get; set; }
    public bool IsLimitReached { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public string? LockMessage { get; set; }
    public string CurrentTimeVn { get; set; } = string.Empty;
}
