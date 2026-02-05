# 🔌 YENI API ENDPOINTS - REFERANS

## 📊 SAĞLIK KAYITLARI (Health Records)

### GET /api/health-records
Yaşlının son 30 günün sağlık kayıtlarını getir

**İstek**:
```bash
curl "http://localhost:5007/api/health-records?token=YOUR_TOKEN"
```

**Parametreler**:
- `token` (query): Elderly user token (mandatory)

**Başarılı Response (200)**:
```json
[
  {
    "id": "7121bf7d-9523-4c0c-9d57-4d826a365f14",
    "elderlyId": "047370e7-d1ec-4e3b-b5e1-8777a6d4db0e",
    "recordType": "tansiyon",
    "value": 170,
    "unit": "mmHg",
    "alertLevel": "critical",
    "timestamp": "2026-01-22T10:41:23.670725Z"
  },
  {
    "id": "8a32cf9e-...",
    "elderlyId": "047370e7-d1ec-4e3b-b5e1-8777a6d4db0e",
    "recordType": "şeker",
    "value": 125,
    "unit": "mg/dL",
    "alertLevel": "normal",
    "timestamp": "2026-01-22T09:15:00.000000Z"
  }
]
```

**Başarısız Response (401)**:
```json
{
  "success": false,
  "message": "Yetkisiz!"
}
```

---

### POST /api/health-records
Yeni sağlık kaydı ekle (tansiyon/şeker ölçümü)

**İstek**:
```bash
curl -X POST "http://localhost:5007/api/health-records?token=YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "recordType": "tansiyon",
    "value": "170",
    "unit": "mmHg"
  }'
```

**Body Parametreleri**:
- `recordType` (string): "tansiyon" | "şeker"
- `value` (string/number): Ölçüm değeri
- `unit` (string): "mmHg" (tansiyon) | "mg/dL" (şeker)

**Alert Kuralları**:
| RecordType | Value | AlertLevel |
|-----------|-------|-----------|
| tansiyon | > 160 | critical 🚨 |
| tansiyon | 140-160 | warning ⚠️ |
| tansiyon | < 140 | normal ✅ |
| şeker | > 250 | critical 🚨 |
| şeker | 180-250 | warning ⚠️ |
| şeker | < 180 | normal ✅ |

**Başarılı Response (200 - Normal)**:
```json
{
  "success": true,
  "message": "Sağlık kaydı başarıyla eklendi!",
  "alertLevel": "normal"
}
```

**Başarılı Response (200 - Kritik)**:
```json
{
  "success": true,
  "message": "Sağlık kaydı başarıyla eklendi!",
  "alertLevel": "critical"
}
```

**Yan Etki (Critical Alert)**:
- Tüm aile üyeleri (ReceiveNotifications=true) otomatik notification alır
- Bildirim başlığı: "🚨 KRİTİK TANSIYÖN" veya "🚨 KRİTİK ŞEKER SEVİYESİ"

---

## 💊 İLAÇ HATIRLATICI (Medication Reminder - Fail-Safe)

### POST /api/medications/{medicationId}/reminded
Ilaç hatırlatması gösterildiğinde çağrıl (15-min fail-safe timer başlar)

**İstek**:
```bash
curl -X POST "http://localhost:5007/api/medications/med-id-123/reminded?token=YOUR_TOKEN"
```

**Parametreler**:
- `medicationId` (path): İlaç ID'si
- `token` (query): Elderly token

**Başarılı Response (200)**:
```json
{
  "success": true,
  "message": "Reminder tracked for fail-safe",
  "medicationId": "med-id-123"
}
```

**İş Akışı (Fail-Safe)**:
```
1. Saat 10:00 - İlaç alması gereken zaman
2. POST /api/medications/{id}/reminded ← Sistem çağrıyor
3. Backend: pendingMedicationReminders["med-id-123"] = 2026-01-22T10:00:00
4. Timer her 60 saniye kontrol ediyor...
5. Saat 10:15 - 15 dakika geçti!
6. Timer: Aileye 🚨 KRİTİK UYARI: İLAÇ UNUTULDU gönder
7. POST /api/medications/{id}/taken ← Hasta ilaç alırsa
8. Backend: pendingMedicationReminders.Remove("med-id-123")
```

---

### POST /api/medications/{medicationId}/taken
Ilaçı aldığını işaretle (Stock azal + Fail-safe kaldır)

**İstek**:
```bash
curl -X POST "http://localhost:5007/api/medications/med-id-123/taken?token=YOUR_TOKEN"
```

**Başarılı Response (200)**:
```json
{
  "success": true,
  "message": "İlaç başarıyla işaretlendi!",
  "stock": 44
}
```

**Stock Uyarı Response (200 - Stock = 3)**:
```json
{
  "success": true,
  "message": "İlaç başarıyla işaretlendi! ⚠️ İlacın azalıyor, eczaneye haber verelim mi?",
  "stock": 3
}
```

**Side Effects**:
- Stock azaltıldı (-1)
- Pending medication list'ten silindi (fail-safe timer'dan çıkar)
- Stock = 3 ise: ⚠️ WARNING notification aileye
- Stock < 3 ise: 🚨 CRITICAL notification aileye

---

## 🎤 SORU KAYDI (Dementia Tracking)

### POST /api/question-log
Yaşlının sorduğu soruyu kaydet (Dementia anomali tespiti)

**İstek**:
```bash
curl -X POST "http://localhost:5007/api/question-log?token=YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"question":"Merhaba, ben kimim?"}'
```

**Body**:
- `question` (string): Sorduğu soru

**Başarılı Response (200)**:
```json
{
  "success": true,
  "message": "Soru kaydedildi",
  "anomalyDetected": false
}
```

**Anomali Tespit Edildi (200)**:
```json
{
  "success": true,
  "message": "Soru kaydedildi",
  "anomalyDetected": true
}
```

**Dementia Anomali Mantığı**:
```
IF (Aynı soru 3+ kez son 60 dakika içinde sorulduysa):
  → ⚠️ UNUTKALLLIK ANOMALISI bildirimi aileye gönder
  → anomalyDetected: true döndür
```

**Örnek**:
```
10:00 - "Doktor kim?" → ✅ Normal (1. kez)
10:05 - "Doktor kim?" → ✅ Normal (2. kez)
10:10 - "Doktor kim?" → ⚠️ ANOMALI! (3. kez) → Aile uyarısı!
```

---

## 🚨 ACİL YARDIM MODU (Panic Mode)

### POST /api/emergency-alert
Acil durumda yaşlının konumunu aileye gönder

**İstek**:
```bash
curl -X POST "http://localhost:5007/api/emergency-alert?token=YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "latitude": 41.0082,
    "longitude": 28.9784,
    "message": "İMDAT!"
  }'
```

**Body**:
- `latitude` (number): GPS enlemi
- `longitude` (number): GPS boylamı  
- `message` (string, optional): Mesaj ("YARDIM ET!" gibi)

**Başarılı Response (200)**:
```json
{
  "success": true,
  "message": "Aile üyeleri uyarıldı!",
  "location": {
    "latitude": 41.0082,
    "longitude": 28.9784
  },
  "mapUrl": "https://maps.google.com/?q=41.0082,28.9784"
}
```

**Notification (Aile'ye gönderilen)**:
```
Başlık: 🚨 ACİL YARDIM TALEBİ
Mesaj: Ayşe Hanım ACİL YARDIM TALEP ETTİ! KONUM: 41.0082, 28.9784 - https://maps.google.com/?q=41.0082,28.9784
Tip: critical
Tüm aile üyeleri alır
```

---

## 📱 FRONTEND İNTEGRASYON (elderly.js)

### Health Records UI
```javascript
// Sayfaya git
goToHealthRecords()  // → healthRecordsScreen göster

// Verileri yükle
loadHealthRecords()  // → GET /api/health-records çağır

// Yeni kayıt ekle
addHealthRecord("tansiyon", "170", "mmHg")  // → POST /api/health-records

// Prompt ile interaktif
showAddHealthRecord()  // → "Tansiyon mı şeker mi?" → kayıt ekle
```

### Panic Mode (Coming Soon)
```javascript
// Voice komutu: "YARDIM ET" veya "İMDAT"
// → navigator.geolocation.getCurrentPosition()
// → POST /api/emergency-alert
// → Family dashboard'da RED PANIC göster + Map
```

---

## ⚠️ HATALARı HANDLE ETME

### Token Hataları
```json
{
  "statusCode": 401,
  "response": {
    "success": false,
    "message": "Yetkisiz!"
  }
}
```

### Medication Bulunamadı
```json
{
  "statusCode": 404,
  "response": {
    "success": false,
    "message": "İlaç bulunamadı!"
  }
}
```

### Geçersiz Body
```json
{
  "statusCode": 400,
  "response": {
    "success": false,
    "message": "Soru boş olamaz!"
  }
}
```

---

## 🧪 CUI CURL ÖRNEKLERI

### Test 1: Login & Token Al
```bash
curl -X POST http://localhost:5007/api/elderly/login \
  -H "Content-Type: application/json" \
  -d '{"email":"elderly@test.com","password":"1234"}'

# Response içinden token'ı kopyala
```

### Test 2: Sağlık Kaydı Ekle
```bash
TOKEN="YOUR_TOKEN_HERE"

# Kritik tansiyon
curl -X POST "http://localhost:5007/api/health-records?token=$TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"recordType":"tansiyon","value":"170","unit":"mmHg"}'

# Uyarı seviyesi şeker
curl -X POST "http://localhost:5007/api/health-records?token=$TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"recordType":"şeker","value":"200","unit":"mg/dL"}'

# Normal tansiyon
curl -X POST "http://localhost:5007/api/health-records?token=$TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"recordType":"tansiyon","value":"120","unit":"mmHg"}'
```

### Test 3: Dementia Anomali Tespiti
```bash
TOKEN="YOUR_TOKEN_HERE"

# Soru 1
curl -X POST "http://localhost:5007/api/question-log?token=$TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"question":"Ben kimim?"}'

# Soru 2 (Aynı soru)
curl -X POST "http://localhost:5007/api/question-log?token=$TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"question":"Ben kimim?"}'

# Soru 3 (Aynı soru - ANOMALI!)
curl -X POST "http://localhost:5007/api/question-log?token=$TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"question":"Ben kimim?"}'

# Response: anomalyDetected: true
```

### Test 4: İlaç Fail-Safe
```bash
TOKEN="YOUR_TOKEN_HERE"
MEDICATION_ID="med-id-from-list"

# 1. İlk hatırlatma
curl -X POST "http://localhost:5007/api/medications/$MEDICATION_ID/reminded?token=$TOKEN"

# 2. 15+ dakika bekle (veya timer tamamlandığında)
# Family dashboard'da ⚨ UYARI göreceksiniz

# 3. İlaç alındı
curl -X POST "http://localhost:5007/api/medications/$MEDICATION_ID/taken?token=$TOKEN"
```

### Test 5: Acil Yardım
```bash
TOKEN="YOUR_TOKEN_HERE"

curl -X POST "http://localhost:5007/api/emergency-alert?token=$TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "latitude": 41.0082,
    "longitude": 28.9784,
    "message": "YARDIM ET!"
  }'
```

---

## 📈 KULLANIM İSTATİSTİKLERİ

Tüm health records, question logs, notifications **bellek'te (in-memory)** tutulmaktadır.  
Üretim ortamında **SQLite** veya **PostgreSQL** kullanılmalıdır.

```csharp
// Current Data Stores
var healthRecords = new List<HealthRecord>();      // Sağlık ölçümleri
var questionHistory = new List<QuestionLog>();     // Sorular
var notifications = new List<Notification>();      // Bildirimler
var pendingMedicationReminders = new Dictionary<string, DateTime>(); // Fail-safe timers
```

---

**Last Updated**: 22 January 2026  
**API Version**: 1.0  
**Status**: Production-Ready (MVP)  
