/* ===========================
   SESLİ ASİSTAN - JAVASCRIPT
   Komutlar: "ilaç ekle", "kızımı ara", "aile", "yardım" vb.
   ===========================
*/

(function () {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) {
        console.warn('Tarayıcı konuşma tanımayı desteklemiyor.');
        return;
    }

    const recognition = new SpeechRecognition();
    recognition.lang = 'tr-TR';
    recognition.continuous = true;
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;

    let listening = false;

    const start = () => {
        if (listening) return;
        listening = true;
        try {
            recognition.start();
        } catch {
            listening = false;
        }
    };

    const stop = () => {
        if (!listening) return;
        try {
            recognition.stop();
        } catch {
            // ignore
        }
    };

    recognition.onend = () => {
        listening = false;
    };

    recognition.onresult = (event) => {
        const transcript = event?.results?.[event.resultIndex]?.[0]?.transcript || '';
        const command = transcript.toLowerCase().trim();
        if (!command) return;

        if (command.includes('ilaç ekle') || command.includes('ilaç ekleme')) {
            if (typeof goToAddMedication === 'function') goToAddMedication();
            return;
        }

        if (command.includes('ilaç')) {
            if (typeof goToMedications === 'function') goToMedications();
            return;
        }

        if (command.includes('kızımı ara') || command.includes('oğlumu ara') || command.includes('aile')) {
            if (typeof goToFamily === 'function') goToFamily();
            if (typeof triggerEmergencyCall === 'function') triggerEmergencyCall();
            return;
        }

        if (command.includes('yardım') || command.includes('acil')) {
            if (typeof showEmergencyConfirm === 'function') showEmergencyConfirm();
            return;
        }
    };

    window.voiceAssistantStart = start;
    window.voiceAssistantStop = stop;
})();

