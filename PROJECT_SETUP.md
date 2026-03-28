# 🏗️ Project Setup & Technical Reference

**Last Updated:** 28 Mart 2026  
**Version:** 1.0.0

---

## ✅ Prerequisites

### Required Software
```bash
# 1. .NET SDK 10.0 (Nightly builds)
dotnet --version
# Output should be: 10.0.0 or higher

# 2. Node.js (for Capacitor build)
node --version
npm --version

# 3. Git
git --version
```

### System Requirements
| Component | Minimum | Recommended |
|-----------|---------|-------------|
| **RAM** | 4 GB | 8 GB |
| **Disk Space** | 2 GB | 5 GB |
| **OS** | Windows 10+ / macOS 12+ / Linux | Latest LTS |
| **.NET SDK** | 10.0.0 | Latest |

---

## 🚀 Getting Started (5 minutes)

### 1. Clone & Setup
```bash
cd /Users/busenurakdeniz/Desktop/ilk\ projem
dotnet restore
cd AsistanApp
npm install
```

### 2. Run Development Server
```bash
dotnet run
# Server: http://localhost:5007
# Swagger: http://localhost:5007/swagger
```

### 3. Test API
```bash
# Login
curl -X POST http://localhost:5007/api/elderly/login \
  -H "Content-Type: application/json" \
  -d '{"email":"elderly@test.com","password":"123"}'

# Get subscription
curl http://localhost:5007/api/subscription?token=YOUR_TOKEN
```

---

## 📦 Project Structure

```
ilk projem/
├── 📄 ilk projem.csproj (Root configuration)
│   ├── Sdk: Microsoft.NET.Sdk.Web
│   ├── TargetFramework: net10.0
│   └── PackageReferences:
│       ├── SignalR (real-time)
│       ├── Swagger (documentation)
│       ├── JWT (authentication)
│       └── i18n (localization)
│
├── AsistanApp/
│   ├── AsistanApp.csproj (Web API project)
│   ├── Program.cs (1,700+ lines)
│   │   ├── API Endpoints (10+)
│   │   ├── SignalR Hubs
│   │   ├── Background Services
│   │   └── Configuration
│   │
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── appsettings.Production.json
│
├── 📚 Documentation/
│   ├── README.md
│   ├── QUICKSTART.md
│   ├── API_REFERENCE.md
│   └── DEPLOYMENT_READY.md
│
└── 📄 Configuration Files
    ├── package.json (Node.js)
    ├── capacitor.config.json (Mobile)
    └── .github/workflows/ (CI/CD)
```

---

## ⚙️ Configuration Files

### appsettings.json (Base)
```json
{
  "EmergencyResponse": {
    "ConfirmationDelaySeconds": 5,
    "MaxDelaySeconds": 30
  }
}
```

### appsettings.Development.json
- **Used when:** `DOTNET_ENVIRONMENT=Development`
- **Default port:** 5007
- **Emergency delay:** 5 seconds
- **Database:** In-memory
- **Use case:** Local development & testing

### appsettings.Production.json
- **Used when:** `DOTNET_ENVIRONMENT=Production`
- **Port:** 443 (HTTPS)
- **Emergency delay:** 8 seconds
- **Database:** Azure SQL
- **Use case:** Production deployment

### Setting Environment
```bash
# Windows
set DOTNET_ENVIRONMENT=Production

# macOS/Linux
export DOTNET_ENVIRONMENT=Production

# Or in .env file
DOTNET_ENVIRONMENT=Production
ASPNETCORE_URLS=https://0.0.0.0:443
```

---

## 🔧 Common Tasks

### Build
```bash
cd AsistanApp
dotnet build
```

### Run
```bash
dotnet run
```

### Watch Mode (Auto-reload)
```bash
dotnet watch run
```

### Clean Build
```bash
dotnet clean
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Generate API Documentation
```bash
# Swagger UI: http://localhost:5007/swagger
# OpenAPI JSON: http://localhost:5007/swagger/v1/swagger.json
```

---

## 🐳 Docker Build

### Build Docker Image
```bash
docker build -t safeguardian:latest -f Dockerfile .
```

### Run Container
```bash
docker run -p 5007:5007 \
  -e DOTNET_ENVIRONMENT=Production \
  safeguardian:latest
```

### Docker Compose
```bash
docker-compose up -d
```

---

## 🔐 Security Checklist

- [ ] Change JWT secret in `appsettings.Production.json`
- [ ] Enable HTTPS (SSL certificate)
- [ ] Set strong email SMTP credentials
- [ ] Configure SMS Twilio keys
- [ ] Enable CORS only for allowed domains
- [ ] Implement rate limiting
- [ ] Enable audit logging
- [ ] Regular security patches (dotnet sdk latest)

---

## 🚨 Troubleshooting

### "Could not find .NET SDK"
```bash
# Solution: Install .NET 10 SDK
# https://dotnet.microsoft.com/download
```

### Port 5007 Already in Use
```bash
# Find process using port
lsof -i :5007

# Kill process (macOS)
kill -9 <PID>

# Or use different port
dotnet run --urls=http://localhost:5008
```

### SignalR Connection Failed
```bash
# Check WebSocket support
# Ensure firewall allows port 5007
# Verify browser supports WebSockets
```

### "NullReferenceException in Program.cs"
```bash
# Solution: Check appsettings.json configuration
# Ensure all required values are set
```

---

## 📊 Performance Tuning

### Kestrel Server (Production)
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
    options.Limits.MaxRequestBodySize = 30_000_000; // 30 MB
});
```

### SignalR Optimization
```json
"SignalR": {
  "KeepAliveInterval": 15000,
  "ClientTimeoutInterval": 30000,
  "MaximumReceiveMessageSize": 32768
}
```

---

## 📈 Monitoring & Logging

### Application Insights (Azure)
```json
"ApplicationInsights": {
  "InstrumentationKey": "YOUR_KEY"
}
```

### Structured Logging
```bash
# Check logs in appsettings.json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

---

## 🔗 Useful Links

- **Docs:** [README.md](./README.md)
- **API Reference:** [QUICK_REFERENCE.md](./QUICK_REFERENCE.md)
- **Deployment:** [DEPLOYMENT_READY.md](./DEPLOYMENT_READY.md)
- **Testing:** [API_TEST_GUIDE.md](./API_TEST_GUIDE.md)
- **.NET 10 Docs:** [https://learn.microsoft.com/dotnet/](https://learn.microsoft.com/dotnet/)
- **SignalR Guide:** [https://learn.microsoft.com/aspnet/core/signalr/](https://learn.microsoft.com/aspnet/core/signalr/)

---

**Questions?** Check [README.md](./README.md) for comprehensive documentation.
