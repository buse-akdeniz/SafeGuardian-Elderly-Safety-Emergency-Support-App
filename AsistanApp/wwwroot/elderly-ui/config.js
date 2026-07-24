// Production API endpoint - enforced for App Store compliance
window.API_BASE = 'https://safeguardian-elderly-safety-emergency-support-ap-production.up.railway.app';
window.API_FALLBACK_BASE = 'https://safeguardian-elderly-safety-emergency-support-ap-production.up.railway.app';

// RevenueCat public SDK keys are safe to ship in the application. They are not
// secret REST API keys. Set these from RevenueCat > Project > API keys.
window.REVENUECAT_CONFIG = Object.freeze({
    iosApiKey: '',
    androidApiKey: '',
    entitlementId: 'premium',
    offeringId: 'default',
    packageId: '$rc_monthly',
    productId: 'com.buseakdeniz.safeguardian.sub_family_monthly_v2'
});
