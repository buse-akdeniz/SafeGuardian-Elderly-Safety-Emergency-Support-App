using Microsoft.AspNetCore.Mvc;
using ilk_projem.Services;

namespace ilk_projem.Controllers;

[ApiController]
[Route("api/mobile-config")]
public sealed class MobileConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public MobileConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public IResult Get()
    {
        return Results.Json(new
        {
            revenueCat = new
            {
                iosApiKey = _configuration["RevenueCat:IosPublicSdkKey"] ?? "",
                androidApiKey = _configuration["RevenueCat:AndroidPublicSdkKey"] ?? "",
                entitlementId = SubscriptionService.PremiumEntitlementId,
                offeringId = "default",
                packageId = "$rc_monthly",
                productId = SubscriptionService.PremiumProductId
            }
        });
    }
}
