using AsistanApp.Services;using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ilk_projem.Services;
using ilk_projem.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ilk_projem.Controllers;

[ApiController]
[Route("api/family")]
public class FamilyController : ControllerBase
{
    [HttpGet("members")]
    public IResult Members([FromServices] HealthDataService svc)
    {
        var token = AuthTokenService.ResolveToken(HttpContext);
        var elderly = svc.GetElderlySession(token);
        if (elderly == null)
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        return Results.Json(new { success = true, members = svc.GetFamilyMembers(elderly.Id) });
    }

    [HttpPost("fall-alert")]
    public async Task<IResult> FallAlert([FromServices] HealthDataService svc, [FromServices] IHubContext<HealthReportHub> hub)
    {
        var token = AuthTokenService.ResolveToken(HttpContext);
        var elderly = svc.GetElderlySession(token);
        if (elderly == null)
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        var json = await JsonDocument.ParseAsync(Request.Body);
        var magnitude = json.RootElement.TryGetProperty("accelerationMagnitude", out var m) ? m.GetDouble() : 0;

        var payload = new
        {
            elderlyId = elderly.Id,
            title = "Düşme Algılandı",
            message = $"{elderly.Name} için düşme riski tespit edildi ({magnitude:F2} m/s²)",
            severity = "critical",
            timestamp = DateTime.Now
        };

        await hub.Clients.Group("family:all").SendAsync("ReceiveFamilyAlert", payload);
        await hub.Clients.Group($"family:{elderly.Id}").SendAsync("ReceiveFamilyAlert", payload);

        return Results.Json(new { success = true });
    }
}
