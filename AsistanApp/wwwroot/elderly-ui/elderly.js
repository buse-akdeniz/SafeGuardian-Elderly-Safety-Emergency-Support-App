/* ===========================
   YAŞLI ASISTANI - GENEL JS
   ===========================
*/

// =================== EKRAN YÖNETIMI ===================

function showScreen(screenId) {
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
        speak(guidance[screenId]);
    }
}

function goHome() {
    showScreen('homeScreen');
}

function goToMedications() {
    showScreen('medicationScreen');
    loadMedications();
}

function goToAddMedication() {
    showScreen('addMedicationScreen');
}

function goToFamily() {
    showScreen('familyScreen');
    loadFamilyMembers();
}

function goToMoodDashboard() {
    showScreen('moodScreen');
    loadMoodAnalysis();
}

function goToMedicationVision() {
    // Yeni sekme aç
    window.open('medication-vision.html', '_blank');
}

function goToHealthRecords() {
    showScreen('healthRecordsScreen');
    loadHealthRecords();
}
    
function loadFamilyMembers() {

function goToAddFamily() {
    showScreen('addFamilyScreen');
}

function showHelp() {
    showScreen('helpScreen');
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    localStorage.removeItem('userName');
    showScreen('loginScreen');
}

// =================== BİLDİRİM ===================

function showNotification(title, message, type = 'success') {
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

// =================== FORM IŞLEYENLER ===================

document.addEventListener('DOMContentLoaded', function() {
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
});

async function handleLogin(e) {
    e.preventDefault();
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;

    try {
        const response = await fetch('http://localhost:5007/api/elderly/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        if (response.ok) {
            const data = await response.json();
            localStorage.setItem('token', data.token);
            localStorage.setItem('userId', data.userId);
            localStorage.setItem('userName', data.name);
            showScreen('homeScreen');
            updateGreeting();
            speak(`Hoş geldiniz ${data.name}`);
        } else {
            showNotification('Hata', 'Giriş başarısız', 'error');
        }
    } catch (error) {
        console.error('Giriş hatası:', error);
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
        const token = localStorage.getItem('token');
        const response = await fetch(`http://localhost:5007/api/medications?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, notes, scheduleTimes: times })
        });

        if (response.ok) {
            showNotification('Başarılı', 'İlaç eklendi', 'success');
            document.getElementById('addMedicationForm').reset();
            setTimeout(() => goToMedications(), 1000);
        }
    } catch (error) {
        console.error('İlaç ekleme hatası:', error);
        showNotification('Hata', 'İlaç eklenemedi', 'error');
    }
}

async function loadMedications() {
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5007/api/medications?token=${token}`);
        if (response.ok) {
            const medications = await response.json();
            const container = document.getElementById('medicationsList');
            container.innerHTML = medications.map(med => `
                <div style="background: rgba(255,255,0,0.1); border-left: 5px solid #ffff00; padding: 20px; margin-bottom: 20px; border-radius: 10px;">
                    <div style="font-size: 32px; color: #ffff00; font-weight: bold; margin-bottom: 10px;">${med.name}</div>
                    <div style="font-size: 24px; color: #ffffff; margin-bottom: 10px;">Saatler: ${med.scheduleTimes?.join(', ') || 'Belirtilmedi'}</div>
                    <div style="font-size: 20px; color: #00ff00; margin-bottom: 15px;">${med.notes || ''}</div>
                    <button class="btn-giant btn-green" onclick="takeMedication(${med.id})" style="margin-top: 10px;">✓ ALDI</button>
                </div>
            `).join('');
        }
    } catch (error) {
        console.error('İlaç yükleme hatası:', error);
    }
}

async function takeMedication(medicationId) {
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5007/api/medications/${medicationId}/taken?token=${token}`, {
            method: 'POST'
        });
        if (response.ok) {
            showNotification('Başarılı', 'İlaç kaydedildi ✓', 'success');
            loadMedications();
        }
    } catch (error) {
        console.error('İlaç alma hatası:', error);
        showNotification('Hata', 'Bir sorun oluştu', 'error');
    }
}

async function loadFamilyMembers() {
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5007/api/family-members?token=${token}`);
        if (response.ok) {
            const members = await response.json();
            const container = document.getElementById('familyList');
            container.innerHTML = members.map(member => `
                <div style="background: rgba(0,255,100,0.1); border-left: 5px solid #00ff00; padding: 20px; margin-bottom: 20px; border-radius: 10px;">
                    <div style="font-size: 32px; color: #ffff00; font-weight: bold; margin-bottom: 10px;">${member.name}</div>
                    <div style="font-size: 24px; color: #ffffff;">${member.relationship || 'Aile Üyesi'}</div>
                    <div style="font-size: 20px; color: #00ff00; margin-top: 10px;">${member.email}</div>
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
    const email = document.getElementById('familyEmail').value;
    const relationship = document.getElementById('familyRelation').value;

    try {
        const token = localStorage.getItem('token');
        const response = await fetch(`http://localhost:5007/api/family-members?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, email, relationship })
        });

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
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5007/api/chat?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: userMessage })
        });

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
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5007/api/notifications?token=${token}`);
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
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5007/api/mood-analysis?token=${token}`);
        if (response.ok) {
            const data = await response.json();
            renderMoodDashboard(data);
        }
    } catch (error) {
        console.error('Ruh hali yükleme hatası:', error);
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
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5007/api/health-records?token=${token}`);
        if (response.ok) {
            const data = await response.json();
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
    const type = prompt('Hangi ölçümü eklemek istersiniz?\n1 = Tansiyon (mmHg)\n2 = Kan Şekeri (mg/dL)');
    if (!type) return;
    
    const recordType = type === '1' ? 'tansiyon' : 'şeker';
    const unit = type === '1' ? 'mmHg' : 'mg/dL';
    const value = prompt(`${recordType} değerini girin (${unit}):`);
    if (!value) return;
    
    addHealthRecord(recordType, value, unit);
}

async function addHealthRecord(recordType, value, unit) {
    const token = localStorage.getItem('token');
    const body = JSON.stringify({
        recordType: recordType,
        value: parseFloat(value),
        unit: unit
    });
    
    try {
        const response = await fetch(`http://localhost:5007/api/health-records?token=${token}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: body
        });
        
        if (response.ok) {
            const result = await response.json();
            speak(`Sağlık kaydı başarıyla eklendi. ${recordType}: ${value} ${unit}`);
            loadHealthRecords();
            
            // If critical alert
            if (result.alertLevel === 'critical') {
                speak('DİKKAT! Kritik seviye. Aile üyeleri uyarıldı. Doktor'a başvurun!');
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
    }, 1500);
});
