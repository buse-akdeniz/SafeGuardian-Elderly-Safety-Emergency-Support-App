using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<HealthDataService>();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5007", "http://localhost", "https://localhost")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseRouting();
app.UseCors();
app.UseStaticFiles();

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

app.MapPost("/api/emergency-alert", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = System.Text.Json.JsonDocument.Parse(body).RootElement;
        string elderlyId = json.TryGetProperty("elderlyId", out var e) ? e.GetString() ?? "elderly-001" : "elderly-001";
        string alertType = json.TryGetProperty("alertType", out var a) ? a.GetString() ?? "emergency_call" : "emergency_call";
        svc.AddEmergencyAlert(elderlyId, alertType, "Acil durum!");
        await hub.Clients.All.SendAsync("ReceiveEmergencyAlert", new { elderlyId, alertType });
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
            svc.AddEmergencyAlert(elderlyId, "fall_detected", $"Düşme algılandı: {accelerationMagnitude:F2} m/s²");
            
            return Results.Json(new { success = true, fallDetected = true, initiateVoiceCheck = true, timeoutSeconds = 15 });
        }
        
        return Results.Json(new { success = true, fallDetected = false });
    }
    catch { return Results.Json(new { success = false }, statusCode: 500); }
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

app.MapPost("/api/emergency-broadcast", async (HttpContext ctx, HealthDataService svc, IHubContext<HealthReportHub> hub) =>
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
        string healthStatus = "normal";
        if ((systolic > 180 || diastolic > 110) || glucose > 180 || heartRate < 50 || heartRate > 120)
            healthStatus = "critical";
        else if ((systolic > 140 || diastolic > 90) || (glucose > 140))
            healthStatus = "warning";
        
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
app.MapGet("/api/pending-tasks/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, count = svc.GetPendingTasks(userId).Count }));
app.MapGet("/api/elderly-status/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, status = new { name = svc.GetUser(userId)?.Name, location = "Evde", pendingTasks = svc.GetPendingTasks(userId).Count } }));
app.MapGet("/api/task-history/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, tasks = svc.GetTaskHistory(userId) }));
app.MapGet("/api/health-analytics/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, analytics = new { } }));
app.MapGet("/api/family-members/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, members = svc.GetFamilyMembers(userId) }));
app.MapGet("/api/emergency-alerts/{userId}", (string userId, HealthDataService svc) => Results.Json(new { success = true, alerts = svc.GetActiveAlerts(userId) }));

app.MapHub<HealthReportHub>("/health-hub");

app.Run("http://localhost:5007");

// MODELS
public class ElderlyUser
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
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
    private List<ElderlyUser> users = new();
    private List<FamilyMember> familyMembers = new();

    public HealthDataService()
    {
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Sample user (elderly)
        users.Add(new ElderlyUser 
        { 
            Id = "elderly-001", 
            Name = "Ahmet Amca", 
            Email = "elderly@test.com", 
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
        
        familyMembers.AddRange(new[] {
            new FamilyMember { Id = "f1", ElderlyId = "elderly-001", Name = "Fatih (Oğlu)", Email = "fatih@test.com", Relationship = "Oğlu" },
            new FamilyMember { Id = "f2", ElderlyId = "elderly-001", Name = "Ayşe (Kızı)", Email = "ayse@test.com", Relationship = "Kızı" }
        });
        
        var now = DateTime.Now;
        tasks.AddRange(new[] {
            new TaskItem { Id = "t1", ElderlyId = "elderly-001", Type = "medication", Description = "Tansiyon İlacı", ScheduledTime = now.AddHours(2), IsCompleted = false },
            new TaskItem { Id = "t2", ElderlyId = "elderly-001", Type = "health_check", Description = "Şeker Kontrolü", ScheduledTime = now.AddHours(4), IsCompleted = false }
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
    public ElderlyUser? GetUser(string userId) => users.FirstOrDefault(u => u.Id == userId);
    public void AddUser(ElderlyUser user) => users.Add(user);
    public List<FamilyMember> GetFamilyMembers(string elderlyId) => familyMembers.Where(f => f.ElderlyId == elderlyId).ToList();
    public List<HealthRecord> GetRecentHealthRecords(string elderlyId, int days = 7) => healthRecords.Where(h => h.ElderlyId == elderlyId && h.RecordedAt >= DateTime.Now.AddDays(-days)).OrderByDescending(h => h.RecordedAt).ToList();
    public List<TaskItem> GetTaskHistory(string elderlyId, int days = 30) => tasks.Where(t => t.ElderlyId == elderlyId && t.CompletedTime >= DateTime.Now.AddDays(-days)).OrderByDescending(t => t.CompletedTime).ToList();
    public List<EmergencyAlert> GetActiveAlerts(string elderlyId) => emergencyAlerts.Where(a => a.ElderlyId == elderlyId && !a.IsResolved).OrderByDescending(a => a.OccurredAt).ToList();
}

// SIGNALR HUB
public class HealthReportHub : Hub
{
    public async Task SendHealthUpdate(string elderlyId, string healthData) => await Clients.All.SendAsync("ReceiveHealthUpdate", new { elderlyId, data = healthData });
    public async Task SendEmergencyAlert(string elderlyId, string alertType) => await Clients.All.SendAsync("ReceiveEmergencyAlert", new { elderlyId, alertType });
    public async Task SendTaskUpdate(string elderlyId, string taskId, string status) => await Clients.All.SendAsync("ReceiveTaskUpdate", new { elderlyId, taskId, status });
    public async Task SendAICriticalAlert(string elderlyId, string alertType) => await Clients.All.SendAsync("ReceiveAICritical", new { elderlyId, alertType, timestamp = DateTime.Now });
    public async Task SendEmergencyEscalation(string elderlyId) => await Clients.All.SendAsync("ReceiveEmergencyEscalation", new { elderlyId, timestamp = DateTime.Now });
    public async Task SendEmergencyBroadcast(object broadcastData) => await Clients.All.SendAsync("ReceiveEmergencyBroadcast", broadcastData);
    public async Task SendAlertCancelled(string elderlyId) => await Clients.All.SendAsync("ReceiveAlertCancelled", new { elderlyId, timestamp = DateTime.Now });
    public override async Task OnConnectedAsync() { Console.WriteLine($"✅ Client Connected: {Context.ConnectionId}"); await base.OnConnectedAsync(); }
}
