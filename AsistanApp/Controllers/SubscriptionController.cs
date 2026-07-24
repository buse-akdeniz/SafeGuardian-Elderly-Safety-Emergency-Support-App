using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AsistanApp.Services;
using ilk_projem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ilk_projem.Controllers;

[ApiController]
[Authorize]
[Route("api/subscription")]
public sealed class SubscriptionController : ControllerBase
{
    private readonly SubscriptionService _subscriptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        SubscriptionService subscriptions,
        IConfiguration configuration,
        ILogger<SubscriptionController> logger)
    {
        _subscriptions = subscriptions;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IResult> Get(CancellationToken cancellationToken)
    {
        var elderlyId = await ResolveElderlyIdAsync();
        if (string.IsNullOrWhiteSpace(elderlyId))
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        var entitlement = await _subscriptions.GetEntitlementAsync(elderlyId, cancellationToken);
        return Results.Json(new
        {
            success = true,
            plan = entitlement.Plan,
            isActive = entitlement.IsActive,
            expiresAt = entitlement.ExpiresAt == DateTime.MinValue
                ? (DateTime?)null
                : entitlement.ExpiresAt,
            willRenew = entitlement.WillRenew,
            isAdUnlockActive = entitlement.IsAdUnlockActive,
            adUnlockUntil = entitlement.AdUnlockUntil,
            hasFullAccess = entitlement.HasFullAccess,
            requiresSubscription = !entitlement.HasFullAccess,
            adsEnabled = !entitlement.IsActive,
            entitlementId = SubscriptionService.PremiumEntitlementId,
            productId = SubscriptionService.PremiumProductId
        });
    }

    [HttpPost("revenuecat-webhook")]
    [AllowAnonymous]
    public async Task<IResult> RevenueCatWebhook(CancellationToken cancellationToken)
    {
        var expectedKey = _configuration["RevenueCat:WebhookAuthKey"]
            ?? Environment.GetEnvironmentVariable("REVENUECAT_WEBHOOK_AUTH_KEY")
            ?? "";
        var suppliedKey = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            _logger.LogError("RevenueCat webhook rejected because no webhook secret is configured.");
            return Results.Json(new { success = false, message = "Webhook is not configured" }, statusCode: 503);
        }

        if (!SecureEquals(suppliedKey, expectedKey)
            && !SecureEquals(suppliedKey, $"Bearer {expectedKey}"))
        {
            _logger.LogWarning(
                "RevenueCat webhook rejected from {IP}",
                HttpContext.Connection.RemoteIpAddress);
            return Results.Json(new { success = false, message = "Unauthorized" }, statusCode: 401);
        }

        JsonDocument payload;
        try
        {
            payload = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return Results.Json(new { success = false, message = "Invalid JSON" }, statusCode: 400);
        }

        using (payload)
        {
            var result = await _subscriptions.HandleRevenueCatWebhookAsync(
                payload.RootElement,
                cancellationToken);

            if (!result.Success)
                return Results.Json(new { success = false, reason = result.Reason }, statusCode: 400);

            return Results.Json(new
            {
                success = true,
                duplicate = result.IsDuplicate,
                ignored = result.IsIgnored,
                eventType = result.EventType,
                appUserId = result.AppUserId,
                reason = result.Reason
            });
        }
    }

    [HttpPost("cancel")]
    public async Task<IResult> Cancel()
    {
        var elderlyId = await ResolveElderlyIdAsync();
        if (string.IsNullOrWhiteSpace(elderlyId))
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        return Results.Json(new
        {
            success = true,
            message = "Abonelik Apple veya Google hesabınızın abonelik ayarlarından yönetilir."
        });
    }

    private Task<string?> ResolveElderlyIdAsync()
    {
        var elderlyId = User.FindFirstValue("elderly_id");
        return Task.FromResult<string?>(string.IsNullOrWhiteSpace(elderlyId) ? null : elderlyId);
    }

    private static bool SecureEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left ?? "");
        var rightBytes = Encoding.UTF8.GetBytes(right ?? "");
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
