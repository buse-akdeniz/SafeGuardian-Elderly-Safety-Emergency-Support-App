(function () {
    'use strict';

    let config = { ...(window.REVENUECAT_CONFIG || {}) };
    const state = {
        configured: false,
        configurePromise: null,
        appUserId: null,
        activePackage: null,
        customerInfo: null,
        listenerAttached: false
    };

    function platform() {
        return window.Capacitor?.getPlatform?.() || 'web';
    }

    function plugin() {
        return window.Capacitor?.Plugins?.Purchases || null;
    }

    function apiKey() {
        if (platform() === 'ios') return String(config.iosApiKey || '').trim();
        if (platform() === 'android') return String(config.androidApiKey || '').trim();
        return '';
    }

    function isNativeSupported() {
        return Boolean(plugin() && (platform() === 'ios' || platform() === 'android'));
    }

    async function resolveApiKey() {
        if (apiKey()) return apiKey();
        const apiBase = String(window.API_BASE || '').replace(/\/$/, '');
        if (!apiBase) return '';
        try {
            const response = await fetch(`${apiBase}/api/mobile-config`);
            if (!response.ok) return '';
            const payload = await response.json();
            config = { ...config, ...(payload?.revenueCat || {}) };
            return apiKey();
        } catch (error) {
            console.warn('[RevenueCat] Mobile configuration could not be loaded:', error);
            return '';
        }
    }

    function entitlementFrom(customerInfo) {
        const entitlement = customerInfo?.entitlements?.active?.[config.entitlementId];
        if (!entitlement?.isActive) return null;
        if (entitlement.productIdentifier !== config.productId) return null;
        return entitlement;
    }

    function hasPremium(customerInfo) {
        return Boolean(entitlementFrom(customerInfo));
    }

    function emitCustomerInfo(customerInfo) {
        state.customerInfo = customerInfo || null;
        window.dispatchEvent(new CustomEvent('safeguardian:customer-info', {
            detail: {
                customerInfo: state.customerInfo,
                entitlement: entitlementFrom(state.customerInfo),
                hasPremium: hasPremium(state.customerInfo)
            }
        }));
    }

    async function configure(appUserId) {
        if (!isNativeSupported()) return false;
        const resolvedApiKey = await resolveApiKey();
        if (!resolvedApiKey) throw new Error('REVENUECAT_PUBLIC_SDK_KEY_MISSING');
        const normalizedUserId = String(appUserId || '').trim() || null;

        if (!state.configurePromise) {
            state.configurePromise = (async () => {
                await plugin().configure({
                    apiKey: resolvedApiKey,
                    appUserID: normalizedUserId,
                    shouldShowInAppMessagesAutomatically: true,
                    diagnosticsEnabled: false
                });
                state.configured = true;
                state.appUserId = normalizedUserId;

                if (!state.listenerAttached && plugin().addCustomerInfoUpdateListener) {
                    state.listenerAttached = true;
                    await plugin().addCustomerInfoUpdateListener((customerInfo) => {
                        emitCustomerInfo(customerInfo);
                    });
                }
                return true;
            })().catch((error) => {
                state.configurePromise = null;
                state.configured = false;
                throw error;
            });
        }

        await state.configurePromise;
        if (normalizedUserId && normalizedUserId !== state.appUserId) {
            const result = await plugin().logIn({ appUserID: normalizedUserId });
            state.appUserId = normalizedUserId;
            emitCustomerInfo(result?.customerInfo);
        }
        return true;
    }

    async function identify(appUserId) {
        const normalized = String(appUserId || '').trim();
        if (!normalized || normalized === 'demo-user') return false;
        return configure(normalized);
    }

    async function loadOffering() {
        if (!await configure(localStorage.getItem('userId'))) return null;
        const offerings = await plugin().getOfferings();
        const offering = offerings?.all?.[config.offeringId] || offerings?.current || null;
        if (!offering) {
            state.activePackage = null;
            return null;
        }

        const selectedPackage = offering.monthly
            || offering.availablePackages?.find(item => item?.identifier === config.packageId)
            || null;

        if (selectedPackage?.product?.identifier !== config.productId) {
            state.activePackage = null;
            throw new Error('REVENUECAT_PRODUCT_MISMATCH');
        }

        state.activePackage = selectedPackage;
        return selectedPackage;
    }

    async function purchase() {
        const selectedPackage = state.activePackage || await loadOffering();
        if (!selectedPackage) throw new Error('REVENUECAT_PACKAGE_NOT_FOUND');
        const result = await plugin().purchasePackage({ aPackage: selectedPackage });
        emitCustomerInfo(result?.customerInfo);
        return {
            customerInfo: result?.customerInfo || null,
            entitlement: entitlementFrom(result?.customerInfo),
            hasPremium: hasPremium(result?.customerInfo),
            productIdentifier: result?.productIdentifier || selectedPackage.product.identifier
        };
    }

    async function restore() {
        if (!await configure(localStorage.getItem('userId'))) return null;
        const result = await plugin().restorePurchases();
        emitCustomerInfo(result?.customerInfo);
        return {
            customerInfo: result?.customerInfo || null,
            entitlement: entitlementFrom(result?.customerInfo),
            hasPremium: hasPremium(result?.customerInfo)
        };
    }

    async function refreshCustomerInfo() {
        if (!await configure(localStorage.getItem('userId'))) return null;
        const result = await plugin().getCustomerInfo();
        const customerInfo = result?.customerInfo || result || null;
        emitCustomerInfo(customerInfo);
        return customerInfo;
    }

    async function logOut() {
        if (!state.configured || !plugin()?.logOut) return;
        await plugin().logOut();
        state.appUserId = null;
        state.customerInfo = null;
        state.activePackage = null;
    }

    function localizedPrice() {
        return state.activePackage?.product?.priceString || '';
    }

    window.SafeGuardianRevenueCat = Object.freeze({
        isNativeSupported,
        configure,
        identify,
        loadOffering,
        purchase,
        restore,
        refreshCustomerInfo,
        logOut,
        hasPremium,
        entitlementFrom,
        localizedPrice,
        get activePackage() { return state.activePackage; },
        get customerInfo() { return state.customerInfo; }
    });
})();
