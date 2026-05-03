# 🚀 Development Server Setup - Otomatik Backend & Web Sync

Bu yapılandırma, backend kodunda değişiklik olduğunda kendini otomatik restart eden ve web dosyalarını simülatöre anında kopyalayan geliştirme ortamı sağlar.

## 📋 Kurulum Tamamlandı

### Yüklenen Paketler
- `chokidar-cli`: Dosya değişikliklerini izleyen CLI tool

### Yapılandırılan Tasks

#### 1. **backend-watch** ⚙️ Backend Otomatik Restart
```bash
dotnet watch --project AsistanApp/AsistanApp.csproj run
```
- Backend'i 5007 portunda başlatır
- C# dosyalarında değişiklik olduğunda otomatik restart
- Terminal panelinde çalışır ve log gösterir

#### 2. **web-sync-watch** 🔄 Web Assets Otomatik Kopya
```bash
npm run cap:copy:watch
```
- `wwwroot/**/*` klasörünü izler
- Değişiklik algılandığında `npx cap copy ios` çalıştırır
- iOS public klasörü her zaman senkronize kalır

#### 3. **dev-server** 🎯 Tüm Sistem (Recommended)
```bash
Paralel çalışan: backend-watch + web-sync-watch
```
- Her ikisini aynı anda başlatır
- En hızlı geliştirme deneyimi sağlar

## 🎮 Kullanım

### Seçenek 1: Komut Paletinden (Önerilen)
1. VS Code'da `Cmd + Shift + P` tuşla (Mac) veya `Ctrl + Shift + P` (Windows/Linux)
2. `Tasks: Run Task` yazıp seç
3. **`dev-server`** seç

### Seçenek 2: Terminal'de Manuel
```bash
cd "/Users/busenurakdeniz/Desktop/ilk projem"

# Sadece Backend Watch
dotnet watch --project AsistanApp/AsistanApp.csproj run

# Sadece Web Sync Watch (ayrı terminal'de)
cd AsistanApp && npm run cap:copy:watch

# veya ikisini tek komutla paralel başlatmak için:
# (Terminal 1'de backend-watch'ı başlat, Terminal 2'de web-sync-watch'ı başlat)
```

## 📡 Geliştirme Akışı

### Tipik Senaryo:
```
1. dev-server task'ını başlat (Cmd+Shift+P → Tasks: Run Task → dev-server)
   ✅ Backend port 5007'de dinlemeye başlar
   ✅ Web file watcher aktif olur

2. wwwroot/elderly-ui/elderly.js'de değişiklik yap
   → Dosya kaydedilir
   → chokidar uyarı alır (3-5 saniye içinde)
   → npx cap copy ios otomatik çalışır
   → iOS public klasörü güncellenir

3. C# dosyasında (e.g., Controllers/AuthController.cs) değişiklik yap
   → dotnet watch uyarı alır (~1-2 saniye içinde)
   → Backend otomatik restart
   → Port 5007 yeniden hazır

4. Simülatörde (iOS) veya Capacitor web app'inde test et
   → Taze kod her zaman hazır
```

## ⚠️ İpuçları

### Backend Watch Sorunları?
Eğer `dotnet watch` çalışmıyorsa:
```bash
# Manuel olarak çalıştır
dotnet run --project AsistanApp/AsistanApp.csproj
```

### Web Sync Sorunları?
Eğer file watching çalışmıyorsa:
```bash
# Manuel olarak bir kez kopyala
cd AsistanApp && npx cap copy ios

# veya tüm assets senkronize et
npx cap sync ios
```

### Log Çıktısını Temizlemek
Tasks panel'inde "Clear" butonuna basın ya da:
```bash
# Terminal panelini temizle
cmd + k (macOS'te)
```

## 🔗 İlişkili Komutlar

```bash
# iOS simulatörü aç
npm run cap:open:ios

# Capacitor sync (sync plugins + copy assets)
npm run cap:sync

# Capacitor run (iOS simulator'da çalıştır)
npm run cap:run:ios
```

## 📊 Status Kontrol

### Backend'in Çalıştığını Doğrula
```bash
curl -I http://127.0.0.1:5007/api/elderly/login
# Beklenen: HTTP 200 OK
```

### Web Assets'in Kopyalandığını Doğrula
```bash
# Kontrol et
ls -la ios/App/App/public/elderly-ui/elderly.js
# Zaman damgası güncel olmalı
```

---

**Kurulum Tamamlandı!** ✅ `dev-server` task'ını çalıştırarak başlayabilirsin.
