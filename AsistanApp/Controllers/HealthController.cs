using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ilk_projem.Data;
using ilk_projem.Models.Persistence;

namespace ilk_projem.Controllers;

[ApiController]
[Authorize]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet("records")]
    public async Task<IResult> Records([FromServices] AppDbContext db)
    {
        var elderlyId = User.FindFirstValue("elderly_id")!;
        var records = await db.HealthRecords.AsNoTracking()
            .Where(r => r.ElderlyId == elderlyId)
            .OrderByDescending(r => r.RecordedAt)
            .Take(200)
            .ToListAsync();

        return Results.Json(new { success = true, records });
    }

    [HttpPost("records")]
    [Authorize(Roles = "Elderly")]
    public async Task<IResult> AddRecord([FromServices] AppDbContext db)
    {
        var json = await JsonDocument.ParseAsync(Request.Body);
        var root = json.RootElement;

        var metricType = root.TryGetProperty("recordType", out var rt) ? rt.GetString() ?? "manual" : "manual";
        var value = root.TryGetProperty("value", out var v) && v.TryGetDouble(out var d) ? d : 0;

        db.HealthRecords.Add(new StoredHealthRecord
        {
            ElderlyId = User.FindFirstValue("elderly_id")!,
            MetricType = metricType,
            Value = value,
            HealthStatus = "normal",
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return Results.Json(new { success = true });
    }
}
