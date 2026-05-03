/* ===========================
   YAŞLI ASISTANI - GENEL JS
   ===========================
*/

// =================== EKRAN YÖNETIMI ===================

const DEFAULT_API_BASE = (window.API_BASE?.trim?.() || 'https://vitaguard.app');
const FALLBACK_API_BASE = (window.API_FALLBACK_BASE?.trim?.() || 'https://safeguardian-elderly-safety-emergency-support-ap-production.up.railway.app');
const IOS_SIMULATOR_API_BASE = FALLBACK_API_BASE;
const DEMO_OFFLINE_TOKEN = 'demo-offline-token';
const IS_CAPACITOR_IOS = Boolean(window.Capacitor) && /iPhone|iPad|iPod/i.test(navigator.userAgent || '');
const API_TIMEOUT_MS = 12000;

// --- TTS ENGINE TEMPORARILY DISABLED ---
// SpeechSynthesis/TextToSpeech blocks are intentionally disabled until app stability is restored.

// --- WebProcess crash prevention ---
// Uncaught promise rejections crash Capacitor iOS WebView; absorb them here.
window.addEventListener('unhandledrejection', (event) => {
    console.warn('[SafeGuardian] Unhandled promise rejection caught (prevented crash):', event.reason);
    event.preventDefault();
});
window.addEventListener('error', (event) => {
    console.warn('[SafeGuardian] Global JS error caught:', event.message, event.filename, event.lineno);
    // Do not re-throw; let the app keep running.
    return true;
});

// --- Backend reachability tracking ---
let _backendUnreachableCount = 0;
const BACKEND_FAIL_THRESHOLD = 2; // trigger offline mode after this many consecutive fails
function _onBackendFail() {
    _backendUnreachableCount++;
    if (_backendUnreachableCount >= BACKEND_FAIL_THRESHOLD && !isOfflineDemoModeEnabled()) {
        sessionStorage.setItem('offlineDemoMode', 'true');
        const banner = document.getElementById('offlineBanner');
        if (banner) { banner.style.display = 'block'; banner.textContent = '📡 Sunucuya bağlanılamıyor — Çevrimdışı modda çalışılıyor.'; }
        console.warn('[SafeGuardian] Backend unreachable threshold reached → offline demo mode activated');
    }
}
function _onBackendSuccess() {
    _backendUnreachableCount = 0;
}

// =================== DİL / I18N ===================
const TRANSLATIONS = {
    tr: {
        emailLabel: 'E-POSTA', passwordLabel: 'ŞİFRE', rememberMeLabel: 'BENİ HATIRLA',
        loginBtn: 'GİRİŞ YAP', registerBtn: 'KAYIT OL', forgotBtn: 'ŞİFREMİ UNUTTUM',
        appleSignIn: 'Sign in with Apple', biometricLoginBtn: '🔓 Face ID ile Giriş',
        registerTitle: 'KAYIT OL', backBtn: '← GERİ', fullNameLabel: 'AD SOYAD',
        phoneLabel: 'TELEFON', birthDateLabel: 'DOĞUM TARİHİ', completeRegBtn: 'KAYDI TAMAMLA',
        logoutBtn: 'ÇIKIŞ', medicationsLabel: 'İLAÇLARIM', familyLabel: 'AİLE', helpLabel: 'YARDIM',
        emergencyBtn: '🚨 ACİL YARDIM', howAreYou: 'NASILSIN?',
        moodGood: '😊 İYİYİM', moodOk: '😐 İDARE EDER', moodBad: '😟 İYİ DEĞİLİM',
        moodLabel: 'RUH HALİ', cameraLabel: 'KAMERA', healthLabel: 'SAĞLIK',
        doctorBtn: '🩺 DOKTORA GÖSTER', voiceBtn: '🎙️ DİNLE / SESİ TEKRARLA',
        moodScreenTitle: 'RUH HALİ TAKIBI', healthScreenTitle: 'SAĞLIK KAYITLARI',
        medicationsTitle: 'İLAÇLARIM', addMedBtn: '➕ YENİ İLAÇ EKLE',
        addMedTitle: 'YENİ İLAÇ EKLE', medNameLabel: 'İLAÇ ADI', medNotesLabel: 'NOTLAR',
        timesLabel: 'SAATLER', saveBtn: 'KAYDET',
        familyTitle: 'AİLE ÜYELERİ', addFamilyBtn: '➕ AİLE ÜYESİ EKLE',
        addFamilyTitle: 'AİLE ÜYESİ EKLE', nameLabel: 'AD', relationLabel: 'İLİŞKİ',
        helpTitle: 'YARDIM', understoodBtn: 'ANLADIM',
        emergencyModalTitle: 'Acil yardım çağrılıyor',
        emergencyModalDesc: '5 saniye içinde onaylanacak. Aileye ve sağlık kuruluşlarına bildirim ile konum paylaşılır. İptal edebilirsiniz.',
        confirmBtn: 'ONAYLA', cancelBtn: 'İPTAL',
        voiceOnboardingTitle: 'Sesli Asistanı Başlat',
        voiceOnboardingDesc: 'Mikrofona dokun ve konuş. İstersen "İlaçlarım", "Aile", "Yardım" diyebilirsin.',
        voiceStartBtn: 'DİNLEMEYİ BAŞLAT', voiceSkipBtn: 'ŞİMDİ DEĞİL',
        settingsBtn: '⚙️ AYARLAR', apiLabel: 'API ADRESİ',
        apiSaveBtn: 'KAYDET', apiClearBtn: 'SIFIRLA',
        largeTextOn: 'YAZIYI BÜYÜT', largeTextOff: 'YAZIYI KÜÇÜLT',
        contrastOn: 'KONTRASTI ARTIR', contrastOff: 'KONTRASTI AZALT',
        simpleModeOn: 'BASİT MOD', simpleModeOff: 'BASİT MOD KAPAT',
        resetViewBtnLabel: 'GÖRÜNÜMÜ SIFIRLA', langLabel: 'DİL',
        sessionExpired: 'Oturum Süresi Doldu', sessionExpiredMsg: 'Lütfen tekrar giriş yapın.',
        connError: 'Bağlantı hatası. API adresini kontrol edin.',
        loginFailed: 'Giriş başarısız. E-posta veya şifre hatalı.',
        errorTitle: 'Hata', successTitle: 'Başarılı',
        welcomeMsg: 'Hoş geldiniz',
        homeGuidance: '',
        medicationGuidance: 'İlaçlarım sayfasındasınız. Aldığınız ilaçlar burada listelenir. Yeni ilaç eklemek için aşağıdaki butona basın.',
        addMedGuidance: 'Yeni ilaç ekle formunda bulunuyorsunuz. İlaç adını ve saatlerini girin.',
        familyGuidance: 'Aile üyeleri sayfasına hoş geldiniz. Sizinle iletişim kuran aile üyeleri burada listelenir.',
        helpGuidance: 'Yardım sayfasında bulunuyorsunuz. Tüm özellikleri burada açıklıyoruz.',
        loginGuidance: '',
        simpleBannerText: 'Basit mod açık: Ek özellikler gizlendi.',
        apiSaved: 'Kaydedildi', apiSavedMsg: 'API adresi güncellendi',
        apiReset: 'Sıfırlandı', apiResetMsg: 'API adresi temizlendi',
        demoHint: 'Destek için: support@vitaguard.app',
        relationSelect: 'Seçin...',
        relationChild: 'Çocuk', relationGrandchild: 'Torun', relationSpouse: 'Eş',
        relationSibling: 'Kardeş', relationOther: 'Diğer',
        accountBtn: '👤 HESAP',
        profileTitle: '👤 HESAP',
        subscriptionTitle: '💳 ABONELİK',
        profileCardTitle: '👤 HESAP BİLGİLERİ',
        userFullName: 'AD SOYAD', userEmail: 'E-POSTA',
        subscriptionStatus: 'ABONE DURUMU', daysRemaining: 'KALAN GÜN',
        premiumPlan: '⭐ PREMIUM', standardPlan: '📦 STANDART',
        upgradePremium: '⭐ PREMIUM KAPAT', subscriptionButton: '💳 ABONELİK KALAN',
        editProfileBtn: '✏️ BİLGİ GÜNCELLE', logoutBtn: '🚪 ÇIKIŞ YAP',
        editLogoutBtn: '🚪 ÇIKIŞ YAP',
        privacyPolicyBtn: '📄 GİZLİLİK SÖZLEŞMESİ',
        termsOfUseBtn: '📘 KULLANIM KOŞULLARI',
        deleteAccountBtn: '🗑️ HESABI KALICI SİL',
        restorePurchasesBtn: '🔄 SATIN ALMALARI GERİ YÜKLE',
        cancelSubscriptionBtn: '⛔ ABONELİĞİ İPTAL ET',
        manageSubscriptionsBtn: '⚙️ ABONELİKLERİ YÖNET',
        subscriptionLegalNote: 'Otomatik yenilemeli aboneliklerde iOS Ayarlar > Apple Kimliği > Abonelikler ekranından yönetim yapılabilir.',
        appleUnavailable: 'Apple girişi bu cihazda kullanılamıyor.',
        appleLoginFailed: 'Apple girişi başarısız oldu.',
        biometricUnavailable: 'Face ID / biyometrik doğrulama desteklenmiyor.',
        biometricNoSession: 'Önce normal giriş yapın. Sonra Face ID ile hızlı giriş kullanabilirsiniz.',
        biometricFailed: 'Biyometrik doğrulama başarısız.',
        subscriptionCancelSuccess: 'Abonelik iptal edildi. Dönem sonuna kadar aktif kalır.',
        subscriptionCancelFailed: 'Abonelik iptal edilemedi.',
        packageInfo: 'PAKET BİLGİLERİ', currentPackage: '📦 MEVCUT PAKET',
        endDate: '📅 BİTİŞ TARİHİ', features: '✨ ÖZELLİKLER',
        basicFeature1: '✓ Temel İlaç Yönetimi',
        basicFeature2: '✓ Aile Üyeleri',
        basicFeature3: '✓ Sesli Asistan',
        closeBtn: '← KAPAT',
        basicFeatures: 'Temel İlaç Yönetimi\nAile Üyeleri\nSesli Asistan',
        premiumFeatures: 'Video Doktor Konsültasyonu\nİnsan Asistanı (24/7)\nRuh Hali Analizi (AI)\nSağlık Trendleri',
        profileUpdated: 'Adınız güncellendi',
        profileUpdateMsg: 'İsminiz başarıyla güncellendi.',
        premiumAlready: 'Premium Aktif',
        premiumAlreadyMsg: 'Zaten premium aboneniz!',
        premiumSelected: 'Premium Başarılı',
        premiumSelectedMsg: 'Tekrardan hoş geldiniz!',
        restoreSuccess: 'Satın Alımlar Geri Yüklendi',
        restoreSuccessMsg: 'Abonelik bilgileriniz güncellendi.',
        restoreFailed: 'Geri Yükleme Başarısız',
        restoreFailedMsg: 'Abonelik bilgileri alınamadı. Lütfen tekrar deneyin.',
        deleteAccountTitle: 'Hesap Silme',
        deleteAccountConfirmMsg: 'Bu işlem hesabınızı ve tüm verileri kalıcı olarak siler. Devam etmek istiyor musunuz?',
        deleteAccountPasswordPrompt: 'Güvenlik için şifrenizi girin:',
        deleteAccountCanceled: 'İptal Edildi',
        deleteAccountCanceledMsg: 'Hesap silme işlemi iptal edildi.',
        deleteAccountNeedPassword: 'Şifre Gerekli',
        deleteAccountNeedPasswordMsg: 'Hesabınızı silmek için şifre girmeniz gerekiyor.',
        deleteAccountFinalPrompt: 'Son onay için SIL yazın:',
        deleteAccountFinalMismatch: 'Onay Eksik',
        deleteAccountFinalMismatchMsg: 'Hesap silme onayı verilmedi.',
        deleteAccountSuccess: 'Hesap Silindi',
        deleteAccountSuccessMsg: 'Hesabınız ve ilişkili veriler kalıcı olarak silindi.',
        deleteAccountFailed: 'Hesap Silinemedi',
        deleteAccountFailedMsg: 'Lütfen bilgilerinizi kontrol edip tekrar deneyin.',
        medNamePlaceholder: 'Örn: Aspirin',
        medNotesPlaceholder: 'Yemekten sonra alınız',
        presetBp: 'TANSİYON', presetSugar: 'ŞEKER', presetChol: 'KOLESTEROL',
        helpMedTitle: '💊 İLAÇLARIM',
        helpMedDesc: 'Günlük ilacınızı alıp almadığınızı takip etmek için bu sayfayı kullanın. İlacınızı aldığınız zaman "ALDI" butonuna basın.',
        helpFamilyTitle: '👨‍👩‍👧 AİLE',
        helpFamilyDesc: 'Çocuklarınız ve torununuz uzaktan bilgi almak için bu sayfada sizinle bağlanabilir.',
        helpVoiceTitle: '🎤 SES KOMUTU',
        helpVoiceDesc: 'Mikrofona konuşarak "İlaç ekle" veya "Ana sayfa" diyerek komut verebilirsiniz.',
        helpEmergencyTitle: '📞 ACİL DURUMDA',
        helpEmergencyDesc: 'Yardım almak için YARDIM butonuna basın ve aile üyeleriniz bilgilendirilecektir.',
        medsEmpty: 'Henüz ilaç eklenmedi',
        voiceHeard: 'Komut alındı',
        voiceUnknown: 'Komutu anlayamadım. Lütfen tekrar edin.',
        loginWelcome: 'Tekrar hoş geldiniz',
        loginSub: '',
        medsTimeLabel: 'Saatler', medsUnspecified: 'Belirtilmedi', medsRemaining: 'Kalan', medsTakenBtn: '✓ İLACIMI İÇTİM',
        familyMemberDefault: 'Aile Üyesi',
        moodThanksTitle: 'Teşekkürler', moodSavedMsg: 'Ruh haliniz kaydedildi', moodSaveError: 'Ruh hali kaydedilemedi',
        moodAverageLabel: 'Ortalama', moodTrendLabel: 'Eğilim', moodTrendImproving: '📈 İyileşiyor', moodTrendDeclining: '📉 Kötüleşiyor', moodTrendStable: '➡️ Sabit',
        moodLastFiveDays: '📋 Son 5 Günün Ruh Hali:', moodInfoTitle: '💡 Bilgi:',
        moodInfoText: 'Ruh haliniz sistem tarafından günlük sohbetleriniz analiz edilerek izleniyor. Anormal bir değişim varsa, aile üyeleriniz otomatik olarak bilgilendirilecektir.',
        healthRecordsTitle: '📊 SAĞLIK KAYITLARI', noRecordsYet: 'Henüz kayıt bulunmamaktadır.',
        healthCritical: '🚨 KRİTİK', healthWarning: '⚠️ UYARI', healthNormal: '✅ NORMAL', healthLastLabel: 'Son', healthLastFiveLabel: 'Son 5 kayıt', addNewRecordBtn: '➕ YENİ KAYIT',
        emailPlaceholder: 'ornek@mail.com',
        passwordPlaceholder: 'Şifrenizi girin',
    },
    en: {
        emailLabel: 'EMAIL', passwordLabel: 'PASSWORD', rememberMeLabel: 'REMEMBER ME',
        loginBtn: 'SIGN IN', registerBtn: 'REGISTER', forgotBtn: 'FORGOT PASSWORD',
        appleSignIn: 'Sign in with Apple', biometricLoginBtn: '🔓 Sign in with Face ID',
        registerTitle: 'REGISTER', backBtn: '← BACK', fullNameLabel: 'FULL NAME',
        phoneLabel: 'PHONE', birthDateLabel: 'DATE OF BIRTH', completeRegBtn: 'COMPLETE REGISTRATION',
        logoutBtn: 'LOGOUT', medicationsLabel: 'MY MEDICATIONS', familyLabel: 'FAMILY', helpLabel: 'HELP',
        emergencyBtn: '🚨 EMERGENCY HELP', howAreYou: 'HOW ARE YOU?',
        moodGood: '😊 FEELING GOOD', moodOk: '😐 SO SO', moodBad: '😟 NOT FEELING WELL',
        moodLabel: 'MOOD', cameraLabel: 'CAMERA', healthLabel: 'HEALTH',
        doctorBtn: '🩺 SHOW DOCTOR', voiceBtn: '🎙️ LISTEN / REPEAT',
        moodScreenTitle: 'MOOD TRACKING', healthScreenTitle: 'HEALTH RECORDS',
        medicationsTitle: 'MY MEDICATIONS', addMedBtn: '➕ ADD MEDICATION',
        addMedTitle: 'ADD MEDICATION', medNameLabel: 'MEDICATION NAME', medNotesLabel: 'NOTES',
        timesLabel: 'TIMES', saveBtn: 'SAVE',
        familyTitle: 'FAMILY MEMBERS', addFamilyBtn: '➕ ADD FAMILY MEMBER',
        addFamilyTitle: 'ADD FAMILY MEMBER', nameLabel: 'NAME', relationLabel: 'RELATION',
        helpTitle: 'HELP', understoodBtn: 'GOT IT',
        emergencyModalTitle: 'Calling emergency help',
        emergencyModalDesc: 'Will be confirmed in 5 seconds. Location and notification will be shared with family and healthcare. You can cancel.',
        confirmBtn: 'CONFIRM', cancelBtn: 'CANCEL',
        voiceOnboardingTitle: 'Start Voice Assistant',
        voiceOnboardingDesc: 'Tap the microphone and speak. You can say "Medications", "Family", "Help".',
        voiceStartBtn: 'START LISTENING', voiceSkipBtn: 'NOT NOW',
        settingsBtn: '⚙️ SETTINGS', apiLabel: 'API ADDRESS',
        apiSaveBtn: 'SAVE', apiClearBtn: 'RESET',
        largeTextOn: 'INCREASE TEXT SIZE', largeTextOff: 'DECREASE TEXT SIZE',
        contrastOn: 'INCREASE CONTRAST', contrastOff: 'DECREASE CONTRAST',
        simpleModeOn: 'SIMPLE MODE', simpleModeOff: 'SIMPLE MODE OFF',
        resetViewBtnLabel: 'RESET DISPLAY', langLabel: 'LANGUAGE',
        sessionExpired: 'Session Expired', sessionExpiredMsg: 'Please login again.',
        connError: 'Connection error. Please check API address.',
        loginFailed: 'Login failed. Please check your email and password.',
        errorTitle: 'Error', successTitle: 'Success',
        welcomeMsg: 'Welcome',
        homeGuidance: '',
        medicationGuidance: 'You are on the Medications page. Your medications are listed here.',
        addMedGuidance: 'You are on the Add Medication form. Enter the medication name and times.',
        familyGuidance: 'Welcome to the Family Members page.',
        helpGuidance: 'You are on the Help page.',
        loginGuidance: '',
        simpleBannerText: 'Simple mode on: Extra features hidden.',
        apiSaved: 'Saved', apiSavedMsg: 'API address updated',
        apiReset: 'Reset', apiResetMsg: 'API address cleared',
        demoHint: 'For support: support@vitaguard.app',
        relationSelect: 'Select...',
        relationChild: 'Child', relationGrandchild: 'Grandchild', relationSpouse: 'Spouse',
        relationSibling: 'Sibling', relationOther: 'Other',
        accountBtn: '👤 ACCOUNT',
        profileTitle: '👤 ACCOUNT',
        subscriptionTitle: '💳 SUBSCRIPTION',
        profileCardTitle: '👤 ACCOUNT DETAILS',
        userFullName: 'FULL NAME', userEmail: 'EMAIL',
        subscriptionStatus: 'SUBSCRIPTION STATUS', daysRemaining: 'DAYS LEFT',
        premiumPlan: '⭐ PREMIUM', standardPlan: '📦 STANDARD',
        upgradePremium: '⭐ UPGRADE PREMIUM', subscriptionButton: '💳 VIEW SUBSCRIPTION',
        editProfileBtn: '✏️ UPDATE INFO', logoutBtn: '🚪 LOGOUT',
        editLogoutBtn: '🚪 LOGOUT',
        privacyPolicyBtn: '📄 PRIVACY POLICY',
        termsOfUseBtn: '📘 TERMS OF USE',
        deleteAccountBtn: '🗑️ DELETE ACCOUNT',
        restorePurchasesBtn: '🔄 RESTORE PURCHASES',
        cancelSubscriptionBtn: '⛔ CANCEL SUBSCRIPTION',
        manageSubscriptionsBtn: '⚙️ MANAGE SUBSCRIPTIONS',
        subscriptionLegalNote: 'For auto-renewable subscriptions, you can manage billing in iOS Settings > Apple ID > Subscriptions.',
        appleUnavailable: 'Apple sign-in is not available on this device.',
        appleLoginFailed: 'Apple sign-in failed.',
        biometricUnavailable: 'Face ID / biometric authentication is unavailable.',
        biometricNoSession: 'Please sign in once with email/password first, then use Face ID quick sign-in.',
        biometricFailed: 'Biometric authentication failed.',
        subscriptionCancelSuccess: 'Subscription cancelled. It stays active until period end.',
        subscriptionCancelFailed: 'Subscription cancel failed.',
        packageInfo: 'PACKAGE INFO', currentPackage: '📦 CURRENT PLAN',
        endDate: '📅 END DATE', features: '✨ FEATURES',
        basicFeature1: '✓ Basic Medication Management',
        basicFeature2: '✓ Family Members',
        basicFeature3: '✓ Voice Assistant',
        closeBtn: '← CLOSE',
        basicFeatures: 'Basic Medication Management\nFamily Members\nVoice Assistant',
        premiumFeatures: 'Video Doctor Consultation\nHuman Assistant (24/7)\nAI Mood Analysis\nHealth Trends',
        profileUpdated: 'Name Updated',
        profileUpdateMsg: 'Your name was successfully updated.',
        premiumAlready: 'Premium Active',
        premiumAlreadyMsg: 'You are already a premium subscriber!',
        premiumSelected: 'Premium Successful',
        premiumSelectedMsg: 'Welcome back!',
        restoreSuccess: 'Purchases Restored',
        restoreSuccessMsg: 'Your subscription details were refreshed.',
        restoreFailed: 'Restore Failed',
        restoreFailedMsg: 'Subscription details could not be loaded. Please try again.',
        deleteAccountTitle: 'Delete Account',
        deleteAccountConfirmMsg: 'This action permanently deletes your account and all data. Do you want to continue?',
        deleteAccountPasswordPrompt: 'For security, enter your password:',
        deleteAccountCanceled: 'Cancelled',
        deleteAccountCanceledMsg: 'Account deletion was cancelled.',
        deleteAccountNeedPassword: 'Password Required',
        deleteAccountNeedPasswordMsg: 'You must enter your password to delete your account.',
        deleteAccountFinalPrompt: 'Type DELETE for final confirmation:',
        deleteAccountFinalMismatch: 'Confirmation Missing',
        deleteAccountFinalMismatchMsg: 'Account deletion confirmation not provided.',
        deleteAccountSuccess: 'Account Deleted',
        deleteAccountSuccessMsg: 'Your account and related data were permanently deleted.',
        deleteAccountFailed: 'Delete Failed',
        deleteAccountFailedMsg: 'Please verify your information and try again.',
        medNamePlaceholder: 'e.g. Aspirin',
        medNotesPlaceholder: 'Take after meal',
        presetBp: 'BLOOD PRESSURE', presetSugar: 'BLOOD SUGAR', presetChol: 'CHOLESTEROL',
        helpMedTitle: '💊 MY MEDICATIONS',
        helpMedDesc: 'Use this page to track daily medicines. Tap "TAKEN" when you take your medicine.',
        helpFamilyTitle: '👨‍👩‍👧 FAMILY',
        helpFamilyDesc: 'Your family can connect and follow your status from this page.',
        helpVoiceTitle: '🎤 VOICE COMMAND',
        helpVoiceDesc: 'You can say commands like "Add medication" or "Home screen".',
        helpEmergencyTitle: '📞 IN EMERGENCY',
        helpEmergencyDesc: 'Press HELP button to alert your family members.',
        medsEmpty: 'No medications added yet',
        voiceHeard: 'Command received',
        voiceUnknown: 'I could not understand the command. Please repeat.',
        loginWelcome: 'Welcome back',
        loginSub: 'Sign in quickly and securely',
        medsTimeLabel: 'Times', medsUnspecified: 'Not specified', medsRemaining: 'Remaining', medsTakenBtn: '✓ MARK AS TAKEN',
        familyMemberDefault: 'Family Member',
        moodThanksTitle: 'Thank You', moodSavedMsg: 'Your mood has been saved', moodSaveError: 'Mood could not be saved',
        moodAverageLabel: 'Average', moodTrendLabel: 'Trend', moodTrendImproving: '📈 Improving', moodTrendDeclining: '📉 Declining', moodTrendStable: '➡️ Stable',
        moodLastFiveDays: '📋 Last 5 Days Mood:', moodInfoTitle: '💡 Info:',
        moodInfoText: 'Your mood is monitored by analyzing daily conversations. If an abnormal change is detected, your family members are automatically informed.',
        healthRecordsTitle: '📊 HEALTH RECORDS', noRecordsYet: 'No records yet.',
        healthCritical: '🚨 CRITICAL', healthWarning: '⚠️ WARNING', healthNormal: '✅ NORMAL', healthLastLabel: 'Last', healthLastFiveLabel: 'Last 5 records', addNewRecordBtn: '➕ NEW RECORD',
        emailPlaceholder: 'example@mail.com',
        passwordPlaceholder: 'Enter your password',
    }
};

function detectPreferredLanguage() {
    const savedLang = localStorage.getItem('appLang');
    if (savedLang && TRANSLATIONS[savedLang]) {
        return savedLang;
    }

    // Capacitor iOS simulator always reports 'en' browser lang regardless of device locale.
    // Default to Turkish so the app launches in TR unless the user explicitly changes it.
    if (IS_CAPACITOR_IOS) return 'tr';

    const browserLanguages = Array.isArray(navigator.languages) && navigator.languages.length
        ? navigator.languages
        : [navigator.language || navigator.userLanguage || 'tr'];

    for (const language of browserLanguages) {
        const normalized = String(language || '').toLowerCase();
        if (normalized.startsWith('tr')) return 'tr';
        if (normalized.startsWith('en')) return 'en';
    }

    return 'tr';
}

let currentLang = detectPreferredLanguage();

function t(key) {
    const dictionary = TRANSLATIONS[currentLang] || TRANSLATIONS.tr;
    if (Object.prototype.hasOwnProperty.call(dictionary, key)) {
        return dictionary[key];
    }
    return key;
}

function getApiBase() {
    const rawStored = localStorage.getItem('apiBaseUrl')?.trim();
    const configured = window.API_BASE?.trim?.();
    const origin = window.location?.origin || '';
    const protocol = window.location?.protocol || '';
    const userAgent = navigator.userAgent || '';
    const isHttpOrigin = /^https?:\/\//i.test(origin);
    const isCapacitorRuntime = Boolean(window.Capacitor);
    const isIosSimulator = /iPhone Simulator|iPad Simulator|Simulator/i.test(userAgent);
    const isCapacitorLocalhost = /^capacitor:\/\/localhost/i.test(origin) || /^capacitor:/i.test(protocol);
    const stored = rawStored && /:3000\b/.test(rawStored)
        ? ''
        : rawStored;

    if (rawStored && !stored) {
        localStorage.removeItem('apiBaseUrl');
    }

    let candidate = stored || configured || (isHttpOrigin ? origin : '');

    // Hosted web'de farklı domain'e yanlış/stale API yazıldıysa (ör. vitaguard.app)
    // CORS + 503'e düşmemek için aynı origin/configured API'ye geri dön.
    if (!isCapacitorRuntime && isHttpOrigin && stored) {
        try {
            const storedOrigin = new URL(/^https?:\/\//i.test(stored) ? stored : `https://${stored}`).origin;
            const sameOrigin = storedOrigin === origin;
            const isLocalDev = /^https?:\/\/(localhost|127\.0\.0\.1|192\.168\.)/i.test(storedOrigin);
            if (!sameOrigin && !isLocalDev) {
                localStorage.removeItem('apiBaseUrl');
                candidate = configured || origin;
            }
        } catch {
            localStorage.removeItem('apiBaseUrl');
            candidate = configured || origin;
        }
    }

    if (!stored && isCapacitorRuntime && isCapacitorLocalhost) {
        candidate = IOS_SIMULATOR_API_BASE;
    }

    if (!candidate && isCapacitorRuntime) {
        candidate = isIosSimulator ? IOS_SIMULATOR_API_BASE : DEFAULT_API_BASE;
    }

    if (isCapacitorRuntime && (isIosSimulator || isCapacitorLocalhost) && /vitaguard\.app/i.test(candidate || '')) {
        candidate = IOS_SIMULATOR_API_BASE;
    }

    if (candidate && !/^https?:\/\//i.test(candidate)) {
        candidate = `http://${candidate}`;
    }

    try {
        return new URL(candidate).origin;
    } catch {
        return (isIosSimulator || isCapacitorLocalhost) ? IOS_SIMULATOR_API_BASE : DEFAULT_API_BASE;
    }
}

const API_BASE = getApiBase();
const PreferencesPlugin = window.Capacitor?.Plugins?.Preferences;
const GeolocationPlugin = window.Capacitor?.Plugins?.Geolocation;

let lastGuidanceText = '';
let emergencyTimer = null;
let ignoreNextA11yClose = false;
let isEmergencyModalOpen = false;
var speechRecognition = null;
var isListening = false;
let lastVoiceCommand = '';
let lastVoiceCommandAt = 0;
const MEDICATION_CONFIRM_WARNING_MS = 15 * 60 * 1000;
const MEDICATION_CONFIRM_CRITICAL_MS = 30 * 60 * 1000;
const medicationConfirmTimers = new Map();
const medicationReminderState = new Map();
let subscriptionCache = null;
let currentMedicationsCache = [];
let careRoutineStarted = false;
let authTokenCache = null;
let userHasInteracted = !IS_CAPACITOR_IOS;
let lastSpokenText = '';
let lastSpokenAt = 0;

const PUBLIC_SCREENS = new Set(['loginScreen', 'registerScreen', 'helpScreen', 'homeScreen']);

function isDemoOfflineToken(value) {
    return String(value || '').trim() === DEMO_OFFLINE_TOKEN;
}

function isOfflineDemoModeEnabled() {
    return sessionStorage.getItem('offlineDemoMode') === 'true';
}

function clearOfflineDemoMode() {
    sessionStorage.removeItem('offlineDemoMode');
}

async function getStoredToken() {
    if (authTokenCache) return authTokenCache;

    if (PreferencesPlugin) {
        try {
            const result = await PreferencesPlugin.get({ key: 'token' });
            const token = result?.value || '';
            if (isDemoOfflineToken(token)) {
                await PreferencesPlugin.remove({ key: 'token' });
            } else if (token) {
                authTokenCache = token;
                return token;
            }
        } catch (error) {
            console.warn('Preferences token okuma hatası:', error);
        }
    }

    const webToken = localStorage.getItem('token') || '';
    if (isDemoOfflineToken(webToken)) {
        localStorage.removeItem('token');
        authTokenCache = null;
        return '';
    }
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
    clearOfflineDemoMode();
    if (!PreferencesPlugin) return;
    try {
        await PreferencesPlugin.remove({ key: 'token' });
    } catch (error) {
        console.warn('Preferences token silme hatası:', error);
    }
}

function hasAuthTokenSync() {
    return Boolean(authTokenCache || localStorage.getItem('token') || isOfflineDemoModeEnabled());
}

function requireAuthToken() {
    if (isOfflineDemoModeEnabled()) {
        return DEMO_OFFLINE_TOKEN;
    }
    const token = authTokenCache || localStorage.getItem('token');
    if (!token) {
        showScreen('loginScreen');
        return null;
    }
    return token;
}

async function requireAuthTokenAsync() {
    if (isOfflineDemoModeEnabled()) {
        return DEMO_OFFLINE_TOKEN;
    }
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
    showNotification(t('sessionExpired'), t('sessionExpiredMsg'), 'error');
    showScreen('loginScreen');
}

function forceCloseLoadingAndRecover() {
    try {
        const loadingLike = ['loadingScreen', 'globalLoading', 'spinnerOverlay', 'overlayLoading'];
        loadingLike.forEach((id) => {
            const el = document.getElementById(id);
            if (!el) return;
            el.classList.remove('active');
            el.style.display = 'none';
            el.setAttribute('hidden', '');
        });
        document.querySelectorAll('[data-loading="true"], .loading, .loading-overlay, .spinner, .spinner-overlay').forEach((el) => {
            el.classList.remove('active');
            el.style.display = 'none';
            el.setAttribute('hidden', '');
        });
    } catch (_) { }

    if (hasAuthTokenSync()) {
        showScreen('homeScreen');
    } else {
        showScreen('loginScreen');
    }
}

async function safeFetch(url, options, fetchOpts = {}) {
    let finalUrl = url;
    let extractedToken = null;
    let parsedUrl = null;
    try {
        const parsed = new URL(url, API_BASE);
        parsedUrl = parsed;
        extractedToken = parsed.searchParams.get('token');
        if (extractedToken) {
            parsed.searchParams.delete('token');
        }
        finalUrl = parsed.toString();
    } catch (error) {
        console.error('Geçersiz API adresi:', { url, apiBase: API_BASE, error });
        if (!fetchOpts.silent) showNotification(t('errorTitle'), 'API adresi geçersiz. Lütfen ayarlardan güncelleyin.', 'error');
        return null;
    }

    const requestOptionsBase = { ...(options || {}) };
    const headers = new Headers(requestOptionsBase.headers || {});
    const fallbackToken = authTokenCache || localStorage.getItem('token');
    const bearer = extractedToken || fallbackToken;
    if (isDemoOfflineToken(bearer)) {
        return null;
    }
    if (bearer && !headers.has('Authorization')) {
        headers.set('Authorization', `Bearer ${bearer}`);
    }
    requestOptionsBase.headers = headers;

    const retryTargets = [finalUrl];
    if (!fetchOpts.disableFallbackRetry && parsedUrl) {
        try {
            const defaultUrl = new URL(`${parsedUrl.pathname}${parsedUrl.search}`, DEFAULT_API_BASE).toString();
            if (!retryTargets.includes(defaultUrl)) {
                retryTargets.push(defaultUrl);
            }

            const secondaryUrl = new URL(`${parsedUrl.pathname}${parsedUrl.search}`, FALLBACK_API_BASE).toString();
            if (!retryTargets.includes(secondaryUrl)) {
                retryTargets.push(secondaryUrl);
            }
        } catch {
            // ignore fallback URL generation failures
        }
    }

    let lastError = null;
    for (let i = 0; i < retryTargets.length; i++) {
        const targetUrl = retryTargets[i];
        const requestOptions = { ...requestOptionsBase };
        const timeoutMs = Number(fetchOpts.timeoutMs || (i === 0 ? API_TIMEOUT_MS : API_TIMEOUT_MS + 6000));
        let timeoutId = null;

        try {
            if (typeof AbortController !== 'undefined' && !requestOptions.signal) {
                const controller = new AbortController();
                requestOptions.signal = controller.signal;
                timeoutId = setTimeout(() => controller.abort(), timeoutMs);
            }

            const response = await fetch(targetUrl, requestOptions);
            if (timeoutId) clearTimeout(timeoutId);

            if (response.status === 401) {
                handleAuthExpired();
                return null;
            }

            if (response.status === 403) {
                const isLast = i === retryTargets.length - 1;
                if (!isLast) {
                    console.warn('403 alındı, alternatif API deneniyor:', { from: targetUrl, to: retryTargets[i + 1] });
                    continue;
                }
            }

            // Fallback URL başarıyla kullanıldıysa eski API override'ı temizle
            if (i > 0) {
                localStorage.removeItem('apiBaseUrl');
            }

            _onBackendSuccess();
            return response;
        } catch (error) {
            if (timeoutId) clearTimeout(timeoutId);
            lastError = error;
            const isLast = i === retryTargets.length - 1;
            if (!isLast) {
                console.warn('İstek başarısız, alternatif API deneniyor:', { from: targetUrl, to: retryTargets[i + 1] });
                continue;
            }
        }
    }

    if (lastError?.name === 'AbortError') {
        console.warn('İstek zaman aşımına uğradı:', finalUrl);
        _onBackendFail();
        forceCloseLoadingAndRecover();
        if (!fetchOpts.silent) {
            showNotification(t('errorTitle'), currentLang === 'en' ? 'Request timed out.' : 'İstek zaman aşımına uğradı.', 'error');
        }
        return null;
    }

    console.warn('Bağlantı hatası:', { finalUrl });
    _onBackendFail();
    if (!fetchOpts.silent) showNotification(t('errorTitle'), t('connError'), 'error');
    return null;
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



function showScreen(screenId) {
    if (!PUBLIC_SCREENS.has(screenId) && !hasAuthTokenSync()) {
        screenId = 'loginScreen';
    }
    document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
    const targetScreen = document.getElementById(screenId);
    if (!targetScreen) {
        console.warn('Ekran bulunamadı:', screenId);
        const fallback = document.getElementById('homeScreen') || document.getElementById('loginScreen');
        if (!fallback) return;
        fallback.classList.add('active');
        triggerVoiceGuidance(fallback.id);
        return;
    }
    targetScreen.classList.add('active');

    // Her ekrana giriş yapılırken otomatik sesli rehberlik
    triggerVoiceGuidance(screenId);
}

function triggerVoiceGuidance(screenId) {
    // Never show guidance text on home screen — it creates unwanted green text overlay.
    // On Capacitor iOS, never auto-speak (causes SSML errors and AVAudioBuffer noise).
    if (screenId === 'homeScreen') {
        updateGuidanceText('');
        return;
    }
    if (IS_CAPACITOR_IOS) return;

    const guidance = {
        'medicationScreen': t('medicationGuidance'),
        'addMedicationScreen': t('addMedGuidance'),
        'familyScreen': t('familyGuidance'),
        'helpScreen': t('helpGuidance'),
        'loginScreen': t('loginGuidance'),
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
        guidanceEl.textContent = text || '';
        // Hide the element entirely when empty to avoid empty green box
        guidanceEl.style.display = text ? '' : 'none';
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
function applyTranslations() {
    const tr = TRANSLATIONS[currentLang] || TRANSLATIONS.tr;
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.getAttribute('data-i18n');
        if (tr[key] !== undefined) el.textContent = tr[key];
    });
    document.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
        const key = el.getAttribute('data-i18n-placeholder');
        if (tr[key] !== undefined) el.setAttribute('placeholder', tr[key]);
    });

    // Dil butonlarının aktif/pasif durumunu güncelle (login + ayarlar menüsü)
    ['tr', 'en'].forEach(lang => {
        document.querySelectorAll(`[data-lang-btn="${lang}"]`).forEach(btn => {
            btn.classList.toggle('active', lang === currentLang);
        });
    });

    // Dinamik toggle butonlarını güncelle
    const a11yToggle = document.getElementById('a11yToggle');
    if (a11yToggle) {
        const isLarge = document.body.classList.contains('large-text');
        a11yToggle.textContent = isLarge ? tr.largeTextOff : tr.largeTextOn;
    }
    const contrastToggle = document.getElementById('contrastToggle');
    if (contrastToggle) {
        const isHighContrast = document.body.classList.contains('high-contrast');
        contrastToggle.textContent = isHighContrast ? tr.contrastOff : tr.contrastOn;
    }
    const simpleHomeToggle = document.getElementById('simpleHomeToggle');
    if (simpleHomeToggle) {
        const isSimple = document.body.classList.contains('simple-home');
        simpleHomeToggle.textContent = isSimple ? tr.simpleModeOff : tr.simpleModeOn;
    }

    // HTML lang attribute
    document.documentElement.lang = currentLang;
    updateGreeting();
}

function setLanguage(lang) {
    if (!TRANSLATIONS[lang]) return;
    currentLang = lang;
    localStorage.setItem('appLang', lang);
    if (speechRecognition) {
        speechRecognition.lang = currentLang === 'en' ? 'en-US' : 'tr-TR';
    }
    applyTranslations();
    updateProfileScreen();
    updateSubscriptionScreen();
    if (isListening) {
        updateVoiceStatus(currentLang === 'en' ? '🎙️ Listening...' : '🎙️ Dinleniyor...');
    }
}

// Inline onclick çağrıları için global erişim
window.setLanguage = setLanguage;
window.applyTranslations = applyTranslations;

function openExternalUrl(url) {
    const targetUrl = String(url || '').trim();
    if (!targetUrl) return;

    const origin = window.location?.origin || '';
    let resolvedUrl = targetUrl;
    let isHttpOrHttps = /^https?:\/\//i.test(targetUrl);
    try {
        resolvedUrl = new URL(targetUrl, origin || undefined).href;
        isHttpOrHttps = /^https?:\/\//i.test(resolvedUrl);
    } catch {
        // Geçersiz URL ise mevcut davranışla dene
    }

    // Capacitor Browser eklentisi `capacitor://localhost/...` gibi local URL'leri açamaz.
    // Yerel/same-origin sayfaları uygulama içinde normal navigation ile aç.
    const isSameOrigin = Boolean(origin) && resolvedUrl.startsWith(origin);
    const isRelativePath = !/^([a-z][a-z\d+\-.]*:)?\/\//i.test(targetUrl) && targetUrl.startsWith('/');
    if (isSameOrigin || isRelativePath || !isHttpOrHttps) {
        window.location.href = resolvedUrl;
        return;
    }

    const browserPlugin = window.Capacitor?.Plugins?.Browser;
    if (browserPlugin?.open) {
        browserPlugin.open({ url: resolvedUrl }).catch(() => {
            const opened = window.open(resolvedUrl, '_blank');
            if (!opened) {
                window.location.href = resolvedUrl;
            }
        });
        return;
    }

    const opened = window.open(resolvedUrl, '_blank');
    if (!opened) {
        window.location.href = resolvedUrl;
    }
}

function openPrivacyPolicy() {
    openExternalUrl('/privacy-policy.html');
}

function openTermsOfUse() {
    openExternalUrl('/terms-of-use.html');
}

function openSubscriptionManagement() {
    openExternalUrl('https://apps.apple.com/account/subscriptions');
}

async function cancelSubscriptionFlow() {
    const token = await requireAuthTokenAsync();
    if (!token) return;

    const confirmed = confirm(currentLang === 'en'
        ? 'Do you want to cancel your subscription?'
        : 'Aboneliğinizi iptal etmek istiyor musunuz?');
    if (!confirmed) return;

    const response = await safeFetch(`${API_BASE}/api/subscription/cancel?token=${token}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
    });
    if (!response) return;

    const payload = await safeReadJson(response, {});
    if (!response.ok || !payload?.success) {
        showNotification(t('errorTitle'), payload?.message || t('subscriptionCancelFailed'), 'error');
        return;
    }

    localStorage.setItem('userPlan', 'Standart');
    subscriptionCache = payload.subscription || null;
    updateProfileScreen();
    updateSubscriptionScreen();
    showNotification(t('successTitle'), payload?.message || t('subscriptionCancelSuccess'));
}

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
    recognition.lang = currentLang === 'en' ? 'en-GB' : 'tr-TR';
    recognition.continuous = true;
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;

    recognition.onstart = () => {
        isListening = true;
        updateVoiceStatus(currentLang === 'en' ? '🎙️ Listening...' : '🎙️ Dinleniyor...');
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

        // Aynı komutun kısa sürede tekrar gelmesini engelle (iOS'da spam log azaltma)
        const now = Date.now();
        if (command === lastVoiceCommand && (now - lastVoiceCommandAt) < 1200) {
            return;
        }
        lastVoiceCommand = command;
        lastVoiceCommandAt = now;

        if (command !== 'test') {
            console.log('Gelen komut:', command);
        }
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
        if (typeof window.voiceAssistantStart === 'function') {
            window.voiceAssistantStart();
            return;
        }
        showNotification(
            currentLang === 'en' ? 'Warning' : 'Uyarı',
            currentLang === 'en' ? 'Voice recognition is not supported on this device.' : 'Bu cihazda ses tanıma desteklenmiyor',
            'error'
        );
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

function extractVoiceNumber(commandText) {
    const normalized = String(commandText || '').replace(',', '.');
    const match = normalized.match(/(\d+(?:\.\d+)?)/);
    if (!match) return null;
    const value = Number.parseFloat(match[1]);
    return Number.isFinite(value) ? value : null;
}

async function handleVoiceCommand(command) {
    const cmd = String(command || '').toLowerCase().trim();
    if (!cmd) {
        speak(t('voiceUnknown'));
        return;
    }

    const numericValue = extractVoiceNumber(cmd);

    if ((cmd.includes('ruh hali') || cmd.includes('mood')) && numericValue !== null) {
        const score = Math.round(numericValue);
        if (score >= 1 && score <= 10) {
            await submitMood(score);
            speak(currentLang === 'en' ? `Mood score saved: ${score}` : `Ruh hali puanı kaydedildi: ${score}`);
            return;
        }
    }

    if ((cmd.includes('tansiyon') || cmd.includes('blood pressure')) && numericValue !== null) {
        await addHealthRecord('tansiyon', numericValue, 'mmHg');
        speak(currentLang === 'en' ? 'Blood pressure saved.' : 'Tansiyon kaydedildi.');
        return;
    }

    if ((cmd.includes('şeker') || cmd.includes('seker') || cmd.includes('sugar') || cmd.includes('glucose')) && numericValue !== null) {
        await addHealthRecord('şeker', numericValue, 'mg/dL');
        speak(currentLang === 'en' ? 'Blood sugar saved.' : 'Kan şekeri kaydedildi.');
        return;
    }

    if (cmd.includes('ilaç') || cmd.includes('ilac') || cmd.includes('medicine') || cmd.includes('medication') || cmd.includes('drug') || cmd.includes('pill')) {
        speak(t('voiceHeard'));
        goToMedications();
        return;
    }

    if (cmd.includes('aile') || cmd.includes('family') || cmd.includes('daughter') || cmd.includes('son') || cmd.includes('call my daughter') || cmd.includes('call my son') || cmd.includes('kızımı') || cmd.includes('oğlumu') || cmd.includes('kızımı ara') || cmd.includes('oğlumu ara')) {
        speak(t('voiceHeard'));
        goToFamily();
        return;
    }

    if (cmd.includes('yardım') || cmd.includes('yardim') || cmd.includes('acil') || cmd.includes('help') || cmd.includes('emergency') || cmd.includes('sos')) {
        speak(t('voiceHeard'));
        showEmergencyConfirm();
        return;
    }

    if (cmd.includes('ana sayfa') || cmd.includes('anasayfa') || cmd.includes('ev') || cmd.includes('home')) {
        speak(t('voiceHeard'));
        goHome();
        return;
    }

    if (cmd.includes('ruh hali') || cmd.includes('mod') || cmd.includes('mood')) {
        speak(t('voiceHeard'));
        goToMoodDashboard();
        return;
    }

    if (cmd.includes('sağlık') || cmd.includes('saglik') || cmd.includes('health')) {
        speak(t('voiceHeard'));
        goToHealthRecords();
        return;
    }

    if (cmd.includes('kayıt ol') || cmd.includes('kayit ol') || cmd.includes('register') || cmd.includes('sign up')) {
        speak(t('voiceHeard'));
        goToRegister();
        return;
    }

    if (cmd.includes('çıkış') || cmd.includes('cikis') || cmd.includes('logout') || cmd.includes('log out') || cmd.includes('sign out')) {
        speak(t('voiceHeard'));
        logout();
        return;
    }

    speak(t('voiceUnknown'));
}

function readAssistantIntentFromUrl() {
    const params = new URLSearchParams(window.location.search);
    const raw =
        params.get('assistant') ||
        params.get('intent') ||
        params.get('voice') ||
        params.get('command') ||
        '';
    return String(raw).toLowerCase().trim();
}

function triggerAssistantEmergencyIntent(source = 'assistant') {
    const token = authTokenCache || localStorage.getItem('token');
    if (!token) {
        localStorage.setItem('pendingAssistantIntent', 'emergency');
        showNotification('Sesli Komut Hazır', 'Acil komut alındı. Lütfen giriş yapın, ardından otomatik çalıştırılacak.', 'success');
        return;
    }

    showScreen('homeScreen');
    setTimeout(() => {
        showNotification('Sesli Komut', `${source} üzerinden ACİL komutu algılandı. Onay ekranı açılıyor.`, 'success');
        showEmergencyConfirm();
    }, 300);
}

function handleAssistantIntentFromUrl() {
    const intent = readAssistantIntentFromUrl();
    if (!intent) return;

    if (intent.includes('emergency') || intent.includes('sos') || intent.includes('acil') || intent.includes('yardim') || intent.includes('yardım')) {
        triggerAssistantEmergencyIntent('Siri/Assistant');
    }

    // URL'i temiz tutalım
    if (window.history && window.history.replaceState) {
        const cleanUrl = `${window.location.origin}${window.location.pathname}`;
        window.history.replaceState({}, document.title, cleanUrl);
    }
}

function runPendingAssistantIntentIfAny() {
    const pending = localStorage.getItem('pendingAssistantIntent');
    if (pending === 'emergency') {
        localStorage.removeItem('pendingAssistantIntent');
        triggerAssistantEmergencyIntent('Bekleyen Sesli Komut');
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
    // In offline/demo mode skip premium check (backend is unreachable so check always fails)
    if (isOfflineDemoModeEnabled()) {
        showScreen('familyScreen');
        loadFamilyMembers();
        return;
    }
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
    // iOS WebView'de yeni sekme yerine aynı sekmede aç
    window.location.href = 'medication-vision.html?returnTo=homeScreen';
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

// =================== HESAP / PROFİL YÖNETIMI ===================

function updateProfileScreen() {
    const userId = localStorage.getItem('userId') || 'elderly-001';
    const userName = localStorage.getItem('userName') || (currentLang === 'en' ? 'User' : 'Kullanıcı');
    const userEmail = localStorage.getItem('userEmail') || localStorage.getItem('rememberedEmail') || '-';
    const userPlanRaw = localStorage.getItem('userPlan') || 'Standart';
    const subscriptionEnd = localStorage.getItem('subscriptionEnd') || '2025-12-31';
    const registrationDate = localStorage.getItem('registrationDate') || new Date().toLocaleDateString(currentLang === 'en' ? 'en-US' : 'tr-TR');
    const normalizedPlan = String(userPlanRaw).toLowerCase().includes('premium') ? 'premium' : 'standard';

    // Kalan günleri hesapla
    const endDate = new Date(subscriptionEnd);
    const today = new Date();
    const daysLeft = Math.max(0, Math.ceil((endDate - today) / (1000 * 60 * 60 * 24)));
    const daysLeftText = daysLeft > 0
        ? (currentLang === 'en' ? `${daysLeft} DAYS` : `${daysLeft} GÜN`)
        : (currentLang === 'en' ? 'EXPIRED' : 'SÜRESİ DOLDU');

    document.getElementById('profileName').textContent = userName;
    document.getElementById('profileEmail').textContent = userEmail;
    document.getElementById('profilePlan').textContent = normalizedPlan === 'premium'
        ? '⭐ PREMIUM'
        : (currentLang === 'en' ? '📦 STANDARD' : '📦 STANDART');
    document.getElementById('profileDaysLeft').textContent = daysLeftText;
}

function updateSubscriptionScreen() {
    const userPlanRaw = localStorage.getItem('userPlan') || 'Standart';
    const subscriptionEnd = localStorage.getItem('subscriptionEnd') || '2025-12-31';
    const isPremium = String(userPlanRaw).toLowerCase().includes('premium');

    document.getElementById('subCurrentPlan').textContent = isPremium
        ? (currentLang === 'en' ? '⭐ PREMIUM (All Features)' : '⭐ PREMIUM (Tüm Özellikler)')
        : (currentLang === 'en' ? '📦 STANDARD' : '📦 STANDART');
    document.getElementById('subEndDate').textContent = subscriptionEnd;

    const premiumFeaturesEl = document.getElementById('premiumFeatures');
    if (!premiumFeaturesEl) return;

    if (isPremium && currentLang === 'en') {
        premiumFeaturesEl.innerHTML = `
            <div>✓ Video Doctor Consultation</div>
            <div>✓ Human Assistant (24/7)</div>
            <div>✓ Mood Analysis (AI)</div>
            <div>✓ Health Trends</div>
        `;
        return;
    }

    if (isPremium) {
        premiumFeaturesEl.innerHTML = `
            <div>✓ Video Doktor Konsültasyonu</div>
            <div>✓ İnsan Asistanı (24/7)</div>
            <div>✓ Ruh Hali Analizi (AI)</div>
            <div>✓ Sağlık Trendleri</div>
        `;
        return;
    }

    premiumFeaturesEl.innerHTML = '';
}

function editProfile() {
    const newName = prompt(currentLang === 'en' ? 'Your new full name:' : 'Yeni ad soyadınız:', localStorage.getItem('userName') || '');
    if (newName && newName.trim()) {
        localStorage.setItem('userName', newName.trim());
        updateProfileScreen();
        speak(currentLang === 'en' ? 'Your name has been updated' : 'Adınız güncellendi', currentLang === 'en' ? 'en-US' : 'tr-TR');
    }
}

function goToSubscription() {
    updateSubscriptionScreen();
    showScreen('subscriptionScreen');
    speak(currentLang === 'en' ? 'You are on the subscription page.' : 'Abone durumu sayfasında bulunuyorsunuz', currentLang === 'en' ? 'en-US' : 'tr-TR');
}

async function restorePurchases() {
    const token = await requireAuthTokenAsync();
    if (!token) return;

    const response = await safeFetch(`${API_BASE}/api/subscription?token=${token}`, {
        method: 'GET'
    });
    if (!response) return;

    const data = await safeReadJson(response, null);
    if (!response.ok || !data) {
        showNotification(t('restoreFailed'), t('restoreFailedMsg'), 'error');
        return;
    }

    const subscription = data.subscription || data;
    const planRaw = String(subscription.plan || subscription.Plan || 'standard').toLowerCase();
    const normalizedPlan = planRaw === 'premium' ? 'Premium' : 'Standart';
    const expiresAtRaw = subscription.expiresAt || subscription.ExpiresAt || '';
    const isActive = subscription.isActive ?? subscription.IsActive ?? false;

    localStorage.setItem('userPlan', isActive ? normalizedPlan : 'Standart');
    if (expiresAtRaw) {
        const parsedDate = new Date(expiresAtRaw);
        if (!Number.isNaN(parsedDate.getTime())) {
            localStorage.setItem('subscriptionEnd', parsedDate.toISOString().split('T')[0]);
        }
    }

    subscriptionCache = subscription;
    updateProfileScreen();
    updateSubscriptionScreen();
    if (!isActive) {
        showNotification(
            t('restoreSuccess'),
            currentLang === 'en'
                ? 'No active purchase found. Plan refreshed as Standard.'
                : 'Aktif satın alma bulunamadı. Paket Standart olarak güncellendi.',
            'error'
        );
        return;
    }

    showNotification(t('restoreSuccess'), t('restoreSuccessMsg'));
}

async function deleteAccountFlow() {
    const token = await requireAuthTokenAsync();
    if (!token) return;

    const confirmed = confirm(t('deleteAccountConfirmMsg'));
    if (!confirmed) {
        showNotification(t('deleteAccountCanceled'), t('deleteAccountCanceledMsg'), 'error');
        return;
    }

    const passwordInput = prompt(t('deleteAccountPasswordPrompt'));
    if (passwordInput === null) {
        showNotification(t('deleteAccountCanceled'), t('deleteAccountCanceledMsg'), 'error');
        return;
    }

    const password = String(passwordInput || '').trim();
    if (!password) {
        showNotification(t('deleteAccountNeedPassword'), t('deleteAccountNeedPasswordMsg'), 'error');
        return;
    }

    const finalText = prompt(t('deleteAccountFinalPrompt'));
    const expectedFinalText = currentLang === 'en' ? 'DELETE' : 'SIL';
    if ((finalText || '').trim().toUpperCase() !== expectedFinalText) {
        showNotification(t('deleteAccountFinalMismatch'), t('deleteAccountFinalMismatchMsg'), 'error');
        return;
    }

    const response = await safeFetch(`${API_BASE}/api/elderly/account?token=${token}`, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ password })
    });
    if (!response) return;

    const payload = await safeReadJson(response, {});
    if (!response.ok || !payload?.success) {
        showNotification(t('deleteAccountFailed'), payload?.message || t('deleteAccountFailedMsg'), 'error');
        return;
    }

    await removeStoredToken();
    localStorage.removeItem('userId');
    localStorage.removeItem('userName');
    localStorage.removeItem('userEmail');
    localStorage.removeItem('userPlan');
    localStorage.removeItem('subscriptionEnd');
    localStorage.removeItem('rememberMe');
    subscriptionCache = null;

    showNotification(t('deleteAccountSuccess'), t('deleteAccountSuccessMsg'));
    showScreen('loginScreen');
}

function goToPremium() {
    const isPremium = localStorage.getItem('userPlan') === 'Premium';
    if (isPremium) {
        showNotification('Premium Aktif', 'Zaten premium aboneniz!');
    } else {
        const title = currentLang === 'en' ? 'Subscription Required' : 'Abonelik Gerekli';
        const message = currentLang === 'en'
            ? 'Opening subscription screen. Apple account page opens only if you tap Manage Subscriptions.'
            : 'Abonelik ekranı açılıyor. Apple hesabı sadece siz "Abonelikleri Yönet" derseniz açılır.';
        showNotification(title, message);
        goToSubscription();
    }
}

function shouldShowDemoHint() {
    const params = new URLSearchParams(window.location.search || '');
    return params.get('demo') === '1' || localStorage.getItem('showDemoHint') === 'true';
}

// =================== BİLDİRİM ===================

function showNotification(title, message, type = 'success') {
    const activeScreenId = document.querySelector('.screen.active')?.id || '';
    if (type === 'success' && activeScreenId === 'homeScreen') {
        return;
    }

    provideFeedback(`${title}. ${message}`, type === 'error' ? [80, 40, 80] : [40]);
    const notification = document.createElement('div');
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${type === 'success' ? '#2563eb' : '#ff3333'};
        color: ${type === 'success' ? 'white' : 'black'};
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

function showGracefulOfflineState(message, type = 'offline') {
    const existing = document.getElementById('gracefulOfflineState');
    if (existing) existing.remove();

    const banner = document.createElement('div');
    banner.id = 'gracefulOfflineState';
    const isSuccess = type === 'success';
    banner.style.cssText = `
        position: fixed;
        left: 50%;
        top: 22px;
        transform: translateX(-50%);
        z-index: 10001;
        width: min(92vw, 920px);
        background: ${isSuccess ? '#2e7d32' : '#ffeb3b'};
        color: ${isSuccess ? '#ffffff' : '#222222'};
        border-radius: 16px;
        box-shadow: 0 8px 24px rgba(0,0,0,0.28);
        padding: 18px 22px;
        font-size: 30px;
        line-height: 1.35;
        font-weight: 800;
        text-align: center;
    `;
    banner.textContent = message;
    document.body.appendChild(banner);

    if (navigator.vibrate) {
        try { navigator.vibrate(isSuccess ? [50, 40, 50] : [120, 80, 120]); } catch { }
    }

    setTimeout(() => {
        if (banner && banner.parentNode) banner.remove();
    }, isSuccess ? 5000 : 8000);
}

function initOfflineResilienceBridge() {
    if (!('serviceWorker' in navigator)) return;

    navigator.serviceWorker.register('/sw.js').catch(err => {
        console.warn('Service Worker kaydı başarısız:', err);
    });

    navigator.serviceWorker.addEventListener('message', (event) => {
        const data = event.data || {};
        if (data.type === 'OFFLINE_DATA_QUEUED') {
            const msg = data.message || 'İnternet yok ama merak etme, verini kaydettim. İnternet gelince otomatik göndereceğim.';
            showGracefulOfflineState(msg, 'offline');
            speak(msg);
        }
        if (data.type === 'OFFLINE_SYNC_COMPLETED') {
            const msg = data.message || 'Çevrimdışı kaydedilen veriler başarıyla sunucuya gönderildi.';
            showGracefulOfflineState(`✅ ${msg}`, 'success');
            speak(msg);
        }
    });

    window.addEventListener('offline', () => {
        showGracefulOfflineState('📡 İnternet yok. Merak etme, ölçümlerini cihazda güvenle saklıyorum.', 'offline');
    });

    window.addEventListener('online', () => {
        showGracefulOfflineState('✅ İnternet geri geldi. Kayıtlı verileri arka planda sunucuya gönderiyorum.', 'success');
    });
}

function provideFeedback(message, pattern = [30]) {
    if (navigator.vibrate) {
        try {
            navigator.vibrate(pattern);
        } catch {
            // ignore
        }
    }
    if (message && (!IS_CAPACITOR_IOS || userHasInteracted)) {
        speak(message);
    }
}

// =================== FORM IŞLEYENLER ===================

document.addEventListener('DOMContentLoaded', async function () {
    const markInteraction = () => {
        userHasInteracted = true;
    };
    document.addEventListener('pointerdown', markInteraction, { passive: true, once: true });
    document.addEventListener('touchstart', markInteraction, { passive: true, once: true });
    document.addEventListener('keydown', markInteraction, { passive: true, once: true });

    handleAssistantIntentFromUrl();

    initOfflineResilienceBridge();

    const testHint = document.getElementById('testHint');
    if (testHint && !shouldShowDemoHint()) {
        testHint.style.display = 'none';
    }

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
            showNotification(t('apiSaved'), t('apiSavedMsg'), 'success');
        });
    }
    if (apiBaseClear && apiBaseInput) {
        apiBaseClear.addEventListener('click', () => {
            localStorage.removeItem('apiBaseUrl');
            apiBaseInput.value = '';
            showNotification(t('apiReset'), t('apiResetMsg'), 'success');
        });
    }

    const a11yToggle = document.getElementById('a11yToggle');
    if (a11yToggle) {
        const isLarge = localStorage.getItem('largeText') === 'true';
        document.body.classList.toggle('large-text', isLarge);
        a11yToggle.setAttribute('aria-pressed', String(isLarge));
        a11yToggle.textContent = isLarge ? t('largeTextOff') : t('largeTextOn');
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
        contrastToggle.textContent = isHighContrast ? t('contrastOff') : t('contrastOn');
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
        simpleHomeToggle.textContent = isSimpleHome ? t('simpleModeOff') : t('simpleModeOn');
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

    // Dil ve çevirileri her açılışta uygula
    currentLang = detectPreferredLanguage();
    applyTranslations();

    // Otomatik giriş (Beni Hatırla)
    const remember = localStorage.getItem('rememberMe') === 'true';
    const token = await getStoredToken();
    if (remember && token) {
        showScreen('homeScreen');
        updateGreeting();
        runPendingAssistantIntentIfAny();

        // Kayıtlı e-posta ve dil ayarını uygula
        const savedEmail = localStorage.getItem('rememberedEmail');
        const emailInput = document.getElementById('email');
        const rememberCheckbox = document.getElementById('rememberMe');
        if (remember && savedEmail && emailInput) {
            emailInput.value = savedEmail;
            if (rememberCheckbox) rememberCheckbox.checked = true;
        }

        // Kayıtlı dil ayarı zaten yukarıda uygulandı
    }

    if (isOfflineDemoModeEnabled()) {
        showScreen('homeScreen');
        updateGreeting();
        showGracefulOfflineState('📡 Demo çevrimdışı mod açık. Sunucuya bağlanmadan temel ekran gösteriliyor.', 'offline');
    }

    // Capacitor iOS: if the backend is not reachable on startup, don't hang on loading screens.
    // Proactively test connectivity and immediately enable offline mode if unreachable.
    if (IS_CAPACITOR_IOS && !isOfflineDemoModeEnabled()) {
        (async () => {
            try {
                const ctrl = new AbortController();
                const tid = setTimeout(() => ctrl.abort(), API_TIMEOUT_MS);
                await fetch(`${API_BASE}/api/health`, { signal: ctrl.signal, method: 'HEAD' }).catch(() => {
                    forceCloseLoadingAndRecover();
                });
                clearTimeout(tid);
            } catch (_) { /* absorbed */ } finally {
                // _onBackendFail will activate offline mode if needed after 2 fails;
                // call it once here for the startup probe.
                if (_backendUnreachableCount === 0) {
                    // if we reach here without error, backend responded — all good.
                }
            }
        })();
    }

    // Butonlara sesli geri bildirim
    document.querySelectorAll('button').forEach(btn => {
        btn.addEventListener('click', () => {
            provideFeedback('', [20]);
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
    if ((token || isOfflineDemoModeEnabled()) && (path.includes('login') || path === '/' || path.endsWith('/index.html'))) {
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
    if (isOfflineDemoModeEnabled() || isDemoOfflineToken(token)) return;
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
    speak('Ruh hali kaydı için sesli komut ver. Örnek: Ruh halim 7.');
    showNotification('Ruh Hali', 'Sesli komut kullanın: "Ruh halim 7"', 'info');
}

async function maybePromptHealth(dateKey, now, recordType, unit, startHour, endHour, question) {
    const key = `${recordType}Asked:${dateKey}`;
    if (localStorage.getItem(key) === 'true') return;
    if (!withinHours(now, startHour, endHour)) return;

    localStorage.setItem(key, 'true');
    const commandHint = recordType === 'tansiyon'
        ? 'Sesli komut: "Tansiyon 12"'
        : 'Sesli komut: "Şeker 110"';
    speak(`Canım, ${question} ${commandHint}`);
    showNotification('Sağlık Kontrolü', commandHint, 'info');
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

    // Sunucuya bağlanmayı dene (sessiz hata modunda)
    try {
        const response = await safeFetch(`${API_BASE}/api/elderly/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        }, { silent: true });

        if (response) {
            const rawText = await response.text();
            let data = null;
            try { data = rawText ? JSON.parse(rawText) : null; } catch { }

            if (response.ok && data?.token) {
                await setStoredToken(data.token);
                localStorage.setItem('userId', data.userId || '');
                localStorage.setItem('userName', data.name || email);
                localStorage.setItem('rememberMe', remember ? 'true' : 'false');
                if (remember) localStorage.setItem('rememberedEmail', email);
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
                    .catch(() => { });
                showScreen('homeScreen');
                updateGreeting();
                speak(`${t('welcomeMsg')} ${data.name}`);
                runPendingAssistantIntentIfAny();
                return;
            }
            // Sunucu hata döndürdü (yanlış şifre vs.)
            const message = data?.message || t('loginFailed');
            showNotification(t('errorTitle'), message, 'error');
            return;
        }
    } catch (error) {
        console.error('Login error:', { error, apiBase: API_BASE, email });
    }

    // Controlled Offline Mode (only if explicitly enabled for local QA)
    const allowOfflineDemo = localStorage.getItem('allowOfflineDemo') === 'true';
    if (allowOfflineDemo) {
        await removeStoredToken();
        sessionStorage.setItem('offlineDemoMode', 'true');
        localStorage.setItem('userId', 'demo-user');
        localStorage.setItem('userName', currentLang === 'en' ? 'Demo User' : 'Demo Kullanıcı');
        localStorage.setItem('rememberMe', remember ? 'true' : 'false');
        if (remember) localStorage.setItem('rememberedEmail', email);
        subscriptionCache = null;
        showScreen('homeScreen');
        updateGreeting();
        showGracefulOfflineState('📡 Demo çevrimdışı mod açık. Sunucuya bağlanmadan temel ekran gösteriliyor.', 'offline');
        runPendingAssistantIntentIfAny();
        return;
    }

    showNotification(t('errorTitle'), t('connError'), 'error');
}

async function handleForgotPassword() {
    const email = prompt('E-posta adresinizi girin:');
    if (!email) return;

    try {
        const response = await safeFetch(`${API_BASE}/api/elderly/reset-password`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email })
        }, { timeoutMs: API_TIMEOUT_MS });

        if (!response) {
            forceCloseLoadingAndRecover();
            return;
        }

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

function setMedicationPreset(name) {
    const medNameInput = document.getElementById('medName');
    if (!medNameInput) return;
    medNameInput.value = name;
    medNameInput.focus();
    speak(`${name}`);
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
        const response = await safeFetch(`${API_BASE}/api/medications?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, notes, scheduleTimes: times })
        });

        if (response?.ok) {
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

async function handleAppleSignInPreview() {
    showNotification('Apple Giriş', 'Sign in with Apple taslağı hazır. Üretimde Apple kimlik doğrulama akışı aktif edilecektir.', 'success');
    speak('Apple ile giriş seçeneği hazır. Mağaza uyumluluğu için aktif edilecek.');
}

async function handleRegister(e) {
    e.preventDefault();
    const fullName = document.getElementById('regFullName').value.trim();
    const phone = document.getElementById('regPhone').value.trim();
    const email = document.getElementById('regEmail').value.trim();
    const birthDate = document.getElementById('regBirthDate').value;

    try {
        const response = await safeFetch(`${API_BASE}/api/elderly-self-enroll`, {
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
        }, { timeoutMs: API_TIMEOUT_MS });

        if (!response) {
            forceCloseLoadingAndRecover();
            return;
        }

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
    const container = document.getElementById('medicationsList');
    if (!token) {
        if (container) container.innerHTML = `<div style="font-size:24px;color:#ffff00;text-align:center;padding:24px;">${t('medsEmpty')}</div>`;
        return;
    }
    try {
        const response = await safeFetch(`${API_BASE}/api/medications?token=${token}`, {}, { silent: true });
        if (!response) {
            // Backend unreachable — show empty state immediately, don't hang
            if (container) container.innerHTML = `<div style="font-size:22px;color:#ffaa00;text-align:center;padding:24px;">📡 Sunucu bağlantısı yok. İlaçlar yüklenemedi.</div>`;
            return;
        }
        if (response.ok) {
            const payload = await safeReadJson(response, []);
            const medications = Array.isArray(payload)
                ? payload
                : (Array.isArray(payload?.items) ? payload.items : (Array.isArray(payload?.medications) ? payload.medications : []));
            currentMedicationsCache = medications;
            console.log('Gelen Veri (ilaçlar):', medications);
            const container = document.getElementById('medicationsList');
            if (!container) return;
            if (!medications.length) {
                container.innerHTML = `<div style="font-size:24px;color:#ffff00;text-align:center;padding:24px;">${t('medsEmpty')}</div>`;
                return;
            }
            container.innerHTML = medications.map(med => `
                <div style="background: rgba(255,255,0,0.1); border-left: 5px solid #ffff00; padding: 20px; margin-bottom: 20px; border-radius: 10px;">
                    <div style="font-size: 32px; color: #ffff00; font-weight: bold; margin-bottom: 10px;">${med.name}</div>
                    <div style="font-size: 24px; color: #ffffff; margin-bottom: 10px;">${t('medsTimeLabel')}: ${med.scheduleTimes?.join(', ') || t('medsUnspecified')}</div>
                    <div style="font-size: 20px; color: #00ff00; margin-bottom: 15px;">${med.notes || ''}</div>
                    ${typeof med.stockCount === 'number' ? `<div style="font-size: 20px; color: ${med.stockCount <= 3 ? '#ff6666' : '#00ccff'}; margin-bottom: 10px;">${t('medsRemaining')}: ${med.stockCount}</div>` : ''}
                    <button class="btn-giant btn-green" onclick="takeMedication(${med.id})" style="margin-top: 10px;">${t('medsTakenBtn')}</button>
                </div>
            `).join('');
            refreshMedicationConfirmTimers(medications);
            scheduleMedicationReminders();
        }
    } catch (error) {
        console.warn('İlaç yükleme hatası:', error);
        if (container) container.innerHTML = `<div style="font-size:22px;color:#ffaa00;text-align:center;padding:24px;">📡 İlaçlar yüklenirken hata oluştu.</div>`;
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
                    <div style="font-size: 24px; color: #ffffff;">${member.relationship || t('familyMemberDefault')}</div>
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
        greeting.textContent = `${t('welcomeMsg')}, ${name}`;
    }
}

function pickPreferredVoice(langCode) {
    // TTS TEMP DISABLED
    // if (IS_CAPACITOR_IOS) return null;
    // if (!('speechSynthesis' in window)) return null;
    // const voices = speechSynthesis.getVoices() || [];
    // if (!voices.length) return null;
    // if (langCode === 'en-GB') {
    //     return voices.find(v => /^en-GB$/i.test(v.lang))
    //         || voices.find(v => /^en-/i.test(v.lang) && /google|samantha|serena|daniel|karen/i.test(v.name))
    //         || voices.find(v => /^en-/i.test(v.lang))
    //         || null;
    // }
    // return voices.find(v => /^tr-TR$/i.test(v.lang))
    //     || voices.find(v => /^tr/i.test(v.lang))
    //     || null;
    return null;
}

function speak(text) {
    // TTS TEMP DISABLED
    // if (!text || typeof text !== 'string') return;
    // if ('speechSynthesis' in window) {
    //   ... intentionally disabled during stabilization ...
    // }
    return;
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
    if (isEmergencyModalOpen) return;
    isEmergencyModalOpen = true;

    const token = requireAuthToken();
    if (!token) {
        isEmergencyModalOpen = false;
        return;
    }

    const modal = document.getElementById('emergencyModal');
    if (modal) {
        modal.classList.add('show');
        speak('Acil yardım onayı. Aileye ve sağlık kuruluşlarına konumla bildirim gönderilecek. İptal etmek için iptal butonuna basın.');
        if (emergencyTimer) {
            clearTimeout(emergencyTimer);
        }
        emergencyTimer = setTimeout(() => {
            if (isEmergencyModalOpen) {
                confirmEmergency();
            }
        }, 5000);
    }
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
    isEmergencyModalOpen = false;
    showNotification('İptal', 'Acil çağrı iptal edildi', 'success');
}

async function confirmEmergency() {
    if (!isEmergencyModalOpen) return;
    isEmergencyModalOpen = false;

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
                location: location?.label || location?.mapsUrl || 'Unknown',
                coords: location?.coords || null
            })
        });
        if (!response) return;
        if (response.ok) {
            showNotification('Gönderildi', 'Acil yardım çağrısı gönderildi', 'success');
            await sendEmergencyNotification(location);
            await sendEmergencyBroadcast(buildEmergencyPayload(location));
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
                mapsUrl: `https://maps.google.com/?q=${latitude},${longitude}`,
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
                    mapsUrl: `https://maps.google.com/?q=${latitude},${longitude}`,
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

function buildEmergencyPayload(location) {
    const coords = location?.coords || null;
    return {
        location: coords
            ? {
                latitude: coords.latitude,
                longitude: coords.longitude,
                accuracy: coords.accuracy,
                mapsUrl: location?.mapsUrl || null
            }
            : (location?.mapsUrl || location?.label || 'Unknown')
    };
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
                severity: 'high',
                location: location?.mapsUrl || location?.label || 'Unknown'
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
    if (!token) {
        renderMoodDashboard({ averageMood: 0, trend: 'stable', recentMoods: [] });
        return;
    }
    try {
        const response = await safeFetch(`${API_BASE}/api/mood-analysis?token=${token}`, {}, { silent: true });
        if (!response) {
            // Backend unreachable — show empty offline placeholder so screen doesn't hang
            renderMoodDashboard({ averageMood: 0, trend: 'stable', recentMoods: [] });
            return;
        }
        if (response.ok) {
            const data = await safeReadJson(response, { averageMood: 0, trend: 'stable', recentMoods: [] });
            document.body.classList.toggle('mood-declining', data.trend === 'declining');
            renderMoodDashboard(data);
        } else {
            renderMoodDashboard({ averageMood: 0, trend: 'stable', recentMoods: [] });
        }
    } catch (error) {
        console.warn('Ruh hali yükleme hatası:', error);
        renderMoodDashboard({ averageMood: 0, trend: 'stable', recentMoods: [] });
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
            showNotification(t('moodThanksTitle'), t('moodSavedMsg'), 'success');
            const severity = score <= 3 ? 'high' : 'normal';
            const moodMessage = score <= 3
                ? `Ruh hali düşük: ${score}/10. Kullanıcı kendini iyi hissetmiyor olabilir.`
                : `Ruh hali bildirimi: ${score}/10`;
            await sendFamilyNotification('mood_update', moodMessage, severity);
            loadMoodAnalysis();
        } else {
            showNotification(t('errorTitle'), t('moodSaveError'), 'error');
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
                ${t('moodAverageLabel')}: ${data.averageMood}/10
            </div>
            <div style="font-size: 24px; color: #ffff00; margin-bottom: 30px;">
                ${t('moodTrendLabel')}: ${data.trend === 'improving' ? t('moodTrendImproving') : data.trend === 'declining' ? t('moodTrendDeclining') : t('moodTrendStable')}
            </div>
        </div>
        
        <div style="background: rgba(255,255,0,0.1); padding: 20px; border-radius: 10px; border-left: 5px solid #ffff00;">
            <h3 style="font-size: 24px; margin-bottom: 15px;">${t('moodLastFiveDays')}</h3>
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
            <strong>${t('moodInfoTitle')}</strong> ${t('moodInfoText')}
        </div>
    `;
}

// ================= HEALTH RECORDS (Sağlık Kayıtları) =================
async function loadHealthRecords() {
    const token = requireAuthToken();
    if (!token) {
        renderHealthRecords([]);
        return;
    }
    try {
        const response = await safeFetch(`${API_BASE}/api/health-records?token=${token}`, {}, { silent: true });
        if (!response) {
            // Backend unreachable — show empty state so screen doesn’t hang
            renderHealthRecords([]);
            return;
        }
        if (response.ok) {
            const data = await safeReadJson(response, []);
            renderHealthRecords(data);
        } else {
            renderHealthRecords([]);
        }
    } catch (error) {
        console.warn('Sağlık kayıtları yükleme hatası:', error);
        renderHealthRecords([]);
    }
}

function renderHealthRecords(records) {
    const healthDiv = document.getElementById('healthRecordsContent') || document.getElementById('healthRecordsScreen');
    if (!healthDiv) return;

    let html = '<div style="padding: 20px;">';
    html += `<h2 style="color: #ffff00; text-align: center; margin-bottom: 30px;">${t('healthRecordsTitle')}</h2>`;

    if (records.length === 0) {
        html += `<div style="color: #ffff00; text-align: center; font-size: 20px;">${t('noRecordsYet')}</div>`;
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
                    ${latest.alertLevel === 'critical' ? t('healthCritical') : latest.alertLevel === 'warning' ? t('healthWarning') : t('healthNormal')}
                </div>
                <div style="color: #999; font-size: 14px;">${t('healthLastLabel')}: ${new Date(latest.timestamp).toLocaleString(currentLang === 'en' ? 'en-US' : 'tr-TR')}</div>
                <div style="color: #666; font-size: 12px; margin-top: 5px;">${t('healthLastFiveLabel')}: ${typeRecords.slice(0, 5).map(r => r.value).join(', ')}</div>
            </div>`;
        });
    }

    html += `<div style="margin-top: 30px;"><button onclick="showAddHealthRecord()" class="btn-mega" style="background: linear-gradient(135deg, #667eea, #764ba2); width: 100%; margin-bottom: 10px;">${t('addNewRecordBtn')}</button></div>`;
    html += '</div>';

    healthDiv.innerHTML = html;
}

function showAddHealthRecord() {
    const type = prompt(currentLang === 'en'
        ? 'Which measurement would you like to add?\n1 = Blood Pressure (mmHg)\n2 = Blood Sugar (mg/dL)\n3 = Cholesterol (mg/dL)'
        : 'Hangi ölçümü eklemek istersiniz?\n1 = Tansiyon (mmHg)\n2 = Kan Şekeri (mg/dL)\n3 = Kolesterol (mg/dL)');
    if (!type) return;

    const recordType = type === '1' ? 'tansiyon' : type === '2' ? 'şeker' : 'kolesterol';
    const recordTypeLabel = type === '1'
        ? (currentLang === 'en' ? 'blood pressure' : 'tansiyon')
        : type === '2'
            ? (currentLang === 'en' ? 'blood sugar' : 'şeker')
            : (currentLang === 'en' ? 'cholesterol' : 'kolesterol');
    const unit = type === '1' ? 'mmHg' : 'mg/dL';
    const value = prompt(currentLang === 'en'
        ? `Enter ${recordTypeLabel} value (${unit}):`
        : `${recordType} değerini girin (${unit}):`);
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
            const result = await safeReadJson(response, {});

            if (result?.queued === true || response.status === 202) {
                const queuedMessage = 'İnternet yok ama merak etme, verini kaydettim. İnternet gelince doktora ve aileye göndereceğim.';
                showGracefulOfflineState(`📌 ${queuedMessage}`, 'offline');
                speak(queuedMessage);
                return;
            }

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
