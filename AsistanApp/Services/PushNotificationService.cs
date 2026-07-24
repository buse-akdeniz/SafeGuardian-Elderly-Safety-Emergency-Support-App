using System.Text.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ilk_projem.Models.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace ilk_projem.Services;

/// <summary>
/// SafeGuardian AI — Push Notification Service
///
/// Sends push notifications via:
///   iOS     → APNs (Apple Push Notification service)
///   Android → FCM (Firebase Cloud Messaging)
///
/// Setup:
///   1. Firebase Console → Project Settings → Cloud Messaging → Server Key
///      → add to appsettings: Firebase:FcmServerKey
///   2. Apple Developer → Certificates → APNs Auth Key (.p8)
///      → add to appsettings: Apns:TeamId, Apns:KeyId, Apns:PrivateKey
///   3. Store device tokens: POST /api/device/register
///
/// Notification triggers (implement in respective controllers):
///   - Emergency alert triggered
///   - Medication reminder (scheduled)
///   - Health anomaly detected
///   - Family member check-in
///   - Subscription billing issue
/// </summary>
public class PushNotificationService
{
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    // ── Notification categories ──────────────────────────────────────────────
    public static class NotificationTypes
    {
        public const string Emergency        = "EMERGENCY_ALERT";
        public const string MedicationTime   = "MEDICATION_REMINDER";
        public const string HealthAlert      = "HEALTH_ALERT";
        public const string FamilyCheckIn    = "FAMILY_CHECKIN";
        public const string BillingIssue     = "BILLING_ISSUE";
        public const string FeatureHighlight = "FEATURE_HIGHLIGHT";  // retention
        public const string WeeklyReport     = "WEEKLY_REPORT";      // engagement
    }

    public PushNotificationService(ILogger<PushNotificationService> logger, IConfiguration config, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _config = config;
        _http   = httpFactory.CreateClient("push");
    }

    // ── Send to single device ────────────────────────────────────────────────
    public async Task<bool> SendAsync(PushNotification notification)
    {
        var tasks = new List<Task<bool>>();

        if (!string.IsNullOrEmpty(notification.FcmToken))
            tasks.Add(SendFcmAsync(notification));

        if (!string.IsNullOrEmpty(notification.ApnsToken))
            tasks.Add(SendApnsAsync(notification));

        if (tasks.Count == 0)
        {
            _logger.LogDebug("No push tokens for user {UserId}", notification.UserId);
            return false;
        }

        var results = await Task.WhenAll(tasks);
        return results.Any(r => r);
    }

    // ── Emergency broadcast (high priority) ──────────────────────────────────
    public async Task BroadcastEmergencyAsync(string elderlyName, IEnumerable<PushNotification> familyTokens)
    {
        var notification = new PushNotification
        {
            Title    = "🚨 EMERGENCY ALERT",
            Body     = $"{elderlyName} has triggered an emergency. Check the app immediately.",
            Type     = NotificationTypes.Emergency,
            Priority = "high",
            Sound    = "emergency.wav",
            Badge    = 1,
            Data     = new Dictionary<string, string>
            {
                ["action"]     = "open_emergency",
                ["elderlyName"] = elderlyName
            }
        };

        var tasks = familyTokens.Select(t =>
        {
            notification.FcmToken  = t.FcmToken;
            notification.ApnsToken = t.ApnsToken;
            return SendAsync(notification);
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("🚨 Emergency broadcast sent for {Name}", elderlyName);
    }

    // ── Medication reminder ───────────────────────────────────────────────────
    public async Task SendMedicationReminderAsync(string medicationName, string? fcmToken, string? apnsToken)
    {
        await SendAsync(new PushNotification
        {
            Title     = "💊 Medication Reminder",
            Body      = $"Time to take {medicationName}",
            Type      = NotificationTypes.MedicationTime,
            Priority  = "normal",
            FcmToken  = fcmToken,
            ApnsToken = apnsToken,
            Data      = new() { ["action"] = "open_medications" }
        });
    }

    // ── Weekly engagement push (retention) ───────────────────────────────────
    public async Task SendWeeklyReportAsync(string elderlyName, string summary, string? fcmToken, string? apnsToken)
    {
        await SendAsync(new PushNotification
        {
            Title     = $"📊 {elderlyName}'s Weekly Report",
            Body      = summary,
            Type      = NotificationTypes.WeeklyReport,
            Priority  = "normal",
            FcmToken  = fcmToken,
            ApnsToken = apnsToken,
            Data      = new() { ["action"] = "open_health_records" }
        });
    }

    // ── FCM (Android + iOS via Firebase) ─────────────────────────────────────
    private async Task<bool> SendFcmAsync(PushNotification notification)
    {
        var serverKey = _config["Firebase:FcmServerKey"];
        if (string.IsNullOrEmpty(serverKey))
        {
            _logger.LogDebug("FCM not configured — skipping");
            return false;
        }

        try
        {
            var payload = new
            {
                to = notification.FcmToken,
                priority = notification.Priority ?? "normal",
                notification = new
                {
                    title = notification.Title,
                    body  = notification.Body,
                    sound = notification.Sound ?? "default",
                    badge = notification.Badge
                },
                data = notification.Data
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
            request.Headers.TryAddWithoutValidation("Authorization", $"key={serverKey}");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var success  = response.IsSuccessStatusCode;

            if (!success)
                _logger.LogWarning("FCM failed: {Status}", response.StatusCode);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM send error");
            return false;
        }
    }

    // ── APNs (iOS direct) ─────────────────────────────────────────────────────
    // For production, use a proper APNs JWT library (e.g. dotAPNS NuGet package)
    // This is a placeholder showing the correct HTTP/2 structure
    private async Task<bool> SendApnsAsync(PushNotification notification)
    {
        var keyId      = _config["Apns:KeyId"];
        var teamId     = _config["Apns:TeamId"];
        var bundleId   = _config["Apns:BundleId"] ?? "com.safeguardian.app";

        if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(teamId))
        {
            _logger.LogDebug("APNs not configured — using FCM for iOS");
            return false;
        }

        try
        {
            // TODO: Install dotAPNS NuGet and implement proper JWT-signed APNs HTTP/2 request
            // PM> dotnet add package dotAPNS
            // Reference: https://github.com/alexalok/dotAPNS
            _logger.LogDebug("APNs placeholder — install dotAPNS package for production");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "APNs send error");
            return false;
        }
    }
}

public class PushNotification
{
    public string? UserId    { get; set; }
    public string? FcmToken  { get; set; }
    public string? ApnsToken { get; set; }
    public string Title      { get; set; } = "";
    public string Body       { get; set; } = "";
    public string Type       { get; set; } = "";
    public string? Priority  { get; set; } = "normal";
    public string? Sound     { get; set; } = "default";
    public int    Badge      { get; set; } = 0;
    public Dictionary<string, string> Data { get; set; } = new();
}

// ── Device token registration endpoint ───────────────────────────────────────
public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/device/register  →  store FCM/APNs token
        app.MapPost("/api/device/register", async (
            HttpContext ctx,
            ilk_projem.Data.AppDbContext db,
            IDataProtectionProvider dataProtection) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ctx.User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();

            var json      = await JsonDocument.ParseAsync(ctx.Request.Body);
            var fcmToken  = json.RootElement.TryGetProperty("fcmToken",  out var f) ? f.GetString() : null;
            var apnsToken = json.RootElement.TryGetProperty("apnsToken", out var a) ? a.GetString() : null;
            var platform  = json.RootElement.TryGetProperty("platform",  out var p) ? p.GetString() : "unknown";

            var rawToken = string.IsNullOrWhiteSpace(apnsToken) ? fcmToken : apnsToken;
            if (string.IsNullOrWhiteSpace(rawToken))
                return Results.BadRequest(new { success = false, message = "Device token is required." });

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
            var encrypted = dataProtection.CreateProtector("SafeGuardian.DeviceTokens.v1").Protect(rawToken);
            var registration = await db.DeviceRegistrations.SingleOrDefaultAsync(x => x.TokenHash == hash);
            if (registration is null)
            {
                registration = new DeviceRegistration { TokenHash = hash };
                db.DeviceRegistrations.Add(registration);
            }
            registration.UserId = userId;
            registration.EncryptedToken = encrypted;
            registration.Platform = platform ?? "unknown";
            registration.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Json(new { success = true });
        }).RequireAuthorization();

        return app;
    }
}
