namespace ilk_projem.Services;

/// <summary>
/// AdMob / Google Mobile Ads Integration Service
///
/// Free tier kullanıcılar için reklam gösterimi.
/// Premium/Family planındakiler reklamsız deneyim yaşar.
///
/// Setup:
///   1. Google AdMob hesabı: https://admob.google.com
///   2. iOS App ID → Info.plist  GADApplicationIdentifier
///   3. Android App ID → AndroidManifest.xml  com.google.android.gms.ads.APPLICATION_ID
///   4. Ad unit ID'leri aşağıya girin
///
/// Mobile tarafta:
///   - iOS:     pod 'Google-Mobile-Ads-SDK'
///   - Android: implementation 'com.google.android.gms:play-services-ads:23.x.x'
///   - Capacitor: npm install @capacitor-community/admob
/// </summary>
public static class AdService
{
    // ── Ad Unit IDs ─────────────────────────────────────────────────────────
    // Production: replace with real IDs from AdMob dashboard
    // Test IDs (safe during development):
    public static class AdUnitIds
    {
        public static class Ios
        {
            public const string AppId           = "ca-app-pub-XXXXXXXXXXXXXXXX~YYYYYYYYYY"; // → Info.plist
            public const string Banner          = "ca-app-pub-3940256099942544/2934735716"; // test
            public const string Interstitial    = "ca-app-pub-3940256099942544/4411468910"; // test
            public const string Rewarded        = "ca-app-pub-3940256099942544/1712485313"; // test
            public const string NativeAdvanced  = "ca-app-pub-3940256099942544/3986624511"; // test
        }

        public static class Android
        {
            public const string AppId           = "ca-app-pub-XXXXXXXXXXXXXXXX~ZZZZZZZZZZ"; // → AndroidManifest.xml
            public const string Banner          = "ca-app-pub-3940256099942544/6300978111"; // test
            public const string Interstitial    = "ca-app-pub-3940256099942544/1033173712"; // test
            public const string Rewarded        = "ca-app-pub-3940256099942544/5224354917"; // test
            public const string NativeAdvanced  = "ca-app-pub-3940256099942544/2247696110"; // test
        }
    }

    // ── Ad placement strategy ────────────────────────────────────────────────
    // Show ads only to Free tier users, never to Premium/Family
    public static AdConfig GetConfigForPlan(string planId) => planId switch
    {
        "premium" => AdConfig.NoAds,
        "family"  => AdConfig.NoAds,
        _         => AdConfig.FreeConfig   // "free" or unknown
    };

    // ── Revenue optimization tips (implement in mobile) ──────────────────────
    // 1. Banner at home screen bottom — low intrusiveness, always visible
    // 2. Interstitial after 3rd medication check — high CPM placement
    // 3. Rewarded ad before AI analysis — user gets premium feature for 24h
    // 4. Native ad in health records list — blends naturally
    //
    // Target CPM estimates (healthcare vertical — very high value):
    //   Banner:       $2-8 CPM
    //   Interstitial: $15-40 CPM
    //   Rewarded:     $20-60 CPM  ← best for elder care apps
    //
    // Combine with upgrade prompt after rewarded ad for conversion funnel
}

public class AdConfig
{
    public bool   ShowBanner       { get; init; }
    public bool   ShowInterstitial { get; init; }
    public bool   ShowRewarded     { get; init; }
    public int    InterstitialFreq { get; init; } = 3;  // every N actions
    public string BannerPosition   { get; init; } = "bottom";

    public static readonly AdConfig NoAds = new()
    {
        ShowBanner       = false,
        ShowInterstitial = false,
        ShowRewarded     = false
    };

    public static readonly AdConfig FreeConfig = new()
    {
        ShowBanner       = true,
        ShowInterstitial = true,
        ShowRewarded     = true,
        InterstitialFreq = 4,
        BannerPosition   = "bottom"
    };
}

/// <summary>
/// Endpoint: GET /api/ads/config
/// Mobile app calls this on startup to get ad configuration for current user.
/// </summary>
public static class AdEndpoints
{
    public static IEndpointRouteBuilder MapAdEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ads/config", async (HttpContext ctx, SubscriptionService subSvc) =>
        {
            var elderlyId = ctx.User.FindFirst("elderly_id")?.Value ?? "";
            var entitlement = await subSvc.GetEntitlementAsync(elderlyId);
            var planId = entitlement.Plan;

            var config = AdService.GetConfigForPlan(planId);

            return Results.Json(new
            {
                success         = true,
                planId,
                showBanner      = config.ShowBanner,
                showInterstitial= config.ShowInterstitial,
                showRewarded    = config.ShowRewarded,
                interstitialFreq= config.InterstitialFreq,
                bannerPosition  = config.BannerPosition,
                adUnits = new
                {
                    ios     = new { banner = AdService.AdUnitIds.Ios.Banner,     interstitial = AdService.AdUnitIds.Ios.Interstitial,     rewarded = AdService.AdUnitIds.Ios.Rewarded },
                    android = new { banner = AdService.AdUnitIds.Android.Banner, interstitial = AdService.AdUnitIds.Android.Interstitial, rewarded = AdService.AdUnitIds.Android.Rewarded }
                }
            });
        }).RequireAuthorization();

        return app;
    }
}
