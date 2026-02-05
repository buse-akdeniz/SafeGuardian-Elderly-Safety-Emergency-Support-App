# 🧪 TEST SENARYOLARI

## Hızlı Test Kılavuzu

### Test 1: Fall Detection
```javascript
await aiProtocol.detectFall(28);
```
**Beklenen:** Kırmızı alert ekranı + "İyi misin?" sorusu

### Test 2: Voice Recognition
- Sistem sorar → Siz yanıt verin
- Olumlu: "İyiyim, tamam"
- Olumsuz: Sessizlik (15 sec timeout)

### Test 3: Language Switch
```javascript
await setLanguage('tr');  // Turkish
await setLanguage('en');  // English
await setLanguage('de');  // German
```

### Test 4: Emergency Broadcast
- Tab 1: Fall detection tetikle
- Tab 2 (Family): Critical alert görmeli

**Tüm testler başarılıysa PRODUCTION READY!** ✅

