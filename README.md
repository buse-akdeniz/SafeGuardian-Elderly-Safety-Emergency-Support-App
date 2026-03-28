# 👵 VitaGuard: Elderly Care Assistant (Yaşlı Bakım Asistanı)
[![Platform](https://img.shields.io/badge/.NET-10-512BD4)](#)
[![Framework](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-5C2D91)](#)
[![Realtime](https://img.shields.io/badge/SignalR-Enabled-00A3E0)](#)
[![Data](https://img.shields.io/badge/EF%20Core%20%2B%20SQLite-Enabled-3C873A)](#)
[![Language](https://img.shields.io/badge/TR--EN-Bilingual-1F6FEB)](#)

---

## 1) Overview | Genel Bakış
**TR:** VitaGuard, yaşlı bireylerin güvenlik, sağlık takibi ve aile iletişimi için tasarlanmış erişilebilir bir bakım platformudur.
**EN:** VitaGuard is an accessibility-first care platform for elderly safety, health tracking, and family communication.
**TR:** Mimari, web-first yaklaşımı ile geliştirilmiş; mobil tarafta Capacitor iOS klasörü üzerinden paketlenebilir.
**EN:** The solution is web-first and can be packaged for mobile via the Capacitor iOS folder.

---

## 2) Core Capabilities | Temel Yetenekler
- **TR:** Yaşlı kullanıcı oturum açma ve oturum doğrulama.
- **EN:** Elderly user login and session validation.
- **TR:** Sağlık kayıtlarını listeleme/ekleme.
- **EN:** Health record read/write flows.
- **TR:** Aile üyeleri yönetimi ve düşme alarmı akışı.
- **EN:** Family member access and fall alert workflow.
- **TR:** Kullanıcı bağlamı (`user-state`) okuma/güncelleme.
- **EN:** User context (`user-state`) retrieval/update.
- **TR:** Acil durum, sağlık veri alımı ve görev tamamlama runtime endpoint’leri.
- **EN:** Runtime endpoints for emergency, health ingestion, and task completion.
- **TR:** SignalR ile gerçek zamanlı olay iletimi.
- **EN:** Real-time event distribution via SignalR.

---

## 3) Architecture | Mimari
### TR
- ASP.NET Core içinde Controller + Minimal API hibrit yaklaşımı.
- İş kuralları uygulama servislerinde (`HealthDataService`) merkezileştirilir.
- Realtime katman `HealthReportHub` üzerinden çalışır (`/health-hub`).
- EF Core + SQLite kalıcılık katmanı kullanılır.
- Statik ön yüz dosyaları backend tarafından sunulur.
### EN
- Hybrid Controller + Minimal API architecture in ASP.NET Core.
- Business rules are centralized in application services (`HealthDataService`).
- Realtime operations run through `HealthReportHub` (`/health-hub`).
- Persistence is implemented with EF Core + SQLite.
- Static front-end assets are served by the backend.

---

## 4) Stack | Teknoloji Yığını
| Layer | TR | EN |
|---|---|---|
| Runtime | .NET 10 | .NET 10 |
| Backend | ASP.NET Core | ASP.NET Core |
| Realtime | SignalR | SignalR |
| ORM/Data | EF Core + SQLite | EF Core + SQLite |
| API Style | Controller + Minimal API | Controller + Minimal API |
| Mobile Bridge | `AsistanApp/ios` (Capacitor) | `AsistanApp/ios` (Capacitor) |

---

## 5) Security & Compliance | Güvenlik ve Uyum
### TR
- Kimlik doğrulamada `Authorization: Bearer <token>` önceliklidir.
- Geri uyumluluk için query/body token çözümleme desteklenir.
- Production ortamında HTTPS yönlendirme + HSTS etkinleşir.
- IP tabanlı rate limiting middleware mevcuttur.
- Sağlık verisi operasyonlarında KVKK/GDPR uyum süreçleri önerilir.
### EN
- `Authorization: Bearer <token>` is prioritized for authentication.
- Query/body token extraction is supported for backward compatibility.
- HTTPS redirection + HSTS are enabled in production.
- IP-based rate limiting middleware is present.
- KVKK/GDPR-aligned operational controls are recommended.
> **TR:** Bu ürün tıbbi teşhis sistemi değildir.
> **EN:** This product is not a medical diagnosis system.

---

## 6) API Summary | API Özeti
### 6.1 Auth / Session
| Method | Endpoint | TR | EN |
|---|---|---|---|
| POST | `/api/auth/elderly/login` | Yaşlı kullanıcı girişi | Elderly login |
| GET | `/api/auth/me` | Oturum profilini döner | Returns session profile |

### 6.2 Core Domain
| Method | Endpoint | TR | EN |
|---|---|---|---|
| GET | `/api/health/records` | Sağlık kayıtlarını listeler | Lists health records |
| POST | `/api/health/records` | Sağlık kaydı ekler | Adds health record |
| GET | `/api/family/members` | Aile üyelerini getirir | Returns family members |
| POST | `/api/family/fall-alert` | Düşme alarmı üretir | Triggers fall alert |
| GET | `/api/user-state` | Kullanıcı durumunu döner | Returns user state |
| POST | `/api/user-state` | Kullanıcı durumunu günceller | Updates user state |

### 6.3 Additional Runtime Endpoints
| Method | Endpoint | TR | EN |
|---|---|---|---|
| POST | `/api/emergency-alert` | Acil durum bildirimi | Emergency alert |
| POST | `/api/health-data` | Çalışma zamanı sağlık verisi | Runtime health ingestion |
| POST | `/api/complete-task` | Görev tamamlama olayı | Task completion event |

### 6.4 Realtime Hub
- **Path:** `/health-hub`
- **TR:** Aile ve bakım akışlarına anlık olay yayınlar.
- **EN:** Broadcasts real-time events for family and care workflows.

---

## 7) Quick Start | Hızlı Başlangıç
### TR
1. Depoyu klonlayın.
2. Bağımlılıkları yükleyin.
3. API’yi çalıştırın.
4. Tarayıcıdan doğrulayın.
### EN
1. Clone the repository.
2. Restore dependencies.
3. Run the API.
4. Verify in browser.

```bash
cd "ilk projem"
dotnet restore AsistanApp/AsistanApp.csproj
dotnet run --project AsistanApp/AsistanApp.csproj
```

- Local URL: `http://127.0.0.1:5007`

---

## 8) Configuration | Yapılandırma
### TR
- Konfigürasyon dosyaları: `appsettings.json`, `appsettings.Development.json`, `appsettings.Staging.json`, `appsettings.Production.json`.
- DB bağlantısı: `ConnectionStrings:DefaultConnection`.
- Varsayılan SQLite: `asistanapp.db`.
- Rate limit ayarları: `RateLimiting:*`.
### EN
- Configuration files: `appsettings.json`, `appsettings.Development.json`, `appsettings.Staging.json`, `appsettings.Production.json`.
- DB connection: `ConnectionStrings:DefaultConnection`.
- Default SQLite file: `asistanapp.db`.
- Rate limit settings: `RateLimiting:*`.

---

## 9) Offline Sync Notes | Çevrimdışı Senkron Notları
### TR
- Ön yüz katmanında çevrimdışı kuyruklama yaklaşımı önerilir.
- Bağlantı geri geldiğinde bekleyen kayıtlar API’ye gönderilmelidir.
- Mobil ağlarda idempotent istek tasarımı önerilir.
### EN
- Front-end offline queue patterns are recommended.
- Pending records should be synced after reconnect.
- Idempotent request design is advised for unstable mobile networks.

---

## 10) Testing | Test Süreci
### TR
- VS Code görevleri ile canlı smoke test akışları mevcuttur.
- Login, sağlık, acil durum, düşme alarmı ve state endpoint’leri doğrulanabilir.
- API rehberlerindeki senaryolarla entegrasyon testi yapılmalıdır.
### EN
- Live smoke tests are available via VS Code tasks.
- Login, health, emergency, fall alert, and state endpoints can be validated.
- Run integration flows based on project API test guides.

---

## 11) Deployment | Dağıtım
### TR
- Dağıtım script’i ve rehberler repoda bulunur (`deploy-production.sh` ve deployment dokümanları).
- Production’da HTTPS/HSTS ve güvenli token taşıma standart olmalıdır.
- Dev/staging/prod yapılandırmaları ayrı tutulmalıdır.
### EN
- Deployment script and guides are available in-repo (`deploy-production.sh` and deployment docs).
- Enforce HTTPS/HSTS and secure token transport in production.
- Keep dev/staging/prod configuration clearly separated.

---

## 12) Localization | Yerelleştirme
### TR
- README TR-EN iki dillidir.
- Son kullanıcı metinleri erişilebilir ve sade tutulmalıdır.
### EN
- This README is bilingual (TR-EN).
- End-user texts should remain clear and accessibility-focused.

---

## 13) Branding Names | Marka İsimleri
### TR
- Ürün adı: **VitaGuard: Elderly Care Assistant**
- Türkçe adı: **Yaşlı Bakım Asistanı**
- Tüm kanallarda isim ve görsel tutarlılık önerilir.
### EN
- Product name: **VitaGuard: Elderly Care Assistant**
- Turkish name: **Yaşlı Bakım Asistanı**
- Maintain naming and visual consistency across channels.

---

## 14) Roadmap | Yol Haritası
### TR
- Rol bazlı yetkilendirme derinleştirme.
- Audit trail/loglama geliştirmeleri.
- Mobil push akışlarının üretim seviyesinde sertleştirilmesi.
- Gelişmiş raporlama ve entegrasyonlar.
### EN
- Expanded role-based authorization.
- Stronger audit trail and observability.
- Production-hardening of mobile push flows.
- Advanced reporting and integrations.

---

## 15) Documentation | Dokümantasyon
### TR/EN
- [QUICK_START.md](QUICK_START.md)
- [API_REFERENCE.md](API_REFERENCE.md)
- [API_TEST_GUIDE.md](API_TEST_GUIDE.md)
- [SYSTEM_OVERVIEW.md](SYSTEM_OVERVIEW.md)
- [SECURITY_CONFIG.md](SECURITY_CONFIG.md)
- [PRODUCTION_DEPLOYMENT_GUIDE.md](PRODUCTION_DEPLOYMENT_GUIDE.md)

---

## 16) Support | Destek
### TR
- Operasyonel destek için kurum içi bakım/teknik ekibi ile iletişime geçin.
- Hata bildirimi: endpoint, zaman damgası ve log kesiti ekleyin.
### EN
- Contact your internal care/technical team for operational support.
- Include endpoint, timestamp, and log excerpt in issue reports.

---

## 17) License | Lisans
### TR
Depo kökünde açık bir ürün lisans dosyası yoksa, kullanım koşulları kurum/proje politikasına tabidir.
### EN
If no explicit product license file exists at repository root, usage terms are governed by your organization/project policy.

---

## 18) Professional Notes | Profesyonel Notlar
### TR
- Bu README, mevcut kod tabanındaki .NET 10 + ASP.NET Core + SignalR + EF Core/SQLite yapısıyla uyumludur.
- API özeti, aktif controller endpoint’leri ve runtime endpoint’leri kapsar.
### EN
- This README is aligned with the current .NET 10 + ASP.NET Core + SignalR + EF Core/SQLite implementation.
- The API summary covers active controller and runtime endpoints.
