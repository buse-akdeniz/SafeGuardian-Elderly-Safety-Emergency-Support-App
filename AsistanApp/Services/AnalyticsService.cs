using System.Text.Json;

namespace ilk_projem.Services;

/// <summary>
/// SafeGuardian AI — Analytics Event Service
///
/// Firebase Analytics-compatible server-side event pipeline.
/// Mobile SDK events (Firebase) are collected client-side.
/// This service tracks server-side events for funnel & revenue analysis.
///
/// Dashboard: Firebase Console → Analytics → Events
/// Funnel:    onboarding_start → onboarding_complete → paywall_shown → purchase
/// </summary>
public class AnalyticsService
{
    private readonly ILogger<AnalyticsService> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    // ── Standard event names (match Firebase naming convention) ─────────────
    public static class Events
    {
        // Acquisition
        public const string AppOpen           = "app_open";
        public const string OnboardingStart   = "onboarding_start";
        public const string OnboardingStep    = "onboarding_step";
        public const string OnboardingComplete = "onboarding_complete";
        public const string SignUp            = "sign_up";
        public const string Login             = "login";

        // Engagement
        public const string MedicationAdded   = "medication_added";
        public const string MedicationTaken   = "medication_taken";
        public const string EmergencyTriggered = "emergency_triggered";
        public const string FamilyMemberAdded = "family_member_added";
        public const string HealthRecordAdded = "health_record_added";
        public const string MoodChecked       = "mood_checked";
        public const string AiAnalysisViewed  = "ai_analysis_viewed";
        public const string VoiceCommandUsed  = "voice_command_used";

        // Monetization (Apple recommended names)
        public const string PaywallShown      = "paywall_shown";
        public const string PaywallDismissed  = "paywall_dismissed";
        public const string PurchaseStarted   = "purchase_started";
        public const string Purchase          = "purchase";           // revenue event
        public const string SubscriptionStart = "subscription_start";
        public const string SubscriptionCancel= "subscription_cancel";
        public const string AdShown           = "ad_impression";
        public const string AdClicked         = "ad_click";
        public const string RewardedAdComplete= "rewarded_ad_complete";

        // Retention
        public const string PushEnabled       = "push_notification_enabled";
        public const string PushOpened        = "push_notification_opened";
        public const string FeatureLocked     = "feature_locked_shown";  // gated feature hit
    }

    public AnalyticsService(ILogger<AnalyticsService> logger, IConfiguration config, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _config = config;
        _http   = httpFactory.CreateClient("analytics");
    }

    // ── Track event ──────────────────────────────────────────────────────────
    public async Task TrackAsync(string eventName, string? userId = null, Dictionary<string, object>? parameters = null)
    {
        try
        {
            var evt = new AnalyticsEvent
            {
                Name       = eventName,
                UserId     = userId,
                Parameters = parameters ?? new(),
                Timestamp  = DateTime.UtcNow
            };

            // Log locally (always)
            _logger.LogInformation("📊 Analytics: {Event} userId={UserId} params={Params}",
                eventName, userId ?? "anon", JsonSerializer.Serialize(evt.Parameters));

            // Send to Firebase Measurement Protocol (optional — requires Firebase API key)
            await SendToFirebaseAsync(evt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Analytics track failed — non-critical, continuing");
        }
    }

    // ── Track purchase (revenue event) ───────────────────────────────────────
    public async Task TrackPurchaseAsync(string userId, string planId, string currency, double value, string transactionId)
    {
        await TrackAsync(Events.Purchase, userId, new()
        {
            ["currency"]        = currency,
            ["value"]           = value,
            ["transaction_id"]  = transactionId,
            ["item_id"]         = planId,
            ["item_name"]       = $"SafeGuardian {planId}",
            ["item_category"]   = "subscription"
        });
    }

    // ── Track paywall shown ──────────────────────────────────────────────────
    public async Task TrackPaywallAsync(string? userId, string trigger, string planShown)
    {
        await TrackAsync(Events.PaywallShown, userId, new()
        {
            ["trigger"]    = trigger,   // e.g. "feature_gate", "onboarding", "menu"
            ["plan_shown"] = planShown
        });
    }

    // ── Firebase Measurement Protocol v2 ────────────────────────────────────
    private async Task SendToFirebaseAsync(AnalyticsEvent evt)
    {
        var apiSecret  = _config["Firebase:MeasurementApiSecret"];
        var measurementId = _config["Firebase:MeasurementId"];

        if (string.IsNullOrEmpty(apiSecret) || string.IsNullOrEmpty(measurementId))
            return; // Not configured — skip silently

        var url  = $"https://www.google-analytics.com/mp/collect?measurement_id={measurementId}&api_secret={apiSecret}";
        var body = new
        {
            client_id  = evt.UserId ?? "server",
            user_id    = evt.UserId,
            timestamp_micros = new DateTimeOffset(evt.Timestamp).ToUnixTimeMilliseconds() * 1000,
            events = new[]
            {
                new
                {
                    name   = evt.Name,
                    @params = evt.Parameters
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
        await _http.PostAsync(url, content);
    }
}

public class AnalyticsEvent
{
    public string Name { get; set; } = "";
    public string? UserId { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
