using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var sqliteConnection = "Data Source=asistan.db";

// Tüm servislerin tanımı
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnection));
builder.Services.AddScoped<HealthDataService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Apple uygulaması için en kritik CORS ayarı
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();
app.MapControllers();
app.Run();
