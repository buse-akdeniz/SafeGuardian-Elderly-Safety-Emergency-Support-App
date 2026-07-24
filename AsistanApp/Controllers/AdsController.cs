using ilk_projem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ilk_projem.Controllers;

[ApiController]
[Route("api/ads")]
public sealed class AdsController : ControllerBase
{
    private readonly AdMobSsvVerifier _verifier;
    private readonly SubscriptionService _subscriptions;
    private readonly ILogger<AdsController> _logger;

    public AdsController(
        AdMobSsvVerifier verifier,
        SubscriptionService subscriptions,
        ILogger<AdsController> logger)
    {
        _verifier = verifier;
        _subscriptions = subscriptions;
        _logger = logger;
    }

    [HttpGet("reward-ssv")]
    public async Task<IResult> RewardSsv(CancellationToken cancellationToken)
    {
        VerifiedAdReward? reward;
        try
        {
            reward = await _verifier.VerifyAsync(Request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdMob SSV verification failed unexpectedly.");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        if (reward is null)
        {
            _logger.LogWarning(
                "Rejected invalid AdMob SSV callback from {IP}",
                HttpContext.Connection.RemoteIpAddress);
            return Results.StatusCode(StatusCodes.Status400BadRequest);
        }

        var applied = await _subscriptions.ApplyVerifiedAdRewardAsync(reward, cancellationToken);
        return Results.Ok(new { success = true, duplicate = !applied });
    }
}
