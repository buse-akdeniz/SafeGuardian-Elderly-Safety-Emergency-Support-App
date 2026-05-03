# Secrets rotation & revocation checklist

Bu dosya, repo'dan kaldırdığın `AsistanApp/.env` içindeki anahtarların sağlayıcı tarafında nasıl rotasyon ve iptal edileceğine dair kısa, uygulanabilir adımları içerir.

1) Twilio
  - Hesap'ta oturum aç.
  - `Account` -> `API Keys & Tokens` bölümünden eski `TWILIO_AUTH_TOKEN`/SID'i iptal et.
  - Yeni API key oluştur, yeni `TWILIO_ACCOUNT_SID` ve `TWILIO_AUTH_TOKEN` değerlerini al.
  - Yeni numara gerekiyorsa `TWILIO_PHONE_NUMBER`'ı güncelle.

2) E-posta (SMTP)
  - Kullandığın e-posta sağlayıcısına (ör: Google Workspace) giriş yap.
  - Uygulama parolaları / API anahtarları bölümünden eski `EMAIL_PASSWORD`/credentials'i iptal et.
  - Yeni uygulama parolası veya SMTP kimlik bilgisi yarat.

3) Apple / App Store Server
  - App Store Connect -> `Users and Access` veya `Keys` bölümünden `APP_STORE_SERVER_API_KEY` ya da `APPLE_SHARED_SECRET` gereksinimlerini güncelle.
  - Mevcut API key'i revoke et ve yeni key'i indir/ekle.

4) JWT_SECRET
  - Uygulama içinde `JWT_SECRET` değişikliğini planla; rolling deploy veya kısa downtime gerektirebilir.
  - Yeni `JWT_SECRET`'i oluştur ve CI/CD secret store'a koy.
  - Tüm çalışan sunucuları yeniden başlat veya uygulamayı yeniden deploy et.

5) Genel: Yeni anahtarları güvenli şekilde sakla
  - Yeni anahtarları kesinlikle repoya yazma.
  - GitHub Actions: `Settings` -> `Secrets and variables` -> `Actions` -> `New repository secret` kullan.
  - Azure DevOps/AWS/Azure/GCP: ilgili secrets manager'a ekle (Key Vault, Secrets Manager, Secret Manager vs.).

6) Test & doğrulama
  - Yeni secrets eklendikten sonra CI pipeline'ını çalıştır; smoke-test'leri (login, subscription, emergency) çalıştırarak doğrula.
  - Loglarda eski secret ile ilgili hata olup olmadığını kontrol et.

7) Kayıt
  - Hangi anahtarlar döndürüldü/iptal edildiğini kısa bir kayıtla not et (ör: Twilio token rotated at 2026-04-24 08:00 UTC).

Not: Ben sağlayıcı tarafında anahtarı iptal edemem; bu adımları takip edip doğrulayınca ben repo ve CI tarafındaki konfigürasyonları da güncelleyebilirim.
