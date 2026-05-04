using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var sqliteConnection = "Data Source=asistan.db";

// Servisleri ekliyoruz (HealthDataService hatasını bu satır çözer)
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnection));
builder.Services.AddScoped<HealthDataService>(); // Eksik olan parça buydu

var app = builder.Build();

// Apple ve tarayıcı erişimi için CORS
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();
app.MapControllers();
app.Run();
