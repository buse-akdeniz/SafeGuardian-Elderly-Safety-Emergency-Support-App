/* ===========================
   SESLİ ASISTAN - JAVASCRIPT
   Erişilebilirlik: Otomatik Rehberlik + Komut İşleme
   ===========================
*/

const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
const recognition = SpeechRecognition ? new SpeechRecognition() : null;

if (recognition) {
    recognition.lang = 'tr-TR';
    recognition.continuous = true;
    recognition.interimResults = true;
    recognition.maxAlternatives = 1;
}

let isListening = false;
const API_BASE = 'http://localhost:5007/api';

// ===========================
// SESLİ KOMUT BAŞLAT
// ===========================

function startVoiceCommand() {
    if (!recognition) {
        speak('Mikrofon desteği yok');
        return;
    }

    if (isListening) return;

    isListening = true;
    updateVoiceStatus('🎤 Dinliyorum...', true);
    recognition.start();
}

// ===========================
// METIN İÇİ KONUŞMA (TTS)
// ===========================

function speak(text) {
    if ('speechSynthesis' in window) {
        speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = 'tr-TR';
        utterance.rate = 0.85;
        utterance.pitch = 1;
        utterance.volume = 1;
        speechSynthesis.speak(utterance);
    }
}

// ===========================
// DURUM GÜNCELLE
// ===========================

function updateVoiceStatus(message, active = false) {
    const el = document.getElementById('voiceStatus');
    if (el) {
        el.textContent = message;
        el.style.display = message ? 'flex' : 'none';
    }
}

// ===========================
// SES TANIMA OLAYLARI
// ===========================

if (recognition) {
    recognition.onstart = () => {
        updateVoiceStatus('🎤 Dinliyorum...', true);
    };

    recognition.onresult = (event) => {
        let transcript = '';
        for (let i = event.resultIndex; i < event.results.length; i++) {
            transcript += event.results[i][0].transcript;
        }

        const command = transcript.toLowerCase().trim();
        updateVoiceStatus(`Anlaşıldı: "${transcript}"`);
        processCommand(command);
    };

    recognition.onerror = () => {
        updateVoiceStatus('❌ Anlaşılamadı');
        isListening = false;
        setTimeout(() => updateVoiceStatus(''), 2000);
    };

    recognition.onend = () => {
        isListening = false;
    };
}

// ===========================
// KOMUT İŞLEME
// ===========================

function processCommand(command) {
    const lowerCommand = command.toLowerCase().trim();
    
    if (lowerCommand.includes('ilaç')) {
        speak('İlaç ekleme ekranına gidiyorum');
        setTimeout(() => goToMedications(), 1000);
    } else if (lowerCommand.includes('anasayfa') || lowerCommand.includes('ana sayfa')) {
        speak('Ana sayfaya dönüyorum');
        setTimeout(() => goHome(), 1000);
    } else if (lowerCommand.includes('çık')) {
        speak('Uygulamadan çıkılıyor');
        setTimeout(() => logout(), 1000);
    } else if (lowerCommand.includes('yardım') || lowerCommand.includes('help')) {
        speak('Yardım sayfasına gidiyorum');
        setTimeout(() => showHelp(), 1000);
    } else {
        // AI ile sohbet et
        sendChatMessage(command);
    }
}

async function sendChatMessage(message) {
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5007/api/chat?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: message })
        });

        if (response.ok) {
            const data = await response.json();
            speak(data.response);
        } else {
            speak('Anlaşılamadı. Lütfen tekrar deneyin.');
        }
    } catch (error) {
        console.error('Chat hatası:', error);
        speak('Bağlantı hatası. Lütfen tekrar deneyin.');
    }
}

// ===========================
// SAYFA NAVİGASYONU
// ===========================

function showScreen(screenId) {
    document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
    const screen = document.getElementById(screenId);
    if (screen) screen.classList.add('active');
}

function goHome() {
    showScreen('homeScreen');
    loadMedications();
}

function goToMedications() {
    showScreen('medicationScreen');
}

// ===========================
// GİRİŞ
// ===========================

document.getElementById('loginForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();

    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;

    try {
        const response = await fetch(`${API_BASE}/elderly/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (data.success) {
            authToken = data.token;
            currentUser = data.user;
            localStorage.setItem('elderlyToken', authToken);
            localStorage.setItem('currentUser', JSON.stringify(currentUser));

            speak(`Merhaba ${data.user.name}. Hoş geldiniz`);
            goHome();
            updateGreeting();
        } else {
            speak('Giriş başarısız');
            alert('❌ ' + data.message);
        }
    } catch (error) {
        speak('Bağlantı hatası');
        console.error(error);
    }
});

// ===========================
// İLAÇLARI YÜKLE
// ===========================

async function loadMedications() {
    try {
        const response = await fetch(`${API_BASE}/medications?token=${authToken}`);
        const data = await response.json();

        const container = document.getElementById('medicationsList');
        if (!container) return;

        container.innerHTML = '';

        if (data.success && data.medications.length > 0) {
            data.medications.forEach(med => {
                const card = document.createElement('div');
                card.className = 'medication-card';
                card.innerHTML = `
                    <div class="med-info">
                        <div class="med-name">💊 ${med.medicationName}</div>
                        <div class="med-time">${med.scheduleTimes.join(' • ')}</div>
                    </div>
                    <button class="btn-take-med" onclick="takeMedication('${med.id}')">✅</button>
                `;
                container.appendChild(card);
            });
            speak(`${data.medications.length} ilacınız bulunmaktadır`);
        } else {
            container.innerHTML = '<p style="text-align: center; color: #888; font-size: 24px;">İlaç eklenmemiştir</p>';
        }
    } catch (error) {
        console.error(error);
    }
}

// ===========================
// İLAÇ ALINDI İŞARET ET
// ===========================

async function takeMedication(medicationId) {
    try {
        const response = await fetch(`${API_BASE}/medications/${medicationId}/taken?token=${authToken}`, {
            method: 'POST'
        });

        const data = await response.json();

        if (data.success) {
            speak('İlacınız kaydedildi. Aile üyelerinize haber verildi');
            loadMedications();
        } else {
            speak('Hata oluştu');
        }
    } catch (error) {
        console.error(error);
    }
}

// ===========================
// İLAÇ EKLE
// ===========================

document.getElementById('addMedicationForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();

    const medName = document.getElementById('medName').value;
    const medNotes = document.getElementById('medNotes').value;
    const timeInputs = document.querySelectorAll('.med-time');
    const times = Array.from(timeInputs)
        .map(input => input.value)
        .filter(val => val !== '');

    if (times.length === 0) {
        speak('Lütfen en az bir saat girin');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/medications?token=${authToken}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                medicationName: medName,
                scheduleTimes: times,
                daysOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],
                notes: medNotes
            })
        });

        const data = await response.json();

        if (data.success) {
            speak(`${medName} başarıyla eklendi`);
            document.getElementById('addMedicationForm').reset();
            goHome();
            loadMedications();
        } else {
            speak('İlaç eklenirken hata oluştu');
        }
    } catch (error) {
        console.error(error);
    }
});

// ===========================
// ÇIKIŞ YAP
// ===========================

function logout() {
    localStorage.removeItem('elderlyToken');
    localStorage.removeItem('currentUser');
    authToken = null;
    currentUser = null;
    showScreen('loginScreen');
    speak('Çıkış yaptınız');
}

// ===========================
// SELAMLAMA GÜNCELLE
// ===========================

function updateGreeting() {
    const user = JSON.parse(localStorage.getItem('currentUser'));
    if (user) {
        const greeting = document.getElementById('greeting');
        if (greeting) {
            greeting.textContent = `Hoş geldiniz, ${user.name}`;
        }
    }
}

// ===========================
// BAŞLATMA
// ===========================

window.addEventListener('DOMContentLoaded', () => {
    if (authToken) {
        currentUser = JSON.parse(localStorage.getItem('currentUser'));
        goHome();
        loadMedications();
        updateGreeting();
    } else {
        showScreen('loginScreen');
    }
});

