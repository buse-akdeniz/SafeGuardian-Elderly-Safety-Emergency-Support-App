/**
 * i18n (Internationalization) - Multi-language Support
 * Supports: Turkish (tr), English (en), German (de)
 */

let currentLanguage = 'en';
let translations = {};

// Detect system language
function detectSystemLanguage() {
    const browserLang = navigator.language || navigator.userLanguage || 'en';
    const lang = browserLang.split('-')[0];
    return ['en', 'tr', 'de'].includes(lang) ? lang : 'en';
}

// Load translation file
async function loadLanguage(lang = 'en') {
    try {
        const response = await fetch(`/api/i18n/${lang}`);
        if (response.ok) {
            translations = await response.json();
            currentLanguage = lang;
            localStorage.setItem('preferredLanguage', lang);
            applyTranslations();
            return true;
        }
    } catch (error) {
        console.error('Error loading language:', error);
    }
    return false;
}

// Get translated text
function t(key) {
    return translations[key] || key;
}

// Apply translations to DOM
function applyTranslations() {
    document.querySelectorAll('[data-i18n]').forEach(element => {
        const key = element.getAttribute('data-i18n');
        if (translations[key]) {
            element.textContent = translations[key];
        }
    });
}

// Change language
async function setLanguage(lang) {
    if (await loadLanguage(lang)) {
        console.log(`✅ Language changed to: ${lang}`);
    }
}

// Initialize i18n on page load
document.addEventListener('DOMContentLoaded', async () => {
    const savedLanguage = localStorage.getItem('preferredLanguage') || detectSystemLanguage();
    await loadLanguage(savedLanguage);
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { t, setLanguage, currentLanguage };
}
