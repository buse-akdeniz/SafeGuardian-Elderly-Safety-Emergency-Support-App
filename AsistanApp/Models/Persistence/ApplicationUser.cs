using Microsoft.AspNetCore.Identity;

namespace ilk_projem.Models.Persistence;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public string AccountType { get; set; } = "Elderly";
    public string? ElderlyOwnerId { get; set; }
    public string BirthDate { get; set; } = "";
    public string BloodType { get; set; } = "";
    public string Allergies { get; set; } = "";
    public string MedicalHistory { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAuthenticatedAt { get; set; }
    public DateTime? DeletionRequestedAt { get; set; }
}

public sealed class RefreshSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string CreatedByIp { get; set; } = "";
    public string? ReplacedByTokenHash { get; set; }
}

public sealed class PrivacyConsent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string PolicyVersion { get; set; } = "";
    public bool Accepted { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public sealed class DeviceRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string EncryptedToken { get; set; } = "";
    public string Platform { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SmsDispatchAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string RecipientHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Succeeded { get; set; }
}
