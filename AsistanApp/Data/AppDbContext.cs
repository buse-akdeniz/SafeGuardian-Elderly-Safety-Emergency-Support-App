using Microsoft.EntityFrameworkCore;
using ilk_projem.Models.Persistence;

namespace ilk_projem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<StoredHealthRecord> HealthRecords => Set<StoredHealthRecord>();
    public DbSet<StoredElderlyUser> ElderlyUsers => Set<StoredElderlyUser>();

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

        modelBuilder.Entity<StoredElderlyUser>(entity =>
        {
            entity.ToTable("ElderlyUsers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(32);
            entity.Property(x => x.BirthDate).HasMaxLength(32);
            entity.Property(x => x.BloodType).HasMaxLength(16);
            entity.Property(x => x.MedicalHistory).HasMaxLength(2000);
            entity.Property(x => x.Allergies).HasMaxLength(2000);
            entity.Property(x => x.DoctorPhone).HasMaxLength(32);
            entity.Property(x => x.Plan).HasMaxLength(32).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });
    }
}
