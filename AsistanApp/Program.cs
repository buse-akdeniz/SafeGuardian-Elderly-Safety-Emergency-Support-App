using ilk_projem.Data;
using AsistanApp.Services;
using Microsoft.EntityFrameworkCore;
using ilk_projem.Controllers;

var builder = WebApplication.CreateBuilder(args);
var sqliteConnection = "Data Source=asistan.db";

// 1. Tüm servisleri buraya tek tek kaydediyoruz
builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnection));

// Hata veren servisler için kesin çözüm:

builder.Services.AddScoped<HealthDataService>();
builder.Services.AddScoped<AuthService>();
var app = builder.Build();

// 2. Apple uygulaman için internet erişim izni (CORS)
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();
app.MapControllers();
app.MapStateEndpoints();
app.MapGet("/health/live", () => Results.Ok(new { ok = true }));
app.Run();
