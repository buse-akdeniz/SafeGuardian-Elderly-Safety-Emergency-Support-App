(function () {
    const IOS_TEST_ADS = {
        appId: 'ca-app-pub-8847006171122424~5915620356',
        bannerId: 'ca-app-pub-8847006171122424/3861742042',
        rewardVideoId: 'ca-app-pub-8847006171122424/6344435151'
    };

    const state = {
        initialized: false,
        initPromise: null,
        bannerVisible: false
    };

    function getAdMob() {
        const cap = window.Capacitor;
        if (!cap || typeof cap.isNativePlatform !== 'function' || !cap.isNativePlatform()) {
            return null;
        }
        return cap.Plugins?.AdMob || null;
    }

    async function initialize() {
        if (state.initialized) return true;
        if (state.initPromise) return state.initPromise;

        const AdMob = getAdMob();
        if (!AdMob) return false;

        state.initPromise = (async () => {
            try {
                await AdMob.initialize({
                    initializeForTesting: true,
                    maxAdContentRating: 'General'
                });

                try {
                    const tracking = await AdMob.trackingAuthorizationStatus();
                    if (tracking?.status === 'notDetermined') {
                        await AdMob.requestTrackingAuthorization();
                    }
                } catch (trackingError) {
                    console.warn('[AdMob] ATT status alınamadı:', trackingError);
                }

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

        const ok = await initialize();
        if (!ok) return false;

        try {
            await AdMob.showBanner({
                adId: IOS_TEST_ADS.bannerId,
                adSize: 'ADAPTIVE_BANNER',
                position: 'BOTTOM_CENTER',
                margin: 0,
                isTesting: true,
                npa: true
            });
            state.bannerVisible = true;
            return true;
        } catch (error) {
            console.warn('[AdMob] banner gösterilemedi:', error);
            return false;
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
    }

    async function updateByElderlyScreen(screenId) {
        const blockedScreens = new Set([
            'loginScreen',
            'registerScreen',
            'forgotPasswordScreen',
            'onboardingScreen'
        ]);

        if (!screenId || blockedScreens.has(screenId)) {
            await hideBanner();
            return;
        }

        await showBanner();
    }

    async function updateByFamilyView(view) {
        if (view === 'dashboard') {
            await showBanner();
            return;
        }

        await hideBanner();
    }

    async function showRewardedAdUnlock() {
        const AdMob = getAdMob();
        if (!AdMob) return { ok: false, rewarded: false, reason: 'unsupported' };

        const ok = await initialize();
        if (!ok) return { ok: false, rewarded: false, reason: 'init-failed' };

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
        };

        await add('onRewardedVideoAdReward', () => {
            rewarded = true;
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
            await AdMob.prepareRewardVideoAd({
                adId: IOS_TEST_ADS.rewardVideoId,
                isTesting: true,
                npa: true
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
        ids: IOS_TEST_ADS
    };

    // ATT / AdMob izin akışını uygulama ilk açılışında tetikle
    window.addEventListener('load', () => {
        if (!getAdMob()) return;
        initialize().catch(() => {
            // no-op
        });
    }, { once: true });
})();
