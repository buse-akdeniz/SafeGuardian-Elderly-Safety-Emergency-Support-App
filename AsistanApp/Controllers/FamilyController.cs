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
    [HttpPost("login")]
    public async Task<IResult> Login([FromServices] HealthDataService svc)
    {
        try
        {
            using var reader = new System.IO.StreamReader(HttpContext.Request.Body);
            var body = await reader.ReadToEndAsync();
            
            if (string.IsNullOrWhiteSpace(body))
                return Results.Json(new { success = false, message = "İstek gövdesi boş" }, statusCode: 400);
            
            var json = System.Text.Json.JsonDocument.Parse(body).RootElement;

            string email = json.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
            string password = json.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Results.Json(new { success = false, message = "E-posta ve şifre zorunludur" }, statusCode: 400);

            var result = svc.AuthenticateFamily(email, password);
            if (result == null)
            {
                return Results.Json(new { success = false, message = "Geçersiz giriş" }, statusCode: 401);
            }

            return Results.Json(new
            {
                success = true,
                token = result.Value.Token,
                recipient = result.Value.Member.Email,
                memberName = result.Value.Member.Name,
                caringFor = result.Value.Member.ElderlyId
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new { success = false, message = "İstek işlenirken hata oluştu: " + ex.Message }, statusCode: 500);
        }
    }

    [HttpGet("members")]
    public async Task<IResult> Members([FromServices] HealthDataService svc)
    {
        var token = AuthTokenService.ResolveToken(HttpContext);
        var elderly = await svc.GetElderlySession(token);
        if (elderly == null)
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        var members = await svc.GetFamilyMembers((int)elderly.Id);
        return Results.Json(new { success = true, members });
    }

    [HttpPost("fall-alert")]
    public async Task<IResult> FallAlert([FromServices] HealthDataService svc, [FromServices] IHubContext<HealthReportHub> hub)
    {
        var token = AuthTokenService.ResolveToken(HttpContext);
        var elderly = await svc.GetElderlySession(token);
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
