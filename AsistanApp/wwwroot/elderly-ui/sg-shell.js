(function () {
  const HEADER_ID = 'sgAppHeader';
  const A11Y_ID = 'a11yControls';

  function getActiveScreenId() {
    return document.querySelector('.screen.active')?.id || 'loginScreen';
  }

  function syncHeader() {
    const header = document.getElementById(HEADER_ID);
    if (!header) return;
    const screenId = getActiveScreenId();
    const showHeader = screenId === 'homeScreen';
    header.classList.toggle('is-hidden', !showHeader);
    document.body.classList.toggle('sg-on-home', showHeader);
    document.body.classList.toggle('sg-on-inner', !showHeader && screenId !== 'loginScreen' && screenId !== 'registerScreen');
  }

  function openSettings() {
    const wrap = document.getElementById(A11Y_ID);
    const menu = document.getElementById('a11yMenu');
    const btn = document.getElementById('a11yMenuBtn');
    if (!wrap || !menu) return;
    wrap.classList.add('is-open');
    menu.removeAttribute('hidden');
    if (btn) btn.setAttribute('aria-expanded', 'true');
  }

  function closeSettings() {
    const wrap = document.getElementById(A11Y_ID);
    const menu = document.getElementById('a11yMenu');
    const btn = document.getElementById('a11yMenuBtn');
    if (!wrap || !menu) return;
    wrap.classList.remove('is-open');
    menu.setAttribute('hidden', '');
    if (btn) btn.setAttribute('aria-expanded', 'false');
  }

  function bindSettingsSheet() {
    const wrap = document.getElementById(A11Y_ID);
    if (!wrap || wrap.dataset.sgBound) return;
    wrap.dataset.sgBound = '1';
    wrap.addEventListener('click', (e) => {
      if (e.target === wrap) closeSettings();
    });
    const bindTap = (el, handler) => {
      if (!el || el.dataset.sgTapBound) return;
      el.dataset.sgTapBound = '1';
      el.addEventListener('click', handler);
      el.addEventListener('touchend', (e) => {
        e.preventDefault();
        handler(e);
      }, { passive: false });
    };

    bindTap(document.getElementById('sgSettingsBtn'), (e) => {
      if (e?.preventDefault) e.preventDefault();
      openSettings();
    });
    bindTap(document.getElementById('sgAccountBtn'), (e) => {
      if (e?.preventDefault) e.preventDefault();
      if (typeof window.showScreen === 'function') window.showScreen('profileScreen');
    });
  }

  function patchShowScreen() {
    if (typeof window.showScreen !== 'function' || window.showScreen.__sgPatched) return;
    const original = window.showScreen;
    window.showScreen = function (screenId) {
      original(screenId);
      closeSettings();
      syncHeader();
      if (typeof window.updateA11yControlsVisibility === 'function') {
        window.updateA11yControlsVisibility(screenId);
      }
    };
    window.showScreen.__sgPatched = true;
  }

  function patchToggleA11y() {
    if (typeof window.toggleA11yMenu !== 'function' || window.toggleA11yMenu.__sgPatched) return;
    window.toggleA11yMenu = function (event) {
      if (event?.stopPropagation) event.stopPropagation();
      const wrap = document.getElementById(A11Y_ID);
      if (wrap?.classList.contains('is-open')) closeSettings();
      else openSettings();
    };
    window.toggleA11yMenu.__sgPatched = true;
  }

  function init() {
    bindSettingsSheet();
    patchShowScreen();
    patchToggleA11y();
    syncHeader();
    const obs = new MutationObserver(syncHeader);
    document.querySelectorAll('.screen').forEach((el) => {
      obs.observe(el, { attributes: true, attributeFilter: ['class'] });
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  window.SGShell = { openSettings, closeSettings, syncHeader };
})();
