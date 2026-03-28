
## 🔐 Güvenlik & Gizlilik (Security & Privacy)

Bu projede güvenlik için aşağıdaki kurallar uygulanmıştır:

### ✅ Yapılanlar:
- **Secret Management:** Gerçek API anahtarları asla kod içine veya README dosyasına yazılmaz.
- **Environment Variables (Ortam Değişkenleri):** Lokal testler için şifreler terminal üzerinden (`export API_KEY="..."`) tanımlanır.
- **Gitignore:** Şifre içeren yapılandırma dosyaları (`appsettings.Secrets.json` gibi) `.gitignore` dosyasına eklenerek GitHub'a sızması engellenmiştir.

### ✅ Secret Yönetimi (GitHub’a yazma)
- **Gerçek API anahtarını README’ye koyma.**
- Lokal kullanım:
  - Mac: `export API_KEY="YOUR_REAL_KEY"`
  - İstek header: `X-API-Key: YOUR_REAL_KEY`
- `appsettings.Secrets.json` kullanıyorsan **.gitignore**’da kalmalı.

### ⚠️ Önemli Not:
Eğer bu projeyi kendi bilgisayarınızda çalıştırmak isterseniz, önce terminalinizde API anahtarınızı tanımlamanız gerekmektedir.