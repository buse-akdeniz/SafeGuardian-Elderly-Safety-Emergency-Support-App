using System.Security.Claims;
using System.Text.Json;
using ilk_projem.Data;
using ilk_projem.Models.Persistence;
using ilk_projem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ilk_projem.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class CompatibilityController : ControllerBase
{
    private readonly AppDbContext _db;

    public CompatibilityController(AppDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet("health")]
    public IResult Health() => Results.Ok(new { success = true, ok = true, serverTime = DateTime.UtcNow });

    [HttpGet("family/subscription")]
    public async Task<IResult> FamilySubscription(
        [FromServices] SubscriptionService subscriptions,
        CancellationToken cancellationToken)
    {
        var entitlement = await subscriptions.GetEntitlementAsync(ElderlyId(), cancellationToken);
        return Results.Ok(new
        {
            success = true,
            plan = entitlement.Plan,
            isActive = entitlement.IsActive,
            entitlement.HasFullAccess,
            entitlement.ExpiresAt,
            entitlement.AdUnlockUntil
        });
    }

    [HttpGet("family/account")]
    public async Task<IResult> FamilyAccount(
        [FromServices] UserManager<ApplicationUser> users)
    {
        var user = await users.FindByIdAsync(UserId());
        return user is null
            ? Results.Unauthorized()
            : Results.Ok(new
            {
                success = true,
                account = new
                {
                    name = user.DisplayName,
                    user.Email,
                    phone = user.PhoneNumber,
                    updatedAt = user.LastAuthenticatedAt
                }
            });
    }

    [HttpPut("family/account")]
    public async Task<IResult> UpdateFamilyAccount(
        [FromBody] FamilyAccountRequest request,
        [FromServices] UserManager<ApplicationUser> users)
    {
        var user = await users.FindByIdAsync(UserId());
        if (user is null) return Results.Unauthorized();
        user.DisplayName = request.Name.Trim();
        user.PhoneNumber = request.Phone?.Trim();
        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await users.SetEmailAsync(user, request.Email.Trim());
            if (!emailResult.Succeeded)
                return Results.BadRequest(new { success = false, message = emailResult.Errors.First().Description });
            await users.SetUserNameAsync(user, request.Email.Trim());
        }
        await users.UpdateAsync(user);
        return Results.Ok(new { success = true, message = "Hesap bilgileri güncellendi" });
    }

    [HttpGet("family/last-contact")]
    public async Task<IResult> FamilyLastContact(CancellationToken cancellationToken)
    {
        var contact = await _db.FamilyContacts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ElderlyId == ElderlyId(), cancellationToken);
        var hours = contact is null ? (double?)null : (DateTime.UtcNow - contact.LastContactAt).TotalHours;
        return Results.Ok(new { success = true, hoursSince = hours });
    }

    [HttpPost("family/contact")]
    public async Task<IResult> MarkFamilyContact(CancellationToken cancellationToken)
    {
        var elderlyId = ElderlyId();
        var contact = await _db.FamilyContacts
            .SingleOrDefaultAsync(x => x.ElderlyId == elderlyId, cancellationToken);
        if (contact is null)
        {
            contact = new StoredFamilyContact { ElderlyId = elderlyId };
            _db.FamilyContacts.Add(contact);
        }
        contact.LastContactAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { success = true, contactAt = contact.LastContactAt });
    }

    [Authorize(Roles = "Family")]
    [HttpGet("family/dashboard/{elderlyId}")]
    public async Task<IResult> FamilyDashboard(
        string elderlyId,
        [FromServices] SubscriptionService subscriptions,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(elderlyId, ElderlyId(), StringComparison.Ordinal))
            return Results.Forbid();
        if (!await subscriptions.HasPremiumAccessAsync(elderlyId, cancellationToken))
            return Results.Json(new { success = false, message = "Premium abonelik gerekli" }, statusCode: 402);

        var elderly = await _db.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == elderlyId && u.AccountType == "Elderly", cancellationToken);
        if (elderly is null) return Results.NotFound();

        var medications = await _db.Medications.AsNoTracking()
            .Where(m => m.ElderlyId == elderlyId)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
        var notifications = await _db.Notifications.AsNoTracking()
            .Where(n => n.ElderlyId == elderlyId)
            .OrderByDescending(n => n.Timestamp)
            .Take(8)
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            elderly = new { elderly.Id, name = elderly.DisplayName, phone = elderly.PhoneNumber },
            todayMedications = medications.Select(m => new
            {
                medicationName = m.Name,
                scheduleTimes = DeserializeTimes(m.ScheduleTimesJson),
                m.Notes,
                takenToday = m.LastTakenAt is null ? [] : new[] { m.LastTakenAt.Value }
            }),
            recentNotifications = notifications.Select(n => new
            {
                n.Type,
                title = n.Severity == "critical" ? "Acil Durum" : "Bildirim",
                n.Message,
                createdAt = n.Timestamp
            })
        });
    }

    [HttpGet("mood-analysis")]
    public async Task<IResult> MoodAnalysis(
        [FromServices] SubscriptionService subscriptions,
        CancellationToken cancellationToken)
    {
        var elderlyId = ElderlyId();
        if (!await subscriptions.HasPremiumAccessAsync(elderlyId, cancellationToken))
            return Results.Json(new { success = false, message = "Premium abonelik gerekli" }, statusCode: 402);
        var recent = await _db.MoodRecords.AsNoTracking()
            .Where(x => x.ElderlyId == elderlyId)
            .OrderByDescending(x => x.Timestamp)
            .Take(5)
            .ToListAsync(cancellationToken);
        return Results.Ok(new
        {
            success = true,
            averageMood = recent.Count == 0 ? 0 : Math.Round(recent.Average(x => x.MoodScore), 1),
            trend = "stable",
            recentMoods = recent
        });
    }

    [Authorize(Roles = "Elderly")]
    [HttpPost("mood")]
    public async Task<IResult> AddMood([FromBody] MoodRequest request, CancellationToken cancellationToken)
    {
        if (request.MoodScore is < 1 or > 10)
            return Results.BadRequest(new { success = false, message = "Mood score must be between 1 and 10." });
        _db.MoodRecords.Add(new StoredMoodRecord
        {
            ElderlyId = ElderlyId(),
            MoodScore = request.MoodScore,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { success = true });
    }

    [HttpGet("health-records")]
    public async Task<IResult> HealthRecords(CancellationToken cancellationToken)
    {
        var records = await _db.HealthRecords.AsNoTracking()
            .Where(x => x.ElderlyId == ElderlyId())
            .OrderByDescending(x => x.RecordedAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                recordType = x.MetricType,
                x.Value,
                unit = x.Notes,
                alertLevel = x.HealthStatus,
                timestamp = x.RecordedAt
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(records);
    }

    [Authorize(Roles = "Elderly")]
    [HttpPost("health-records")]
    public async Task<IResult> AddHealthRecord(
        [FromBody] HealthRecordRequest request,
        CancellationToken cancellationToken)
    {
        var alert = request.Value > 180 ? "critical" : request.Value > 140 ? "warning" : "normal";
        _db.HealthRecords.Add(new StoredHealthRecord
        {
            ElderlyId = ElderlyId(),
            MetricType = request.RecordType,
            Value = request.Value,
            Notes = request.Unit,
            HealthStatus = alert,
            RecordedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { success = true, alertLevel = alert, healthStatus = alert });
    }

    [HttpGet("medications")]
    public async Task<IResult> Medications(CancellationToken cancellationToken)
    {
        var items = await _db.Medications.AsNoTracking()
            .Where(x => x.ElderlyId == ElderlyId())
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return Results.Ok(items.Select(ToMedicationResponse));
    }

    [Authorize(Roles = "Elderly")]
    [HttpPost("medications")]
    public async Task<IResult> AddMedication(
        [FromBody] MedicationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { success = false, message = "İlaç adı zorunludur." });
        _db.Medications.Add(new StoredMedication
        {
            ElderlyId = ElderlyId(),
            Name = request.Name.Trim(),
            Notes = request.Notes?.Trim() ?? "",
            ScheduleTimesJson = JsonSerializer.Serialize(request.ScheduleTimes ?? []),
            StockCount = 30
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { success = true });
    }

    [Authorize(Roles = "Elderly")]
    [HttpPost("medications/{id:int}/taken")]
    public async Task<IResult> TakeMedication(int id, CancellationToken cancellationToken)
    {
        var medication = await _db.Medications
            .SingleOrDefaultAsync(x => x.Id == id && x.ElderlyId == ElderlyId(), cancellationToken);
        if (medication is null) return Results.NotFound(new { success = false, message = "İlaç bulunamadı" });
        medication.LastTakenAt = DateTime.UtcNow;
        medication.StockCount = Math.Max(0, (medication.StockCount ?? 0) - 1);
        await _db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new
        {
            success = true,
            medication = ToMedicationResponse(medication),
            stockCount = medication.StockCount
        });
    }

    [HttpGet("family-members")]
    public async Task<IResult> FamilyMembers(
        [FromServices] SubscriptionService subscriptions,
        CancellationToken cancellationToken)
    {
        var elderlyId = ElderlyId();
        if (!await subscriptions.HasPremiumAccessAsync(elderlyId, cancellationToken))
            return Results.Json(new { success = false, message = "Premium abonelik gerekli" }, statusCode: 402);
        var members = await _db.FamilyMembers.AsNoTracking()
            .Where(x => x.ElderlyId == elderlyId)
            .ToListAsync(cancellationToken);
        return Results.Ok(new { success = true, members });
    }

    [Authorize(Roles = "Elderly")]
    [HttpPost("family-members")]
    public async Task<IResult> AddFamilyMember(
        [FromBody] FamilyMemberRequest request,
        [FromServices] SubscriptionService subscriptions,
        CancellationToken cancellationToken)
    {
        var elderlyId = ElderlyId();
        if (!await subscriptions.HasPremiumAccessAsync(elderlyId, cancellationToken))
            return Results.Json(new { success = false, message = "Premium abonelik gerekli" }, statusCode: 402);
        if (await _db.FamilyMembers.AnyAsync(x => x.ElderlyId == elderlyId && x.Email == request.Email, cancellationToken))
            return Results.Conflict(new { success = false, message = "Bu aile üyesi zaten kayıtlı." });
        _db.FamilyMembers.Add(new StoredFamilyMember
        {
            Id = Guid.NewGuid().ToString("N"),
            ElderlyId = elderlyId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Relationship = request.Relationship?.Trim() ?? "",
            PhoneNumber = request.PhoneNumber?.Trim() ?? ""
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { success = true });
    }

    [HttpGet("notifications")]
    public async Task<IResult> Notifications(CancellationToken cancellationToken)
    {
        var items = await _db.Notifications.AsNoTracking()
            .Where(x => x.ElderlyId == ElderlyId())
            .OrderByDescending(x => x.Timestamp)
            .Take(100)
            .ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    [HttpGet("doctor/report")]
    public async Task<IResult> DoctorReport(CancellationToken cancellationToken)
    {
        var elderlyId = ElderlyId();
        var elderly = await _db.Users.AsNoTracking()
            .Where(x => x.Id == elderlyId)
            .Select(x => new
            {
                x.DisplayName,
                x.Email,
                x.PhoneNumber,
                x.BirthDate,
                x.BloodType,
                x.Allergies,
                x.MedicalHistory
            })
            .SingleAsync(cancellationToken);
        var healthRecords = await _db.HealthRecords.AsNoTracking()
            .Where(x => x.ElderlyId == elderlyId)
            .OrderByDescending(x => x.RecordedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var medications = await _db.Medications.AsNoTracking()
            .Where(x => x.ElderlyId == elderlyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var moods = await _db.MoodRecords.AsNoTracking()
            .Where(x => x.ElderlyId == elderlyId)
            .OrderByDescending(x => x.Timestamp)
            .Take(30)
            .ToListAsync(cancellationToken);
        return Results.Ok(new
        {
            success = true,
            generatedAt = DateTime.UtcNow,
            elderly = new
            {
                name = elderly.DisplayName,
                elderly.Email,
                phone = elderly.PhoneNumber,
                elderly.BirthDate,
                elderly.BloodType,
                elderly.Allergies,
                elderly.MedicalHistory
            },
            healthRecords = healthRecords.Select(x => new
            {
                timestamp = x.RecordedAt,
                recordType = x.MetricType,
                x.Value,
                x.Systolic,
                x.Diastolic,
                x.Glucose,
                x.HeartRate,
                status = x.HealthStatus,
                x.Notes
            }),
            medications = medications.Select(ToMedicationResponse),
            mood = new
            {
                average = moods.Count == 0 ? (double?)null : Math.Round(moods.Average(x => x.MoodScore), 1),
                trend = "stable"
            }
        });
    }

    [HttpPost("send-notification")]
    public async Task<IResult> SendNotification(
        [FromBody] NotificationRequest request,
        CancellationToken cancellationToken)
    {
        _db.Notifications.Add(new StoredNotification
        {
            ElderlyId = ElderlyId(),
            Type = request.Type ?? "info",
            Message = request.Message,
            Severity = request.Severity ?? "normal"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { success = true, notificationStored = true });
    }

    [Authorize(Roles = "Elderly")]
    [HttpPost("emergency-alert")]
    [HttpPost("emergency-broadcast")]
    public async Task<IResult> EmergencyAlert(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        _db.EmergencyAlerts.Add(new StoredEmergencyAlert
        {
            Id = Guid.NewGuid().ToString("N"),
            ElderlyId = ElderlyId(),
            AlertType = payload.TryGetProperty("type", out var type) ? type.GetString() ?? "emergency" : "emergency",
            Description = payload.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "",
            OccurredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { success = true, acknowledged = true });
    }

    [HttpPost("emergency-sms/test")]
    public IResult EmergencySmsTest() => Results.Ok(new
    {
        success = true,
        simulated = true,
        smsDispatched = false
    });

    [Authorize(Roles = "Elderly")]
    [EnableRateLimiting("sms")]
    [HttpPost("emergency-sms/dispatch")]
    public async Task<IResult> DispatchEmergencySms(
        [FromBody] EmergencySmsRequest request,
        [FromServices] EmergencySmsService sms,
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var elderlyId = ElderlyId();
        var dailyLimit = configuration.GetValue("Sms:DailyPerUserLimit", 10);
        var sentToday = await _db.SmsDispatchAudits
            .CountAsync(x => x.UserId == UserId()
                && x.Succeeded
                && x.CreatedAt >= DateTime.UtcNow.AddHours(-24), cancellationToken);
        if (sentToday >= dailyLimit)
            return Results.Json(new { success = false, message = "Günlük SMS güvenlik limiti aşıldı." }, statusCode: 429);
        if (!sms.IsConfigured)
            return Results.Json(new { success = false, message = "SMS provider is not configured." }, statusCode: 503);

        var allowed = await _db.FamilyMembers.AsNoTracking()
            .Where(x => x.ElderlyId == elderlyId && x.PhoneNumber != "")
            .Select(x => x.PhoneNumber)
            .ToListAsync(cancellationToken);
        var allowedNormalized = allowed.Select(NormalizePhone)
            .Where(x => x.Length >= 10)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (allowedNormalized.Length == 0)
            return Results.BadRequest(new { success = false, message = "Kayıtlı acil durum alıcısı bulunamadı." });

        var message = (request.Message ?? "SafeGuardian acil yardım isteği.").Trim();
        if (!string.IsNullOrWhiteSpace(request.Location))
            message = $"{message}\nKonum: {request.Location.Trim()}";
        message = message[..Math.Min(message.Length, 500)];

        var sent = 0;
        foreach (var recipient in allowedNormalized.Take(Math.Max(0, dailyLimit - sentToday)))
        {
            var succeeded = await sms.SendAsync(recipient, message, cancellationToken);
            _db.SmsDispatchAudits.Add(new SmsDispatchAudit
            {
                UserId = UserId(),
                RecipientHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(recipient))),
                Succeeded = succeeded,
                CreatedAt = DateTime.UtcNow
            });
            if (succeeded) sent++;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return sent > 0
            ? Results.Ok(new { success = true, dispatched = sent })
            : Results.Json(new { success = false, message = "SMS gönderilemedi." }, statusCode: 502);
    }

    [HttpPost("chat")]
    public IResult Chat([FromBody] ChatRequest request) => Results.Ok(new
    {
        success = true,
        reply = string.IsNullOrWhiteSpace(request.Message)
            ? "Size nasıl yardımcı olabilirim?"
            : "Mesajınız alındı. Acil bir durum varsa acil yardım düğmesini kullanın."
    });

    private string UserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    private string ElderlyId() =>
        User.FindFirstValue("elderly_id") is { Length: > 0 } id
            ? id
            : throw new UnauthorizedAccessException();

    private static string[] DeserializeTimes(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch { return []; }
    }

    private static string NormalizePhone(string value)
    {
        var trimmed = (value ?? "").Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return trimmed.StartsWith('+') ? $"+{digits}" : digits;
    }

    private static object ToMedicationResponse(StoredMedication medication) => new
    {
        medication.Id,
        medication.Name,
        medication.Notes,
        scheduleTimes = DeserializeTimes(medication.ScheduleTimesJson),
        medication.StockCount,
        medication.LastTakenAt,
        medication.CreatedAt
    };
}

public sealed record FamilyAccountRequest(string Name, string Email, string? Phone);
public sealed record MoodRequest(int MoodScore);
public sealed record HealthRecordRequest(string RecordType, double Value, string Unit);
public sealed record MedicationRequest(string Name, string? Notes, string[]? ScheduleTimes);
public sealed record FamilyMemberRequest(
    string Name,
    string Email,
    string? Relationship,
    string? PhoneNumber);
public sealed record NotificationRequest(string Message, string? Type, string? Severity);
public sealed record ChatRequest(string Message);
public sealed record EmergencySmsRequest(string? Message, string? Location);
