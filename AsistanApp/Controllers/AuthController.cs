using AsistanApp.Services;using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ilk_projem.Services;

namespace ilk_projem.Controllers;

[ApiController]
[Route("api/elderly")]
public class AuthController : ControllerBase
{
    [HttpPost("elderly/login")]
    public async Task<IResult> ElderlyLogin([FromServices] HealthDataService svc)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonDocument.Parse(body).RootElement;

        var email = json.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
        var password = json.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";

        var result = svc.AuthenticateElderly(email, password);
        if (!result.HasValue)
        {
            return Results.Json(new { success = false, message = "Geçersiz kimlik bilgileri" }, statusCode: 401);
        }

        return Results.Json(new
        {
            success = true,
            token = result.Value.Token,
            userId = result.Value.User.Id,
            name = result.Value.User.Name
        });
    }

    [HttpGet("me")]
    public IResult Me([FromServices] HealthDataService svc)
    {
        var token = AuthTokenService.ResolveToken(HttpContext);
        var elderly = svc.GetElderlySession(token);
        if (elderly == null)
        {
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
        }

        return Results.Json(new { success = true, user = new { elderly.Id, elderly.Name, elderly.Email } });
    }
}
