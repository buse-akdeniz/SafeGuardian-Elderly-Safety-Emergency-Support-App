using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ilk_projem.Models.Persistence;

namespace ilk_projem.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<StoredHealthRecord>   HealthRecords   { get; set; } = null!;
        public DbSet<StoredSubscription>   Subscriptions   { get; set; } = null!;
        public DbSet<StoredFamilyMember>   FamilyMembers   { get; set; } = null!;
        public DbSet<StoredMedication>     Medications     { get; set; } = null!;
        public DbSet<StoredMoodRecord>     MoodRecords     { get; set; } = null!;
        public DbSet<StoredEmergencyAlert> EmergencyAlerts { get; set; } = null!;
        public DbSet<StoredFamilyContact>  FamilyContacts  { get; set; } = null!;
        public DbSet<StoredTaskItem>       TaskItems       { get; set; } = null!;
        public DbSet<StoredRevenueCatEvent> RevenueCatEvents { get; set; } = null!;
        public DbSet<StoredAdRewardTransaction> AdRewardTransactions { get; set; } = null!;
        public DbSet<RefreshSession> RefreshSessions { get; set; } = null!;
        public DbSet<PrivacyConsent> PrivacyConsents { get; set; } = null!;
        public DbSet<DeviceRegistration> DeviceRegistrations { get; set; } = null!;
        public DbSet<SmsDispatchAudit> SmsDispatchAudits { get; set; } = null!;
        public DbSet<StoredNotification> Notifications { get; set; } = null!;
        public DbSet<StoredUserState> UserStates { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StoredSubscription>()
                .HasIndex(s => s.ElderlyId)
                .IsUnique();

            modelBuilder.Entity<StoredRevenueCatEvent>()
                .HasIndex(e => e.EventId)
                .IsUnique();

            modelBuilder.Entity<StoredAdRewardTransaction>()
                .HasIndex(e => e.TransactionId)
                .IsUnique();

            modelBuilder.Entity<StoredHealthRecord>()
                .HasIndex(r => new { r.ElderlyId, r.RecordedAt });

            modelBuilder.Entity<ApplicationUser>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(u => u.ElderlyOwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefreshSession>()
                .HasIndex(s => s.TokenHash)
                .IsUnique();
            modelBuilder.Entity<RefreshSession>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PrivacyConsent>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeviceRegistration>()
                .HasIndex(d => d.TokenHash)
                .IsUnique();
            modelBuilder.Entity<DeviceRegistration>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SmsDispatchAudit>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            ConfigureOwnedData<StoredHealthRecord>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredMedication>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredMoodRecord>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredFamilyMember>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredEmergencyAlert>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredTaskItem>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredFamilyContact>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredSubscription>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredAdRewardTransaction>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredNotification>(modelBuilder, x => x.ElderlyId);
            ConfigureOwnedData<StoredUserState>(modelBuilder, x => x.ElderlyId);
        }

        private static void ConfigureOwnedData<TEntity>(
            ModelBuilder modelBuilder,
            System.Linq.Expressions.Expression<Func<TEntity, object?>> foreignKey)
            where TEntity : class
        {
            modelBuilder.Entity<TEntity>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(foreignKey)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}