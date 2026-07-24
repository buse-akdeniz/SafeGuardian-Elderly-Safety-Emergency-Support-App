using System.Security.Claims;
using AsistanApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ilk_projem.Services;
using ilk_projem.Hubs;
using ilk_projem.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ilk_projem.Controllers;

[ApiController]
[Route("api/family")]
public class FamilyController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] AuthService auth,
        CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(
            request.Email,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            "Family",
            cancellationToken);
        if (result is null)
            return Results.Json(new { success = false, message = "Geçersiz giriş" }, statusCode: 401);

        return Results.Json(new
        {
            success = true,
            token = result.Value.Tokens.AccessToken,
            refreshToken = result.Value.Tokens.RefreshToken,
            expiresAt = result.Value.Tokens.ExpiresAt,
            recipient = result.Value.User.Email,
            memberName = result.Value.User.DisplayName,
            caringFor = result.Value.User.ElderlyOwnerId
        });
    }

    [Authorize(Roles = "Elderly")]
    [HttpGet("members")]
    public async Task<IResult> Members([FromServices] AppDbContext db, CancellationToken cancellationToken)
    {
        var elderlyId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var members = await db.Users
            .AsNoTracking()
            .Where(u => u.AccountType == "Family" && u.ElderlyOwnerId == elderlyId)
            .Select(u => new { u.Id, name = u.DisplayName, u.Email })
            .ToListAsync(cancellationToken);
        return Results.Json(new { success = true, members });
    }

    [Authorize(Roles = "Elderly")]
    [HttpPost("fall-alert")]
    public async Task<IResult> FallAlert([FromServices] IHubContext<HealthReportHub> hub)
    {
        var elderlyId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";
        var elderlyName = User.FindFirstValue("name") ?? "SafeGuardian kullanıcısı";

        var json = await JsonDocument.ParseAsync(Request.Body);
        var magnitude = json.RootElement.TryGetProperty("accelerationMagnitude", out var m) ? m.GetDouble() : 0;

        var payload = new
        {
            elderlyId,
            title = "Düşme Algılandı",
            message = $"{elderlyName} için düşme riski tespit edildi ({magnitude:F2} m/s²)",
            severity = "critical",
            timestamp = DateTime.Now
        };

        await hub.Clients.Group("family:all").SendAsync("ReceiveFamilyAlert", payload);
        await hub.Clients.Group($"family:{elderlyId}").SendAsync("ReceiveFamilyAlert", payload);

        return Results.Json(new { success = true });
    }
}
