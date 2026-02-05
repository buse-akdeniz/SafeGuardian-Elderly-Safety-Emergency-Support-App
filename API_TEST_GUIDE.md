# 🧪 API Test Guide - Yaşlı Bakım Sistemi

## 🔑 Authentication

All endpoints require a token. Get token by:

```bash
# POST /api/login
{
  "email": "elderly@test.com",
  "password": "1234"
}

# Response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

Then append `?token=YOUR_TOKEN` to all requests.

---

## 1️⃣ Hayati Belirtiler (Health Symptoms)

### Record Blood Pressure
```bash
POST http://localhost:5007/api/health-symptoms?token=YOUR_TOKEN

{
  "symptomType": "blood_pressure",
  "value": "175",
  "unit": "mmHg",
  "method": "voice",
  "notes": "Sabah ölçümü"
}

Response:
{
  "success": true,
  "message": "Hayati belirtiler kaydedildi",
  "requiresAttention": true  // BP > 160
}
```

### Record Blood Sugar
```bash
POST http://localhost:5007/api/health-symptoms?token=YOUR_TOKEN

{
  "symptomType": "blood_sugar",
  "value": "280",
  "unit": "mg/dL",
  "method": "sensor",
  "notes": "Akşam ölçümü"
}

Response:
{
  "success": true,
  "requiresAttention": true  // Sugar > 250
}
```

### Record Chills (Üşüme)
```bash
POST http://localhost:5007/api/health-symptoms?token=YOUR_TOKEN

{
  "symptomType": "chills",
  "value": "1",
  "unit": "severity",
  "method": "voice",
  "notes": "Şiddetli üşüyorum"
}

Response:
{
  "success": true,
  "message": "Uyarı: Üşüme belirtileri tespit edildi!"
}
```

### Get Health History
```bash
GET http://localhost:5007/api/health-symptoms?token=YOUR_TOKEN

Response:
[
  {
    "id": "abc123",
    "symptomType": "blood_pressure",
    "value": 175,
    "unit": "mmHg",
    "recordedAt": "2024-01-15T10:30:00Z",
    "requiresAttention": true,
    "notes": "Sabah ölçümü"
  },
  ...
]
```

---

## 2️⃣ Takvim Etkinlikleri (Calendar Events)

### Add Doctor Appointment
```bash
POST http://localhost:5007/api/calendar/events?token=YOUR_TOKEN

{
  "eventType": "doctor_appointment",
  "title": "Kardiyolog Randevusu",
  "eventTime": "2024-02-10T14:00:00Z",
  "notes": "Uzman: Dr. Ahmet"
}

Response:
{
  "success": true,
  "message": "Etkinlik eklendi",
  "eventId": "event123"
}
```

### Add Family Call (Granddaughter Visit)
```bash
POST http://localhost:5007/api/calendar/events?token=YOUR_TOKEN

{
  "eventType": "granddaughter_visit",
  "title": "Ayşe ile Video Çağrısı",
  "eventTime": "2024-01-20T19:00:00Z",
  "notes": "Pazartesi akşamı"
}

Response:
{
  "success": true,
  "message": "Etkinlik eklendi",
  "eventId": "event456"
}
```

### Get Upcoming Events
```bash
GET http://localhost:5007/api/calendar/events?token=YOUR_TOKEN

Response:
[
  {
    "id": "event123",
    "eventType": "doctor_appointment",
    "title": "Kardiyolog Randevusu",
    "eventTime": "2024-02-10T14:00:00Z",
    "isCompleted": false,
    "notes": "Uzman: Dr. Ahmet",
    "reminderMinutesBeforeEvent": 30
  },
  ...
]
```

---

## 3️⃣ Düşme Algılama (Fall Detection)

### Report Potential Fall
```bash
POST http://localhost:5007/api/fall-detection?token=YOUR_TOKEN

{
  "accelerationX": "35.2",
  "accelerationY": "28.5",
  "accelerationZ": "42.1"
}

Calculation:
ImpactForce = √(35.2² + 28.5² + 42.1²) = √(3,505.1) = 59.2 m/s²

Response:
{
  "success": true,
  "message": "Düşme algılandı! 'İyi misin?' diye sor...",
  "fallDetected": true,
  "impactForce": 59.2
}

// Family receives: 🚨 DÜŞME ALGILANDI - ACIL
// "Ali düştü! Cevap bekleniyor... (10 saniye içinde bilgi veriniz)"
```

### False Alarm (No Fall)
```bash
POST http://localhost:5007/api/fall-detection?token=YOUR_TOKEN

{
  "accelerationX": "5.2",
  "accelerationY": "3.1",
  "accelerationZ": "2.5"
}

Response:
{
  "success": true,
  "fallDetected": false,
  "impactForce": 6.54
}
```

---

## 4️⃣ Öz Bakım Hatırlatıcıları (Self-Care Reminders)

### Add Water Intake Reminder
```bash
POST http://localhost:5007/api/self-care-reminders?token=YOUR_TOKEN

{
  "reminderType": "water_intake",
  "reminderText": "Su içmeliyiz!",
  "dayOfWeek": "daily",
  "frequencyPerDay": 8
}

Response:
{
  "success": true,
  "message": "Hatırlatıcı eklendi",
  "reminderId": "reminder123"
}
```

### Add Shower Reminder
```bash
POST http://localhost:5007/api/self-care-reminders?token=YOUR_TOKEN

{
  "reminderType": "shower",
  "reminderText": "Duş almışız mı?",
  "dayOfWeek": "friday",
  "frequencyPerDay": 1
}

Response:
{
  "success": true,
  "message": "Hatırlatıcı eklendi",
  "reminderId": "reminder456"
}
```

### Add Meal Reminder
```bash
POST http://localhost:5007/api/self-care-reminders?token=YOUR_TOKEN

{
  "reminderType": "meal",
  "reminderText": "Yemek zamanı! Neyin var mı?",
  "dayOfWeek": "daily",
  "frequencyPerDay": 3
}

Response:
{
  "success": true,
  "message": "Hatırlatıcı eklendi",
  "reminderId": "reminder789"
}
```

### Get All Reminders
```bash
GET http://localhost:5007/api/self-care-reminders?token=YOUR_TOKEN

Response:
[
  {
    "id": "reminder123",
    "reminderType": "water_intake",
    "reminderText": "Su içmeliyiz!",
    "isCompleted": false,
    "dayOfWeek": "daily",
    "frequencyPerDay": 8
  },
  ...
]
```

### Mark Reminder as Complete
```bash
POST http://localhost:5007/api/self-care-reminders/reminder123/complete?token=YOUR_TOKEN

Response:
{
  "success": true,
  "message": "Görev tamamlandı! Aferin sana!"
}
```

---

## 🧪 cURL Examples for Quick Testing

### Test Health Symptom (Critical)
```bash
curl -X POST http://localhost:5007/api/health-symptoms?token=YOUR_TOKEN \
  -H "Content-Type: application/json" \
  -d '{"symptomType":"blood_pressure","value":"170","unit":"mmHg","method":"voice","notes":"Alarm test"}'
```

### Test Fall Detection
```bash
curl -X POST http://localhost:5007/api/fall-detection?token=YOUR_TOKEN \
  -H "Content-Type: application/json" \
  -d '{"accelerationX":"35","accelerationY":"30","accelerationZ":"40"}'
```

### Test Calendar Event
```bash
curl -X POST http://localhost:5007/api/calendar/events?token=YOUR_TOKEN \
  -H "Content-Type: application/json" \
  -d '{"eventType":"doctor_appointment","title":"Test Appointment","eventTime":"2024-02-10T14:00:00Z","notes":"Test"}'
```

### Get Calendar Events
```bash
curl http://localhost:5007/api/calendar/events?token=YOUR_TOKEN
```

---

## ✅ Expected Behaviors

### Critical Alert Trigger (BP > 160)
- System: Sets `requiresAttention = true`
- Notification: 🚨 KRİTİK TANSIYÖN UYARISI
- Routing: All family members with `ReceiveNotifications = true` receive alert
- Message: "Ali'nin tansiyonu 170 mmHg (NORMAL: <140). Derhal doktor çağrıl!"

### Critical Alert Trigger (Sugar > 250)
- System: Sets `requiresAttention = true`
- Notification: 🚨 KRİTİK ŞEKER SEVİYESİ
- Routing: All family members
- Message: "Ali'nin şeker seviyesi 280 mg/dL (NORMAL: <100). İnsülin kontrolü gerekebilir!"

### Fall Detection (Impact > 30 m/s²)
- System: Sets `FallDetected = true`, `Status = "pending_confirmation"`
- Frontend: Voice prompt "İyi misin?" (Wait 10 sec)
- If no response: Get location → Send GPS to family
- Notification: 🚨 DÜŞME ALGILANDI - ACIL

### Self-Care Completion
- Frontend: Show "Görev tamamlandı! Aferin sana!" message
- Backend: Sets `IsCompleted = true`, `CompletedAt = DateTime.UtcNow`
- UI: Clear notification from screen

---

## 🔍 Troubleshooting

### "Yetkisiz!" Error
- Missing or invalid token
- Solution: Get new token from `/api/login`

### "Hatırlatıcı bulunamadı!"
- Wrong `reminderId` or belongs to different user
- Solution: Check ID matches your reminders list

### Health Alert Not Showing
- User doesn't have `ReceiveNotifications = true`
- Solution: Update family member settings in database

---

## 📊 Data Validation Rules

| Field | Required | Type | Example |
|-------|----------|------|---------|
| symptomType | Yes | string | "blood_pressure" |
| value | Yes | decimal | "170.5" |
| unit | Yes | string | "mmHg" |
| method | Yes | string | "voice" \| "manual" \| "sensor" |
| eventType | Yes | string | "doctor_appointment" |
| eventTime | Yes | DateTime | "2024-02-10T14:00:00Z" |
| accelerationX/Y/Z | Yes | decimal | "35.2" |

---

## 🎯 Next: UserState API

Coming soon:
```bash
GET /api/user-state?token=TOKEN
POST /api/user-state?token=TOKEN
```

Will enable context-aware UI switching based on time of day and current task.

---

**Test Server:** http://localhost:5007
**Postman Collection:** (To be created)
**Last Updated:** 2024 (Current Implementation)
