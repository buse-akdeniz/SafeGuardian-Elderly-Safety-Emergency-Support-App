# ⚡ Dev Server - Quick Start

## 🎯 En Hızlı Yol

**Mac'ta: `Cmd + Shift + P` → `Tasks: Run Task` → `dev-server` seç**

**Windows/Linux'ta: `Ctrl + Shift + P` → `Tasks: Run Task` → `dev-server` seç**

✅ Backend otomatik restart aktif
✅ Web assets otomatik kopya aktif
✅ Geliştirmeye hazır!

---

## 📊 Task Özeti

| Task | Fonksiyon | Port | Reload |
|------|-----------|------|--------|
| **dev-server** | Backend + Web Sync paralel | 5007 | ~1-5 sec |
| **backend-watch** | Sadece Backend watch | 5007 | ~1-2 sec |
| **web-sync-watch** | Sadece Web assets sync | N/A | ~3-5 sec |

---

## 🔄 Workflow

```
1. dev-server başlat (Cmd+Shift+P)
   ↓
2. wwwroot/*.html veya elderly-ui/*.js değiştir & kaydet
   ↓
3. Otomatik kopyalanır (3-5 saniye)
   ↓
4. Simülatörde refresh et (Cmd+R veya İOS içinde)
   ↓
5. Yeni kod live!
```

---

## 🧪 Test Komutları

```bash
# Backend çalışıyor mu?
curl -I http://127.0.0.1:5007/api/elderly/login

# iOS assets güncel mi?
ls -la ios/App/App/public/elderly-ui/elderly.js
stat ios/App/App/public/elderly-ui/elderly.js
```

---

## 📖 Detaylı Kılavuz

👉 Bkz. [DEV_SERVER_GUIDE.md](DEV_SERVER_GUIDE.md)
