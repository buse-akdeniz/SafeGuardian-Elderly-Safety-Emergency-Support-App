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
    public DbSet<StoredFamilyMember> FamilyMembers => Set<StoredFamilyMember>();
    public DbSet<StoredMedication> Medications => Set<StoredMedication>();
    public DbSet<StoredMoodRecord> MoodRecords => Set<StoredMoodRecord>();
    public DbSet<StoredEmergencyAlert> EmergencyAlerts => Set<StoredEmergencyAlert>();
    public DbSet<StoredTaskItem> TaskItems => Set<StoredTaskItem>();
    public DbSet<StoredFamilyContact> FamilyContacts => Set<StoredFamilyContact>();

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

        modelBuilder.Entity<StoredFamilyMember>(entity =>
        {
            entity.ToTable("FamilyMembers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ElderlyId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Relationship).HasMaxLength(64);
            entity.Property(x => x.PhoneNumber).HasMaxLength(32);
            entity.HasIndex(x => new { x.ElderlyId, x.Email });
        });

        modelBuilder.Entity<StoredMedication>(entity =>
        {
            entity.ToTable("Medications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ElderlyId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.ScheduleTimesJson).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<StoredMoodRecord>(entity =>
        {
            entity.ToTable("MoodRecords");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ElderlyId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Timestamp).IsRequired();
        });

        modelBuilder.Entity<StoredEmergencyAlert>(entity =>
        {
            entity.ToTable("EmergencyAlerts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ElderlyId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.AlertType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.OccurredAt).IsRequired();
        });

        modelBuilder.Entity<StoredTaskItem>(entity =>
        {
            entity.ToTable("TaskItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ElderlyId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(512).IsRequired();
            entity.Property(x => x.CompletionMethod).HasMaxLength(64);
            entity.Property(x => x.ScheduledTime).IsRequired();
        });

        modelBuilder.Entity<StoredFamilyContact>(entity =>
        {
            entity.ToTable("FamilyContacts");
            entity.HasKey(x => x.ElderlyId);
            entity.Property(x => x.ElderlyId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.LastContactAt).IsRequired();
            entity.Property(x => x.LastReminderSentAt);
        });
    }
}
