using ilk_projem.Data;using AsistanApp.Services;using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var sqliteConnection = "Data Source=asistan.db";

// 1. Tüm servisleri buraya tek tek kaydediyoruz
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnection));

// Hata veren servisler için kesin çözüm:
builder.Services.AddScoped<HealthDataService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// 2. Apple uygulaman için internet erişim izni (CORS)
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();
app.MapControllers();
app.Run();
