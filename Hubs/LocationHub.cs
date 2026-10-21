using Microsoft.AspNetCore.SignalR;

namespace Giahuy0790.Hubs;

/// <summary>
/// SignalR Hub truyền nhận tọa độ vị trí GPS và cảnh báo vùng an toàn thời gian thực
/// </summary>
public class LocationHub : Hub
{
    private readonly ILogger<LocationHub> _logger;

    public LocationHub(ILogger<LocationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Phụ huynh / Người giám hộ đăng ký theo dõi thời gian thực của 1 bé cụ thể
    /// </summary>
    public async Task JoinKidTracking(int kidId)
    {
        var groupName = $"kid_{kidId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} đã tham gia theo dõi vị trí cho Kid ID: {KidId}", Context.ConnectionId, kidId);
    }

    /// <summary>
    /// Rời khỏi nhóm theo dõi của bé
    /// </summary>
    public async Task LeaveKidTracking(int kidId)
    {
        var groupName = $"kid_{kidId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} đã rời khỏi nhóm theo dõi Kid ID: {KidId}", Context.ConnectionId, kidId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client SignalR kết nối vào LocationHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client SignalR ngắt kết nối khỏi LocationHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
