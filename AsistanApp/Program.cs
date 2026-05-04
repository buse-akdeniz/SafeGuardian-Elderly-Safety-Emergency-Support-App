using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var sqliteConnection = "Data Source=asistan.db";

// Servis Tanımları (Hataları bu kısım çözer)
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnection));

// Eğer HealthDataService dosyası varsa, bu satır o 6 hatayı bitirir:
builder.Services.AddScoped<HealthDataService>();

var app = builder.Build();

// Apple uygulaması erişimi için CORS ayarı
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();
app.MapControllers();
app.Run();
