using System.Text.Json;
using ilk_projem.Data;
using ilk_projem.Models.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ilk_projem.Services;

public sealed class SubscriptionService
{
    public const string PremiumProductId = "com.buseakdeniz.safeguardian.sub_family_monthly_v2";
    public const string PremiumEntitlementId = "premium";

    private static readonly HashSet<string> ActivationEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "INITIAL_PURCHASE",
        "RENEWAL",
        "UNCANCELLATION",
        "PRODUCT_CHANGE",
        "NON_RENEWING_PURCHASE"
    };

    private readonly AppDbContext _db;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(AppDbContext db, ILogger<SubscriptionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SubscriptionEntitlement> GetEntitlementAsync(
        string elderlyId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var subscription = await _db.Subscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.ElderlyId == elderlyId, cancellationToken);

        if (subscription is null)
            return SubscriptionEntitlement.Free();

        var premiumActive = subscription.IsActive
            && subscription.PlanId == PremiumEntitlementId
            && subscription.ExpiresAt > now;
        var adUnlockActive = subscription.AdUnlockUntil is { } adUntil && adUntil > now;

        return new SubscriptionEntitlement(
            premiumActive ? PremiumEntitlementId : "free",
            premiumActive,
            subscription.ExpiresAt,
            subscription.WillRenew,
            adUnlockActive,
            subscription.AdUnlockUntil,
            premiumActive || adUnlockActive,
            subscription.ProductId);
    }

    public async Task<RevenueCatWebhookResult> HandleRevenueCatWebhookAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        if (!payload.TryGetProperty("event", out var revenueEvent))
            return RevenueCatWebhookResult.Ignored("missing_event");

        var eventId = GetString(revenueEvent, "id");
        var eventType = GetString(revenueEvent, "type").ToUpperInvariant();
        var appUserId = GetString(revenueEvent, "app_user_id");
        var productId = GetString(revenueEvent, "product_id");

        if (string.IsNullOrWhiteSpace(eventType))
            return RevenueCatWebhookResult.Ignored("missing_event_type");

        if (eventType is "TEST" or "TRANSFER")
            return RevenueCatWebhookResult.Accepted(eventType, appUserId);

        if (string.IsNullOrWhiteSpace(eventId))
            return RevenueCatWebhookResult.Rejected("missing_event_id");

        if (await _db.RevenueCatEvents.AnyAsync(e => e.EventId == eventId, cancellationToken))
            return RevenueCatWebhookResult.Duplicate(eventId, eventType);

        if (string.IsNullOrWhiteSpace(appUserId) || appUserId.StartsWith("$RCAnonymousID:", StringComparison.Ordinal))
            return RevenueCatWebhookResult.Rejected("unmapped_app_user_id");
        if (!await _db.Users.AnyAsync(
                u => u.Id == appUserId && u.AccountType == "Elderly",
                cancellationToken))
            return RevenueCatWebhookResult.Rejected("unknown_app_user_id");

        if (!string.IsNullOrWhiteSpace(productId)
            && !string.Equals(productId, PremiumProductId, StringComparison.Ordinal))
            return RevenueCatWebhookResult.Rejected("unknown_product");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var subscription = await _db.Subscriptions
                .SingleOrDefaultAsync(s => s.ElderlyId == appUserId, cancellationToken);

            subscription ??= new StoredSubscription
            {
                ElderlyId = appUserId,
                CreatedAt = DateTime.UtcNow
            };

            if (subscription.Id == 0)
                _db.Subscriptions.Add(subscription);

            ApplyEvent(subscription, revenueEvent, eventType, productId);

            _db.RevenueCatEvents.Add(new StoredRevenueCatEvent
            {
                EventId = eventId,
                EventType = eventType,
                AppUserId = appUserId,
                ProductId = productId,
                ProcessedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "RevenueCat event processed: {EventType} {EventId} user={AppUserId}",
                eventType, eventId, appUserId);
            return RevenueCatWebhookResult.Accepted(eventType, appUserId);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (await _db.RevenueCatEvents.AnyAsync(e => e.EventId == eventId, cancellationToken))
                return RevenueCatWebhookResult.Duplicate(eventId, eventType);

            _logger.LogError(ex, "RevenueCat event persistence failed: {EventId}", eventId);
            throw;
        }
    }

    public async Task<bool> HasPremiumAccessAsync(
        string elderlyId,
        CancellationToken cancellationToken = default)
    {
        var entitlement = await GetEntitlementAsync(elderlyId, cancellationToken);
        return entitlement.HasFullAccess;
    }

    public async Task GrantAdUnlockAsync(
        string elderlyId,
        DateTime unlockUntil,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _db.Subscriptions
            .SingleOrDefaultAsync(s => s.ElderlyId == elderlyId, cancellationToken);

        subscription ??= new StoredSubscription
        {
            ElderlyId = elderlyId,
            PlanId = "free",
            CreatedAt = DateTime.UtcNow
        };

        if (subscription.Id == 0)
            _db.Subscriptions.Add(subscription);

        subscription.AdUnlockUntil = unlockUntil;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ApplyVerifiedAdRewardAsync(
        VerifiedAdReward reward,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Users.AnyAsync(
                u => u.Id == reward.ElderlyId && u.AccountType == "Elderly",
                cancellationToken))
            return false;
        if (await _db.AdRewardTransactions
                .AnyAsync(r => r.TransactionId == reward.TransactionId, cancellationToken))
            return false;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var subscription = await _db.Subscriptions
                .SingleOrDefaultAsync(s => s.ElderlyId == reward.ElderlyId, cancellationToken);
            subscription ??= new StoredSubscription
            {
                ElderlyId = reward.ElderlyId,
                PlanId = "free",
                CreatedAt = now
            };

            if (subscription.Id == 0)
                _db.Subscriptions.Add(subscription);

            var unlockFrom = subscription.AdUnlockUntil is { } current && current > now
                ? current
                : now;
            subscription.AdUnlockUntil = unlockFrom.AddHours(12);
            subscription.UpdatedAt = now;

            _db.AdRewardTransactions.Add(new StoredAdRewardTransaction
            {
                TransactionId = reward.TransactionId,
                ElderlyId = reward.ElderlyId,
                AdUnitId = reward.AdUnitId,
                RewardAmount = reward.RewardAmount,
                VerifiedAt = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    private static void ApplyEvent(
        StoredSubscription subscription,
        JsonElement revenueEvent,
        string eventType,
        string productId)
    {
        var now = DateTime.UtcNow;
        var expiration = GetUnixMilliseconds(revenueEvent, "expiration_at_ms");
        var purchased = GetUnixMilliseconds(revenueEvent, "purchased_at_ms");

        subscription.EntitlementId = PremiumEntitlementId;
        subscription.ProductId = string.IsNullOrWhiteSpace(productId)
            ? subscription.ProductId
            : productId;
        subscription.Platform = GetString(revenueEvent, "store").ToLowerInvariant() switch
        {
            "app_store" => "ios",
            "play_store" => "android",
            var store => store
        };
        subscription.TransactionId = GetString(revenueEvent, "transaction_id");
        subscription.OriginalTransactionId = GetString(revenueEvent, "original_transaction_id");
        subscription.Environment = GetString(revenueEvent, "environment");
        subscription.UpdatedAt = now;

        if (ActivationEvents.Contains(eventType))
        {
            if (expiration is null)
                throw new InvalidOperationException("RevenueCat activation event is missing expiration_at_ms.");

            subscription.PlanId = PremiumEntitlementId;
            subscription.IsActive = expiration > now;
            subscription.WillRenew = true;
            subscription.ExpiresAt = expiration.Value;
            subscription.CancelledAt = null;
            if (purchased is { } purchasedAt && subscription.CreatedAt > purchasedAt)
                subscription.CreatedAt = purchasedAt;
            return;
        }

        switch (eventType)
        {
            case "CANCELLATION":
                subscription.WillRenew = false;
                subscription.CancelledAt = now;
                if (expiration is { } cancellationExpiry)
                    subscription.ExpiresAt = cancellationExpiry;
                subscription.IsActive = subscription.ExpiresAt > now;
                break;

            case "EXPIRATION":
                subscription.IsActive = false;
                subscription.WillRenew = false;
                subscription.ExpiresAt = expiration ?? now;
                break;

            case "BILLING_ISSUE":
                subscription.WillRenew = false;
                if (expiration is { } billingExpiry)
                    subscription.ExpiresAt = billingExpiry;
                subscription.IsActive = subscription.ExpiresAt > now;
                break;
        }
    }

    private static string GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static DateTime? GetUnixMilliseconds(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;

        long milliseconds;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out milliseconds))
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out milliseconds))
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        return null;
    }
}

public sealed record SubscriptionEntitlement(
    string Plan,
    bool IsActive,
    DateTime ExpiresAt,
    bool WillRenew,
    bool IsAdUnlockActive,
    DateTime? AdUnlockUntil,
    bool HasFullAccess,
    string ProductId)
{
    public static SubscriptionEntitlement Free() =>
        new("free", false, DateTime.MinValue, false, false, null, false, "");
}

public sealed record RevenueCatWebhookResult(
    bool Success,
    bool IsDuplicate,
    bool IsIgnored,
    string EventType,
    string AppUserId,
    string Reason)
{
    public static RevenueCatWebhookResult Accepted(string eventType, string appUserId) =>
        new(true, false, false, eventType, appUserId, "");
    public static RevenueCatWebhookResult Duplicate(string eventId, string eventType) =>
        new(true, true, false, eventType, "", eventId);
    public static RevenueCatWebhookResult Ignored(string reason) =>
        new(true, false, true, "", "", reason);
    public static RevenueCatWebhookResult Rejected(string reason) =>
        new(false, false, false, "", "", reason);
}
