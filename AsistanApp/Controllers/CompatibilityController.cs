using Microsoft.AspNetCore.Mvc;
using ilk_projem.Services;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace ilk_projem.Controllers;

[ApiController]
[Route("api")]
public class CompatibilityController : ControllerBase
{
    private static readonly ConcurrentDictionary<int, List<MedicationItem>> MedicationsByUser = new();
    private static readonly ConcurrentDictionary<int, List<HealthRecordItem>> HealthByUser = new();
    private static readonly ConcurrentDictionary<int, List<MoodItem>> MoodByUser = new();
    private static readonly ConcurrentDictionary<int, List<FamilyMemberItem>> FamilyByUser = new();
    private static readonly ConcurrentDictionary<int, List<NotificationItem>> NotificationsByUser = new();
    private static readonly ConcurrentDictionary<int, SubscriptionItem> SubscriptionByUser = new();
    private static readonly ConcurrentDictionary<int, FamilyAccountItem> FamilyAccountByUser = new();

    private int ResolveUserId()
    {
        var token = AuthTokenService.ResolveToken(HttpContext);
        return string.IsNullOrWhiteSpace(token) ? 1 : 1;
    }

    private IConfiguration Config => HttpContext.RequestServices.GetRequiredService<IConfiguration>();

    private string GetSetting(string key, string? fallbackEnv = null)
    {
        var value = Config[key];
        if (!string.IsNullOrWhiteSpace(value)) return value;
        return string.IsNullOrWhiteSpace(fallbackEnv) ? string.Empty : (Environment.GetEnvironmentVariable(fallbackEnv) ?? string.Empty);
    }

    private string GetFirstSetting(params (string key, string? env)[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var value = GetSetting(candidate.key, candidate.env);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return string.Empty;
    }

    private bool IsReviewerModeEnabled()
    {
        var raw = GetSetting("ReviewerMode:Enabled", "REVIEWER_MODE_ENABLED");
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private string GetReviewerTestPhoneNumber()
    {
        var configured = GetSetting("ReviewerMode:TestPhoneNumber", "REVIEWER_TEST_PHONE_NUMBER");
        return string.IsNullOrWhiteSpace(configured) ? "+1234567890" : configured;
    }

    private static string NormalizePhone(string? value)
        => new string((value ?? string.Empty).Where(ch => char.IsDigit(ch) || ch == '+').ToArray());

    [HttpGet("health")]
    public IResult Health() => Results.Json(new { success = true, ok = true, serverTime = DateTime.UtcNow });

    [HttpPost("elderly-self-enroll")]
    public IResult ElderlySelfEnroll() => Results.Json(new { success = true, tempPassword = "Review123!", message = "Kayıt tamamlandı" });

    [HttpPost("elderly/reset-password")]
    public IResult ElderlyResetPassword() => Results.Json(new { success = true, tempPassword = "Review123!", message = "Geçici şifre oluşturuldu" });

    [HttpDelete("elderly/account")]
    public IResult DeleteElderlyAccount() => Results.Json(new { success = true, message = "Hesap silindi" });

    [HttpGet("subscription")]
    public IResult GetSubscription()
    {
        var userId = ResolveUserId();
        var sub = SubscriptionByUser.GetOrAdd(userId, _ => new SubscriptionItem
        {
            Plan = "standard",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMonths(1)
        });

        return Results.Json(new
        {
            success = true,
            plan = sub.Plan,
            isActive = sub.IsActive,
            expiresAt = sub.ExpiresAt
        });
    }

    [HttpPost("subscription/cancel")]
    public IResult CancelSubscription()
    {
        var userId = ResolveUserId();
        SubscriptionByUser[userId] = new SubscriptionItem
        {
            Plan = "standard",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMonths(1)
        };

        return Results.Json(new
        {
            success = true,
            message = "Abonelik iptal edildi. Dönem sonuna kadar aktif.",
            subscription = SubscriptionByUser[userId]
        });
    }

    [HttpGet("family/subscription")]
    public IResult GetFamilySubscription() => GetSubscription();

    [HttpPost("family/subscription/cancel")]
    public IResult CancelFamilySubscription() => CancelSubscription();

    [HttpGet("family/account")]
    public IResult GetFamilyAccount()
    {
        var userId = ResolveUserId();
        var account = FamilyAccountByUser.GetOrAdd(userId, _ => new FamilyAccountItem
        {
            Name = "Aile Üyesi",
            Email = "family@example.com",
            Phone = "+90 500 000 0000",
            UpdatedAt = DateTime.UtcNow
        });

        return Results.Json(new { success = true, account });
    }

    [HttpPut("family/account")]
    public async Task<IResult> UpsertFamilyAccount()
    {
        var userId = ResolveUserId();
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        var current = FamilyAccountByUser.GetOrAdd(userId, _ => new FamilyAccountItem());
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(raw).RootElement;
            current.Name = json.TryGetProperty("name", out var n) ? n.GetString() ?? current.Name : current.Name;
            current.Email = json.TryGetProperty("email", out var e) ? e.GetString() ?? current.Email : current.Email;
            current.Phone = json.TryGetProperty("phone", out var p) ? p.GetString() ?? current.Phone : current.Phone;
            current.UpdatedAt = DateTime.UtcNow;
        }
        catch { }

        FamilyAccountByUser[userId] = current;
        return Results.Json(new { success = true, account = current, message = "Hesap bilgileri güncellendi" });
    }

    [HttpDelete("family/account")]
    public IResult DeleteFamilyAccount()
    {
        var userId = ResolveUserId();
        FamilyAccountByUser.TryRemove(userId, out _);
        return Results.Json(new { success = true, message = "Aile hesabı silindi" });
    }

    [HttpGet("family/last-contact")]
    public IResult FamilyLastContact() => Results.Json(new { success = true, hoursSince = 2 });

    [HttpPost("family/contact")]
    public IResult MarkFamilyContact()
    {
        var userId = ResolveUserId();
        var list = NotificationsByUser.GetOrAdd(userId, _ => new List<NotificationItem>());
        list.Add(new NotificationItem
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Type = "info",
            Message = "Aile paneli bağlantısı güncellendi",
            Severity = "normal",
            Timestamp = DateTime.UtcNow
        });

        return Results.Json(new { success = true, contactAt = DateTime.UtcNow });
    }

    [HttpGet("family/dashboard/{elderlyId}")]
    public IResult FamilyDashboard([FromRoute] string elderlyId)
    {
        var userId = ResolveUserId();
        var meds = MedicationsByUser.GetOrAdd(userId, _ => new List<MedicationItem>
        {
            new MedicationItem
            {
                Id = 1,
                Name = "Aspirin",
                Notes = "Yemek sonrası",
                ScheduleTimes = new List<string> { "09:00" },
                LastTakenAt = null,
                StockCount = 14
            }
        });

        var notifications = NotificationsByUser.GetOrAdd(userId, _ => new List<NotificationItem>
        {
            new NotificationItem
            {
                Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Type = "info",
                Message = "Sistem normal çalışıyor",
                Severity = "normal",
                Timestamp = DateTime.UtcNow
            }
        });

        var todayMeds = meds.Select(m => new
        {
            medicationName = m.Name,
            scheduleTimes = m.ScheduleTimes,
            notes = m.Notes,
            takenToday = m.LastTakenAt.HasValue ? new[] { m.LastTakenAt.Value } : Array.Empty<DateTime>()
        }).ToList();

        var recent = notifications
            .OrderByDescending(x => x.Timestamp)
            .Take(8)
            .Select(n => new
            {
                type = string.IsNullOrWhiteSpace(n.Type) ? "info" : n.Type,
                title = n.Severity?.Equals("critical", StringComparison.OrdinalIgnoreCase) == true ? "Acil Durum" : "Bildirim",
                message = n.Message,
                createdAt = n.Timestamp
            })
            .ToList();

        return Results.Json(new
        {
            elderly = new
            {
                id = elderlyId,
                name = "Review Elderly",
                age = 75,
                phone = "+90 555 123 4567"
            },
            todayMedications = todayMeds,
            recentNotifications = recent
        });
    }

    [HttpGet("mood-analysis")]
    public IResult GetMoodAnalysis()
    {
        var userId = ResolveUserId();
        var list = MoodByUser.GetOrAdd(userId, _ => new List<MoodItem>());
        var recent = list.OrderByDescending(x => x.Timestamp).Take(5).ToList();
        var avg = recent.Count > 0 ? Math.Round(recent.Average(x => x.MoodScore), 1) : 0;

        return Results.Json(new
        {
            success = true,
            averageMood = avg,
            trend = "stable",
            recentMoods = recent
        });
    }

    [HttpPost("mood")]
    public async Task<IResult> AddMood()
    {
        var userId = ResolveUserId();
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();
        var score = 5;
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(raw).RootElement;
            score = json.TryGetProperty("moodScore", out var s) ? s.GetInt32() : 5;
        }
        catch { }

        var list = MoodByUser.GetOrAdd(userId, _ => new List<MoodItem>());
        list.Add(new MoodItem { MoodScore = score, Timestamp = DateTime.UtcNow });

        return Results.Json(new { success = true });
    }

    [HttpGet("health-records")]
    public IResult GetHealthRecords()
    {
        var userId = ResolveUserId();
        var list = HealthByUser.GetOrAdd(userId, _ => new List<HealthRecordItem>());
        return Results.Json(list.OrderByDescending(x => x.Timestamp).Take(200).ToList());
    }

    [HttpPost("health-records")]
    public async Task<IResult> AddHealthRecord()
    {
        var userId = ResolveUserId();
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        var type = "manual";
        var unit = "unit";
        var value = 0d;

        try
        {
            var json = System.Text.Json.JsonDocument.Parse(raw).RootElement;
            type = json.TryGetProperty("recordType", out var rt) ? rt.GetString() ?? "manual" : "manual";
            unit = json.TryGetProperty("unit", out var un) ? un.GetString() ?? "unit" : "unit";
            value = json.TryGetProperty("value", out var vv) ? vv.GetDouble() : 0d;
        }
        catch { }

        var alert = "normal";
        if (type.Contains("tansiyon", StringComparison.OrdinalIgnoreCase) && value >= 180) alert = "critical";
        else if (type.Contains("şeker", StringComparison.OrdinalIgnoreCase) && value >= 200) alert = "critical";
        else if (value > 140) alert = "warning";

        var list = HealthByUser.GetOrAdd(userId, _ => new List<HealthRecordItem>());
        list.Add(new HealthRecordItem
        {
            RecordType = type,
            Value = value,
            Unit = unit,
            AlertLevel = alert,
            Timestamp = DateTime.UtcNow
        });

        return Results.Json(new { success = true, alertLevel = alert, healthStatus = alert == "critical" ? "critical" : "normal" });
    }

    [HttpGet("medications")]
    public IResult GetMedications()
    {
        var userId = ResolveUserId();
        var meds = MedicationsByUser.GetOrAdd(userId, _ => new List<MedicationItem>());
        return Results.Json(meds.OrderBy(x => x.Id).ToList());
    }

    [HttpPost("medications")]
    public async Task<IResult> AddMedication()
    {
        var userId = ResolveUserId();
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        var name = "İlaç";
        var notes = "";
        var scheduleTimes = new List<string>();

        try
        {
            var json = System.Text.Json.JsonDocument.Parse(raw).RootElement;
            name = json.TryGetProperty("name", out var n) ? n.GetString() ?? "İlaç" : "İlaç";
            notes = json.TryGetProperty("notes", out var no) ? no.GetString() ?? "" : "";
            if (json.TryGetProperty("scheduleTimes", out var st) && st.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                scheduleTimes = st.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
        }
        catch { }

        var meds = MedicationsByUser.GetOrAdd(userId, _ => new List<MedicationItem>());
        var nextId = meds.Count == 0 ? 1 : meds.Max(x => x.Id) + 1;
        meds.Add(new MedicationItem
        {
            Id = nextId,
            Name = name,
            Notes = notes,
            ScheduleTimes = scheduleTimes,
            StockCount = 30,
            LastTakenAt = null
        });

        return Results.Json(new { success = true });
    }

    [HttpPost("medications/{id:int}/taken")]
    public IResult TakeMedication([FromRoute] int id)
    {
        var userId = ResolveUserId();
        var meds = MedicationsByUser.GetOrAdd(userId, _ => new List<MedicationItem>());
        var med = meds.FirstOrDefault(x => x.Id == id);
        if (med == null)
        {
            return Results.Json(new { success = false, message = "İlaç bulunamadı" }, statusCode: 404);
        }

        med.LastTakenAt = DateTime.UtcNow;
        med.StockCount = Math.Max(0, med.StockCount - 1);

        return Results.Json(new { success = true, medication = med, stockCount = med.StockCount });
    }

    [HttpGet("family-members")]
    public IResult GetFamilyMembers()
    {
        var userId = ResolveUserId();
        var members = FamilyByUser.GetOrAdd(userId, _ => new List<FamilyMemberItem>());
        return Results.Json(new { success = true, members });
    }

    [HttpPost("family-members")]
    public async Task<IResult> AddFamilyMember()
    {
        var userId = ResolveUserId();
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        var name = "Aile Üyesi";
        var email = "family@test.com";
        var relationship = "Diğer";
        var phoneNumber = "";

        try
        {
            var json = System.Text.Json.JsonDocument.Parse(raw).RootElement;
            name = json.TryGetProperty("name", out var n) ? n.GetString() ?? name : name;
            email = json.TryGetProperty("email", out var e) ? e.GetString() ?? email : email;
            relationship = json.TryGetProperty("relationship", out var r) ? r.GetString() ?? relationship : relationship;
            phoneNumber = json.TryGetProperty("phoneNumber", out var p) ? p.GetString() ?? "" : "";
        }
        catch { }

        var members = FamilyByUser.GetOrAdd(userId, _ => new List<FamilyMemberItem>());
        var nextId = members.Count == 0 ? 1 : members.Max(x => x.Id) + 1;
        members.Add(new FamilyMemberItem
        {
            Id = nextId,
            Name = name,
            Email = email,
            Relationship = relationship,
            PhoneNumber = phoneNumber
        });

        return Results.Json(new { success = true });
    }

    [HttpGet("notifications")]
    public IResult GetNotifications()
    {
        var userId = ResolveUserId();
        var list = NotificationsByUser.GetOrAdd(userId, _ => new List<NotificationItem>());
        return Results.Json(list.OrderByDescending(x => x.Timestamp).Take(100).ToList());
    }

    [HttpPost("send-notification")]
    public async Task<IResult> SendNotification()
    {
        var userId = ResolveUserId();
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        var type = "info";
        var message = "Bildirim";
        var severity = "normal";
        var recipientPhone = string.Empty;
        var location = string.Empty;

        try
        {
            var json = System.Text.Json.JsonDocument.Parse(raw).RootElement;
            type = json.TryGetProperty("type", out var t) ? t.GetString() ?? type : type;
            message = json.TryGetProperty("message", out var m) ? m.GetString() ?? message : message;
            severity = json.TryGetProperty("severity", out var s) ? s.GetString() ?? severity : severity;
            recipientPhone = json.TryGetProperty("phoneNumber", out var p) ? p.GetString() ?? "" : recipientPhone;
            if (string.IsNullOrWhiteSpace(recipientPhone) && json.TryGetProperty("recipientPhone", out var rp)) recipientPhone = rp.GetString() ?? "";
            location = json.TryGetProperty("location", out var l) ? l.GetString() ?? "" : location;
        }
        catch { }

        var list = NotificationsByUser.GetOrAdd(userId, _ => new List<NotificationItem>());
        list.Add(new NotificationItem
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Type = type,
            Message = message,
            Severity = severity,
            Timestamp = DateTime.UtcNow
        });

        var reviewerTestNumber = NormalizePhone(GetReviewerTestPhoneNumber());
        var normalizedRecipient = NormalizePhone(recipientPhone);
        var shouldSimulateSms = IsReviewerModeEnabled()
            || (!string.IsNullOrWhiteSpace(reviewerTestNumber) && normalizedRecipient == reviewerTestNumber);

        return Results.Json(new
        {
            success = true,
            notificationStored = true,
            sms = new
            {
                simulated = shouldSimulateSms,
                dispatched = false,
                recipientPhone = recipientPhone,
                reason = shouldSimulateSms
                    ? "Reviewer mode/test phone number matched. SMS dispatch simulated."
                    : "No SMS provider is wired in this compatibility endpoint; app notification stored only."
            },
            location
        });
    }

    [HttpPost("emergency-alert")]
    public IResult EmergencyAlert() => Results.Json(new { success = true, acknowledged = true });

    [HttpPost("emergency-broadcast")]
    public IResult EmergencyBroadcast() => Results.Json(new { success = true, sent = true });

    [HttpPost("emergency-sms/test")]
    public IResult EmergencySmsTest()
    {
        return Results.Json(new
        {
            success = true,
            simulated = true,
            smsDispatched = false,
            message = "Review güvenli mod: Gerçek SMS gönderilmedi, yalnızca uygulama içi test kaydı oluşturuldu."
        });
    }

    [HttpPost("emergency-sms/dispatch")]
    public async Task<IResult> DispatchEmergencySms()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        var phoneNumber = string.Empty;
        var message = "Emergency assistance requested.";
        var location = string.Empty;

        try
        {
            var json = System.Text.Json.JsonDocument.Parse(raw).RootElement;
            phoneNumber = json.TryGetProperty("phoneNumber", out var p) ? p.GetString() ?? "" : phoneNumber;
            message = json.TryGetProperty("message", out var m) ? m.GetString() ?? message : message;
            location = json.TryGetProperty("location", out var l) ? l.GetString() ?? "" : location;
        }
        catch { }

        var reviewerTestNumber = NormalizePhone(GetReviewerTestPhoneNumber());
        var normalizedPhone = NormalizePhone(phoneNumber);
        var simulate = IsReviewerModeEnabled()
            || (!string.IsNullOrWhiteSpace(reviewerTestNumber) && normalizedPhone == reviewerTestNumber);

        var sid = GetFirstSetting(
            ("Sms:Twilio:AccountSid", "TWILIO_ACCOUNT_SID"),
            ("Twilio:AccountSid", "TWILIO_SID")
        );
        var authToken = GetFirstSetting(
            ("Sms:Twilio:AuthToken", "TWILIO_AUTH_TOKEN"),
            ("Twilio:AuthToken", "TWILIO_TOKEN")
        );
        var fromNumber = GetFirstSetting(
            ("Sms:FromNumber", "TWILIO_PHONE_NUMBER"),
            ("Sms:Twilio:FromNumber", "TWILIO_FROM_NUMBER"),
            ("Twilio:FromNumber", "TWILIO_CALLER_ID")
        );
        var providerConfigured = !string.IsNullOrWhiteSpace(sid)
            && !string.IsNullOrWhiteSpace(authToken)
            && !string.IsNullOrWhiteSpace(fromNumber);

        if (simulate)
        {
            return Results.Json(new
            {
                success = true,
                simulated = true,
                smsDispatched = false,
                phoneNumber,
                location,
                message = "Reviewer mode active: SMS flow simulated, no real Twilio request sent."
            });
        }

        return Results.Json(new
        {
            success = true,
            simulated = false,
            smsDispatched = false,
            providerConfigured,
            phoneNumber,
            location,
            message = providerConfigured
                ? "Twilio credentials are sourced from environment variables. Wire real dispatch here when production SMS is enabled."
                : "Twilio credentials are missing from environment variables; no SMS sent."
        });
    }

    [HttpPost("chat")]
    public async Task<IResult> Chat()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();
        var message = "";
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(raw).RootElement;
            message = json.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
        }
        catch { }

        var response = string.IsNullOrWhiteSpace(message)
            ? "Size nasıl yardımcı olabilirim?"
            : $"Mesajınızı aldım: {message}";

        return Results.Json(new { success = true, response });
    }

    private sealed class MedicationItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Notes { get; set; } = "";
        public List<string> ScheduleTimes { get; set; } = new();
        public int StockCount { get; set; } = 30;
        public DateTime? LastTakenAt { get; set; }
    }

    private sealed class HealthRecordItem
    {
        public string RecordType { get; set; } = "";
        public double Value { get; set; }
        public string Unit { get; set; } = "";
        public string AlertLevel { get; set; } = "normal";
        public DateTime Timestamp { get; set; }
    }

    private sealed class MoodItem
    {
        public int MoodScore { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private sealed class FamilyMemberItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Relationship { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
    }

    private sealed class NotificationItem
    {
        public long Id { get; set; }
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public string Severity { get; set; } = "normal";
        public DateTime Timestamp { get; set; }
    }

    private sealed class SubscriptionItem
    {
        public string Plan { get; set; } = "standard";
        public bool IsActive { get; set; } = true;
        public DateTime ExpiresAt { get; set; }
    }

    private sealed class FamilyAccountItem
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
    }
}
