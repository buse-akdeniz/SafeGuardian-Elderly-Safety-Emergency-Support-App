(function (global) {
  function isNativeApp() {
    try {
      return global.Capacitor?.isNativePlatform?.() === true;
    } catch {
      return false;
    }
  }

  function isDebugMode() {
    try {
      return new URLSearchParams(global.location?.search || '').get('debug') === '1';
    } catch {
      return false;
    }
  }

  function isProductionApp() {
    return isNativeApp() && !isDebugMode();
  }

  function escapeHtml(value) {
    return String(value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function hideDevOnlyUi() {
    if (!isProductionApp()) return;
    document.querySelectorAll('.dev-only, #devApiSection').forEach((el) => {
      el.style.display = 'none';
    });
  }

  global.SafeGuardianProd = {
    isNativeApp,
    isDebugMode,
    isProductionApp,
    escapeHtml,
    hideDevOnlyUi,
  };
})(window);
