# ⚡ QUICK REFERENCE - Yaşlı Bakım Sistemi APIs

## 🚀 Server Start
```bash
cd /Users/busenurakdeniz/Desktop/ilk\ projem/AsistanApp
dotnet run
# Server: http://localhost:5007
```

---

## 🔐 Get Token (Required for all requests)
```bash
curl -X POST http://localhost:5007/api/login \
  -H "Content-Type: application/json" \
  -d '{"email":"elderly@test.com","password":"1234"}'

# Response: {"token": "eyJ..."}
# Use: ?token=YOUR_TOKEN on all requests
```

---

## 📍 All Available Endpoints (10 Total)

### Health Symptoms (Hayati Belirtiler)
```
POST /api/health-symptoms?token=TOKEN        → Record BP, sugar, chills, dizziness
GET  /api/health-symptoms?token=TOKEN        → Get last 30 days
```

### Calendar (Takvim)
```
POST /api/calendar/events?token=TOKEN        → Add doctor/family event
GET  /api/calendar/events?token=TOKEN        → Get upcoming events (7d back, 30d forward)
```

### Fall Detection (Düşme Algılama)
```
POST /api/fall-detection?token=TOKEN         → Submit accelerometer data (X,Y,Z)
```

### Self-Care (Öz Bakım)
```
POST /api/self-care-reminders?token=TOKEN                    → Add reminder
GET  /api/self-care-reminders?token=TOKEN                    → Get all reminders
POST /api/self-care-reminders/{id}/complete?token=TOKEN      → Mark complete
```

### User State (Durum Yönetimi)
```
GET  /api/user-state?token=TOKEN             → Get current context
POST /api/user-state?token=TOKEN             → Set context
```

---

## 💊 Quick Requests

### Record Critical Blood Pressure
```bash
TOKEN="your_token_here"

curl -X POST http://localhost:5007/api/health-symptoms?token=$TOKEN \
  -H "Content-Type: application/json" \
  -d '{
    "symptomType":"blood_pressure",
    "value":"175",
    "unit":"mmHg",
    "method":"voice",
    "notes":"Sabah ölçümü"
  }'

# Family gets: 🚨 KRİTİK TANSIYÖN UYARISI
```

### Record Blood Sugar
```bash
curl -X POST http://localhost:5007/api/health-symptoms?token=$TOKEN \
  -H "Content-Type: application/json" \
  -d '{
    "symptomType":"blood_sugar",
    "value":"280",
    "unit":"mg/dL",
    "method":"sensor",
    "notes":"Gece ölçümü"
  }'

# If >250: Family gets critical alert 🚨
```

### Report a Fall
```bash
curl -X POST http://localhost:5007/api/fall-detection?token=$TOKEN \
  -H "Content-Type: application/json" \
  -d '{
    "accelerationX":"35",
    "accelerationY":"28",
    "accelerationZ":"42"
  }'

# Impact = 59.2 m/s² → FALL DETECTED
# Family gets: 🚨 DÜŞME ALGILANDI - ACIL
```

### Add Doctor Appointment
```bash
curl -X POST http://localhost:5007/api/calendar/events?token=$TOKEN \
  -H "Content-Type: application/json" \
  -d '{
    "eventType":"doctor_appointment",
    "title":"Kardiyolog Randevusu",
    "eventTime":"2024-02-15T14:00:00Z",
    "notes":"Dr. Ahmet ile"
  }'

# Reminder: 30 minutes before event
```

### Add Water Intake Reminder (8x Daily)
```bash
curl -X POST http://localhost:5007/api/self-care-reminders?token=$TOKEN \
  -H "Content-Type: application/json" \
  -d '{
    "reminderType":"water_intake",
    "reminderText":"Su içmeliyiz!",
    "dayOfWeek":"daily",
    "frequencyPerDay":8
  }'
```

### Change Screen Context (For Emergencies)
```bash
curl -X POST http://localhost:5007/api/user-state?token=$TOKEN \
  -H "Content-Type: application/json" \
  -d '{
    "currentContext":"emergency",
    "screenPriority":"emergency",
    "isAssistantActive":true
  }'

# Frontend displays: 🚨 ACIL DURUM
```

---

## 🎯 Critical Thresholds (Auto-Alerts to Family)

| Metric | Normal | Warning | Critical 🚨 |
|--------|--------|---------|-----------|
| Blood Pressure | <140 | 140-160 | >160 |
| Blood Sugar | <100 | 100-250 | >250 |
| Fall Impact | - | - | >30 m/s² |
| Chills | - | Any ⚠️ | - |
| Dizziness | - | Any ⚠️ | - |

---

## 📱 Frontend Integration

### Include JavaScript
```html
<script src="/js/state-based-ui.js"></script>
```

### Automatic Features
- ✅ Polls `/api/user-state` every 5 seconds
- ✅ Renders context-based screen (medication/meal/water/home)
- ✅ Shows 90% HUGE buttons during task times
- ✅ TTS announces screen changes in Turkish
- ✅ Haptic feedback on button press
- ✅ Enlarged touch areas (+20px padding)
- ✅ Proximity sensor auto-wake (if device supports)

### Manual Control (JavaScript Console)
```javascript
// Get current state
window.uiManager.fetchAndRenderState();

// Change to medication time
window.uiManager.setContext('medication_time', 'medication');

// Change to emergency
window.uiManager.setContext('emergency', 'emergency');

// Test haptic feedback
window.accessibilityManager.triggerHaptic('success');

// Stop polling (for testing)
window.uiManager.stopPolling();
```

---

## 🔍 View Current Data

### Get All Health Records
```bash
curl http://localhost:5007/api/health-symptoms?token=$TOKEN
```

### Get Upcoming Calendar Events
```bash
curl http://localhost:5007/api/calendar/events?token=$TOKEN
```

### Get All Reminders
```bash
curl http://localhost:5007/api/self-care-reminders?token=$TOKEN
```

### Get Current User State
```bash
curl http://localhost:5007/api/user-state?token=$TOKEN
```

---

## ⏰ Automatic Time-Based Context (Backend)

| Time | Context | Screen Shows | Button Text |
|------|---------|--------------|-------------|
| 09:00-11:00 | medication_time | 💊 İLAÇ SAATİ | İlacını İçtim |
| 12:00-14:00 | meal_time | 🍽️ YEMEK SAATİ | Yemek Yedim |
| 18:00-22:00 | water_time | 💧 SU SAATİ | Su İçtim |
| Otherwise | home | 👋 Hoş Geldin | 🎤 Dinle |

*Automatic - no manual intervention needed!*

---

## 🔐 Security Notes

✅ All endpoints require valid token
✅ Each user sees only their own data
✅ Family alerts filtered by ReceiveNotifications=true
✅ Critical alerts immediately cascade to all eligible family members

⚠️ Pending: HTTPS enforcement, data encryption

---

## 🧪 Test Credentials

**Elderly User:**
- Email: `elderly@test.com`
- Password: `1234`

**Family Member:**
- Email: `ali@example.com`
- Password: `1234`

---

## 📊 Response Format

### Success
```json
{
  "success": true,
  "message": "İşlem başarılı",
  "data": {...}
}
```

### Error
```json
{
  "success": false,
  "message": "Hata açıklaması"
}
```

### Critical Alert
```json
{
  "success": true,
  "message": "Hayati belirtiler kaydedildi",
  "requiresAttention": true
}
```

---

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| "Yetkisiz!" | Get new token from /api/login |
| No family alert | Check ReceiveNotifications=true in family member settings |
| Screen not changing | Clear browser cache, check token expiry |
| No haptic feedback | Check mobile device supports navigator.vibrate |
| TTS not working | Check browser language supports tr-TR |

---

## 🎯 Common Workflows

### Daily Medication Check
1. **09:00:** System auto-changes context to medication_time
2. **Frontend:** Shows HUGE medication button
3. **User:** Taps button (even with tremor or arthritis - easy!)
4. **Success:** Phone vibrates, shows "✅ Tamamlandı!"
5. **Family:** Sees medication logged on dashboard

### Fall Emergency
1. **Impact detected:** >30 m/s²
2. **System:** POST /api/fall-detection
3. **User:** Hears "İyi misin?" (voice prompt)
4. **Family:** Gets 🚨 alert within 1 second
5. **Location:** GPS shared with family (if available)

### Health Crisis
1. **User:** Records BP 175 mmHg
2. **System:** Detects critical (>160)
3. **Family:** Gets 🚨 KRİTİK TANSIYÖN UYARISI
4. **Family:** Can call doctor/911
5. **Logged:** All readings available for doctor

---

## 📞 Support

- **Elderly User:** User-friendly voice interface
- **Family:** Real-time alerts + history dashboard
- **Doctor:** Can access anonymized health records

---

## 🚀 What's Next

**Phase 2 (Pending):**
- [ ] SignalR real-time alerts (replace 5-second polling)
- [ ] Error handling & fallback UI
- [ ] GDPR data deletion endpoints
- [ ] Production HTTPS deployment

**Ready to deploy for:**
- ✅ UAT with elderly test users
- ✅ Family dashboard testing
- ✅ Medical accuracy verification
- ✅ Accessibility testing on various devices

---

**Last Updated:** Current Implementation
**Build Status:** ✅ 0 Errors
**Server:** http://localhost:5007
