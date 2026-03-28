using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ilk_projem.Controllers;
using ilk_projem.Data;
using ilk_projem.Hubs;
using ilk_projem.Models;
using ilk_projem.Models.Persistence;
using ilk_projem.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5007");
}

builder.Services.AddScoped<HealthDataService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ISmsSender, SmsSender>();
builder.Services.AddControllers();
builder.Services.AddSignalR();
var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=asistanapp.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnection));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ElderlyUsers (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    Email TEXT NOT NULL,
    PasswordHash TEXT NOT NULL,
    PhoneNumber TEXT NULL,
    BirthDate TEXT NULL,
    BloodType TEXT NULL,
    MedicalHistory TEXT NULL,
    Allergies TEXT NULL,
    DoctorPhone TEXT NULL,
    CreatedAt TEXT NOT NULL,
    IsActive INTEGER NOT NULL,
    Plan TEXT NOT NULL,
    SubscriptionStartDate TEXT NOT NULL,
    SubscriptionExpiresAt TEXT NOT NULL,
    SubscriptionIsActive INTEGER NOT NULL,
    HasAIAnalysis INTEGER NOT NULL,
    HasFallDetection INTEGER NOT NULL,
    HasLiveLocation INTEGER NOT NULL,
    HasEmergencyIntegration INTEGER NOT NULL
);");
    db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_ElderlyUsers_Email ON ElderlyUsers (Email);");
}

// Rate Limiting Middleware
var rateLimitStore = new Dictionary<string, List<long>>();
var rateLimitLock = new object();
var rateLimitingEnabled = builder.Configuration.GetValue<bool?>("RateLimiting:Enabled") ?? true;
var rateLimitRequestsPerMinute = builder.Configuration.GetValue<int?>("RateLimiting:RequestsPerMinute")
    ?? (builder.Environment.IsDevelopment() ? 300 : 100);
var rateLimitWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:WindowSeconds") ?? 60;

rateLimitRequestsPerMinute = Math.Max(1, rateLimitRequestsPerMinute);
rateLimitWindowSeconds = Math.Max(1, rateLimitWindowSeconds);
var rateLimitWindowMs = rateLimitWindowSeconds * 1000L;

if (rateLimitingEnabled)
{
    app.Use(async (ctx, next) =>
    {
        string clientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bool limitExceeded = false;
        int remaining = 0;

        lock (rateLimitLock)
        {
            if (!rateLimitStore.ContainsKey(clientIp))
                rateLimitStore[clientIp] = new List<long>();

            // Remove old requests outside the window
            rateLimitStore[clientIp].RemoveAll(t => t < now - rateLimitWindowMs);

            // Check if limit exceeded
            if (rateLimitStore[clientIp].Count >= rateLimitRequestsPerMinute)
            {
                limitExceeded = true;
                remaining = 0;
            }
            else
            {
                remaining = rateLimitRequestsPerMinute - rateLimitStore[clientIp].Count;
                rateLimitStore[clientIp].Add(now);
            }
        }

        if (limitExceeded)
        {
            ctx.Response.StatusCode = 429; // Too Many Requests
            ctx.Response.Headers.Append("Retry-After", rateLimitWindowSeconds.ToString());
            ctx.Response.Headers.Append("X-RateLimit-Limit", rateLimitRequestsPerMinute.ToString());
            ctx.Response.Headers.Append("X-RateLimit-Remaining", "0");
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                error = $"Rate limit exceeded. Max {rateLimitRequestsPerMinute} requests per {rateLimitWindowSeconds} seconds."
            });
            return;
        }

        ctx.Response.Headers.Append("X-RateLimit-Limit", rateLimitRequestsPerMinute.ToString());
        ctx.Response.Headers.Append("X-RateLimit-Remaining", remaining.ToString());

        await next();
    });
}

app.UseRouting();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCors("AllowAll");
app.UseStaticFiles();
app.MapControllers();

string ResolveToken(HttpContext ctx, JsonElement? body = null)
{
    var authHeader = ctx.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        var bearerToken = authHeader.Substring("Bearer ".Length).Trim();
        if (!string.IsNullOrWhiteSpace(bearerToken)) return bearerToken;
    }

    if (body.HasValue && body.Value.TryGetProperty("token", out var t))
    {
        var fromBody = t.GetString() ?? "";
        if (!string.IsNullOrWhiteSpace(fromBody)) return fromBody;
    }

    return ctx.Request.Query["token"].ToString();
}

app.MapGet("/", async (HttpContext ctx) =>
{
    ctx.Response.ContentType = "text/html; charset=utf-8";
    var filePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "index.html");
    return Results.File(System.IO.File.ReadAllBytes(filePath), "text/html");
});

app.MapGet("/family", async (HttpContext ctx) =>
{
    ctx.Response.ContentType = "text/html; charset=utf-8";
    var filePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "family-dashboard.html");
    return Results.File(System.IO.File.ReadAllBytes(filePath), "text/html");
});

app.MapFallback(async (HttpContext ctx) =>
{
    ctx.Response.ContentType = "text/html; charset=utf-8";
    var filePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "index.html");
    return Results.File(System.IO.File.ReadAllBytes(filePath), "text/html");
});

// Controller-style endpoint grouping
app.MapStateEndpoints();

// APIs
app.MapPost("/api/complete-task", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string taskId = json.GetProperty("taskId").GetString() ?? "unknown";
        string method = json.TryGetProperty("method", out var m) ? m.GetString() ?? "button" : "button";
        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        svc.CompleteTask(taskId, method);
        await hub.Clients.All.SendAsync("ReceiveTaskUpdate", new { elderlyId, taskId, status = "completed" });
        return Results.Json(new { success = true });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

app.MapPost("/api/health-data", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        string metricType = json.GetProperty("metricType").GetString() ?? "unknown";
        double value = json.GetProperty("value").GetDouble();
        svc.AddHealthRecord(elderlyId, metricType, value);
        await hub.Clients.All.SendAsync("ReceiveHealthUpdate", new { elderlyId, metricType, value });
        return Results.Json(new { success = true });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

app.MapPost("/api/emergency-alert", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub, ISmsSender smsSender) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        string alertType = json.TryGetProperty("alertType", out var a) ? a.GetString() ?? "emergency_call" : "emergency_call";
        string location = json.TryGetProperty("location", out var l) ? l.GetString() ?? "Unknown" : "Unknown";
        svc.AddEmergencyAlert(elderlyId, alertType, "Acil durum!");
        await hub.Clients.All.SendAsync("ReceiveEmergencyAlert", new { elderlyId, alertType });

        var elderly = svc.GetUser(elderlyId);
        var elderName = elderly?.Name ?? "Yaşlı kullanıcı";
        var message = $"Acil yardım çağrısı: {elderName}. Konum: {location}";

        var familyNumbers = svc.GetFamilyMembers(elderlyId)
            .Select(m => m.PhoneNumber)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var phone in familyNumbers)
        {
            await smsSender.SendAsync(phone, message);
        }

        var emergencyNumber = !string.IsNullOrWhiteSpace(elderly?.DoctorPhone)
            ? elderly!.DoctorPhone
            : smsSender.EmergencyServicesNumber;

        if (!string.IsNullOrWhiteSpace(emergencyNumber))
        {
            await smsSender.SendAsync(emergencyNumber, message);
        }

        return Results.Json(new { success = true });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

// AI EMERGENCY PROTOCOL ENDPOINTS
app.MapPost("/api/ai-health-check", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        
        // Health threshold check (Critical: >180/110 for BP, >180 for glucose, <50 or >120 for heart rate)
        string healthStatus = json.TryGetProperty("healthStatus", out var h) ? h.GetString() ?? "normal" : "normal";
        
        if (healthStatus == "critical")
        {
            var alert = new { elderlyId, type = "health_critical", timestamp = DateTime.Now, requiresVoiceCheck = true };
            await hub.Clients.All.SendAsync("ReceiveAICritical", alert);
            svc.AddEmergencyAlert(elderlyId, "health_critical", "Kritik sağlık verisi");
        }
        
        return Results.Json(new { success = true, requiresVoiceCheck = healthStatus == "critical" });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

app.MapPost("/api/ai-fall-detection", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        double accelerationMagnitude = json.GetProperty("accelerationMagnitude").GetDouble();
        
        // Fall detection threshold: sudden acceleration > 25 m/s²
        if (accelerationMagnitude > 25)
        {
            var alert = new { elderlyId, type = "fall_detected", timestamp = DateTime.Now, requiresVoiceCheck = true, accelerationMagnitude };
            await hub.Clients.All.SendAsync("ReceiveAICritical", alert);
            await hub.Clients.Group("family:all").SendAsync("ReceiveFallDetected", new
            {
                elderlyId,
                accelerationMagnitude,
                title = "Düşme Algılandı",
                timestamp = DateTime.Now
            });
            var familyAlert = new
            {
                elderlyId,
                type = "family_fall_alert",
                title = "Düşme algılandı",
                message = $"{elderlyId} için düşme riski tespit edildi ({accelerationMagnitude:F2} m/s²)",
                timestamp = DateTime.Now,
                severity = "critical"
            };
            await hub.Clients.Group("family:all").SendAsync("ReceiveFamilyAlert", familyAlert);
            await hub.Clients.Group($"family:{elderlyId}").SendAsync("ReceiveFamilyAlert", familyAlert);
            svc.AddEmergencyAlert(elderlyId, "fall_detected", $"Düşme algılandı: {accelerationMagnitude:F2} m/s²");
            
            return Results.Json(new { success = true, fallDetected = true, initiateVoiceCheck = true, timeoutSeconds = 15 });
        }
        
        return Results.Json(new { success = true, fallDetected = false });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

app.MapPost("/api/family-alert", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;

        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        string title = json.TryGetProperty("title", out var t) ? t.GetString() ?? "Aile Uyarısı" : "Aile Uyarısı";
        string message = json.TryGetProperty("message", out var m) ? m.GetString() ?? "Yeni uyarı var" : "Yeni uyarı var";
        string severity = json.TryGetProperty("severity", out var s) ? s.GetString() ?? "warning" : "warning";

        var alert = new
        {
            elderlyId,
            title,
            message,
            severity,
            timestamp = DateTime.Now
        };

        await hub.Clients.Group("family:all").SendAsync("ReceiveFamilyAlert", alert);
        await hub.Clients.Group($"family:{elderlyId}").SendAsync("ReceiveFamilyAlert", alert);
        svc.AddEmergencyAlert(elderlyId, "family_alert", message);

        return Results.Json(new { success = true });
    }
    catch
    {
        return Results.Json(new { success = false }, statusCode: 500);
    }
});

app.MapPost("/api/ai-voice-check", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        string voiceInput = json.TryGetProperty("voiceInput", out var v) ? v.GetString() ?? "" : "";
        double emotionScore = json.TryGetProperty("emotionScore", out var o) ? o.GetDouble() : 0.5;
        
        // Voice analysis: positive response ("okay", "fine", "good")
        bool positiveResponse = voiceInput.ToLower().Contains("iyiyim") || voiceInput.ToLower().Contains("tamam") || 
                                voiceInput.ToLower().Contains("alright") || voiceInput.ToLower().Contains("fine") ||
                                voiceInput.ToLower().Contains("okay") || voiceInput.ToLower().Contains("okay");
        
        // Emotion analysis: if score < 0.3 (panic/distress), escalate even with positive words
        bool isInDistress = emotionScore < 0.3;
        
        if (!positiveResponse || isInDistress)
        {
            // Voice check failed - initiate emergency protocol
            var emergencyData = new 
            { 
                elderlyId, 
                type = "emergency_escalation", 
                reason = positiveResponse ? "distress_detected" : "timeout",
                timestamp = DateTime.Now,
                location = "pending",
                healthData = "pending"
            };
            
            await hub.Clients.All.SendAsync("ReceiveEmergencyEscalation", emergencyData);
            svc.AddEmergencyAlert(elderlyId, "emergency_escalation", "Acil durum protokolü başlatıldı - konuşma kontrolü başarısız");
            
            return Results.Json(new { success = true, emergencyEscalated = true, nextStep = "broadcast_emergency" });
        }
        
        // Positive response received - cancel alert
        await hub.Clients.All.SendAsync("ReceiveAlertCancelled", new { elderlyId, reason = "positive_voice_response" });
        
        return Results.Json(new { success = true, emergencyEscalated = false, message = "OK confirmed by voice" });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

app.MapPost("/api/ai-silence-monitor", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        int silenceDurationSeconds = json.GetProperty("silenceDurationSeconds").GetInt32();
        
        // Silence timeout: 15 seconds
        if (silenceDurationSeconds >= 15)
        {
            var emergencyData = new 
            { 
                elderlyId, 
                type = "silence_timeout", 
                timestamp = DateTime.Now,
                silenceDuration = silenceDurationSeconds
            };
            
            await hub.Clients.All.SendAsync("ReceiveEmergencyEscalation", emergencyData);
            svc.AddEmergencyAlert(elderlyId, "silence_timeout", $"15 saniye sessizlik - Acil durum tetiklendi");
            
            return Results.Json(new { success = true, emergencyEscalated = true });
        }
        
        return Results.Json(new { success = true, emergencyEscalated = false });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

app.MapPost("/api/emergency-broadcast", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub, ISmsSender smsSender) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        
        var broadcastData = new 
        {
            elderlyId,
            timestamp = DateTime.Now,
            status = "CRITICAL",
            location = json.TryGetProperty("location", out var l) ? l.GetString() : "Unknown",
            healthData = new {
                bloodPressure = json.TryGetProperty("bloodPressure", out var bp) ? bp.GetString() : "N/A",
                heartRate = json.TryGetProperty("heartRate", out var hr) ? hr.GetInt32() : 0,
                temperature = json.TryGetProperty("temperature", out var t) ? t.GetDouble() : 0
            },
            audioFile = json.TryGetProperty("audioFile", out var a) ? a.GetString() : null,
            emergencyContact = svc.GetFamilyMembers(elderlyId)
        };
        
        // Broadcast to all family members
        await hub.Clients.All.SendAsync("ReceiveEmergencyBroadcast", broadcastData);
        svc.AddEmergencyAlert(elderlyId, "emergency_broadcast", "Acil durum - Aile ve sağlık kuruluşları bilgilendirildi");

        var elderly = svc.GetUser(elderlyId);
        var elderName = elderly?.Name ?? "Yaşlı kullanıcı";
        var location = broadcastData.location ?? "Unknown";
        var message = $"ACİL DURUM: {elderName}. Konum: {location}";

        var familyNumbers = svc.GetFamilyMembers(elderlyId)
            .Select(m => m.PhoneNumber)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var phone in familyNumbers)
        {
            await smsSender.SendAsync(phone, message);
        }

        var emergencyNumber = !string.IsNullOrWhiteSpace(elderly?.DoctorPhone)
            ? elderly!.DoctorPhone
            : smsSender.EmergencyServicesNumber;

        if (!string.IsNullOrWhiteSpace(emergencyNumber))
        {
            await smsSender.SendAsync(emergencyNumber, message);
        }
        
        return Results.Json(new { success = true, broadcastSent = true, contacts = broadcastData.emergencyContact.Count });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

// Device & Subscription APIs
app.MapPost("/api/device-register", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string deviceId = json.GetProperty("deviceId").GetString() ?? Guid.NewGuid().ToString();
        string elderlyId = json.GetProperty("elderlyId").GetString() ?? "elderly-001";
        
        // UUID-based auto-login
        return Results.Json(new { success = true, deviceId, autoLoginToken = $"token_{deviceId}_{DateTime.Now.Ticks}" });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

// ELDERLY SELF-ENROLLMENT ENDPOINT (Yaşlıların bağımsız kaydolması)
app.MapPost("/api/elderly-self-enroll", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        
        // Extract enrollment data
        string deviceId = json.TryGetProperty("deviceId", out var d) ? d.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
        string fullName = json.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "Adı Bilinmeyen" : "Adı Bilinmeyen";
        string phone = json.TryGetProperty("phone", out var p) ? p.GetString() ?? "" : "";
        string email = json.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
        string birthDate = json.TryGetProperty("birthDate", out var bd) ? bd.GetString() ?? "" : "";
        string bloodType = json.TryGetProperty("bloodType", out var bt) ? bt.GetString() ?? "" : "";
        string medicalConditions = json.TryGetProperty("medicalConditions", out var mc) ? mc.GetString() ?? "" : "";
        string allergies = json.TryGetProperty("allergies", out var a) ? a.GetString() ?? "" : "";
        string doctorPhone = json.TryGetProperty("doctorPhone", out var dp) ? dp.GetString() ?? "" : "";
        string plan = json.TryGetProperty("plan", out var pl) ? pl.GetString() ?? "standard" : "standard";
        string requestedPassword = json.TryGetProperty("password", out var pw) ? pw.GetString() ?? "" : "";
        string finalPassword = requestedPassword;
        bool isTempPassword = false;
        if (string.IsNullOrWhiteSpace(finalPassword))
        {
            finalPassword = new Random().Next(100000, 999999).ToString();
            isTempPassword = true;
        }
        
        // Validate required fields
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(birthDate))
        {
            return Results.Json(new { success = false, message = "Zorunlu alanlar eksik (Ad, Telefon, Doğum Tarihi)" }, statusCode: 400);
        }
        
        // Phone format validation (Turkish format)
        var phoneRegex = new System.Text.RegularExpressions.Regex(@"^(\+90|0)?[1-9]\d{9}$");
        if (!phoneRegex.IsMatch(phone.Replace(" ", "").Replace("-", "")))
        {
            return Results.Json(new { success = false, message = "Geçersiz telefon numarası formatı" }, statusCode: 400);
        }
        
        // Create elderly user record
        var newElderly = new ElderlyUser
        {
            Id = deviceId,
            Name = fullName,
            Email = email,
            Password = string.Empty,
            PasswordHash = PasswordSecurity.HashPassword(finalPassword),
            PhoneNumber = phone,
            BirthDate = birthDate,
            BloodType = bloodType,
            MedicalHistory = medicalConditions,
            Allergies = allergies,
            DoctorPhone = doctorPhone,
            Subscription = new Subscription
            {
                UserId = deviceId,
                Plan = plan,
                StartDate = DateTime.Now,
                ExpiresAt = plan == "premium" ? DateTime.Now.AddMonths(1) : DateTime.Now.AddYears(1),
                IsActive = true,
                HasAIAnalysis = plan == "premium",
                HasFallDetection = plan == "premium",
                HasLiveLocation = plan == "premium",
                HasEmergencyIntegration = true
            },
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        
        // Add to system
        svc.AddUser(newElderly);
        
        return Results.Json(new 
        { 
            success = true, 
            message = "Kaydolma başarılı! Hoş geldiniz.",
            deviceId = deviceId,
            autoLoginToken = $"token_{deviceId}_{DateTime.Now.Ticks}",
            tempPassword = isTempPassword ? finalPassword : null,
            user = new 
            {
                id = newElderly.Id,
                name = newElderly.Name,
                plan = newElderly.Subscription.Plan,
                features = new
                {
                    aiVoiceAnalysis = newElderly.Subscription.HasAIAnalysis,
                    fallDetection = newElderly.Subscription.HasFallDetection,
                    liveLocation = newElderly.Subscription.HasLiveLocation,
                    emergencyIntegration = newElderly.Subscription.HasEmergencyIntegration
                }
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = $"Kaydolma hatası: {ex.Message}" }, statusCode: 500);
    }
});

app.MapGet("/api/subscription/{userId}", (string userId, HealthDataService svc) =>
{
    // Check subscription status
    var user = svc.GetUser(userId);
    
    if (user == null)
    {
        return Results.Json(new { success = false, message = "Kullanıcı bulunamadı" }, statusCode: 404);
    }
    
    var subscription = new 
    {
        userId,
        plan = user.Subscription?.Plan ?? "standard",
        isPremium = user.Subscription?.Plan == "premium",
        expiresAt = user.Subscription?.ExpiresAt ?? DateTime.Now.AddYears(1),
        isActive = user.Subscription?.IsActive ?? false,
        features = new 
        {
            aiVoiceAnalysis = user.Subscription?.HasAIAnalysis ?? false,
            fallDetection = user.Subscription?.HasFallDetection ?? false,
            liveLocation = user.Subscription?.HasLiveLocation ?? false,
            emergencyIntegration = user.Subscription?.HasEmergencyIntegration ?? true
        }
    };
    return Results.Json(new { success = true, subscription });
});

// HEALTH STATISTICS ENDPOINTS
// Sağlık verisi kaydet (kan şekeri, tansiyon, nabız vb.)
string EvaluateHealthStatus(int? systolic, int? diastolic, int? glucose, int? heartRate)
{
    bool critical =
        (systolic.HasValue && systolic.Value > 180) ||
        (diastolic.HasValue && diastolic.Value > 110) ||
        (glucose.HasValue && glucose.Value > 180) ||
        (heartRate.HasValue && (heartRate.Value < 50 || heartRate.Value > 120));

    if (critical) return "critical";

    bool warning =
        (systolic.HasValue && systolic.Value > 140) ||
        (diastolic.HasValue && diastolic.Value > 90) ||
        (glucose.HasValue && glucose.Value > 140) ||
        (heartRate.HasValue && (heartRate.Value < 60 || heartRate.Value > 100));

    return warning ? "warning" : "normal";
}

app.MapPost("/api/health/classify", async (HttpContext ctx) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;

        int? systolic = json.TryGetProperty("systolic", out var s) ? (int?)s.GetInt32() : null;
        int? diastolic = json.TryGetProperty("diastolic", out var di) ? (int?)di.GetInt32() : null;
        int? glucose = json.TryGetProperty("glucose", out var g) ? (int?)g.GetInt32() : null;
        int? heartRate = json.TryGetProperty("heartRate", out var hr) ? (int?)hr.GetInt32() : null;

        var healthStatus = EvaluateHealthStatus(systolic, diastolic, glucose, heartRate);
        return Results.Json(new { success = true, healthStatus });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = $"Hata: {ex.Message}" }, statusCode: 400);
    }
});

app.MapPost("/api/health-stats/add", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        
        string deviceId = json.TryGetProperty("deviceId", out var d) ? d.GetString() ?? "elderly-001" : "elderly-001";
        int? systolic = json.TryGetProperty("systolic", out var s) ? (int?)s.GetInt32() : null;
        int? diastolic = json.TryGetProperty("diastolic", out var di) ? (int?)di.GetInt32() : null;
        int? glucose = json.TryGetProperty("glucose", out var g) ? (int?)g.GetInt32() : null;
        int? heartRate = json.TryGetProperty("heartRate", out var hr) ? (int?)hr.GetInt32() : null;
        string notes = json.TryGetProperty("notes", out var n) ? n.GetString() ?? "" : "";
        
        // Determine health status based on readings
        string healthStatus = EvaluateHealthStatus(systolic, diastolic, glucose, heartRate);
        
        // Add health record
        svc.AddComprehensiveHealthRecord(deviceId, systolic, diastolic, glucose, heartRate, healthStatus, notes);
        
        return Results.Json(new 
        { 
            success = true, 
            message = "Sağlık verisi kaydedildi",
            healthStatus = healthStatus,
            timestamp = DateTime.Now
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = $"Hata: {ex.Message}" }, statusCode: 500);
    }
});

// Sağlık geçmişi getir (son N gün)
app.MapGet("/api/health-stats/{deviceId}", (string deviceId, HealthDataService svc, int days = 7) =>
{
    try
    {
        var healthRecords = svc.GetHealthRecords(deviceId, days);
        
        if (healthRecords == null || healthRecords.Count == 0)
        {
            return Results.Json(new { success = true, message = "Veri bulunamadı", data = new List<object>() });
        }
        
        // Format response
        var formatted = healthRecords.Select(r => new 
        {
            timestamp = r.Timestamp,
            systolic = r.Systolic,
            diastolic = r.Diastolic,
            glucose = r.Glucose,
            heartRate = r.HeartRate,
            status = r.HealthStatus,
            notes = r.Notes
        }).ToList();
        
        return Results.Json(new { success = true, data = formatted, count = formatted.Count });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = $"Hata: {ex.Message}" }, statusCode: 500);
    }
});

// Sağlık istatistikleri özetini getir (ortalamalar, trend)
app.MapGet("/api/health-stats/summary/{deviceId}", (string deviceId, HealthDataService svc, int days = 7) =>
{
    try
    {
        var healthRecords = svc.GetHealthRecords(deviceId, days);
        
        if (healthRecords == null || healthRecords.Count == 0)
        {
            return Results.Json(new { success = true, message = "Veri bulunamadı", summary = new { } });
        }
        
        // Calculate averages
        double avgSystolic = healthRecords.Where(r => r.Systolic.HasValue).Average(r => r.Systolic!.Value);
        double avgDiastolic = healthRecords.Where(r => r.Diastolic.HasValue).Average(r => r.Diastolic!.Value);
        double avgGlucose = healthRecords.Where(r => r.Glucose.HasValue).Average(r => r.Glucose!.Value);
        double avgHeartRate = healthRecords.Where(r => r.HeartRate.HasValue).Average(r => r.HeartRate!.Value);
        
        // Get trends (compare first half with second half)
        int midPoint = healthRecords.Count / 2;
        var firstHalf = healthRecords.Take(midPoint).ToList();
        var secondHalf = healthRecords.Skip(midPoint).ToList();
        
        double firstAvgSystolic = firstHalf.Where(r => r.Systolic.HasValue).Average(r => r.Systolic!.Value);
        double secondAvgSystolic = secondHalf.Where(r => r.Systolic.HasValue).Average(r => r.Systolic!.Value);
        string systolicTrend = secondAvgSystolic < firstAvgSystolic ? "decreasing" : secondAvgSystolic > firstAvgSystolic ? "increasing" : "stable";
        
        double firstAvgGlucose = firstHalf.Where(r => r.Glucose.HasValue).Average(r => r.Glucose!.Value);
        double secondAvgGlucose = secondHalf.Where(r => r.Glucose.HasValue).Average(r => r.Glucose!.Value);
        string glucoseTrend = secondAvgGlucose < firstAvgGlucose ? "decreasing" : secondAvgGlucose > firstAvgGlucose ? "increasing" : "stable";
        
        var summary = new 
        {
            period = $"Son {days} gün",
            recordCount = healthRecords.Count,
            averages = new 
            {
                systolic = Math.Round(avgSystolic, 1),
                diastolic = Math.Round(avgDiastolic, 1),
                glucose = Math.Round(avgGlucose, 1),
                heartRate = Math.Round(avgHeartRate, 1)
            },
            trends = new 
            {
                systolic = systolicTrend,
                glucose = glucoseTrend
            },
            criticalRecords = healthRecords.Count(r => r.HealthStatus == "critical"),
            warningRecords = healthRecords.Count(r => r.HealthStatus == "warning")
        };
        
        return Results.Json(new { success = true, summary });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = $"Hata: {ex.Message}" }, statusCode: 500);
    }
});

app.MapGet("/api/i18n/{language}", (string language) =>
{
    var validLanguages = new[] { "en", "tr", "de" };
    if (!validLanguages.Contains(language)) language = "en";
    
    return Results.File(
        System.IO.File.ReadAllBytes(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), $"wwwroot/i18n/{language}.json")
        ),
        "application/json; charset=utf-8"
    );
});

app.MapPost("/api/debug-logs", (HttpContext ctx) => Results.Json(new { success = true }));

app.MapPost("/api/family/login", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;

        string email = json.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
        string password = json.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";

        var result = svc.AuthenticateFamily(email, password);
        if (result == null)
        {
            return Results.Json(new { success = false, message = "Geçersiz giriş" }, statusCode: 401);
        }

        return Results.Json(new
        {
            success = true,
            token = result.Value.Token,
            recipient = result.Value.Member.Email,
            memberName = result.Value.Member.Name,
            caringFor = result.Value.Member.ElderlyId
        });
    }
    catch
    {
        return Results.Json(new { success = false }, statusCode: 500);
    }
});

app.MapGet("/api/family/dashboard/{elderlyId}", (string elderlyId, string token, HealthDataService svc) =>
{
    var member = svc.GetFamilySession(token);
    if (member == null)
    {
        return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
    }

    if (!string.Equals(member.ElderlyId, elderlyId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(new { success = false, message = "Erişim reddedildi" }, statusCode: 403);
    }

    var elderly = svc.GetUser(elderlyId);
    if (elderly == null)
    {
        return Results.Json(new { success = false, message = "Kullanıcı bulunamadı" }, statusCode: 404);
    }

    int age = 0;
    if (!string.IsNullOrWhiteSpace(elderly.BirthDate) &&
        DateTime.TryParse(elderly.BirthDate, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out var birthDate))
    {
        var today = DateTime.Today;
        age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
    }
    if (age <= 0) age = 75;

    var todayDate = DateTime.Today;
    var meds = svc.GetMedications(elderly.Id)
        .Select(m => new
        {
            medicationName = m.Name,
            scheduleTimes = m.ScheduleTimes ?? new List<string>(),
            notes = m.Notes ?? "",
            takenToday = (m.LastTakenAt.HasValue && m.LastTakenAt.Value.Date == todayDate)
                ? new[] { m.LastTakenAt.Value }
                : Array.Empty<DateTime>()
        })
        .ToList();

    return Results.Json(new
    {
        success = true,
        elderly = new
        {
            id = elderly.Id,
            name = elderly.Name,
            age,
            phone = string.IsNullOrWhiteSpace(elderly.PhoneNumber) ? "-" : elderly.PhoneNumber
        },
        todayMedications = meds,
        recentNotifications = Array.Empty<object>()
    });
});

// ELDERLY LOGIN ENDPOINT (Yaşlı giriş)
app.MapPost("/api/elderly/login", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;

        string email = json.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
        string password = json.TryGetProperty("password", out var pw) ? pw.GetString() ?? "" : "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Results.Json(new { success = false, message = "E-posta ve şifre zorunludur" }, statusCode: 400);
        }

        var result = svc.AuthenticateElderly(email, password);
        if (result == null)
        {
            return Results.Json(new { success = false, message = "Geçersiz e-posta veya şifre" }, statusCode: 401);
        }

        return Results.Json(new
        {
            success = true,
            token = result.Value.Token,
            userId = result.Value.User.Id,
            name = result.Value.User.Name
        });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

// ELDERLY RESET PASSWORD ENDPOINT (Şifre sıfırlama)
app.MapPost("/api/elderly/reset-password", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;

        string email = json.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
        var result = svc.ResetElderlyPassword(email);
        if (!result.Success)
        {
            return Results.Json(new { success = false, message = result.Message }, statusCode: 400);
        }

        return Results.Json(new
        {
            success = true,
            message = result.Message
        });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
});

// DATA DELETION ENDPOINT (App Store compliance)
app.MapDelete("/api/elderly/account", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        var json = await JsonDocument.ParseAsync(ctx.Request.Body);
        var password = json.RootElement.TryGetProperty("password", out var pw)
            ? pw.GetString() ?? ""
            : "";

        var token = ResolveToken(ctx, json.RootElement);
        var result = svc.DeleteElderlyAccount(token, password);
        if (!result.Success)
        {
            return Results.Json(new { success = false, message = result.Message }, statusCode: 400);
        }

        return Results.Json(new { success = true, message = "Hesap ve ilişkili veriler kalıcı olarak silindi" });
    }
    catch
    {
        return Results.Json(new { success = false, message = "Hesap silme işlemi başarısız" }, statusCode: 500);
    }
});

// SIGN IN WITH APPLE ENDPOINT
app.MapPost("/api/auth/apple-signin", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        var json = await JsonDocument.ParseAsync(ctx.Request.Body);
        var identityToken = json.RootElement.TryGetProperty("identityToken", out var it)
            ? it.GetString() ?? ""
            : "";
        var userId = json.RootElement.TryGetProperty("userId", out var uid)
            ? uid.GetString() ?? ""
            : "";
        var email = json.RootElement.TryGetProperty("email", out var em)
            ? em.GetString() ?? userId
            : userId;
        var fullName = json.RootElement.TryGetProperty("fullName", out var fn)
            ? fn.GetString() ?? "Apple User"
            : "Apple User";

        var (valid, appleUserId, _) = AppleAuthService.VerifyAppleToken(identityToken, userId);
        if (!valid)
        {
            return Results.Json(new { success = false, message = "Apple token doğrulması başarısız" }, statusCode: 401);
        }

        var existingUser = svc.GetUser(userId) ?? svc.GetUserByEmail(email);
        if (existingUser != null)
        {
            var token = $"elderly_{existingUser.Id}_{Guid.NewGuid():N}";
            svc.SetElderlySession(token, existingUser);
            return Results.Json(new
            {
                success = true,
                token,
                userId = existingUser.Id,
                name = existingUser.Name,
                message = "Apple ile başarıyla giriş yapıldı"
            });
        }

        var newUser = new ElderlyUser
        {
            Id = userId,
            Name = fullName,
            Email = email,
            Password = string.Empty,
            PasswordHash = PasswordSecurity.HashPassword(Guid.NewGuid().ToString()),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Subscription = new Subscription
            {
                UserId = userId,
                Plan = "standard",
                StartDate = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                HasEmergencyIntegration = true
            }
        };

        svc.AddUser(newUser);
        var newToken = $"elderly_{newUser.Id}_{Guid.NewGuid():N}";
        svc.SetElderlySession(newToken, newUser);

        return Results.Json(new
        {
            success = true,
            token = newToken,
            userId = newUser.Id,
            name = newUser.Name,
            isNewUser = true,
            message = "Apple ile başarıyla kaydolundu"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = $"Apple giriş hatası: {ex.Message}" }, statusCode: 500);
    }
});

// HEALTHKIT DATA EXPORT ENDPOINT
app.MapGet("/api/health/export-healthkit", (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        var token = ResolveToken(ctx);
        var elderly = svc.GetElderlySession(token);
        if (elderly == null)
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        var records = svc.GetHealthRecords(elderly.Id, 30);
        var healthKitExport = records.Select(r => HealthKitService.FormatForHealthKit(
            elderly.Id,
            r.MetricType,
            r.Value,
            r.Systolic,
            r.Diastolic
        )).ToList();

        return Results.Json(new
        {
            success = true,
            exportDate = DateTime.UtcNow.ToString("O"),
            userId = elderly.Id,
            recordCount = healthKitExport.Count,
            format = "HealthKit-Compatible",
            samples = healthKitExport
        });
    }
    catch
    {
        return Results.Json(new { success = false, message = "HealthKit export başarısız" }, statusCode: 500);
    }
});

app.MapGet("/api/family/me", (string token, HealthDataService svc) =>
{
    var member = svc.GetFamilySession(token);
    if (member == null)
    {
        return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
    }

    return Results.Json(new
    {
        success = true,
        recipient = member.Email,
        memberName = member.Name,
        caringFor = member.ElderlyId
    });
});

app.MapPost("/api/family/contact", (string token, HealthDataService svc) =>
{
    var member = svc.GetFamilySession(token);
    if (member == null)
    {
        return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
    }

    svc.RecordFamilyContact(member.ElderlyId);
    return Results.Json(new { success = true, recordedAt = DateTime.Now });
});

app.MapGet("/api/family/last-contact", (string token, HealthDataService svc) =>
{
    var elderly = svc.GetElderlySession(token);
    if (elderly == null)
    {
        return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
    }

    var last = svc.GetLastFamilyContact(elderly.Id);
    var hours = last.HasValue ? Math.Round((DateTime.Now - last.Value).TotalHours, 1) : (double?)null;
    return Results.Json(new { success = true, lastContact = last, hoursSince = hours });
});

app.MapPost("/api/family/logout", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string token = json.TryGetProperty("token", out var t) ? t.GetString() ?? "" : "";

        var member = svc.GetFamilySession(token);
        if (member == null)
        {
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
        }

        var removed = svc.RemoveFamilySession(token);
        return Results.Json(new { success = removed });
    }
    catch
    {
        return Results.Json(new { success = false }, statusCode: 500);
    }
});

app.MapPost("/api/send-notification", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub, ISmsSender smsSender) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;

        string type = json.TryGetProperty("type", out var t) ? t.GetString() ?? "info" : "info";
        string message = json.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
        string severity = json.TryGetProperty("severity", out var s) ? s.GetString() ?? "normal" : "normal";
        var location = json.TryGetProperty("location", out var l) ? l.GetString() ?? "Unknown" : "Unknown";

        var token = ResolveToken(ctx, json);
        var elderly = svc.GetElderlySession(token);
        if (elderly == null)
        {
            return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
        }

        var payload = new
        {
            type,
            message,
            severity,
            location,
            timestamp = DateTime.Now
        };

        await hub.Clients.All.SendAsync("ReceiveNotification", payload);

        var elderName = elderly.Name ?? "Yaşlı kullanıcı";
        var smsMessage = string.IsNullOrWhiteSpace(location)
            ? $"{elderName}: {message}"
            : $"{elderName}: {message}. Konum: {location}";

        var familyNumbers = svc.GetFamilyMembers(elderly.Id)
            .Select(m => m.PhoneNumber)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var phone in familyNumbers)
        {
            await smsSender.SendAsync(phone, smsMessage);
        }

        var isHigh = string.Equals(severity, "high", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("emergency", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("critical", StringComparison.OrdinalIgnoreCase);

        if (isHigh)
        {
            var emergencyNumber = !string.IsNullOrWhiteSpace(elderly.DoctorPhone)
                ? elderly.DoctorPhone
                : smsSender.EmergencyServicesNumber;

            if (!string.IsNullOrWhiteSpace(emergencyNumber))
            {
                await smsSender.SendAsync(emergencyNumber, smsMessage);
            }
        }

        return Results.Json(new { success = true });
    }
    catch
    {
        return Results.Json(new { success = false }, statusCode: 500);
    }
});

app.MapGet("/api/pending-tasks/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, count = svc.GetPendingTasks(userId).Count }));
app.MapGet("/api/elderly-status/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, status = new { name = svc.GetUser(userId)?.Name, location = "Evde", pendingTasks = svc.GetPendingTasks(userId).Count } }));
app.MapGet("/api/task-history/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, tasks = svc.GetTaskHistory(userId) }));
app.MapGet("/api/health-analytics/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, analytics = new { } }));
app.MapGet("/api/family-members/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, members = svc.GetFamilyMembers(userId) }));
app.MapGet("/api/subscription", (HttpContext ctx, HealthDataService svc) =>
{
    var token = ResolveToken(ctx);
    var elderly = svc.GetElderlySession(token);
    if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
    var sub = elderly.Subscription ?? new Subscription { UserId = elderly.Id, Plan = "standard", IsActive = false };
    return Results.Json(new { plan = sub.Plan, isActive = sub.IsActive, userId = elderly.Id });
});
app.MapGet("/api/family-members", (HttpContext ctx, HealthDataService svc) =>
{
    var token = ResolveToken(ctx);
    var elderly = svc.GetElderlySession(token);
    if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
    return Results.Json(new { success = true, members = svc.GetFamilyMembers(elderly.Id) });
});
app.MapPost("/api/family-members", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        var json = await JsonDocument.ParseAsync(ctx.Request.Body);
        string token = ResolveToken(ctx, json.RootElement);

        var elderly = svc.GetElderlySession(token);
        if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        string name = json.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        string email = json.RootElement.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
        string relationship = json.RootElement.TryGetProperty("relationship", out var r) ? r.GetString() ?? "" : "";
        string phoneNumber = json.RootElement.TryGetProperty("phoneNumber", out var p) ? p.GetString() ?? "" : "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            return Results.Json(new { success = false, message = "İsim ve e-posta zorunludur" }, statusCode: 400);
        }

        var member = new FamilyMember
        {
            Id = Guid.NewGuid().ToString("N"),
            ElderlyId = elderly.Id,
            Name = name,
            Email = email,
            Relationship = relationship,
            PhoneNumber = phoneNumber
        };

        svc.AddFamilyMember(member);
        return Results.Json(new { success = true, member });
    }
    catch
    {
        return Results.Json(new { success = false, message = "İşlem başarısız" }, statusCode: 500);
    }
});
app.MapGet("/api/medications", (HttpContext ctx, HealthDataService svc) =>
{
    var token = ResolveToken(ctx);
    var elderly = svc.GetElderlySession(token);
    if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
    return Results.Json(svc.GetMedications(elderly.Id));
});
app.MapPost("/api/medications", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        var json = await JsonDocument.ParseAsync(ctx.Request.Body);
        string token = ResolveToken(ctx, json.RootElement);

        var elderly = svc.GetElderlySession(token);
        if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        string name = json.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        string notes = json.RootElement.TryGetProperty("notes", out var no) ? no.GetString() ?? "" : "";
        int? stockCount = json.RootElement.TryGetProperty("stockCount", out var sc) && sc.TryGetInt32(out var scVal) ? scVal : null;
        var scheduleTimes = new List<string>();
        if (json.RootElement.TryGetProperty("scheduleTimes", out var st) && st.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in st.EnumerateArray())
            {
                var time = item.GetString();
                if (!string.IsNullOrWhiteSpace(time)) scheduleTimes.Add(time);
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.Json(new { success = false, message = "İlaç adı zorunludur" }, statusCode: 400);
        }

        var medication = svc.AddMedication(elderly.Id, name, notes, scheduleTimes, stockCount);
        return Results.Json(new { success = true, medication });
    }
    catch
    {
        return Results.Json(new { success = false, message = "İşlem başarısız" }, statusCode: 500);
    }
});
app.MapPost("/api/medications/{id}/taken", (HttpContext ctx, int id, HealthDataService svc) =>
{
    var token = ResolveToken(ctx);
    var elderly = svc.GetElderlySession(token);
    if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

    var medication = svc.MarkMedicationTaken(elderly.Id, id);
    if (medication == null) return Results.Json(new { success = false, message = "İlaç bulunamadı" }, statusCode: 404);

    return Results.Json(new { success = true, medication, stockCount = medication.StockCount });
});
app.MapGet("/api/health-records", (HttpContext ctx, HealthDataService svc) =>
{
    var token = ResolveToken(ctx);
    var elderly = svc.GetElderlySession(token);
    if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

    var records = svc.GetHealthRecords(elderly.Id, 30)
        .OrderByDescending(r => r.Timestamp ?? r.RecordedAt)
        .Select(r =>
        {
            string recordType = "genel";
            string unit = r.MetricType switch
            {
                "glucose" => "mg/dL",
                "blood_pressure" => "mmHg",
                "heart_rate" => "bpm",
                _ => ""
            };
            object value = r.Value;

            if (r.Systolic.HasValue || r.Diastolic.HasValue)
            {
                recordType = "tansiyon";
                unit = "mmHg";
                value = $"{r.Systolic ?? 0}/{r.Diastolic ?? 0}";
            }
            else if (r.Glucose.HasValue)
            {
                recordType = "şeker";
                unit = "mg/dL";
                value = r.Glucose.Value;
            }
            else if (r.HeartRate.HasValue)
            {
                recordType = "nabız";
                unit = "bpm";
                value = r.HeartRate.Value;
            }
            else if (!string.IsNullOrWhiteSpace(r.MetricType))
            {
                recordType = r.MetricType;
                if (recordType.Contains("tansiyon", StringComparison.OrdinalIgnoreCase))
                {
                    unit = "mmHg";
                }
                else if (recordType.Contains("şeker", StringComparison.OrdinalIgnoreCase))
                {
                    unit = "mg/dL";
                }
            }

            string level = r.HealthStatus ?? r.Status ?? "normal";
            string alertLevel = level.Contains("critical", StringComparison.OrdinalIgnoreCase)
                ? "critical"
                : level.Contains("warning", StringComparison.OrdinalIgnoreCase)
                    ? "warning"
                    : "normal";

            return new
            {
                recordType,
                value,
                unit,
                alertLevel,
                timestamp = r.Timestamp ?? r.RecordedAt
            };
        });

    return Results.Json(records);
});
app.MapPost("/api/health-records", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        var json = await JsonDocument.ParseAsync(ctx.Request.Body);
        string token = ResolveToken(ctx, json.RootElement);

        var elderly = svc.GetElderlySession(token);
        if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        string recordType = json.RootElement.TryGetProperty("recordType", out var rt) ? rt.GetString() ?? "" : "";
        double value = json.RootElement.TryGetProperty("value", out var v) && v.TryGetDouble(out var val) ? val : 0;
        string unit = json.RootElement.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "";

        if (string.IsNullOrWhiteSpace(recordType))
        {
            return Results.Json(new { success = false, message = "Kayıt tipi zorunludur" }, statusCode: 400);
        }

        svc.AddHealthRecord(elderly.Id, recordType, value);
        return Results.Json(new { success = true, healthStatus = "normal", alertLevel = "normal" });
    }
    catch
    {
        return Results.Json(new { success = false, message = "İşlem başarısız" }, statusCode: 500);
    }
});
app.MapGet("/api/doctor/report", (HttpContext ctx, HealthDataService svc) =>
{
    var token = ResolveToken(ctx);
    var elderly = svc.GetElderlySession(token);
    if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

    var meds = svc.GetMedications(elderly.Id)
        .Select(m => new
        {
            id = m.Id,
            name = m.Name,
            notes = m.Notes,
            scheduleTimes = m.ScheduleTimes,
            stockCount = m.StockCount,
            lastTakenAt = m.LastTakenAt
        });

    var records = svc.GetHealthRecords(elderly.Id, 30)
        .OrderByDescending(r => r.Timestamp ?? r.RecordedAt)
        .Select(r => new
        {
            recordType = r.MetricType,
            value = r.Value,
            systolic = r.Systolic,
            diastolic = r.Diastolic,
            glucose = r.Glucose,
            heartRate = r.HeartRate,
            status = r.HealthStatus ?? r.Status,
            notes = r.Notes,
            timestamp = r.Timestamp ?? r.RecordedAt
        });

    var mood = svc.GetMoodAnalysis(elderly.Id);

    return Results.Json(new
    {
        success = true,
        elderly = new
        {
            id = elderly.Id,
            name = elderly.Name,
            birthDate = elderly.BirthDate,
            phone = elderly.PhoneNumber,
            bloodType = elderly.BloodType,
            allergies = elderly.Allergies,
            medicalHistory = elderly.MedicalHistory
        },
        medications = meds,
        healthRecords = records,
        mood = new
        {
            average = mood.AverageMood,
            trend = mood.Trend,
            recent = mood.RecentMoods.Select(m => new { score = m.MoodScore, timestamp = m.Timestamp })
        }
    });
});
app.MapGet("/api/mood-analysis", (HttpContext ctx, HealthDataService svc) =>
{
    var token = ResolveToken(ctx);
    var elderly = svc.GetElderlySession(token);
    if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

    var analysis = svc.GetMoodAnalysis(elderly.Id);
    return Results.Json(new
    {
        averageMood = analysis.AverageMood,
        trend = analysis.Trend,
        recentMoods = analysis.RecentMoods.Select(m => new { moodScore = m.MoodScore, timestamp = m.Timestamp })
    });
});
app.MapPost("/api/mood", async (HttpContext ctx, HealthDataService svc) =>
{
    try
    {
        var json = await JsonDocument.ParseAsync(ctx.Request.Body);
        string token = ResolveToken(ctx, json.RootElement);

        var elderly = svc.GetElderlySession(token);
        if (elderly == null) return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);

        int moodScore = json.RootElement.TryGetProperty("moodScore", out var ms) && ms.TryGetInt32(out var score) ? score : 0;
        if (moodScore <= 0) return Results.Json(new { success = false, message = "Geçersiz ruh hali" }, statusCode: 400);

        svc.AddMoodRecord(elderly.Id, moodScore, "manual");
        return Results.Json(new { success = true });
    }
    catch
    {
        return Results.Json(new { success = false, message = "İşlem başarısız" }, statusCode: 500);
    }
});
app.MapGet("/api/emergency-alerts/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, alerts = svc.GetActiveAlerts(userId) }));

app.MapHub<HealthReportHub>("/health-hub");

app.Run();

// MODELS
public class ElderlyUser
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    
    // Yaşlı Self-Enrollment için yeni alanlar
    public string PhoneNumber { get; set; } = "";
    public string BirthDate { get; set; } = "";
    public string BloodType { get; set; } = "";
    public string MedicalHistory { get; set; } = "";
    public string Allergies { get; set; } = "";
    public string DoctorPhone { get; set; } = "";
    public Subscription? Subscription { get; set; }
    public bool IsActive { get; set; } = true;
}

// Abonelik bilgileri
public class Subscription
{
    public string UserId { get; set; } = "";
    public string Plan { get; set; } = "standard"; // standard, premium
    public DateTime StartDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasAIAnalysis { get; set; } = false;      // Premium
    public bool HasFallDetection { get; set; } = false;   // Premium
    public bool HasLiveLocation { get; set; } = false;    // Premium
    public bool HasEmergencyIntegration { get; set; } = true; // Both
}

public class TaskItem
{
    public string Id { get; set; } = "";
    public string ElderlyId { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime ScheduledTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public bool IsCompleted { get; set; }
    public string CompletionMethod { get; set; } = "";
}

public class HealthRecord
{
    public string Id { get; set; } = "";
    public string ElderlyId { get; set; } = "";
    public string MetricType { get; set; } = "";
    public double Value { get; set; }
    public DateTime RecordedAt { get; set; }
    public string Status { get; set; } = "";
    
    // Kan Şekeri, Tansiyon, Nabız için yeni alanlar
    public int? Systolic { get; set; }      // Sistolik (üst tansiyon)
    public int? Diastolic { get; set; }     // Diastolik (alt tansiyon)
    public int? Glucose { get; set; }       // Kan şekeri
    public int? HeartRate { get; set; }     // Nabız
    public string? HealthStatus { get; set; } // critical, warning, normal
    public string? Notes { get; set; }      // Notlar
    public DateTime? Timestamp { get; set; } // Timestamp
}

public class FamilyMember
{
    public string Id { get; set; } = "";
    public string ElderlyId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
}

public class Medication
{
    public int Id { get; set; }
    public string ElderlyId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<string> ScheduleTimes { get; set; } = new();
    public int? StockCount { get; set; }
    public DateTime? LastTakenAt { get; set; }
}

public class MoodRecord
{
    public string ElderlyId { get; set; } = "";
    public int MoodScore { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = "manual";
}

public class MoodAnalysis
{
    public int AverageMood { get; set; }
    public string Trend { get; set; } = "stable";
    public List<MoodRecord> RecentMoods { get; set; } = new();
}

public class EmergencyAlert
{
    public string Id { get; set; } = "";
    public string ElderlyId { get; set; } = "";
    public string AlertType { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public string Description { get; set; } = "";
    public bool IsResolved { get; set; }
}

// SERVICE
public class HealthDataService
{
    private List<TaskItem> tasks = new();
    private List<HealthRecord> healthRecords = new();
    private List<EmergencyAlert> emergencyAlerts = new();
    private List<ElderlyUser> _users = new();
    private List<FamilyMember> familyMembers = new();
    private List<Medication> medications = new();
    private List<MoodRecord> moodRecords = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, UserState> userStates = new();
    private int medicationIdSeq = 1;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, FamilyMember> FamilySessions = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ElderlyUser> ElderlySessions = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> lastFamilyContact = new();

    private readonly IConfiguration _configuration;
    private readonly AppDbContext _db;

    public HealthDataService(IConfiguration configuration, AppDbContext db)
    {
        _configuration = configuration;
        _db = db;
        LoadUsersFromDb();

        if (!_users.Any())
        {
            var seeded = new ElderlyUser
            {
                Id = "elderly-001",
                Name = "Buse",
                Email = "elderly@test.com",
                Password = string.Empty,
                PasswordHash = PasswordSecurity.HashPassword("123"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Subscription = new Subscription
                {
                    UserId = "elderly-001",
                    Plan = "premium",
                    StartDate = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    IsActive = true,
                    HasAIAnalysis = true,
                    HasFallDetection = true,
                    HasLiveLocation = true,
                    HasEmergencyIntegration = true
                }
            };

            _users.Add(seeded);
            UpsertUser(seeded);
        }

        InitializeSampleData();
    }

    private void LoadUsersFromDb()
    {
        List<StoredElderlyUser> storedUsers;
        try
        {
            storedUsers = _db.ElderlyUsers.AsNoTracking().ToList();
        }
        catch
        {
            return;
        }

        if (!storedUsers.Any())
        {
            return;
        }

        _users = storedUsers.Select(s => new ElderlyUser
        {
            Id = s.Id,
            Name = s.Name,
            Email = s.Email,
            PasswordHash = s.PasswordHash,
            PhoneNumber = s.PhoneNumber,
            BirthDate = s.BirthDate,
            BloodType = s.BloodType,
            MedicalHistory = s.MedicalHistory,
            Allergies = s.Allergies,
            DoctorPhone = s.DoctorPhone,
            CreatedAt = s.CreatedAt,
            IsActive = s.IsActive,
            Subscription = new Subscription
            {
                UserId = s.Id,
                Plan = s.Plan,
                StartDate = s.SubscriptionStartDate,
                ExpiresAt = s.SubscriptionExpiresAt,
                IsActive = s.SubscriptionIsActive,
                HasAIAnalysis = s.HasAIAnalysis,
                HasFallDetection = s.HasFallDetection,
                HasLiveLocation = s.HasLiveLocation,
                HasEmergencyIntegration = s.HasEmergencyIntegration
            }
        }).ToList();
    }

    private void UpsertUser(ElderlyUser user)
    {
        var existing = _db.ElderlyUsers.FirstOrDefault(x => x.Id == user.Id);
        if (existing == null)
        {
            existing = new StoredElderlyUser { Id = user.Id };
            _db.ElderlyUsers.Add(existing);
        }

        existing.Name = user.Name;
        existing.Email = user.Email;
        existing.PasswordHash = user.PasswordHash;
        existing.PhoneNumber = user.PhoneNumber;
        existing.BirthDate = user.BirthDate;
        existing.BloodType = user.BloodType;
        existing.MedicalHistory = user.MedicalHistory;
        existing.Allergies = user.Allergies;
        existing.DoctorPhone = user.DoctorPhone;
        existing.CreatedAt = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt;
        existing.IsActive = user.IsActive;
        existing.Plan = user.Subscription?.Plan ?? "standard";
        existing.SubscriptionStartDate = user.Subscription?.StartDate ?? DateTime.UtcNow;
        existing.SubscriptionExpiresAt = user.Subscription?.ExpiresAt ?? DateTime.UtcNow.AddYears(1);
        existing.SubscriptionIsActive = user.Subscription?.IsActive ?? true;
        existing.HasAIAnalysis = user.Subscription?.HasAIAnalysis ?? false;
        existing.HasFallDetection = user.Subscription?.HasFallDetection ?? false;
        existing.HasLiveLocation = user.Subscription?.HasLiveLocation ?? false;
        existing.HasEmergencyIntegration = user.Subscription?.HasEmergencyIntegration ?? true;

        _db.SaveChanges();
    }

    private void InitializeSampleData()
    {
        // Sample user (elderly)
        if (!_users.Any(u => u.Id == "elderly-001"))
        {
            _users.Add(new ElderlyUser 
        { 
            Id = "elderly-001", 
            Name = "Ahmet Amca", 
            Email = "elderly@test.com", 
            Password = string.Empty,
            PasswordHash = PasswordSecurity.HashPassword("123"),
            PhoneNumber = "+90 555 123 4567",
            BirthDate = "1948-04-12",
            CreatedAt = DateTime.Now,
            Subscription = new Subscription
            {
                UserId = "elderly-001",
                Plan = "premium",
                StartDate = DateTime.Now.AddMonths(-1),
                ExpiresAt = DateTime.Now.AddMonths(11),
                IsActive = true,
                HasAIAnalysis = true,
                HasFallDetection = true,
                HasLiveLocation = true,
                HasEmergencyIntegration = true
            }
            });

            var seededUser = _users.FirstOrDefault(u => u.Id == "elderly-001");
            if (seededUser != null)
            {
                UpsertUser(seededUser);
            }
        }
        
        familyMembers.AddRange(new[] {
            new FamilyMember { Id = "f1", ElderlyId = "elderly-001", Name = "Fatih (Oğlu)", Email = "fatih@test.com", Relationship = "Oğlu", PhoneNumber = "+905551112233" },
            new FamilyMember { Id = "f2", ElderlyId = "elderly-001", Name = "Ayşe (Kızı)", Email = "ayse@test.com", Relationship = "Kızı", PhoneNumber = "+905554445566" }
        });
        
        var now = DateTime.Now;
        tasks.AddRange(new[] {
            new TaskItem { Id = "t1", ElderlyId = "elderly-001", Type = "medication", Description = "Tansiyon İlacı", ScheduledTime = now.AddHours(2), IsCompleted = false },
            new TaskItem { Id = "t2", ElderlyId = "elderly-001", Type = "health_check", Description = "Şeker Kontrolü", ScheduledTime = now.AddHours(4), IsCompleted = false }
        });

        medications.AddRange(new[]
        {
            new Medication
            {
                Id = medicationIdSeq++,
                ElderlyId = "elderly-001",
                Name = "Tansiyon İlacı",
                Notes = "Yemekten sonra",
                ScheduleTimes = new List<string> { "09:00", "21:00" },
                StockCount = 20,
                LastTakenAt = null
            },
            new Medication
            {
                Id = medicationIdSeq++,
                ElderlyId = "elderly-001",
                Name = "Şeker İlacı",
                Notes = "Sabah",
                ScheduleTimes = new List<string> { "08:30" },
                StockCount = 12,
                LastTakenAt = null
            }
        });

        moodRecords.AddRange(new[]
        {
            new MoodRecord { ElderlyId = "elderly-001", MoodScore = 6, Timestamp = now.AddDays(-4), Source = "auto" },
            new MoodRecord { ElderlyId = "elderly-001", MoodScore = 7, Timestamp = now.AddDays(-3), Source = "auto" },
            new MoodRecord { ElderlyId = "elderly-001", MoodScore = 7, Timestamp = now.AddDays(-2), Source = "auto" },
            new MoodRecord { ElderlyId = "elderly-001", MoodScore = 8, Timestamp = now.AddDays(-1), Source = "auto" },
            new MoodRecord { ElderlyId = "elderly-001", MoodScore = 7, Timestamp = now, Source = "auto" }
        });
        
        // Sample health records (7 gün gerçekçi veri)
        var healthData = new[] {
            (date: now.AddDays(-6), sys: 125, dia: 80, glucose: 110, hr: 72, status: "normal"),
            (date: now.AddDays(-5), sys: 130, dia: 82, glucose: 115, hr: 75, status: "normal"),
            (date: now.AddDays(-4), sys: 135, dia: 85, glucose: 118, hr: 70, status: "warning"),
            (date: now.AddDays(-3), sys: 128, dia: 80, glucose: 105, hr: 68, status: "normal"),
            (date: now.AddDays(-2), sys: 132, dia: 84, glucose: 112, hr: 73, status: "warning"),
            (date: now.AddDays(-1), sys: 126, dia: 81, glucose: 108, hr: 71, status: "normal"),
            (date: now, sys: 129, dia: 82, glucose: 110, hr: 72, status: "normal")
        };
        
        foreach (var (date, sys, dia, glucose, hr, status) in healthData)
        {
            healthRecords.Add(new HealthRecord
            {
                Id = Guid.NewGuid().ToString(),
                ElderlyId = "elderly-001",
                Systolic = sys,
                Diastolic = dia,
                Glucose = glucose,
                HeartRate = hr,
                HealthStatus = status,
                Status = status,
                Timestamp = date,
                RecordedAt = date,
                MetricType = "comprehensive"
            });
        }
        
        // Legacy format uyumluluğu
        healthRecords.AddRange(new[] {
            new HealthRecord { Id = "h1", ElderlyId = "elderly-001", MetricType = "blood_pressure", Value = 125, RecordedAt = DateTime.Now.AddDays(-1), Status = "normal" },
            new HealthRecord { Id = "h2", ElderlyId = "elderly-001", MetricType = "glucose", Value = 110, RecordedAt = DateTime.Now.AddDays(-1), Status = "normal" }
        });
    }

    public List<TaskItem> GetPendingTasks(string elderlyId) => tasks.Where(t => t.ElderlyId == elderlyId && !t.IsCompleted).ToList();
    public void CompleteTask(string taskId, string method) { var t = tasks.FirstOrDefault(x => x.Id == taskId); if (t != null) { t.IsCompleted = true; t.CompletedTime = DateTime.Now; t.CompletionMethod = method; } }
    
    // Eski AddHealthRecord metodu (geriye uyumluluk)
    public void AddHealthRecord(string elderlyId, string metricType, double value) => 
        healthRecords.Add(new HealthRecord { Id = Guid.NewGuid().ToString(), ElderlyId = elderlyId, MetricType = metricType, Value = value, RecordedAt = DateTime.Now, Timestamp = DateTime.Now, Status = "normal" });
    
    // Yeni AddComprehensiveHealthRecord metodu (kan şekeri, tansiyon, nabız)
    public void AddComprehensiveHealthRecord(string elderlyId, int? systolic, int? diastolic, int? glucose, int? heartRate, string healthStatus, string notes = "")
    {
        var record = new HealthRecord
        {
            Id = Guid.NewGuid().ToString(),
            ElderlyId = elderlyId,
            Systolic = systolic,
            Diastolic = diastolic,
            Glucose = glucose,
            HeartRate = heartRate,
            HealthStatus = healthStatus,
            Notes = notes,
            RecordedAt = DateTime.Now,
            Timestamp = DateTime.Now,
            Status = healthStatus
        };
        healthRecords.Add(record);
    }
    
    // Sağlık kayıtlarını getir (son N gün)
    public List<HealthRecord> GetHealthRecords(string elderlyId, int days = 7)
    {
        return healthRecords
            .Where(h => h.ElderlyId == elderlyId && (h.Timestamp ?? h.RecordedAt) >= DateTime.Now.AddDays(-days))
            .OrderBy(h => h.Timestamp ?? h.RecordedAt)
            .ToList();
    }
    
    public void AddEmergencyAlert(string elderlyId, string alertType, string desc) => emergencyAlerts.Add(new EmergencyAlert { Id = Guid.NewGuid().ToString(), ElderlyId = elderlyId, AlertType = alertType, OccurredAt = DateTime.Now, Description = desc, IsResolved = false });
    public ElderlyUser? GetUser(string userId) => _users.FirstOrDefault(u => u.Id == userId);
    public ElderlyUser? GetUserByEmail(string email) => _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    public void AddUser(ElderlyUser user)
    {
        _users.Add(user);
        UpsertUser(user);
    }
    public void SetElderlySession(string token, ElderlyUser user)
    {
        ElderlySessions[token] = user;
    }
    public List<FamilyMember> GetFamilyMembers(string elderlyId) => familyMembers.Where(f => f.ElderlyId == elderlyId).ToList();
    public void AddFamilyMember(FamilyMember member) => familyMembers.Add(member);
    public void RecordFamilyContact(string elderlyId)
    {
        if (string.IsNullOrWhiteSpace(elderlyId)) return;
        lastFamilyContact[elderlyId] = DateTime.Now;
    }

    public DateTime? GetLastFamilyContact(string elderlyId)
    {
        if (string.IsNullOrWhiteSpace(elderlyId)) return null;
        return lastFamilyContact.TryGetValue(elderlyId, out var timestamp) ? timestamp : null;
    }
    public List<Medication> GetMedications(string elderlyId) => medications.Where(m => m.ElderlyId == elderlyId).ToList();
    public Medication AddMedication(string elderlyId, string name, string notes, List<string> scheduleTimes, int? stockCount)
    {
        var medication = new Medication
        {
            Id = medicationIdSeq++,
            ElderlyId = elderlyId,
            Name = name,
            Notes = notes ?? "",
            ScheduleTimes = scheduleTimes ?? new List<string>(),
            StockCount = stockCount,
            LastTakenAt = null
        };
        medications.Add(medication);
        return medication;
    }
    public Medication? MarkMedicationTaken(string elderlyId, int medicationId)
    {
        var medication = medications.FirstOrDefault(m => m.Id == medicationId && m.ElderlyId == elderlyId);
        if (medication == null) return null;
        medication.LastTakenAt = DateTime.Now;
        if (medication.StockCount.HasValue && medication.StockCount.Value > 0)
        {
            medication.StockCount--;
        }
        return medication;
    }
    public void AddMoodRecord(string elderlyId, int moodScore, string source = "manual")
    {
        moodRecords.Add(new MoodRecord
        {
            ElderlyId = elderlyId,
            MoodScore = moodScore,
            Timestamp = DateTime.Now,
            Source = source
        });
    }
    public MoodAnalysis GetMoodAnalysis(string elderlyId, int count = 5)
    {
        var recent = moodRecords
            .Where(m => m.ElderlyId == elderlyId)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .ToList();

        if (recent.Count == 0)
        {
            recent = new List<MoodRecord>
            {
                new MoodRecord { ElderlyId = elderlyId, MoodScore = 7, Timestamp = DateTime.Now }
            };
        }

        var average = (int)Math.Round(recent.Average(m => m.MoodScore));
        var trend = "stable";
        if (recent.Count >= 2)
        {
            var first = recent.First().MoodScore;
            var last = recent.Last().MoodScore;
            if (last - first >= 2) trend = "improving";
            else if (first - last >= 2) trend = "declining";
        }

        return new MoodAnalysis
        {
            AverageMood = average,
            Trend = trend,
            RecentMoods = recent
        };
    }
    public List<HealthRecord> GetRecentHealthRecords(string elderlyId, int days = 7) => healthRecords.Where(h => h.ElderlyId == elderlyId && h.RecordedAt >= DateTime.Now.AddDays(-days)).OrderByDescending(h => h.RecordedAt).ToList();
    public List<TaskItem> GetTaskHistory(string elderlyId, int days = 30) => tasks.Where(t => t.ElderlyId == elderlyId && t.CompletedTime >= DateTime.Now.AddDays(-days)).OrderByDescending(t => t.CompletedTime).ToList();
    public List<EmergencyAlert> GetActiveAlerts(string elderlyId) => emergencyAlerts.Where(a => a.ElderlyId == elderlyId && !a.IsResolved).OrderByDescending(a => a.OccurredAt).ToList();

    public UserState GetUserState(string elderlyId)
    {
        if (string.IsNullOrWhiteSpace(elderlyId))
        {
            return new UserState();
        }

        return userStates.GetOrAdd(elderlyId, id =>
        {
            var now = DateTime.Now;
            var currentContext = "home";
            var screenPriority = "normal";

            var time = now.Hour * 100 + now.Minute;
            if (time >= 900 && time < 1100)
            {
                currentContext = "medication_time";
                screenPriority = "medication";
            }
            else if (time >= 1200 && time < 1400)
            {
                currentContext = "meal_time";
                screenPriority = "meal";
            }
            else if (time >= 1800 && time < 2200)
            {
                currentContext = "water_time";
                screenPriority = "normal";
            }

            return new UserState
            {
                ElderlyId = id,
                CurrentContext = currentContext,
                ActiveTaskId = "",
                ScreenPriority = screenPriority,
                IsAssistantActive = true,
                UpdatedAt = now
            };
        });
    }

    public UserState SetUserState(string elderlyId, UserState update)
    {
        if (string.IsNullOrWhiteSpace(elderlyId)) return update;

        update.ElderlyId = elderlyId;
        update.UpdatedAt = DateTime.Now;
        userStates[elderlyId] = update;
        return update;
    }

    public (string Token, FamilyMember Member)? AuthenticateFamily(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return null;
        if (password != "1234") return null;
        var member = familyMembers.FirstOrDefault(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (member == null) return null;
        var token = Guid.NewGuid().ToString("N");
        FamilySessions[token] = member;
        return (token, member);
    }

    public (string Token, ElderlyUser User)? AuthenticateElderly(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return null;
        var normalizedEmail = email.Trim();
        var normalizedPassword = password.Trim();
        Console.WriteLine($"Giriş deneniyor: {normalizedEmail}");
        var user = _users.FirstOrDefault(u =>
            u.IsActive &&
            u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
        if (user == null) return null;

        var verified = PasswordSecurity.VerifyPassword(normalizedPassword, user.PasswordHash);

        // Backward-compatible migration from plaintext passwords
        if (!verified && !string.IsNullOrWhiteSpace(user.Password))
        {
            verified = string.Equals(normalizedPassword, user.Password, StringComparison.Ordinal);
            if (verified)
            {
                user.PasswordHash = PasswordSecurity.HashPassword(normalizedPassword);
                user.Password = string.Empty;
                UpsertUser(user);
            }
        }

        if (!verified) return null;

        var token = $"elderly_{user.Id}_{Guid.NewGuid():N}";
        ElderlySessions[token] = user;
        return (token, user);
    }

    public (bool Success, string? TempPassword, string Message) ResetElderlyPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, null, "E-posta adresi zorunludur");
        }

        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user == null)
        {
            return (false, null, "Bu e-posta ile kayıt bulunamadı");
        }

        var tempPassword = new Random().Next(100000, 999999).ToString();
        user.PasswordHash = PasswordSecurity.HashPassword(tempPassword);
        user.Password = string.Empty;
        UpsertUser(user);

        var emailResult = SendResetPasswordEmail(user, tempPassword);
        if (!emailResult.Success)
        {
            return (false, tempPassword, emailResult.Message);
        }

        return (true, null, "Geçici şifre e-posta adresinize gönderildi");
    }

    public (bool Success, string Message) DeleteElderlyAccount(string token, string password)
    {
        var user = GetElderlySession(token);
        if (user == null)
        {
            return (false, "Geçersiz oturum");
        }

        if (!PasswordSecurity.VerifyPassword(password, user.PasswordHash))
        {
            return (false, "Şifre doğrulanamadı");
        }

        _users.RemoveAll(u => u.Id == user.Id);
        tasks.RemoveAll(t => t.ElderlyId == user.Id);
        healthRecords.RemoveAll(h => h.ElderlyId == user.Id);
        emergencyAlerts.RemoveAll(a => a.ElderlyId == user.Id);
        familyMembers.RemoveAll(f => f.ElderlyId == user.Id);
        medications.RemoveAll(m => m.ElderlyId == user.Id);
        moodRecords.RemoveAll(m => m.ElderlyId == user.Id);
        userStates.TryRemove(user.Id, out _);
        lastFamilyContact.TryRemove(user.Id, out _);

        foreach (var sessionToken in ElderlySessions
                     .Where(kvp => kvp.Value.Id == user.Id)
                     .Select(kvp => kvp.Key)
                     .ToList())
        {
            ElderlySessions.TryRemove(sessionToken, out _);
        }

        var storedUser = _db.ElderlyUsers.FirstOrDefault(u => u.Id == user.Id);
        if (storedUser != null)
        {
            _db.ElderlyUsers.Remove(storedUser);
        }

        var storedRecords = _db.HealthRecords.Where(r => r.ElderlyId == user.Id).ToList();
        if (storedRecords.Count > 0)
        {
            _db.HealthRecords.RemoveRange(storedRecords);
        }

        _db.SaveChanges();

        return (true, "Hesap başarıyla silindi");
    }

    private (bool Success, string Message) SendResetPasswordEmail(ElderlyUser user, string tempPassword)
    {
        var settings = _configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();

        if (string.IsNullOrWhiteSpace(settings.SmtpServer) ||
            settings.SmtpPort <= 0 ||
            string.IsNullOrWhiteSpace(settings.FromAddress) ||
            string.IsNullOrWhiteSpace(settings.Username) ||
            string.IsNullOrWhiteSpace(settings.Password))
        {
            return (false, "E-posta gönderilemedi: SMTP ayarları eksik");
        }

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(settings.FromAddress, settings.FromName);
            message.To.Add(user.Email);
            message.Subject = "Şifre Sıfırlama";
            message.Body = $"Merhaba {user.Name},\n\nGeçici şifreniz: {tempPassword}\n\nGiriş yaptıktan sonra şifrenizi değiştirin.";

            using var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort)
            {
                EnableSsl = settings.UseStartTls,
                Credentials = new NetworkCredential(settings.Username, settings.Password),
                Timeout = settings.TimeoutSeconds * 1000
            };

            client.Send(message);
            return (true, "E-posta gönderildi");
        }
        catch (Exception ex)
        {
            return (false, $"E-posta gönderilemedi: {ex.Message}");
        }
    }


public class EmailSettings
{
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseStartTls { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 10;
}
    public FamilyMember? GetFamilySession(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        return FamilySessions.TryGetValue(token, out var member) ? member : null;
    }

    public ElderlyUser? GetElderlySession(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (ElderlySessions.TryGetValue(token, out var user)) return user;

        var parts = token.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && string.Equals(parts[0], "elderly", StringComparison.OrdinalIgnoreCase))
        {
            var userId = parts[1];
            var resolved = _users.FirstOrDefault(u => u.Id == userId);
            if (resolved != null)
            {
                ElderlySessions[token] = resolved;
                return resolved;
            }
        }

        return null;
    }

    public bool RemoveFamilySession(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        return FamilySessions.TryRemove(token, out _);
    }
}

public interface ISmsSender
{
    string EmergencyServicesNumber { get; }
    Task<bool> SendAsync(string to, string message);
}

public static class PasswordSecurity
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Şifre boş olamaz", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], "PBKDF2", StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public class SmsSender : ISmsSender
{
    private readonly SmsSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<SmsSender> logger)
    {
        _settings = configuration.GetSection("Sms").Get<SmsSettings>() ?? new SmsSettings();
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        ApplyEnvironmentFallback();
    }

    public string EmergencyServicesNumber => _settings.EmergencyServicesNumber ?? "";

    public async Task<bool> SendAsync(string to, string message)
    {
        if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(message)) return false;

        if (!string.Equals(_settings.Provider, "twilio", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("SMS provider not configured. Skipping SMS to {To}", to);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Twilio.AccountSid) ||
            string.IsNullOrWhiteSpace(_settings.Twilio.AuthToken) ||
            string.IsNullOrWhiteSpace(_settings.FromNumber))
        {
            _logger.LogWarning("SMS settings are incomplete. Skipping SMS to {To}", to);
            return false;
        }

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://api.twilio.com/2010-04-01/Accounts/{_settings.Twilio.AccountSid}/Messages.json");

        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.Twilio.AccountSid}:{_settings.Twilio.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = to,
            ["From"] = _settings.FromNumber,
            ["Body"] = message
        });

        try
        {
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SMS send failed: {Status}", response.StatusCode);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send failed");
            return false;
        }
    }

    private void ApplyEnvironmentFallback()
    {
        var env = LoadEnvFileIfPresent();

        string accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID") ?? env.GetValueOrDefault("TWILIO_ACCOUNT_SID") ?? "";
        string authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? env.GetValueOrDefault("TWILIO_AUTH_TOKEN") ?? "";
        string fromNumber = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER") ?? env.GetValueOrDefault("TWILIO_PHONE_NUMBER") ?? "";

        if (string.IsNullOrWhiteSpace(_settings.Twilio.AccountSid) && !string.IsNullOrWhiteSpace(accountSid))
        {
            _settings.Twilio.AccountSid = accountSid;
        }

        if (string.IsNullOrWhiteSpace(_settings.Twilio.AuthToken) && !string.IsNullOrWhiteSpace(authToken))
        {
            _settings.Twilio.AuthToken = authToken;
        }

        if (string.IsNullOrWhiteSpace(_settings.FromNumber) && !string.IsNullOrWhiteSpace(fromNumber))
        {
            _settings.FromNumber = fromNumber;
        }

        if (string.IsNullOrWhiteSpace(_settings.Provider) && !string.IsNullOrWhiteSpace(_settings.Twilio.AccountSid))
        {
            _settings.Provider = "twilio";
        }
    }

    private Dictionary<string, string> LoadEnvFileIfPresent()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".env");
            if (!System.IO.File.Exists(path)) return result;
            foreach (var line in System.IO.File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
                var idx = trimmed.IndexOf('=');
                if (idx <= 0) continue;
                var key = trimmed.Substring(0, idx).Trim();
                var value = trimmed.Substring(idx + 1).Trim();
                if (!result.ContainsKey(key)) result[key] = value;
            }
        }
        catch
        {
            // ignore
        }
        return result;
    }
}

public class SmsSettings
{
    public string Provider { get; set; } = ""; // twilio
    public string FromNumber { get; set; } = "";
    public string EmergencyServicesNumber { get; set; } = "";
    public TwilioSettings Twilio { get; set; } = new TwilioSettings();
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = "";
    public string AuthToken { get; set; } = "";
}

// SIGNALR HUB
