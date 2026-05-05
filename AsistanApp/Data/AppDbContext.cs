using Microsoft.EntityFrameworkCore;

namespace ilk_projem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<object> HealthRecords { get; set; }
    }
}