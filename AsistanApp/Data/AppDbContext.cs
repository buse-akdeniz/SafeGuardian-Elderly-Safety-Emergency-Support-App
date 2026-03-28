using Microsoft.EntityFrameworkCore;
using ilk_projem.Models.Persistence;

namespace ilk_projem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<StoredHealthRecord> HealthRecords => Set<StoredHealthRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StoredHealthRecord>(entity =>
        {
            entity.ToTable("HealthRecords");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ElderlyId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.MetricType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.HealthStatus).HasMaxLength(32).IsRequired();
            entity.Property(x => x.RecordedAt).IsRequired();
        });
    }
}
