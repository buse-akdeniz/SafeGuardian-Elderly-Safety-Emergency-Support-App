namespace ilk_projem.Models.Persistence;

public class StoredElderlyUser
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string BirthDate { get; set; } = "";
    public string BloodType { get; set; } = "";
    public string MedicalHistory { get; set; } = "";
    public string Allergies { get; set; } = "";
    public string DoctorPhone { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string Plan { get; set; } = "standard";
    public DateTime SubscriptionStartDate { get; set; } = DateTime.UtcNow;
    public DateTime SubscriptionExpiresAt { get; set; } = DateTime.UtcNow.AddYears(1);
    public bool SubscriptionIsActive { get; set; } = true;
    public bool HasAIAnalysis { get; set; }
    public bool HasFallDetection { get; set; }
    public bool HasLiveLocation { get; set; }
    public bool HasEmergencyIntegration { get; set; } = true;
}
