/* ===========================
   YAŞLI ASISTANI - GENEL JS
   ===========================
*/

// =================== EKRAN YÖNETIMI ===================

const DEFAULT_API_BASE = 'http://localhost:5007';

function getApiBase() {
    const rawStored = localStorage.getItem('apiBaseUrl')?.trim();
    const configured = window.API_BASE?.trim?.();
    const origin = window.location?.origin || '';
    const userAgent = navigator.userAgent || '';
    const isHttpOrigin = /^https?:\/\//i.test(origin);
    const isCapacitorRuntime = Boolean(window.Capacitor);
    const isIosSimulator = /iPhone Simulator|iPad Simulator|Simulator/i.test(userAgent);
    const isPrivateLanAddress = /^https?:\/\/(192\.168\.|10\.|172\.(1[6-9]|2\d|3[0-1])\.)/i.test(rawStored || '');

    const stored = rawStored && /:3000\b/.test(rawStored)
        ? ''
        : rawStored;

    if (rawStored && !stored) {
        localStorage.removeItem('apiBaseUrl');
    }

    if (isCapacitorRuntime && isIosSimulator && isPrivateLanAddress) {
        localStorage.removeItem('apiBaseUrl');
    }

    let candidate = stored || configured || (isHttpOrigin ? origin : '');

    if (!candidate && isCapacitorRuntime) {
        candidate = DEFAULT_API_BASE;
    }

    if (candidate && !/^https?:\/\//i.test(candidate)) {
        candidate = `http://${candidate}`;
    }

    try {
        return new URL(candidate).origin;
    } catch {
        return DEFAULT_API_BASE;
    }
}

const API_BASE = getApiBase();
const PreferencesPlugin = window.Capacitor?.Plugins?.Preferences;
const GeolocationPlugin = window.Capacitor?.Plugins?.Geolocation;

let lastGuidanceText = '';
let emergencyTimer = null;
let ignoreNextA11yClose = false;
var speechRecognition = null;
var isListening = false;
const MEDICATION_CONFIRM_WARNING_MS = 15 * 60 * 1000;
const MEDICATION_CONFIRM_CRITICAL_MS = 30 * 60 * 1000;
const medicationConfirmTimers = new Map();
const medicationReminderState = new Map();
let subscriptionCache = null;
let currentMedicationsCache = [];
let careRoutineStarted = false;
let authTokenCache = null;

const PUBLIC_SCREENS = new Set(['loginScreen', 'registerScreen', 'helpScreen', 'homeScreen']);

async function getStoredToken() {
    if (authTokenCache) return authTokenCache;

    if (PreferencesPlugin) {
        try {
            const result = await PreferencesPlugin.get({ key: 'token' });
            const token = result?.value || '';
            if (token) {
                authTokenCache = token;
                return token;
            }
        } catch (error) {
            console.warn('Preferences token okuma hatası:', error);
        }
    }

    const webToken = localStorage.getItem('token') || '';
    if (webToken && PreferencesPlugin) {
        try {
            await PreferencesPlugin.set({ key: 'token', value: webToken });
        } catch (error) {
            console.warn('Preferences token taşıma hatası:', error);
        }
    }

    authTokenCache = webToken || null;
    return webToken;
}

async function setStoredToken(value) {
    const tokenValue = String(value || '');
    authTokenCache = tokenValue || null;
    localStorage.setItem('token', tokenValue);
    if (!PreferencesPlugin) return;
    try {
        await PreferencesPlugin.set({ key: 'token', value: tokenValue });
    } catch (error) {
        console.warn('Preferences token yazma hatası:', error);
    }
}

async function removeStoredToken() {
    authTokenCache = null;
    localStorage.removeItem('token');
    if (!PreferencesPlugin) return;
    try {
        await PreferencesPlugin.remove({ key: 'token' });
    } catch (error) {
        console.warn('Preferences token silme hatası:', error);
    }
}

function hasAuthTokenSync() {
    return Boolean(authTokenCache || localStorage.getItem('token'));
}

function requireAuthToken() {
    const token = authTokenCache || localStorage.getItem('token');
    if (!token) {
        showScreen('loginScreen');
        return null;
    }
    return token;
}

async function requireAuthTokenAsync() {
    const token = await getStoredToken();
    if (!token) {
        showScreen('loginScreen');
        return null;
    }
    return token;
}

function handleAuthExpired() {
    removeStoredToken();
    localStorage.removeItem('userId');
    localStorage.removeItem('userName');
    localStorage.removeItem('rememberMe');
    showNotification('Oturum Süresi Doldu', 'Lütfen tekrar giriş yapın.', 'error');
    showScreen('loginScreen');
}

async function safeFetch(url, options) {
    let finalUrl = url;
    try {
        finalUrl = new URL(url, API_BASE).toString();
    } catch (error) {
        console.error('Geçersiz API adresi:', { url, apiBase: API_BASE, error });
        showNotification('Hata', 'API adresi geçersiz. Lütfen ayarlardan güncelleyin.', 'error');
        return null;
    }

    try {
        const response = await fetch(finalUrl, options);
        if (response.status === 401 || response.status === 403) {
            handleAuthExpired();
            return null;
        }
        return response;
    } catch (error) {
        console.error('Bağlantı hatası:', { finalUrl, error });
        showNotification('Hata', 'API bağlantısı kurulamadı. IP adresini kontrol edin.', 'error');
        return null;
    }
}

async function safeReadJson(response, fallbackValue) {
    if (!response) return fallbackValue;
    try {
        const rawText = await response.text();
        if (!rawText) return fallbackValue;
        let data = JSON.parse(rawText);
        if (!Array.isArray(data) && data?.items) data = data.items;
        return data ?? fallbackValue;
    } catch (error) {
        console.warn('JSON parse hatası:', error);
        return fallbackValue;
    }
}

async function ensurePremiumAccess(featureName) {
    const token = requireAuthToken();
    if (!token) return false;

    const storedPlan = localStorage.getItem('userPlan');
    if (storedPlan && storedPlan !== 'premium') {
        speak('Bu özellik sadece aile paketi üyeleri içindir.');
        showNotification('Abonelik Gerekli', 'Lütfen abone olun.', 'error');
        return false;
    }
    if (storedPlan === 'premium') return true;

    if (!subscriptionCache) {
        const response = await safeFetch(`${API_BASE}/api/subscription?token=${token}`);
        if (!response) return false;
        if (response.ok) {
            subscriptionCache = await safeReadJson(response, null);
        }
    }

    const plan = subscriptionCache?.plan || subscriptionCache?.Plan || 'standard';
    const isActive = subscriptionCache?.isActive ?? subscriptionCache?.IsActive ?? true;
    localStorage.setItem('userPlan', String(plan).toLowerCase());

    const isPremium = String(plan).toLowerCase() === 'premium' && isActive;
    if (!isPremium) {
        speak('Bu özellik sadece aile paketi üyeleri içindir.');
        showNotification('Abonelik Gerekli', `${featureName} için premium abonelik gerekir.`, 'error');
        return false;
    }
    return true;
}

async function ensurePremiumAccess(featureName) {
    const token = requireAuthToken();
    if (!token) return false;
    if (!subscriptionCache) {
        const response = await safeFetch(`${API_BASE}/api/subscription?token=${token}`);
        if (!response) return false;
        if (response.ok) {
            subscriptionCache = await response.json();
        }
    }
    const plan = subscriptionCache?.plan || subscriptionCache?.Plan || 'standard';
    const isActive = subscriptionCache?.isActive ?? subscriptionCache?.IsActive ?? true;
    const isPremium = String(plan).toLowerCase() === 'premium' && isActive;
    if (!isPremium) {
        showNotification('Abonelik Gerekli', `${featureName} için premium abonelik gerekir.`, 'error');
        return false;
    }
    return true;
}

function showScreen(screenId) {
    if (!PUBLIC_SCREENS.has(screenId) && !hasAuthTokenSync()) {
        screenId = 'loginScreen';
    }
    document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
    document.getElementById(screenId).classList.add('active');

    // Her ekrana giriş yapılırken otomatik sesli rehberlik
    triggerVoiceGuidance(screenId);
}

function triggerVoiceGuidance(screenId) {
    const guidance = {
        'homeScreen': 'Ana sayfaya hoş geldiniz. Üç buton göreceksiniz: İlaçlarım, Aile, Yardım. Ses komutu kullanabilirsiniz.',
        'medicationScreen': 'İlaçlarım sayfasındasınız. Aldığınız ilaçlar burada listelenir. Yeni ilaç eklemek için aşağıdaki butonuna basın.',
        'addMedicationScreen': 'Yeni ilaç ekle formunda bulunuyorsunuz. İlaç adını ve saatlerini girin.',
        'familyScreen': 'Aile üyeleri sayfasına hoş geldiniz. Sizinle iletişim kuran aile üyeleri burada listelenir.',
        'helpScreen': 'Yardım sayfasında bulunuyorsunuz. Tüm özellikleri burada açıklıyoruz.',
        'loginScreen': 'Giriş sayfasına hoş geldiniz. E-posta ve şifrenizi girin.'
    };

    if (guidance[screenId]) {
        lastGuidanceText = guidance[screenId];
        updateGuidanceText(guidance[screenId]);
        speak(guidance[screenId]);
    }
}

function updateGuidanceText(text) {
    const guidanceEl = document.getElementById('voiceGuidance');
    if (guidanceEl) {
        guidanceEl.textContent = text;
    }
}

function toggleA11yMenu(event) {
    if (event && event.stopPropagation) {
        event.stopPropagation();
    }
    const a11yMenuBtn = document.getElementById('a11yMenuBtn');
    const a11yMenu = document.getElementById('a11yMenu');
    if (!a11yMenuBtn || !a11yMenu) return;
    const isOpen = !a11yMenu.hasAttribute('hidden');
    if (isOpen) {
        a11yMenu.setAttribute('hidden', '');
        a11yMenuBtn.setAttribute('aria-expanded', 'false');
    } else {
        a11yMenu.removeAttribute('hidden');
        a11yMenuBtn.setAttribute('aria-expanded', 'true');
        ignoreNextA11yClose = true;
    }
}

// Debug: force menu open if still blocked
function forceOpenA11yMenu() {
    const a11yMenuBtn = document.getElementById('a11yMenuBtn');
    const a11yMenu = document.getElementById('a11yMenu');
    if (!a11yMenuBtn || !a11yMenu) return;
    a11yMenu.removeAttribute('hidden');
    a11yMenuBtn.setAttribute('aria-expanded', 'true');
}

function repeatGuidance() {
    if (lastGuidanceText) {
        speak(lastGuidanceText);
    }
}

function initSpeechRecognition() {
    if (speechRecognition) return speechRecognition;
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) {
        console.warn('Tarayıcı konuşma tanımayı desteklemiyor.');
        return null;
    }

    const recognition = new SpeechRecognition();
    recognition.lang = 'tr-TR';
    recognition.continuous = true;
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;

    recognition.onstart = () => {
        isListening = true;
        updateVoiceStatus('🎙️ Dinleniyor...');
    };

    recognition.onend = () => {
        isListening = false;
        updateVoiceStatus('');
    };

    recognition.onerror = (event) => {
        console.warn('Konuşma tanıma hatası:', event?.error || event);
        isListening = false;
        updateVoiceStatus('');
    };

    recognition.onresult = (event) => {
        const transcript = event?.results?.[event.resultIndex]?.[0]?.transcript || '';
        const command = transcript.toLowerCase().trim();
        if (!command) return;
        console.log('Gelen komut:', command);
        handleVoiceCommand(command);
    };

    speechRecognition = recognition;
    return recognition;
}

function updateVoiceStatus(text) {
    const statusEl = document.getElementById('voiceStatus');
    if (statusEl) {
        statusEl.textContent = text;
    }
}

function startVoiceCommand() {
    const recognition = initSpeechRecognition();
    if (!recognition) {
        showNotification('Uyarı', 'Tarayıcı ses tanımayı desteklemiyor', 'error');
        return;
    }
    if (isListening) return;
    try {
        recognition.start();
    } catch (error) {
        console.warn('Konuşma tanıma başlatılamadı:', error);
    }
}

function stopVoiceCommand() {
    if (speechRecognition && isListening) {
        try {
            speechRecognition.stop();
        } catch (error) {
            console.warn('Konuşma tanıma durdurulamadı:', error);
        }
    }
}

function toggleListening() {
    repeatGuidance();
    if (isListening) {
        stopVoiceCommand();
    } else {
        startVoiceCommand();
    }
}

async function shareDoctorReport() {
    const token = requireAuthToken();
    if (!token) return;
    const url = `${window.location.origin}/doctor-report.html?token=${encodeURIComponent(token)}`;
    if (navigator.share) {
        try {
            await navigator.share({ title: 'Doktor Raporu', text: 'Doktor raporu bağlantısı', url });
            showNotification('Paylaşıldı', 'Doktor raporu gönderildi', 'success');
            return;
        } catch (error) {
            console.warn('Paylaşım iptal edildi:', error);
        }
    }
    try {
        await navigator.clipboard.writeText(url);
        showNotification('Kopyalandı', 'Doktor raporu bağlantısı kopyalandı', 'success');
    } catch (error) {
        console.error('Kopyalama hatası:', error);
        showNotification('Hata', 'Bağlantı kopyalanamadı', 'error');
    }
}

function handleVoiceCommand(command) {
    if (command.includes('ilaç')) {
        goToMedications();
        return;
    }

    if (command.includes('aile') || command.includes('kızımı') || command.includes('oğlumu') || command.includes('oğlumu') || command.includes('kızımı ara')) {
        goToFamily();
        return;
    }

    if (command.includes('yardım') || command.includes('acil')) {
        showEmergencyConfirm();
        return;
    }

    if (command.includes('ana sayfa') || command.includes('ev') || command.includes('anasayfa')) {
        goHome();
        return;
    }

    if (command.includes('ruh hali') || command.includes('mod')) {
        goToMoodDashboard();
        return;
    }

    if (command.includes('sağlık')) {
        goToHealthRecords();
        return;
    }

    if (command.includes('kayıt ol')) {
        goToRegister();
        return;
    }

    if (command.includes('çıkış')) {
        logout();
        return;
    }
}

function goHome() {
    if (!requireAuthToken()) return;
    showScreen('homeScreen');
}

function goToMedications() {
    if (!requireAuthToken()) return;
    showScreen('medicationScreen');
    loadMedications();
}

function goToAddMedication() {
    showScreen('addMedicationScreen');
}

function goToFamily() {
    if (!requireAuthToken()) return;
    ensurePremiumAccess('Aile').then(hasAccess => {
        if (!hasAccess) return;
        showScreen('familyScreen');
        loadFamilyMembers();
    });
}

function goToMoodDashboard() {
    if (!requireAuthToken()) return;
    showScreen('moodScreen');
    loadMoodAnalysis();
}

function goToMedicationVision() {
    // Yeni sekme aç
    window.open('medication-vision.html', '_blank');
}

function goToHealthRecords() {
    if (!requireAuthToken()) return;
    showScreen('healthRecordsScreen');
    loadHealthRecords();
}

function goToAddFamily() {
    showScreen('addFamilyScreen');
}

function goToRegister() {
    showScreen('registerScreen');
}

function showHelp() {
    showScreen('helpScreen');
}

function logout() {
    removeStoredToken();
    localStorage.removeItem('userId');
    localStorage.removeItem('userName');
    localStorage.removeItem('rememberMe');
    subscriptionCache = null;
    localStorage.removeItem('userPlan');
    showScreen('loginScreen');
}

// =================== BİLDİRİM ===================

function showNotification(title, message, type = 'success') {
    provideFeedback(`${title}. ${message}`, type === 'error' ? [80, 40, 80] : [40]);
    const notification = document.createElement('div');
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${type === 'success' ? '#00cc00' : '#ff3333'};
        color: black;
        padding: 20px 30px;
        border-radius: 10px;
        font-size: 20px;
        z-index: 10000;
        animation: slideInRight 0.3s;
        font-weight: bold;
    `;
    notification.innerHTML = `${title}<br>${message}`;
    document.body.appendChild(notification);

    setTimeout(() => {
        notification.style.animation = 'slideOutRight 0.3s';
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

function provideFeedback(message, pattern = [30]) {
    if (navigator.vibrate) {
        try {
            navigator.vibrate(pattern);
        } catch {
            // ignore
        }
    }
    if (message) {
        speak(message);
    }
}

// =================== FORM IŞLEYENLER ===================

document.addEventListener('DOMContentLoaded', async function () {
    const a11yMenuBtn = document.getElementById('a11yMenuBtn');
    const a11yMenu = document.getElementById('a11yMenu');
    if (a11yMenuBtn && a11yMenu) {
        a11yMenuBtn.addEventListener('click', (event) => {
            event.stopPropagation();
            toggleA11yMenu(event);
        });

        a11yMenuBtn.addEventListener('touchstart', (event) => {
            event.stopPropagation();
            toggleA11yMenu(event);
        }, { passive: true });

        a11yMenu.addEventListener('touchstart', (event) => {
            event.stopPropagation();
        }, { passive: true });

        a11yMenu.addEventListener('click', (event) => {
            event.stopPropagation();
        });

        document.addEventListener('focusin', (event) => {
            const target = event.target;
            if (a11yMenu.contains(target)) {
                return;
            }
        });

        document.addEventListener('click', (event) => {
            const target = event.target;
            if (ignoreNextA11yClose) {
                ignoreNextA11yClose = false;
                return;
            }
            if (!a11yMenu.contains(target) && !a11yMenuBtn.contains(target)) {
                if (!a11yMenu.hasAttribute('hidden')) {
                    a11yMenu.setAttribute('hidden', '');
                    a11yMenuBtn.setAttribute('aria-expanded', 'false');
                }
            }
        });
    }

    const apiBaseInput = document.getElementById('apiBaseInput');
    const apiBaseSave = document.getElementById('apiBaseSave');
    const apiBaseClear = document.getElementById('apiBaseClear');
    if (apiBaseInput) {
        const stored = localStorage.getItem('apiBaseUrl');
        apiBaseInput.value = stored || '';
    }
    if (apiBaseSave && apiBaseInput) {
        apiBaseSave.addEventListener('click', () => {
            const value = apiBaseInput.value.trim();
            if (!value) return;
            localStorage.setItem('apiBaseUrl', value);
            showNotification('Kaydedildi', 'API adresi güncellendi', 'success');
        });
    }
    if (apiBaseClear && apiBaseInput) {
        apiBaseClear.addEventListener('click', () => {
            localStorage.removeItem('apiBaseUrl');
            apiBaseInput.value = '';
            showNotification('Sıfırlandı', 'API adresi temizlendi', 'success');
        });
    }

    const a11yToggle = document.getElementById('a11yToggle');
    if (a11yToggle) {
        const isLarge = localStorage.getItem('largeText') === 'true';
        document.body.classList.toggle('large-text', isLarge);
        a11yToggle.setAttribute('aria-pressed', String(isLarge));
        a11yToggle.textContent = isLarge ? 'YAZIYI KÜÇÜLT' : 'YAZIYI BÜYÜT';
        a11yToggle.addEventListener('click', () => toggleLargeText(a11yToggle));
        a11yToggle.addEventListener('touchstart', (event) => {
            event.stopPropagation();
            toggleLargeText(a11yToggle);
        }, { passive: true });
    }

    const contrastToggle = document.getElementById('contrastToggle');
    if (contrastToggle) {
        const isHighContrast = localStorage.getItem('highContrast') === 'true';
        document.body.classList.toggle('high-contrast', isHighContrast);
        contrastToggle.setAttribute('aria-pressed', String(isHighContrast));
        contrastToggle.textContent = isHighContrast ? 'KONTRASTI AZALT' : 'KONTRASTI ARTIR';
        contrastToggle.addEventListener('click', () => toggleHighContrast(contrastToggle));
        contrastToggle.addEventListener('touchstart', (event) => {
            event.stopPropagation();
            toggleHighContrast(contrastToggle);
        }, { passive: true });
    }

    const simpleHomeToggle = document.getElementById('simpleHomeToggle');
    if (simpleHomeToggle) {
        const isSimpleHome = localStorage.getItem('simpleHome') === 'true';
        document.body.classList.toggle('simple-home', isSimpleHome);
        simpleHomeToggle.setAttribute('aria-pressed', String(isSimpleHome));
        simpleHomeToggle.textContent = isSimpleHome ? 'BASİT MOD KAPAT' : 'BASİT MOD';
        simpleHomeToggle.addEventListener('click', () => toggleSimpleHome(simpleHomeToggle));
        simpleHomeToggle.addEventListener('touchstart', (event) => {
            event.stopPropagation();
            toggleSimpleHome(simpleHomeToggle);
        }, { passive: true });
    }

    const resetViewBtn = document.getElementById('resetViewBtn');
    if (resetViewBtn) {
        resetViewBtn.addEventListener('click', resetViewSettings);
        resetViewBtn.addEventListener('touchstart', (event) => {
            event.stopPropagation();
            resetViewSettings();
        }, { passive: true });
    }

    // Giriş Formu
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handleLogin);
    }

    // İlaç Ekleme Formu
    const addMedForm = document.getElementById('addMedicationForm');
    if (addMedForm) {
        addMedForm.addEventListener('submit', handleAddMedication);
    }

    // Aile Üyesi Ekleme Formu
    const addFamilyForm = document.getElementById('addFamilyForm');
    if (addFamilyForm) {
        addFamilyForm.addEventListener('submit', handleAddFamily);
    }

    // Kayıt Formu
    const registerFormElement = document.getElementById('registerForm');
    if (registerFormElement) {
        registerFormElement.addEventListener('submit', handleRegister);
    }

    // Otomatik giriş (Beni Hatırla)
    const remember = localStorage.getItem('rememberMe') === 'true';
    const token = await getStoredToken();
    if (remember && token) {
        showScreen('homeScreen');
        updateGreeting();
    }

    // Butonlara sesli geri bildirim
    document.querySelectorAll('button').forEach(btn => {
        btn.addEventListener('click', () => {
            provideFeedback('', [20]);
            const label = btn.getAttribute('aria-label') || btn.textContent.trim();
            if (label) {
                speak(label);
            }
        });
    });

    // Sesli asistan onboarding (ilk kullanım)
    const voiceOnboarding = document.getElementById('voiceOnboarding');
    const voiceOnboardingStart = document.getElementById('voiceOnboardingStart');
    const voiceOnboardingSkip = document.getElementById('voiceOnboardingSkip');
    if (voiceOnboarding && voiceOnboardingStart && voiceOnboardingSkip) {
        const isDone = localStorage.getItem('voiceOnboardingDone') === 'true';
        if (!isDone) {
            voiceOnboarding.classList.add('active');
            setTimeout(() => {
                speak('Sesli asistanı başlatmak için dinlemeyi başlat düğmesine dokunun.');
            }, 400);
        }

        voiceOnboardingStart.addEventListener('click', () => {
            localStorage.setItem('voiceOnboardingDone', 'true');
            voiceOnboarding.classList.remove('active');
            startVoiceCommand();
            updateGuidanceText('Dinleme açık. Şimdi konuşabilirsiniz.');
            speak('Dinleme açık. Şimdi konuşabilirsiniz.');
        });

        voiceOnboardingSkip.addEventListener('click', () => {
            localStorage.setItem('voiceOnboardingDone', 'true');
            voiceOnboarding.classList.remove('active');
            speak('Dilediğiniz zaman Dinle düğmesine dokunabilirsiniz.');
        });
    }
});

window.addEventListener('load', async () => {
    const token = await getStoredToken();
    const path = window.location.pathname || '';
    if (token && (path.includes('login') || path === '/' || path.endsWith('/index.html'))) {
        showScreen('homeScreen');
        updateGreeting();
    }
    startCareRoutine();
});

function toggleLargeText(buttonEl) {
    const isLarge = document.body.classList.toggle('large-text');
    localStorage.setItem('largeText', String(isLarge));
    if (buttonEl) {
        buttonEl.setAttribute('aria-pressed', String(isLarge));
        buttonEl.textContent = isLarge ? 'YAZIYI KÜÇÜLT' : 'YAZIYI BÜYÜT';
    }
}

function toggleHighContrast(buttonEl) {
    const isHighContrast = document.body.classList.toggle('high-contrast');
    localStorage.setItem('highContrast', String(isHighContrast));
    if (buttonEl) {
        buttonEl.setAttribute('aria-pressed', String(isHighContrast));
        buttonEl.textContent = isHighContrast ? 'KONTRASTI AZALT' : 'KONTRASTI ARTIR';
    }
}

function resetViewSettings() {
    localStorage.removeItem('largeText');
    localStorage.removeItem('highContrast');
    localStorage.removeItem('simpleHome');
    document.body.classList.remove('large-text', 'high-contrast', 'simple-home');
    location.reload();
}

function toggleSimpleHome(buttonEl) {
    const isSimpleHome = document.body.classList.toggle('simple-home');
    localStorage.setItem('simpleHome', String(isSimpleHome));
    if (buttonEl) {
        buttonEl.setAttribute('aria-pressed', String(isSimpleHome));
        buttonEl.textContent = isSimpleHome ? 'BASİT MOD KAPAT' : 'BASİT MOD';
    }
}

function startCareRoutine() {
    if (careRoutineStarted) return;
    careRoutineStarted = true;
    setTimeout(() => {
        maybeRunCareRoutine();
    }, 2000);
    setInterval(() => {
        maybeRunCareRoutine();
    }, 60000);
}

function getDateKey(date = new Date()) {
    return date.toISOString().slice(0, 10);
}

function withinHours(now, startHour, endHour) {
    const h = now.getHours();
    return h >= startHour && h < endHour;
}

async function maybeRunCareRoutine() {
    const token = await getStoredToken();
    if (!token) return;
    const now = new Date();
    const dateKey = getDateKey(now);

    await maybePromptMood(dateKey, now);
    await maybePromptHealth(dateKey, now, 'tansiyon', 'mmHg', 9, 12, 'Tansiyonunu ölçtün mü?');
    await maybePromptHealth(dateKey, now, 'şeker', 'mg/dL', 13, 18, 'Şekerini ölçtün mü?');
    await maybeRemindMedications(dateKey, now);
    await maybeNotifyFamilyIfNoContact(dateKey);
}

async function maybePromptMood(dateKey, now) {
    const key = `moodAsked:${dateKey}`;
    if (localStorage.getItem(key) === 'true') return;
    if (!withinHours(now, 9, 21)) return;

    localStorage.setItem(key, 'true');
    speak('Bugün nasılsın? Ruh halini 1 ile 10 arasında söyleyebilirsin.');
    showNotification('Ruh Hali', 'Bugün nasılsın? 1-10 arası söyleyebilirsin.', 'info');
    const input = prompt('Ruh halin (1-10):');
    const score = Number.parseInt(String(input || '').trim(), 10);
    if (!Number.isNaN(score) && score >= 1 && score <= 10) {
        await submitMood(score);
    }
}

async function maybePromptHealth(dateKey, now, recordType, unit, startHour, endHour, question) {
    const key = `${recordType}Asked:${dateKey}`;
    if (localStorage.getItem(key) === 'true') return;
    if (!withinHours(now, startHour, endHour)) return;

    localStorage.setItem(key, 'true');
    speak(`Canım, ${question} Değerini girer misin?`);
    showNotification('Sağlık Kontrolü', question, 'info');
    const input = prompt(`${recordType.toUpperCase()} değeri (${unit}):`);
    const value = Number.parseFloat(String(input || '').replace(',', '.'));
    if (!Number.isNaN(value) && value > 0) {
        await addHealthRecord(recordType, value, unit);
    }
}

async function maybeRemindMedications(dateKey, now) {
    if (!currentMedicationsCache.length) return;

    const currentMinutes = now.getHours() * 60 + now.getMinutes();
    for (const med of currentMedicationsCache) {
        const times = Array.isArray(med.scheduleTimes) ? med.scheduleTimes : [];
        for (const time of times) {
            const [h, m] = String(time).split(':').map(v => Number.parseInt(v, 10));
            if (Number.isNaN(h) || Number.isNaN(m)) continue;
            const targetMinutes = h * 60 + m;
            const diff = Math.abs(targetMinutes - currentMinutes);
            const remindKey = `medReminder:${dateKey}:${med.id}:${time}`;
            if (diff <= 5 && localStorage.getItem(remindKey) !== 'true') {
                localStorage.setItem(remindKey, 'true');
                const message = `${med.name} ilacını alma zamanı.`;
                speak(`Canım, ${message}`);
                showNotification('İlaç Hatırlatma', message, 'normal');
            }
        }
    }
}

async function maybeNotifyFamilyIfNoContact(dateKey) {
    const key = `familyContactReminder:${dateKey}`;
    if (localStorage.getItem(key) === 'true') return;

    try {
        const token = requireAuthToken();
        if (!token) return;
        const response = await safeFetch(`${API_BASE}/api/family/last-contact?token=${token}`);
        if (!response) return;
        if (!response.ok) return;
        const data = await safeReadJson(response, null);
        const hoursSince = data?.hoursSince ?? null;
        if (hoursSince !== null && hoursSince >= 24) {
            localStorage.setItem(key, 'true');
            await sendFamilyNotification('family_contact_missing', 'Uzun süredir aileden arama yok. Lütfen kullanıcıyla iletişim kurun.', 'normal');
        }
    } catch (error) {
        console.warn('Aile iletişim kontrolü başarısız:', error);
    }
}

async function handleLogin(e) {
    e.preventDefault();
    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value.trim();
    const remember = document.getElementById('rememberMe')?.checked;

    try {
        const response = await safeFetch(`${API_BASE}/api/elderly/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        if (!response) {
            return;
        }

        const rawText = await response.text();
        console.log('Gelen yanıt:', rawText);

        let data = null;
        try {
            data = rawText ? JSON.parse(rawText) : null;
        } catch (parseError) {
            console.warn('Yanıt JSON değil:', parseError);
        }

        if (response.ok && data?.token) {
            await setStoredToken(data.token);
            localStorage.setItem('userId', data.userId);
            localStorage.setItem('userName', data.name);
            localStorage.setItem('rememberMe', remember ? 'true' : 'false');
            localStorage.removeItem('userPlan');
            subscriptionCache = null;
            safeFetch(`${API_BASE}/api/subscription?token=${data.token}`)
                .then(res => res ? safeReadJson(res, null) : null)
                .then(sub => {
                    if (!sub) return;
                    subscriptionCache = sub;
                    const plan = sub?.plan || sub?.Plan;
                    if (plan) localStorage.setItem('userPlan', String(plan).toLowerCase());
                })
                .catch(() => {});
            showScreen('homeScreen');
            updateGreeting();
            speak(`Hoş geldiniz ${data.name}`);
        } else {
            const message = data?.message || rawText || 'Giriş başarısız';
            showNotification('Hata', message, 'error');
        }
    } catch (error) {
        console.error('Giriş hatası:', { error, apiBase: API_BASE, email });
        showNotification('Hata', 'Bağlantı hatası. API adresini kontrol edin.', 'error');
    }
}

async function handleForgotPassword() {
    const email = prompt('E-posta adresinizi girin:');
    if (!email) return;

    try {
        const response = await fetch(`${API_BASE}/api/elderly/reset-password`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email })
        });

        if (response.ok) {
            let tempPassword = null;
            let message = 'Geçici şifre oluşturuldu';
            try {
                const data = await response.json();
                tempPassword = data?.tempPassword || null;
                if (data?.message) message = data.message;
            } catch {
                // JSON değilse geç
            }
            showNotification('Başarılı', message, 'success');
            if (tempPassword) {
                showNotification('Geçici Şifre', `Şifreniz: ${tempPassword}`, 'success');
            }
        } else {
            let errorMessage = 'İşlem başarısız';
            try {
                const data = await response.json();
                if (data?.message) errorMessage = data.message;
            } catch {
                // JSON değilse geç
            }
            showNotification('Hata', errorMessage, 'error');
        }
    } catch (error) {
        console.error('Şifre sıfırlama hatası:', error);
        showNotification('Hata', 'Bağlantı hatası', 'error');
    }
}

async function handleAddMedication(e) {
    e.preventDefault();
    const name = document.getElementById('medName').value;
    const notes = document.getElementById('medNotes').value;
    const times = Array.from(document.querySelectorAll('.med-time'))
        .filter(input => input.value)
        .map(input => input.value);

    if (times.length === 0) {
        showNotification('Uyarı', 'En az bir saat seçin', 'error');
        return;
    }

    try {
            const token = await requireAuthTokenAsync();
            if (!token) return;
        const response = await fetch(`${API_BASE}/api/medications?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, notes, scheduleTimes: times })
        });

        if (response.ok) {
            showNotification('Başarılı', 'İlaç eklendi', 'success');
            document.getElementById('addMedicationForm').reset();
            await sendFamilyNotification('medication_added', `Yeni ilaç eklendi: ${name}`, 'normal');
            setTimeout(() => goToMedications(), 1000);
        }
    } catch (error) {
        console.error('İlaç ekleme hatası:', error);
        showNotification('Hata', 'İlaç eklenemedi', 'error');
    }
}

async function handleRegister(e) {
    e.preventDefault();
    const fullName = document.getElementById('regFullName').value.trim();
    const phone = document.getElementById('regPhone').value.trim();
    const email = document.getElementById('regEmail').value.trim();
    const birthDate = document.getElementById('regBirthDate').value;

    try {
        const response = await fetch(`${API_BASE}/api/elderly-self-enroll`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                deviceId: crypto?.randomUUID?.() || undefined,
                fullName,
                phone,
                email,
                birthDate,
                plan: 'standard'
            })
        });

        if (response.ok) {
            let tempPassword = null;
            try {
                const data = await response.json();
                tempPassword = data?.tempPassword || null;
            } catch {
                // JSON değilse geç
            }
            showNotification('Başarılı', 'Kayıt tamamlandı', 'success');
            if (tempPassword) {
                showNotification('Geçici Şifre', `Şifreniz: ${tempPassword}`, 'success');
            }
            showScreen('loginScreen');
        } else {
            let errorMessage = 'Kayıt başarısız';
            try {
                const data = await response.json();
                if (data?.message) {
                    errorMessage = data.message;
                }
            } catch {
                // JSON değilse varsayılan mesajı kullan
            }
            showNotification('Hata', errorMessage, 'error');
        }
    } catch (error) {
        console.error('Kayıt hatası:', error);
        showNotification('Hata', 'Bağlantı hatası', 'error');
    }
}

async function loadMedications() {
    const token = requireAuthToken();
    if (!token) return;
    try {
        const response = await safeFetch(`${API_BASE}/api/medications?token=${token}`);
        if (!response) return;
        if (response.ok) {
            const medications = await safeReadJson(response, []);
            currentMedicationsCache = Array.isArray(medications) ? medications : [];
            console.log('Gelen Veri (ilaçlar):', medications);
            const container = document.getElementById('medicationsList');
            container.innerHTML = medications.map(med => `
                <div style="background: rgba(255,255,0,0.1); border-left: 5px solid #ffff00; padding: 20px; margin-bottom: 20px; border-radius: 10px;">
                    <div style="font-size: 32px; color: #ffff00; font-weight: bold; margin-bottom: 10px;">${med.name}</div>
                    <div style="font-size: 24px; color: #ffffff; margin-bottom: 10px;">Saatler: ${med.scheduleTimes?.join(', ') || 'Belirtilmedi'}</div>
                    <div style="font-size: 20px; color: #00ff00; margin-bottom: 15px;">${med.notes || ''}</div>
                    ${typeof med.stockCount === 'number' ? `<div style="font-size: 20px; color: ${med.stockCount <= 3 ? '#ff6666' : '#00ccff'}; margin-bottom: 10px;">Kalan: ${med.stockCount}</div>` : ''}
                    <button class="btn-giant btn-green" onclick="takeMedication(${med.id})" style="margin-top: 10px;">✓ İLACIMI İÇTİM</button>
                </div>
            `).join('');
            refreshMedicationConfirmTimers(medications);
            scheduleMedicationReminders();
        }
    } catch (error) {
        console.error('İlaç yükleme hatası:', error);
    }
}

async function takeMedication(medicationId) {
    const token = requireAuthToken();
    if (!token) return;
    try {
        const response = await safeFetch(`${API_BASE}/api/medications/${medicationId}/taken?token=${token}`, {
            method: 'POST'
        });
        if (!response) return;
        if (response.ok) {
            const payload = await response.json().catch(() => null);
            clearMedicationConfirmTimer(medicationId);
            showNotification('Başarılı', 'İlaç kaydedildi ✓', 'success');
            const medName = payload?.medication?.name || `ID: ${medicationId}`;
            await sendFamilyNotification('medication_taken', `İlaç alındı: ${medName}`, 'normal');
            if (payload?.stockCount === 0) {
                showNotification('Uyarı', 'İlaç kutusu bitti', 'error');
                await sendFamilyNotification('medication_stock_empty', 'İlaç kutusu bitti', 'high');
            }
            loadMedications();
        }
    } catch (error) {
        console.error('İlaç alma hatası:', error);
        showNotification('Hata', 'Bir sorun oluştu', 'error');
    }
}

function scheduleMedicationReminders() {
    if (medicationReminderState.has('initialized')) return;
    medicationReminderState.set('initialized', true);
    setTimeout(() => {
        maybeRemindMedications(getDateKey(), new Date());
    }, 2000);
}

function refreshMedicationConfirmTimers(medications) {
    const ids = new Set(medications.map(m => m.id));
    for (const [id, timer] of medicationConfirmTimers.entries()) {
        if (!ids.has(id)) {
            clearTimeout(timer.warningId);
            clearTimeout(timer.criticalId);
            medicationConfirmTimers.delete(id);
        }
    }
    medications.forEach(med => scheduleMedicationConfirmTimer(med));
}

function scheduleMedicationConfirmTimer(med) {
    if (!med?.id) return;
    if (medicationConfirmTimers.has(med.id)) return;

    const lastTaken = med.lastTakenAt ? new Date(med.lastTakenAt) : null;
    const elapsed = lastTaken ? Date.now() - lastTaken.getTime() : Number.MAX_SAFE_INTEGER;
    if (elapsed < MEDICATION_CONFIRM_WARNING_MS) return;

    const warningId = setTimeout(async () => {
        await sendFamilyNotification('medication_unconfirmed', `İlaç onayı gelmedi: ${med.name}`, 'normal');
        showNotification('Uyarı', 'İlaç onayı alınmadı', 'error');
    }, MEDICATION_CONFIRM_WARNING_MS);

    const criticalId = setTimeout(async () => {
        medicationConfirmTimers.delete(med.id);
        await sendFamilyNotification('medication_unconfirmed_critical', `İlaç hala onaylanmadı: ${med.name}`, 'high');
        showNotification('Acil', 'İlaç hala onaylanmadı', 'error');
    }, MEDICATION_CONFIRM_CRITICAL_MS);

    medicationConfirmTimers.set(med.id, { warningId, criticalId });
}

function clearMedicationConfirmTimer(medicationId) {
    const timers = medicationConfirmTimers.get(medicationId);
    if (timers) {
        clearTimeout(timers.warningId);
        clearTimeout(timers.criticalId);
        medicationConfirmTimers.delete(medicationId);
    }
}

async function loadFamilyMembers() {
    const token = requireAuthToken();
    if (!token) return;
    try {
        const response = await safeFetch(`${API_BASE}/api/family-members?token=${token}`);
        if (!response) return;
        if (response.ok) {
            const payload = await safeReadJson(response, { members: [] });
            const members = Array.isArray(payload) ? payload : (payload.members || []);
            console.log('Gelen Veri (aile):', members);
            const container = document.getElementById('familyList');
            container.innerHTML = members.map(member => `
                <div style="background: rgba(0,255,100,0.1); border-left: 5px solid #00ff00; padding: 20px; margin-bottom: 20px; border-radius: 10px;">
                    <div style="font-size: 32px; color: #ffff00; font-weight: bold; margin-bottom: 10px;">${member.name}</div>
                    <div style="font-size: 24px; color: #ffffff;">${member.relationship || 'Aile Üyesi'}</div>
                    <div style="font-size: 20px; color: #00ff00; margin-top: 10px;">${member.email}</div>
                    ${member.phoneNumber ? `<div style="font-size: 20px; color: #00ccff; margin-top: 8px;">${member.phoneNumber}</div>` : ''}
                </div>
            `).join('');
        }
    } catch (error) {
        console.error('Aile yükleme hatası:', error);
    }
}

async function handleAddFamily(e) {
    e.preventDefault();
    const name = document.getElementById('familyName').value;
    const phoneNumber = document.getElementById('familyPhone')?.value?.trim() || '';
    const email = document.getElementById('familyEmail').value;
    const relationship = document.getElementById('familyRelation').value;

    try {
        const hasAccess = await ensurePremiumAccess('Aile üyesi ekleme');
        if (!hasAccess) return;
        const token = requireAuthToken();
        if (!token) return;
        const response = await safeFetch(`${API_BASE}/api/family-members?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, email, relationship, phoneNumber })
        });
        if (!response) return;

        if (response.ok) {
            showNotification('Başarılı', 'Aile üyesi eklendi', 'success');
            document.getElementById('addFamilyForm').reset();
            setTimeout(() => goToFamily(), 1000);
        }
    } catch (error) {
        console.error('Aile ekleme hatası:', error);
        showNotification('Hata', 'Aile üyesi eklenemedi', 'error');
    }
}

function updateGreeting() {
    const name = localStorage.getItem('userName') || 'Arkadaş';
    const greeting = document.getElementById('greeting');
    if (greeting) {
        greeting.textContent = `Hoş geldiniz, ${name}`;
    }
}

function speak(text) {
    if ('speechSynthesis' in window) {
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = 'tr-TR';
        utterance.rate = 0.85;
        speechSynthesis.speak(utterance);
    }
}

async function triggerEmergencyCall() {
    const token = requireAuthToken();
    if (!token) return;
    try {
        const response = await safeFetch(`${API_BASE}/api/family-members?token=${token}`);
        if (!response) return;
        if (response.ok) {
            const payload = await response.json();
            const members = Array.isArray(payload) ? payload : (payload.members || []);
            const phone = members.find(m => m.phoneNumber)?.phoneNumber || '';
            if (phone) {
                provideFeedback('Aile üyeniz aranıyor. Lütfen sakin olun.', [100, 50, 100]);
                window.location.href = `tel:${phone}`;
            } else {
                showNotification('Uyarı', 'Kayıtlı telefon bulunamadı', 'error');
            }
        }
    } catch (error) {
        console.error('Aile telefonu alınamadı:', error);
    }
}

function showEmergencyConfirm() {
    ensurePremiumAccess('SOS').then(hasAccess => {
        if (!hasAccess) return;
        const modal = document.getElementById('emergencyModal');
        if (modal) {
            modal.classList.add('show');
            speak('Acil yardım onayı. Aileye ve sağlık kuruluşlarına konumla bildirim gönderilecek. İptal etmek için iptal butonuna basın.');
            if (emergencyTimer) {
                clearTimeout(emergencyTimer);
            }
            emergencyTimer = setTimeout(() => confirmEmergency(), 5000);
        }
    });
}

function cancelEmergency() {
    if (emergencyTimer) {
        clearTimeout(emergencyTimer);
        emergencyTimer = null;
    }
    const modal = document.getElementById('emergencyModal');
    if (modal) {
        modal.classList.remove('show');
    }
    showNotification('İptal', 'Acil çağrı iptal edildi', 'success');
}

async function confirmEmergency() {
    if (emergencyTimer) {
        clearTimeout(emergencyTimer);
        emergencyTimer = null;
    }
    const modal = document.getElementById('emergencyModal');
    if (modal) {
        modal.classList.remove('show');
    }
    const token = requireAuthToken();
    if (!token) return;
    try {
        const location = await getCurrentLocation();
        const response = await safeFetch(`${API_BASE}/api/emergency-alert?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                location: location?.label || 'Unknown',
                coords: location?.coords || null
            })
        });
        if (!response) return;
        if (response.ok) {
            showNotification('Gönderildi', 'Acil yardım çağrısı gönderildi', 'success');
            await sendEmergencyNotification(location);
            await sendEmergencyBroadcast({
                location: location?.label || 'Unknown'
            });
            await triggerEmergencyCall();
        } else {
            showNotification('Hata', 'Acil çağrı gönderilemedi', 'error');
        }
    } catch (error) {
        console.error('Acil çağrı hatası:', error);
        showNotification('Hata', 'Bağlantı hatası', 'error');
    }
}

async function getCurrentLocation() {
    if (GeolocationPlugin && typeof GeolocationPlugin.getCurrentPosition === 'function') {
        try {
            if (typeof GeolocationPlugin.requestPermissions === 'function') {
                await GeolocationPlugin.requestPermissions();
            }
            const position = await GeolocationPlugin.getCurrentPosition({
                enableHighAccuracy: true,
                timeout: 5000,
                maximumAge: 10000
            });
            const { latitude, longitude, accuracy } = position.coords;
            return {
                label: `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`,
                coords: { latitude, longitude, accuracy }
            };
        } catch (error) {
            console.warn('Capacitor geolocation alınamadı, web fallback kullanılacak:', error);
        }
    }

    if (!('geolocation' in navigator)) {
        return null;
    }

    const isSecure = window.isSecureContext || ['localhost', '127.0.0.1'].includes(window.location.hostname);
    if (!isSecure) {
        return null;
    }

    return new Promise(resolve => {
        const timeoutId = setTimeout(() => resolve(null), 4000);
        navigator.geolocation.getCurrentPosition(
            position => {
                clearTimeout(timeoutId);
                const { latitude, longitude, accuracy } = position.coords;
                resolve({
                    label: `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`,
                    coords: { latitude, longitude, accuracy }
                });
            },
            () => {
                clearTimeout(timeoutId);
                resolve(null);
            },
            { enableHighAccuracy: true, timeout: 3000, maximumAge: 10000 }
        );
    });
}

async function sendEmergencyNotification(location) {
    const token = requireAuthToken();
    if (!token) return;
    const message = location?.label
        ? `Acil yardım çağrısı. Konum: ${location.label}`
        : 'Acil yardım çağrısı. Konum alınamadı.';
    try {
        const response = await safeFetch(`${API_BASE}/api/send-notification?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                type: 'emergency_alert',
                message,
                severity: 'high'
            })
        });
        if (!response) return;
    } catch (error) {
        console.error('Bildirim gönderme hatası:', error);
    }
}

async function sendEmergencyBroadcast(payload) {
    const token = requireAuthToken();
    if (!token) return;
    try {
        const response = await safeFetch(`${API_BASE}/api/emergency-broadcast?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload || {})
        });
        if (!response) return;
    } catch (error) {
        console.error('Acil yayın hatası:', error);
    }
}

async function sendFamilyNotification(type, message, severity = 'normal') {
    const token = requireAuthToken();
    if (!token) return;
    const location = await getCurrentLocation();
    const payload = {
        type,
        message,
        severity,
        location: location?.label || 'Unknown',
        recipient: 'all'
    };
    try {
        const response = await safeFetch(`${API_BASE}/api/send-notification?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (!response) return;
    } catch (error) {
        console.error('Bildirim gönderme hatası:', error);
    }
}

// Stil kuralları
const style = document.createElement('style');
style.innerHTML = `
    @keyframes slideInRight {
        from { transform: translateX(400px); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
    @keyframes slideOutRight {
        from { transform: translateX(0); opacity: 1; }
        to { transform: translateX(400px); opacity: 0; }
    }
`;
document.head.appendChild(style);

// =================== AI SOHBET ===================

async function chat(userMessage) {
    const token = requireAuthToken();
    if (!token) return null;
    try {
        const response = await safeFetch(`${API_BASE}/api/chat?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: userMessage })
        });
        if (!response) return null;

        if (response.ok) {
            const data = await response.json();
            return data.response;
        }
    } catch (error) {
        console.error('Chat hatası:', error);
    }
    return null;
}

async function startSmartDialog() {
    const name = localStorage.getItem('userName') || 'Arkadaş';
    const hour = new Date().getHours();

    let greeting = '';
    if (hour < 12) {
        greeting = `Günaydın ${name}! Bugün nasılsın?`;
    } else if (hour < 18) {
        greeting = `İyi öğlenler ${name}! Bugün nasıl gidiyor?`;
    } else {
        greeting = `İyi akşamlar ${name}! Günün nasıl geçti?`;
    }

    speak(greeting);

    // Voice cevapı dinle ve AI'ye gönder
    setTimeout(() => startVoiceCommand(), 2000);
}

// =================== BİLDİRİM ALMETTAMLARI ===================

async function loadNotifications() {
    const token = requireAuthToken();
    if (!token) return [];
    try {
        const response = await safeFetch(`${API_BASE}/api/notifications?token=${token}`);
        if (!response) return [];
        if (response.ok) {
            const notifs = await response.json();
            return notifs;
        }
    } catch (error) {
        console.error('Bildirim yükleme hatası:', error);
    }
    return [];
}

// =================== RUH HALİ ANALİZİ ===================

async function loadMoodAnalysis() {
    const token = requireAuthToken();
    if (!token) return;
    try {
        const response = await safeFetch(`${API_BASE}/api/mood-analysis?token=${token}`);
        if (!response) return;
        if (response.ok) {
            const data = await safeReadJson(response, { averageMood: 0, trend: 'stable', recentMoods: [] });
            document.body.classList.toggle('mood-declining', data.trend === 'declining');
            renderMoodDashboard(data);
        }
    } catch (error) {
        console.error('Ruh hali yükleme hatası:', error);
    }
}

async function submitMood(score) {
    const token = requireAuthToken();
    if (!token) return;
    try {
        const response = await safeFetch(`${API_BASE}/api/mood?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ moodScore: score })
        });
        if (!response) return;
        if (response.ok) {
            showNotification('Teşekkürler', 'Ruh haliniz kaydedildi', 'success');
            const severity = score <= 3 ? 'high' : 'normal';
            const moodMessage = score <= 3
                ? `Ruh hali düşük: ${score}/10. Kullanıcı kendini iyi hissetmiyor olabilir.`
                : `Ruh hali bildirimi: ${score}/10`;
            await sendFamilyNotification('mood_update', moodMessage, severity);
            loadMoodAnalysis();
        } else {
            showNotification('Hata', 'Ruh hali kaydedilemedi', 'error');
        }
    } catch (error) {
        console.error('Ruh hali kaydı hatası:', error);
    }
}

function renderMoodDashboard(data) {
    const dashboard = document.getElementById('moodDashboard');

    let trendEmoji = '😊';
    if (data.trend === 'declining') {
        trendEmoji = '😔';
    } else if (data.trend === 'improving') {
        trendEmoji = '😄';
    }

    const moodColor = data.averageMood > 7 ? '#00ff00' : data.averageMood > 4 ? '#ffff00' : '#ff6666';

    dashboard.innerHTML = `
        <div style="text-align: center; padding: 20px;">
            <div style="font-size: 80px; margin-bottom: 20px;">${trendEmoji}</div>
            <div style="font-size: 32px; font-weight: bold; color: ${moodColor}; margin-bottom: 10px;">
                Ortalama: ${data.averageMood}/10
            </div>
            <div style="font-size: 24px; color: #ffff00; margin-bottom: 30px;">
                Eğilim: ${data.trend === 'improving' ? '📈 İyileşiyor' : data.trend === 'declining' ? '📉 Kötüleşiyor' : '➡️ Sabit'}
            </div>
        </div>
        
        <div style="background: rgba(255,255,0,0.1); padding: 20px; border-radius: 10px; border-left: 5px solid #ffff00;">
            <h3 style="font-size: 24px; margin-bottom: 15px;">📋 Son 5 Günün Ruh Hali:</h3>
            <div style="display: grid; gap: 10px;">
                ${data.recentMoods.map((mood, idx) => `
                    <div style="display: flex; align-items: center; gap: 15px; padding: 10px; background: rgba(0,200,100,0.1); border-radius: 8px;">
                        <div style="font-size: 20px; font-weight: bold; color: #ffff00; min-width: 60px;">
                            ${mood.moodScore}/10
                        </div>
                        <div style="flex: 1; font-size: 18px; color: #ffffff;">
                            ${new Date(mood.timestamp).toLocaleString('tr-TR', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
        
        <div style="margin-top: 20px; padding: 20px; background: rgba(0,150,255,0.15); border-radius: 10px; border-left: 5px solid #0096ff; font-size: 18px; line-height: 1.6;">
            <strong>💡 Bilgi:</strong> Ruh haliniz sistem tarafından günlük sohbetleriniz analiz edilerek izleniyor. 
            Anormal bir değişim varsa, aile üyeleriniz otomatik olarak bilgilendirilecektir.
        </div>
    `;
}

// ================= HEALTH RECORDS (Sağlık Kayıtları) =================
async function loadHealthRecords() {
    const token = requireAuthToken();
    if (!token) return;
    try {
        const response = await safeFetch(`${API_BASE}/api/health-records?token=${token}`);
        if (!response) return;
        if (response.ok) {
            const data = await safeReadJson(response, []);
            renderHealthRecords(data);
        }
    } catch (error) {
        console.error('Sağlık kayıtları yükleme hatası:', error);
    }
}

function renderHealthRecords(records) {
    const healthDiv = document.getElementById('healthRecordsContent') || document.getElementById('healthRecordsScreen');
    if (!healthDiv) return;

    let html = '<div style="padding: 20px;">';
    html += '<h2 style="color: #ffff00; text-align: center; margin-bottom: 30px;">📊 SAĞLIK KAYITLARI</h2>';

    if (records.length === 0) {
        html += '<div style="color: #ffff00; text-align: center; font-size: 20px;">Henüz kayıt bulunmamaktadır.</div>';
    } else {
        // Group by record type
        const byType = {};
        records.forEach(r => {
            if (!byType[r.recordType]) byType[r.recordType] = [];
            byType[r.recordType].push(r);
        });

        // Display each type
        Object.keys(byType).forEach(type => {
            const typeRecords = byType[type];
            const latest = typeRecords[0];
            const icon = type.includes('tansiyon') ? '🫀' : '🩸';
            const alertColor = latest.alertLevel === 'critical' ? '#ff0000' : latest.alertLevel === 'warning' ? '#ffaa00' : '#00ff00';

            html += `<div style="background: #333; padding: 20px; margin: 10px 0; border: 3px solid ${alertColor}; border-radius: 10px;">
                <div style="font-size: 28px; color: #ffff00; margin-bottom: 10px;">
                    ${icon} ${type.toUpperCase()}: ${latest.value} ${latest.unit}
                </div>
                <div style="color: ${alertColor}; font-size: 18px; margin-bottom: 10px;">
                    ${latest.alertLevel === 'critical' ? '🚨 KRİTİK' : latest.alertLevel === 'warning' ? '⚠️ UYARI' : '✅ NORMAL'}
                </div>
                <div style="color: #999; font-size: 14px;">Son: ${new Date(latest.timestamp).toLocaleString('tr-TR')}</div>
                <div style="color: #666; font-size: 12px; margin-top: 5px;">Son 5 kayıt: ${typeRecords.slice(0, 5).map(r => r.value).join(', ')}</div>
            </div>`;
        });
    }

    html += '<div style="margin-top: 30px;"><button onclick="showAddHealthRecord()" class="btn-mega" style="background: linear-gradient(135deg, #667eea, #764ba2); width: 100%; margin-bottom: 10px;">➕ YENİ KAYIT</button></div>';
    html += '</div>';

    healthDiv.innerHTML = html;
}

function showAddHealthRecord() {
    const type = prompt('Hangi ölçümü eklemek istersiniz?\n1 = Tansiyon (mmHg)\n2 = Kan Şekeri (mg/dL)\n3 = Kolesterol (mg/dL)');
    if (!type) return;

    const recordType = type === '1' ? 'tansiyon' : type === '2' ? 'şeker' : 'kolesterol';
    const unit = type === '1' ? 'mmHg' : 'mg/dL';
    const value = prompt(`${recordType} değerini girin (${unit}):`);
    if (!value) return;

    addHealthRecord(recordType, value, unit);
}

async function addHealthRecord(recordType, value, unit) {
    const token = requireAuthToken();
    if (!token) return;
    const numericValue = Number.parseFloat(String(value).replace(',', '.'));
    let localSeverity = 'normal';
    if (recordType === 'tansiyon') {
        if (numericValue >= 180) localSeverity = 'high';
        else if (numericValue >= 140) localSeverity = 'warning';
    }
    if (recordType === 'şeker') {
        if (numericValue >= 200) localSeverity = 'high';
        else if (numericValue >= 140) localSeverity = 'warning';
    }
    const body = JSON.stringify({
        recordType: recordType,
        value: numericValue,
        unit: unit
    });

    try {
        const response = await safeFetch(`${API_BASE}/api/health-records?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: body
        });
        if (!response) return;

        if (response.ok) {
            const result = await response.json();
            speak(`Sağlık kaydı başarıyla eklendi. ${recordType}: ${value} ${unit}`);
            loadHealthRecords();

            const severity = localSeverity === 'high' || result.healthStatus === 'critical' ? 'high' : (localSeverity === 'warning' ? 'normal' : 'normal');
            await sendFamilyNotification('health_record', `Sağlık kaydı: ${recordType} ${value} ${unit}`, severity);

            // If critical alert
            if (localSeverity === 'high' || result.alertLevel === 'critical' || result.healthStatus === 'critical') {
                speak("DİKKAT! Kritik seviye. Aile üyeleri uyarıldı. Doktor'a başvurun!");
                await sendEmergencyBroadcast({
                    location: (await getCurrentLocation())?.label || 'Unknown',
                    notes: `Kritik sağlık verisi: ${recordType} ${value} ${unit}`
                });
            }
        }
    } catch (error) {
        console.error('Sağlık kaydı ekleme hatası:', error);
    }
}

const currentScreen = document.querySelector('.screen.active');
if (currentScreen && currentScreen.id === 'homeScreen') {
    startSmartDialog();
}
