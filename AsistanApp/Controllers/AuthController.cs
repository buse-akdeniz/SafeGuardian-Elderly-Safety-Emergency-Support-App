using AsistanApp.Services;using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ilk_projem.Services;

namespace ilk_projem.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    // Demo account for App Review - expired subscription
    private const string DemoEmail = "review.elderly@safeguardian.app";
    private const string DemoPassword = "Review123!";
    private const string DemoToken = "demo-review-elderly-expired-token";

    [HttpPost("elderly/login")]
    public async Task<IResult> ElderlyLogin([FromServices] HealthDataService svc)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            
            if (string.IsNullOrWhiteSpace(body))
                return Results.Json(new { success = false, message = "İstek gövdesi boş" }, statusCode: 400);
            
            var json = JsonDocument.Parse(body).RootElement;

            var email = json.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
            var password = json.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Results.Json(new { success = false, message = "E-posta ve şifre zorunludur" }, statusCode: 400);

            // Check for demo account
            if (string.Equals(email, DemoEmail, StringComparison.OrdinalIgnoreCase) && password == DemoPassword)
            {
                return Results.Json(new
                {
                    success = true,
                    token = DemoToken,
                    userId = 999,
                    name = "Test User"
                });
            }

            var result = await svc.AuthenticateElderly(email, password);
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
        catch (Exception ex)
        {
            return Results.Json(new { success = false, message = "İstek işlenirken hata oluştu: " + ex.Message }, statusCode: 500);
        }
    }

    [HttpGet("me")]
    public async Task<IResult> Me([FromServices] HealthDataService svc)
    {
        var token = AuthTokenService.ResolveToken(HttpContext);
        var elderly = await svc.GetElderlySession(token);
        if (elderly == null)
        {
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
        }

        return Results.Json(new { success = true, user = new { elderly.Id, elderly.Name, elderly.Email } });
    }
}
