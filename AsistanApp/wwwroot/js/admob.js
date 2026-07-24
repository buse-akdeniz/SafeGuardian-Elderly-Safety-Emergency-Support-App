(function () {
    const IOS_ADS = {
        appId: 'ca-app-pub-8847006171122424~5915620356',
        bannerId: 'ca-app-pub-8847006171122424/3861742042',
        rewardVideoId: 'ca-app-pub-8847006171122424/6344435151'
    };

    // Play iç test için Google test ID; prod öncesi AdMob'dan Android birimlerini al
    const ANDROID_ADS = {
        appId: 'ca-app-pub-3940256099942544~3347511713',
        bannerId: 'ca-app-pub-3940256099942544/6300978111',
        rewardVideoId: 'ca-app-pub-3940256099942544/5224354917'
    };

    function getPlatformAds() {
        const platform = window.Capacitor?.getPlatform?.() || 'web';
        return platform === 'android' ? ANDROID_ADS : IOS_ADS;
    }

    function getActiveAds() {
        return getPlatformAds();
    }

    const state = {
        initialized: false,
        initPromise: null,
        bannerVisible: false,
        bannerLoading: false,
        rewardedAdShowing: false,
        consentReady: false,
        consentPromise: null
    };

    const AD_BANNER_CLASS = 'ad-banner-visible';

    function getBannerOffsetPx() {
        const width = Number(window.innerWidth || 0);
        if (width >= 1024) return 110;
        if (width >= 768) return 100;
        return 70;
    }

    function updateBannerLayout(isVisible) {
        const root = document.documentElement;
        const body = document.body;
        if (!root || !body) return;

        if (isVisible) {
            root.style.setProperty('--sg-ad-banner-offset', `${getBannerOffsetPx()}px`);
            body.classList.add(AD_BANNER_CLASS);
            return;
        }

        root.style.setProperty('--sg-ad-banner-offset', '0px');
        body.classList.remove(AD_BANNER_CLASS);
    }

    function getAdMob() {
        const cap = window.Capacitor;
        if (!cap || typeof cap.isNativePlatform !== 'function' || !cap.isNativePlatform()) {
            return null;
        }
        return cap.Plugins?.AdMob || null;
    }

    async function ensureConsent(force = false) {
        const AdMob = getAdMob();
        if (!AdMob?.requestConsentInfo) return true;
        if (state.consentReady && !force) return true;
        if (state.consentPromise && !force) return state.consentPromise;

        state.consentPromise = (async () => {
            try {
                if (force && AdMob.resetConsentInfo) {
                    await AdMob.resetConsentInfo();
                }
                let consent = await AdMob.requestConsentInfo();
                if (consent?.status === 'REQUIRED' && consent?.isConsentFormAvailable) {
                    consent = await AdMob.showConsentForm();
                }
                state.consentReady = consent?.status === 'OBTAINED'
                    || consent?.status === 'NOT_REQUIRED';
                return state.consentReady;
            } catch (error) {
                console.warn('[AdMob] UMP consent flow failed:', error);
                state.consentReady = false;
                return false;
            } finally {
                state.consentPromise = null;
            }
        })();
        return state.consentPromise;
    }

    async function initialize() {
        if (state.initialized) return true;
        if (state.initPromise) return state.initPromise;

        const AdMob = getAdMob();
        if (!AdMob) return false;

        state.initPromise = (async () => {
            try {
                const consentReady = await ensureConsent();
                if (!consentReady) return false;
                await AdMob.initialize({
                    initializeForTesting: !(window.SafeGuardianProd?.isProductionApp?.() === true),
                    maxAdContentRating: 'General'
                });

                state.initialized = true;
                return true;
            } catch (error) {
                console.warn('[AdMob] initialize başarısız:', error);
                return false;
            } finally {
                state.initPromise = null;
            }
        })();

        return state.initPromise;
    }

    async function showBanner() {
        const AdMob = getAdMob();
        if (!AdMob) return false;
        if (state.bannerVisible || state.bannerLoading) {
            updateBannerLayout(true);
            return true;
        }

        const ok = await initialize();
        if (!ok) return false;

        const ads = getActiveAds();
        state.bannerLoading = true;
        try {
            await AdMob.showBanner({
                adId: ads.bannerId,
                adSize: 'ADAPTIVE_BANNER',
                position: 'BOTTOM_CENTER',
                margin: 0,
                isTesting: !(window.SafeGuardianProd?.isProductionApp?.() === true),
                npa: true
            });
            state.bannerVisible = true;
            updateBannerLayout(true);
            return true;
        } catch (error) {
            console.warn('[AdMob] banner gösterilemedi:', error);
            updateBannerLayout(false);
            return false;
        } finally {
            state.bannerLoading = false;
        }
    }

    async function hideBanner() {
        const AdMob = getAdMob();
        if (!AdMob || !state.bannerVisible) return;

        try {
            await AdMob.hideBanner();
        } catch (error) {
            console.warn('[AdMob] banner gizlenemedi:', error);
        }
        state.bannerVisible = false;
        updateBannerLayout(false);
    }

    function hasPremiumAccess() {
        try {
            const plan = String(localStorage.getItem('userPlan') || '').toLowerCase();
            const endRaw = localStorage.getItem('subscriptionEnd') || '';
            const adUnlockRaw = localStorage.getItem('adUnlockUntil') || '';
            const now = Date.now();
            if (plan === 'premium' && endRaw) {
                const end = new Date(endRaw).getTime();
                if (!Number.isNaN(end) && end > now) return true;
            }
            if (adUnlockRaw) {
                const unlock = new Date(adUnlockRaw).getTime();
                if (!Number.isNaN(unlock) && unlock > now) return true;
            }
        } catch (_) { /* ignore */ }
        return false;
    }

    async function updateByElderlyScreen(screenId) {
        const blockedScreens = new Set([
            'loginScreen',
            'registerScreen',
            'forgotPasswordScreen',
            'onboardingScreen',
            'subscriptionScreen'
        ]);

        if (!screenId || blockedScreens.has(screenId) || hasPremiumAccess()) {
            await hideBanner();
            return;
        }

        await showBanner();
    }

    async function updateByFamilyView(view) {
        if (view === 'dashboard' && !hasPremiumAccess()) {
            await showBanner();
            return;
        }

        await hideBanner();
    }

    async function showRewardedAdUnlock() {
        const AdMob = getAdMob();
        if (!AdMob) return { ok: false, rewarded: false, reason: 'unsupported' };
        if (state.rewardedAdShowing) return { ok: false, rewarded: false, reason: 'already-showing' };

        const ok = await initialize();
        if (!ok) return { ok: false, rewarded: false, reason: 'init-failed' };

        state.rewardedAdShowing = true;
        let rewarded = false;
        let completed = false;
        const handles = [];

        const add = async (eventName, callback) => {
            try {
                const handle = await AdMob.addListener(eventName, callback);
                if (handle) handles.push(handle);
            } catch {
                // ignore listener failures
            }
        };

        const cleanup = async () => {
            for (const handle of handles) {
                try {
                    await handle.remove();
                } catch {
                    // ignore
                }
            }
            state.rewardedAdShowing = false;
        };

        await add('onRewardedVideoAdReward', () => {
            rewarded = true;
            // do NOT set completed here — wait for dismiss event
        });
        await add('onRewardedVideoAdDismissed', () => {
            completed = true;
        });
        await add('onRewardedVideoAdFailedToShow', () => {
            completed = true;
        });
        await add('onRewardedVideoAdFailedToLoad', () => {
            completed = true;
        });

        try {
            const ads = getActiveAds();
            const userId = String(localStorage.getItem('userId') || '').trim();
            if (!userId || userId === 'demo-user') {
                await cleanup();
                return { ok: false, rewarded: false, reason: 'missing-user-id' };
            }
            await AdMob.prepareRewardVideoAd({
                adId: ads.rewardVideoId,
                isTesting: !(window.SafeGuardianProd?.isProductionApp?.() === true),
                npa: true,
                ssv: {
                    userId,
                    customData: 'safeguardian-12h'
                }
            });
            await AdMob.showRewardVideoAd();
        } catch (error) {
            console.warn('[AdMob] reward video gösterilemedi:', error);
            await cleanup();
            return { ok: false, rewarded: false, reason: 'show-failed' };
        }

        const startedAt = Date.now();
        while (!completed && Date.now() - startedAt < 90000) {
            await new Promise(resolve => setTimeout(resolve, 250));
        }

        await cleanup();
        return { ok: true, rewarded };
    }

    window.SafeGuardianAds = {
        initialize,
        showBanner,
        hideBanner,
        updateByElderlyScreen,
        updateByFamilyView,
        showRewardedAdUnlock,
        showPrivacyChoices: () => ensureConsent(true),
        ids: getActiveAds(),
        getPlatformAds
    };

    // ATT / AdMob izin akışını uygulama ilk açılışında tetikle
    window.addEventListener('load', () => {
        if (!getAdMob()) return;
        initialize().catch(() => {
            // no-op
        });
    }, { once: true });

    window.addEventListener('resize', () => {
        if (!state.bannerVisible) return;
        updateBannerLayout(true);
    });
})();
