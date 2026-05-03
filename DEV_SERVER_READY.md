# 🚀 Otomatik Dev Server Kurulum - TAMAM ✅

## 📋 Yapılandırılmış Bileşenler

### ✅ Backend Watcher
- **dotnet watch** ile otomatik restart
- C# dosyası değiştiğinde ~1-2 saniye içinde yeniden başlar
- Port: 5007

### ✅ Web Asset Sync
- **chokidar-cli** ile `wwwroot/**` dosyalarını izler
- Değişiklik ~3-5 saniye içinde `ios/App/App/public/` klasörüne kopyalanır
- Capacitor iOS WebView her zaman fresh kodu görür

### ✅ VS Code Integration
- `.vscode/tasks.json`: 3 adet task tanımlanmış
- `.vscode/launch.json`: Debug configurations hazır
- `package.json`: Web sync script eklenmiş

---

## 🎮 Kullanım - 2 Seçenek

### Seçenek A: Komut Paletinden (👍 Önerilen)
```
1. Mac:   Cmd + Shift + P
   Win:   Ctrl + Shift + P
2. "Tasks: Run Task" yaz ve seç
3. "dev-server" seç ve Enter
4. ✅ Backend + Web Sync otomatik başlar
```

### Seçenek B: VS Code Sidebar'dan
1. VS Code'da "Run and Debug" panelini aç
2. "Dev Server (Backend + Web Sync)" konfigürasyonunu seç
3. Yeşil Play butona basarak başlat

---

## 📊 Task'lar Nelerdir?

| Task | Ne Yapar | Terminal |
|------|----------|----------|
| `dev-server` | Backend + Web Sync paralel | Shared |
| `backend-watch` | Sadece dotnet watch | New |
| `web-sync-watch` | Sadece web file watching | New |

---

## 🔄 Tipik Geliştirme Akışı

```
1️⃣  dev-server task'ını başlat (Cmd+Shift+P → Tasks: Run Task)
   
2️⃣  Terminal panelinde çıktıları gözle:
   - Backend: "Application started. Press Ctrl+C to shut down."
   - Web Sync: "Watching ..."

3️⃣  Kod değiştir ve kaydet, örneğin:
   - wwwroot/elderly-ui/elderly.js
   - wwwroot/index.html
   
4️⃣  Otomatik olarak iOS public klasörüne kopyalanır:
   - ios/App/App/public/elderly-ui/elderly.js ← updated
   
5️⃣  Simulatör/Device'da refresh et (Cmd+R veya app içi button)
   
6️⃣  Yeni kod live! 🎉
```

---

## 🔍 Doğrulama

Kurulumun doğru olduğunu test etmek için:

```bash
# Terminal'de çalıştır
cd "/Users/busenurakdeniz/Desktop/ilk projem"
bash verify-dev-setup.sh
```

Çıktı:
```
✅ All checks passed! Ready to run dev-server
```

---

## ⚙️ Manuel Komutlar (İsteğe Bağlı)

Eğer VS Code task'larını kullanmak istemezsen:

```bash
# Terminal 1: Backend watch
cd "/Users/busenurakdeniz/Desktop/ilk projem"
dotnet watch --project AsistanApp/AsistanApp.csproj run

# Terminal 2: Web sync watch (ayrı terminal'de)
cd "/Users/busenurakdeniz/Desktop/ilk projem/AsistanApp"
npm run cap:copy:watch
```

---

## 🧪 Hızlı Test

Backend çalışıyor mu?
```bash
curl -I http://127.0.0.1:5007/api/elderly/login
# Expected: HTTP/1.1 200 OK
```

Web assets kopyalanmış mı?
```bash
# Her ikisinin timestamp'i yakın olmalı
ls -la wwwroot/elderly-ui/elderly.js
ls -la ios/App/App/public/elderly-ui/elderly.js
```

---

## 📖 İlgili Dosyalar

- [QUICK_DEV_START.md](QUICK_DEV_START.md) - Quick reference
- [DEV_SERVER_GUIDE.md](DEV_SERVER_GUIDE.md) - Detaylı kılavuz
- [verify-dev-setup.sh](verify-dev-setup.sh) - Doğrulama scripti

---

## ✨ Bitti!

Dev server hazır. Sevinç! 🎉

**Sıradaki adım: `Cmd+Shift+P` → `Tasks: Run Task` → `dev-server`**
