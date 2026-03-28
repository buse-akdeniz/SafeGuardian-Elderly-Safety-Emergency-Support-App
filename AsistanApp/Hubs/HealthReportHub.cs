using Microsoft.AspNetCore.SignalR;

namespace ilk_projem.Hubs;

public class HealthReportHub : Hub
{
    public async Task SendHealthUpdate(string elderlyId, string healthData) => await Clients.All.SendAsync("ReceiveHealthUpdate", new { elderlyId, data = healthData });
    public async Task SendEmergencyAlert(string elderlyId, string alertType) => await Clients.All.SendAsync("ReceiveEmergencyAlert", new { elderlyId, alertType });
    public async Task SendTaskUpdate(string elderlyId, string taskId, string status) => await Clients.All.SendAsync("ReceiveTaskUpdate", new { elderlyId, taskId, status });
    public async Task SendAICriticalAlert(string elderlyId, string alertType) => await Clients.All.SendAsync("ReceiveAICritical", new { elderlyId, alertType, timestamp = DateTime.Now });
    public async Task SendEmergencyEscalation(string elderlyId) => await Clients.All.SendAsync("ReceiveEmergencyEscalation", new { elderlyId, timestamp = DateTime.Now });
    public async Task SendEmergencyBroadcast(object broadcastData) => await Clients.All.SendAsync("ReceiveEmergencyBroadcast", broadcastData);
    public async Task SendFamilyAlert(object alertData) => await Clients.Group("family:all").SendAsync("ReceiveFamilyAlert", alertData);
    public async Task SendFallDetectedAlert(string elderlyId, double accelerationMagnitude)
        => await Clients.Group("family:all").SendAsync("ReceiveFallDetected", new
        {
            elderlyId,
            accelerationMagnitude,
            title = "Düşme Algılandı",
            timestamp = DateTime.Now
        });
    public async Task SendAlertCancelled(string elderlyId) => await Clients.All.SendAsync("ReceiveAlertCancelled", new { elderlyId, timestamp = DateTime.Now });

    public async Task JoinFamilyGroup(string recipient)
    {
        var group = string.IsNullOrWhiteSpace(recipient) ? "family:all" : $"family:{recipient}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public async Task LeaveFamilyGroup(string recipient)
    {
        var group = string.IsNullOrWhiteSpace(recipient) ? "family:all" : $"family:{recipient}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"✅ Client Connected: {Context.ConnectionId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, "family:all");
        await base.OnConnectedAsync();
    }
}
