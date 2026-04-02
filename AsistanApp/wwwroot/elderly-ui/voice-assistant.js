/* ===========================
   SESLİ ASİSTAN - JAVASCRIPT
   Komutlar: "ilaç ekle", "kızımı ara", "aile", "yardım" vb.
   ===========================
*/

(function () {
    // Ana dosya olan elderly.js zaten gelişmiş sesli komut yönetimi içeriyor.
    // Bu dosya sadece geriye dönük uyumluluk köprüsü bırakır ve çift recognition başlatmaz.
    const start = () => {
        if (typeof window.startVoiceCommand === 'function') {
            window.startVoiceCommand();
        }
    };

    const stop = () => {
        if (typeof window.stopVoiceCommand === 'function') {
            window.stopVoiceCommand();
        }
    };

    window.voiceAssistantStart = start;
    window.voiceAssistantStop = stop;
})();

