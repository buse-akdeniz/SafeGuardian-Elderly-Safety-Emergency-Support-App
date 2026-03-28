using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ilk_projem.Services;
using ilk_projem.Data;
using ilk_projem.Models.Persistence;

namespace ilk_projem.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet("records")]
    public IResult Records([FromServices] HealthDataService svc)
    {
        var token = AuthTokenService.ResolveToken(HttpContext);
        var elderly = svc.GetElderlySession(token);
        if (elderly == null)
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        var records = svc.GetHealthRecords(elderly.Id, 30)
            .OrderByDescending(r => r.Timestamp ?? r.RecordedAt)
            .Take(200)
            .ToList();

        return Results.Json(new { success = true, records });
    }

    [HttpPost("records")]
    public async Task<IResult> AddRecord([FromServices] HealthDataService svc, [FromServices] AppDbContext db)
    {
        var token = AuthTokenService.ResolveToken(HttpContext);
        var elderly = svc.GetElderlySession(token);
        if (elderly == null)
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        var json = await JsonDocument.ParseAsync(Request.Body);
        var root = json.RootElement;

        var metricType = root.TryGetProperty("recordType", out var rt) ? rt.GetString() ?? "manual" : "manual";
        var value = root.TryGetProperty("value", out var v) && v.TryGetDouble(out var d) ? d : 0;

        svc.AddHealthRecord(elderly.Id, metricType, value);

        db.HealthRecords.Add(new StoredHealthRecord
        {
            ElderlyId = elderly.Id,
            MetricType = metricType,
            Value = value,
            HealthStatus = "normal",
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return Results.Json(new { success = true });
    }
}
