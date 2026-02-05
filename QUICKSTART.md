# 🚀 VitaGuard - Quick Start Guide (Production Ready)

**Status:** ✅ Production-Ready  
**Build:** 0 Errors, 68 Warnings  
**Version:** 1.0.0  
**Last Updated:** 2024-01-22

---

## 📋 Table of Contents

1. [Development Setup](#development-setup)
2. [Running Locally](#running-locally)
3. [API Testing](#api-testing)
4. [Localization Testing](#localization-testing)
5. [Real-Time Testing (SignalR)](#real-time-testing-signalr)
6. [Deployment to Azure](#deployment-to-azure)
7. [Troubleshooting](#troubleshooting)

---

## 🛠️ Development Setup

### Prerequisites
- **OS:** macOS, Linux, or Windows
- **.NET SDK:** 8.0 or later
- **Git:** Latest version
- **Azure CLI:** (Optional, for deployment)
- **VS Code:** (Recommended editor)

### Installation Steps

```bash
# 1. Clone repository
git clone https://github.com/your-org/vitaguard.git
cd "ilk projem"

# 2. Restore NuGet packages
dotnet restore AsistanApp/AsistanApp.csproj

# 3. Build project
dotnet build AsistanApp/AsistanApp.csproj --configuration Debug

# 4. (Optional) Run unit tests
dotnet test AsistanApp.Tests/AsistanApp.Tests.csproj || echo "No tests found"
```

---

## ▶️ Running Locally

### Start Development Server

```bash
cd AsistanApp
dotnet run
```

**Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5007
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to exit.
```

### Access Application

- **Home:** http://localhost:5007
- **Swagger API Docs:** http://localhost:5007/swagger
- **SignalR Hub:** ws://localhost:5007/hubs/health-alerts

---

## 🧪 API Testing

### Using curl

```bash
# 1. Get Health Data
curl -X GET http://localhost:5007/api/health/elderly-1 \
  -H "Authorization: Bearer YOUR_TOKEN"

# 2. Log Health Metric
curl -X POST http://localhost:5007/api/health/log \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "userId": "elderly-1",
    "metricType": "blood_pressure",
    "value": 145,
    "timestamp": "2024-01-22T14:30:00Z"
  }'

# 3. Get Medications
curl -X GET http://localhost:5007/api/medications \
  -H "Authorization: Bearer YOUR_TOKEN"

# 4. Trigger Emergency Alert
curl -X POST http://localhost:5007/api/emergency-alert-with-confirmation \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"alertType": "emergency"}'

# 5. Confirm Emergency
curl -X POST http://localhost:5007/api/emergency-confirmation \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"confirmed": true}'
```

### Using Postman

1. Open [Postman](https://www.postman.com)
2. Import: `.github/resources/vitaguard-postman.json`
3. Set environment: `localhost` or `production`
4. Run requests with pre-configured headers

### Using Swagger UI

1. Open http://localhost:5007/swagger
2. Click "Authorize" button
3. Paste JWT token: `Bearer YOUR_TOKEN`
4. Try out endpoints directly

---

## 🌍 Localization Testing

### Test Turkish (tr-TR)

```bash
curl -X GET http://localhost:5007/api/health/elderly-1 \
  -H "Accept-Language: tr-TR" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Response uses Turkish strings:
# "medication_time": "💊 İLAÇ SAATİ"
# "critical_bp": "🚨 KRİTİK TANSIYÖN UYARISI"
```

### Test English (en-US)

```bash
curl -X GET http://localhost:5007/api/health/elderly-1 \
  -H "Accept-Language: en-US" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Response uses English strings:
# "medication_time": "MEDICATION TIME"
# "critical_bp": "CRITICAL BLOOD PRESSURE ALERT"
```

### Test German (de-DE)

```bash
curl -X GET http://localhost:5007/api/health/elderly-1 \
  -H "Accept-Language: de-DE" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Response uses German strings:
# "medication_time": "MEDIKAMENTENZEIT"
# "critical_bp": "KRITISCHE BLUTDRUCKWARNUNG"
```

---

## 🔔 Real-Time Testing (SignalR)

### Using WebSocket Client

```bash
# Install wscat: npm install -g wscat

# Connect to SignalR hub
wscat -c ws://localhost:5007/hubs/health-alerts

# Connected! Waiting for messages...
# {type: 1, target: "ReceiveCriticalAlert", arguments: [{...}]}
```

### JavaScript Client

```javascript
// Include SignalR library
<script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/signalr.min.js"></script>

<script>
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5007/hubs/health-alerts")
    .withAutomaticReconnect()
    .build();

// Listen for critical alerts
connection.on("ReceiveCriticalAlert", (alert) => {
    console.log("🚨 Critical Alert:", alert);
    console.log("Title:", alert.title);
    console.log("Message:", alert.message);
    console.log("Type:", alert.alertType);
});

// Listen for emergency detection
connection.on("NotifyEmergencyDetected", (emergency) => {
    console.log("📢 Emergency Detected:", emergency);
});

// Start connection
connection.start().catch(err => console.error(err));

// Stop connection when done
// connection.stop();
</script>
```

### Test Emergency Workflow

```bash
# 1. Connect to SignalR
# (See JavaScript client above)

# 2. Trigger emergency alert
curl -X POST http://localhost:5007/api/emergency-alert-with-confirmation \
  -H "Authorization: Bearer YOUR_TOKEN"

# 3. WebSocket receives:
# {
#   "elderlyId": "elderly-1",
#   "alertTitle": "🚨 ACİL DURUM",
#   "alertMessage": "Yardım çağırmamı ister misin?",
#   "alertType": "emergency_pending",
#   "timestamp": "2024-01-22T14:30:00Z"
# }

# 4. Confirm emergency (within 5 seconds)
curl -X POST http://localhost:5007/api/emergency-confirmation \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"confirmed": true}'

# 5. WebSocket receives confirmation:
# {
#   "elderlyId": "elderly-1",
#   "alertTitle": "✅ ACİL DURUM ONAYLANDI",
#   "alertMessage": "Aile uyarılıyor...",
#   "alertType": "emergency_confirmed",
#   "timestamp": "2024-01-22T14:30:02Z"
# }
```

---

## ☁️ Deployment to Azure

### Step 1: Prepare Azure

```bash
# Login to Azure
az login

# Create resource group
az group create --name vitaguard-rg --location eastus

# Create App Service plan
az appservice plan create \
  --name vitaguard-plan \
  --resource-group vitaguard-rg \
  --sku B2 \
  --is-linux

# Create App Service
az webapp create \
  --name vitaguard-production \
  --resource-group vitaguard-rg \
  --plan vitaguard-plan \
  --runtime "DOTNETCORE|8.0"
```

### Step 2: Configure Database

```bash
# Create Azure SQL Database
az sql server create \
  --name vitaguard-sql \
  --resource-group vitaguard-rg \
  --admin-user vitaguard_admin \
  --admin-password "YOUR_SECURE_PASSWORD"

az sql db create \
  --server vitaguard-sql \
  --name VitaGuardDb \
  --resource-group vitaguard-rg \
  --edition Basic
```

### Step 3: Deploy Application

#### Option A: Using GitHub Actions (Recommended)

```bash
# 1. Setup GitHub secrets
# AZURE_CREDENTIALS (from: az ad sp create-for-rbac --...)
# DATABASE_PASSWORD
# JWT_SECRET_KEY

# 2. Push to GitHub
git push origin main

# 3. CI/CD pipeline automatically:
#    - Builds application
#    - Runs security checks
#    - Deploys to staging
#    - Waits for approval
#    - Deploys to production
```

#### Option B: Using Deploy Script

```bash
# Make script executable
chmod +x deploy-production.sh

# Run deployment
./deploy-production.sh production 1.0.0

# Script will:
# 1. Build release
# 2. Create deployment package
# 3. Deploy to Azure
# 4. Verify health
# 5. Setup monitoring
```

#### Option C: Manual Deployment

```bash
# 1. Build release
dotnet publish AsistanApp/AsistanApp.csproj \
  --configuration Release \
  --output ./publish

# 2. Create zip
cd publish
zip -r ../vitaguard.zip .
cd ..

# 3. Deploy to Azure
az webapp deployment source config-zip \
  --name vitaguard-production \
  --resource-group vitaguard-rg \
  --src vitaguard.zip

# 4. Verify
curl https://vitaguard.app/health
```

### Step 4: Configure SSL Certificate

```bash
# Automatically managed by Azure (free HTTPS)
# Or configure Let's Encrypt:

# az webapp config ssl bind \
#   --name vitaguard-production \
#   --resource-group vitaguard-rg \
#   --certificate-file vitaguard.pfx \
#   --certificate-password YOUR_PASSWORD
```

### Step 5: Configure Application Settings

```bash
az webapp config appsettings set \
  --name vitaguard-production \
  --resource-group vitaguard-rg \
  --settings \
  ConnectionStrings__VitaGuardDb="Server=vitaguard-sql.database.windows.net;Database=VitaGuardDb;..." \
  Jwt__SecretKey="YOUR_JWT_SECRET" \
  ASPNETCORE_ENVIRONMENT="Production"
```

---

## 🐛 Troubleshooting

### Issue: "dotnet: command not found"

**Solution:**
```bash
# Install .NET 8.0
# macOS:
brew install dotnet

# Linux:
sudo apt install dotnet-sdk-8.0

# Windows:
choco install dotnet
```

### Issue: "Connection refused" on localhost:5007

**Solution:**
```bash
# Check if port 5007 is already in use
lsof -i :5007

# Kill existing process
kill -9 PID

# Or run on different port
dotnet run --urls=http://localhost:5008
```

### Issue: "SignalR connection failed"

**Solution:**
```bash
# Verify SignalR hub is registered
# Check Program.cs line ~1650:
# app.MapHub<HealthAlertHub>("/hubs/health-alerts");

# Test connection
wscat -c ws://localhost:5007/hubs/health-alerts

# If connection refused, check:
# 1. WebSocket protocol enabled
# 2. Firewall allows port 5007
# 3. CORS configured correctly
```

### Issue: "Localization strings not showing"

**Solution:**
```bash
# Verify JSON files exist:
ls Resources/tr-TR.json Resources/en-US.json Resources/de-DE.json

# Check middleware order in Program.cs:
# app.UseRequestLocalization(locOptions.Value);  <- Must be first

# Test with header:
curl -H "Accept-Language: tr-TR" http://localhost:5007/api/health/test
```

### Issue: "Swagger not accessible"

**Solution:**
```bash
# Swagger only enabled in Development
# Verify ASPNETCORE_ENVIRONMENT:
echo $ASPNETCORE_ENVIRONMENT  # Should be "Development"

# Set if needed:
export ASPNETCORE_ENVIRONMENT=Development

# Test:
curl http://localhost:5007/swagger
```

### Issue: "Build fails with warnings"

**Solution:**
```bash
# Warnings are usually safe (nullability)
# To suppress specific warning:

# In AsistanApp.csproj:
# <PropertyGroup>
#   <NoWarn>$(NoWarn);CS8618</NoWarn>
# </PropertyGroup>

# Or rebuild clean:
rm -rf bin/ obj/
dotnet clean
dotnet build
```

---

## 📊 Performance Optimization

### Local Development
```bash
# Incremental build (faster)
dotnet build

# Watch mode (auto-rebuild)
dotnet watch run

# Profile startup time
dotnet run --configuration Release | head -20
```

### Production Optimization
```bash
# Use Release configuration
dotnet publish --configuration Release

# Enable ReadyToRun
# In AsistanApp.csproj:
# <PropertyGroup>
#   <PublishReadyToRun>true</PublishReadyToRun>
# </PropertyGroup>

# Trim unused code
# <PropertyGroup>
#   <PublishTrimmed>true</PublishTrimmed>
# </PropertyGroup>
```

---

## 📞 Support & Resources

### Documentation Files
- [API_DOCUMENTATION.md](./API_DOCUMENTATION.md) - API reference
- [PRODUCTION_DEPLOYMENT_GUIDE.md](./PRODUCTION_DEPLOYMENT_GUIDE.md) - Deployment steps
- [BRANDING_GUIDE.md](./BRANDING_GUIDE.md) - Brand identity
- [SECURITY_CONFIG.md](./SECURITY_CONFIG.md) - Security best practices
- [IMPLEMENTATION_CHECKLIST.md](./IMPLEMENTATION_CHECKLIST.md) - Feature completeness

### Contact
- **Email:** support@vitaguard.app
- **Issues:** https://github.com/your-org/vitaguard/issues
- **Docs:** https://vitaguard.app/docs

### Next Steps
1. ✅ Run locally: `dotnet run`
2. ✅ Test APIs: Use Swagger or Postman
3. ✅ Test SignalR: Connect via WebSocket
4. ✅ Test localization: Set Accept-Language header
5. ✅ Deploy to Azure: Follow deployment section

---

**Ready to deploy? Follow [PRODUCTION_DEPLOYMENT_GUIDE.md](./PRODUCTION_DEPLOYMENT_GUIDE.md)** 🚀
