# 🚀 PRODUCTION DEPLOYMENT & CONFIGURATION GUIDE

## Phase 1: Release Configuration (Production Hazırlığı)

### 1.1 Build Configuration
```bash
# Production build oluştur (Release mode)
cd /Users/busenurakdeniz/Desktop/ilk\ projem/AsistanApp
dotnet publish -c Release -o ./publish

# Dosya boyutu kontrol et
du -sh ./publish
```

### 1.2 Environment Configuration
Proje kök dizininde `appsettings.Production.json` oluştur:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Information"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5007"
      },
      "Https": {
        "Url": "https://0.0.0.0:5008",
        "Certificate": {
          "Path": "/etc/vitaguard/certificate.pfx",
          "Password": "YOUR_CERT_PASSWORD"
        }
      }
    }
  },
  "Database": {
    "ConnectionString": "Server=your-server.database.windows.net;Database=VitaGuard;User Id=dbuser;Password=SecurePassword!;Encrypt=true;Connection Timeout=30;"
  },
  "Authentication": {
    "JwtSecret": "your-secret-key-min-32-characters-long-here",
    "TokenExpiryHours": 24
  },
  "SignalR": {
    "Enabled": true,
    "TransportType": "WebSocket"
  }
}
```

### 1.3 Release Build Script
```bash
#!/bin/bash
# release.sh - Production build script

set -e

echo "🔧 Production Release Build başlıyor..."
echo "📝 Git commit hash'i alınıyor..."
COMMIT_HASH=$(git rev-parse --short HEAD)
echo "✓ Commit: $COMMIT_HASH"

echo "🧹 Eski build dosyaları temizleniyor..."
rm -rf ./publish
rm -rf ./bin/Release

echo "📦 Production build..."
dotnet publish -c Release -o ./publish --self-contained false

echo "📊 Build istatistikleri:"
du -sh ./publish
find ./publish -name "*.dll" | wc -l | xargs echo "DLL dosyaları:"
find ./publish -name "*.json" | wc -l | xargs echo "JSON dosyaları:"

echo "✅ Production release hazır! (/publish klasöründe)"
echo "📌 Sonraki adım: Azure/AWS'ye yükle"
```

### 1.4 appsettings'den Debug Logging Kaldır
Program.cs'e ekle (logging configuration):

```csharp
// Development ortamında detaylı logging
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    Console.WriteLine("🔧 DEVELOPMENT MODE - Swagger aktif");
}
else
{
    // Production: hatalı logging
    app.UseExceptionHandler("/error");
    Console.WriteLine("🚀 PRODUCTION MODE - Swagger deaktif");
}
```

---

## Phase 2: Database Migration (SQL Server'a Geçiş)

### 2.1 Entity Framework Core Setup
```bash
# Paket yükle
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

### 2.2 DbContext Oluştur
```csharp
using Microsoft.EntityFrameworkCore;

public class VitaGuardDbContext : DbContext
{
    public VitaGuardDbContext(DbContextOptions<VitaGuardDbContext> options) 
        : base(options)
    {
    }

    public DbSet<ElderlyUser> ElderlyUsers { get; set; }
    public DbSet<HealthSymptom> HealthSymptoms { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<FallDetectionLog> FallDetectionLogs { get; set; }
    public DbSet<SelfCareReminder> SelfCareReminders { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    // ... diğer DbSet'ler
}
```

### 2.3 Program.cs'e EF Core Ekle
```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=VitaGuard;Trusted_Connection=true;";

builder.Services.AddDbContext<VitaGuardDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### 2.4 Migration Oluştur
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2.5 Data Migration Script (In-Memory → SQL)
```csharp
using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VitaGuardDbContext>();
    
    // In-memory verilerini SQL'e aktar
    foreach (var elderly in elderlyUsers)
    {
        context.ElderlyUsers.Add(elderly);
    }
    
    foreach (var symptom in healthSymptoms)
    {
        context.HealthSymptoms.Add(symptom);
    }
    
    // ... diğer entityler
    
    await context.SaveChangesAsync();
    Console.WriteLine("✅ Veri migrasyonu tamamlandı!");
}
```

---

## Phase 3: SSL Certificate & HTTPS

### 3.1 Self-Signed Certificate (Testing)
```bash
# Development sertifikası oluştur
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# Sertifika kontrol et
dotnet dev-certs https --check --verbose
```

### 3.2 Production Certificate (Let's Encrypt)

**Option A: Azure App Service (Otomatik)**
```bash
# Azure'da barındırıyorsan, .com domainini bağla
# Azure Portal → TLS/SSL → Add domain → Let's Encrypt otomatik
```

**Option B: Manual Installation**
```bash
# Certbot yükle (Ubuntu/Linux)
sudo apt-get install certbot python3-certbot-nginx

# Certificate oluştur
sudo certbot certonly --standalone -d vitaguard.app

# PFX'e dönüştür
openssl pkcs12 -export -out certificate.pfx \
    -inkey /etc/letsencrypt/live/vitaguard.app/privkey.pem \
    -in /etc/letsencrypt/live/vitaguard.app/fullchain.pem
```

### 3.3 Kestrel HTTPS Configuration
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5007); // HTTP
    options.ListenAnyIP(5008, listenOptions =>
    {
        listenOptions.UseHttps("/etc/vitaguard/certificate.pfx", "password");
    });
});
```

---

## Phase 4: Cloud Deployment (Azure)

### 4.1 Azure App Service Setup

```bash
# Azure CLI yükle
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login
az login

# Resource group oluştur
az group create --name VitaGuardRG --location eastus

# App Service plan oluştur
az appservice plan create \
    --name VitaGuardPlan \
    --resource-group VitaGuardRG \
    --sku B1 \
    --is-linux

# App Service oluştur
az webapp create \
    --resource-group VitaGuardRG \
    --plan VitaGuardPlan \
    --name vitaguard-api \
    --runtime "DOTNETCORE|8.0"
```

### 4.2 Database Setup (Azure SQL)

```bash
# SQL Server oluştur
az sql server create \
    --name vitaguard-server \
    --resource-group VitaGuardRG \
    --location eastus \
    --admin-user dbadmin \
    --admin-password ComplexPassword123!

# Database oluştur
az sql db create \
    --resource-group VitaGuardRG \
    --server vitaguard-server \
    --name VitaGuard \
    --edition Standard \
    --capacity 10
```

### 4.3 Deployment (GitHub Actions)

`.github/workflows/azure-deployment.yml`:
```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.x'
    
    - name: Build
      run: |
        cd AsistanApp
        dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: vitaguard-api
        publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
        package: './AsistanApp/publish'
```

---

## Phase 5: API Documentation (Swagger/OpenAPI)

### 5.1 Swagger Kontrolü
```bash
# Server çalışırken:
curl http://localhost:5007/swagger/v1/swagger.json | jq
```

### 5.2 OpenAPI Export
```bash
# OpenAPI JSON dosyasını indir
curl http://localhost:5007/swagger/v1/swagger.json \
    -o openapi.json

# Redoc ile dokümantasyon oluştur
npm install -g redoc-cli
redoc-cli build openapi.json -o api-docs.html
```

### 5.3 Postman Collection Oluştur
```bash
# Swagger'dan Postman'a aktar
curl http://localhost:5007/swagger/v1/swagger.json \
    -o VitaGuard-API.postman_collection.json

# Postman'a import et
# https://www.postman.com/downloads/
```

---

## Phase 6: Monitoring & Logging

### 6.1 Application Insights (Azure)

```bash
# Application Insights oluştur
az monitor app-insights component create \
    --app vitaguard-insights \
    --location eastus \
    --resource-group VitaGuardRG \
    --application-type web
```

### 6.2 Program.cs'e Logging Ekle

```csharp
using Microsoft.ApplicationInsights;

var instrumentationKey = builder.Configuration["ApplicationInsights:InstrumentationKey"];
builder.Services.AddApplicationInsightsTelemetry(instrumentationKey);

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("✅ VitaGuard API başlatıldı");
```

### 6.3 Health Check Endpoint

```csharp
app.MapGet("/health", () => new 
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    uptime = GC.TotalMemory(false),
    version = "1.0.0"
});
```

---

## Phase 7: Security Hardening

### 7.1 CORS Configuration

```csharp
var allowedOrigins = new[] 
{
    "https://vitaguard.app",
    "https://*.vitaguard.app",
    "https://dashboard.vitaguard.app"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("VitaGuardPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

app.UseCors("VitaGuardPolicy");
```

### 7.2 Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "fixed", configureOptions: options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });
});

app.UseRateLimiter();
```

### 7.3 Security Headers

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    
    await next();
});
```

---

## Deployment Checklist

- [ ] Sürüm numarası güncelle (v1.0.0)
- [ ] Changelog oluştur
- [ ] Release notes yaz
- [ ] Tüm testler geç (unit + integration)
- [ ] Code review yapıl
- [ ] Security audit tamamla
- [ ] Performance test geç (load test)
- [ ] Database backup al
- [ ] SSL sertifikası yapılandır
- [ ] CORS politikası kontrol et
- [ ] Rate limiting yapılandır
- [ ] Logging ve monitoring aktif et
- [ ] Health check çalışıyor mu kontrol et
- [ ] Swagger dokumentasyonu güncelle
- [ ] GitHub Actions workflow çalışıyor mu kontrol et
- [ ] Azure credentials güvenli mi kontrol et
- [ ] Staging ortamında test et
- [ ] Production'a deploy et
- [ ] Smoke test (temel fonksiyonlar)
- [ ] Monitoring dashboards bak

---

## Production URLs

- **API Base:** https://api.vitaguard.app
- **Dashboard:** https://dashboard.vitaguard.app
- **API Docs:** https://api.vitaguard.app/swagger
- **Health Check:** https://api.vitaguard.app/health

---

## Emergency Rollback

```bash
# Önceki versiyon'a dön
az webapp deployment slot swap --name vitaguard-api \
    --resource-group VitaGuardRG \
    --slot staging

# Database rollback
az sql db restore --name VitaGuard \
    --server vitaguard-server \
    --resource-group VitaGuardRG \
    --backup-name "VitaGuard_backup_2024_01_22"
```

---

**Status:** ✅ Ready for Production
**Last Updated:** 2024-01-22
