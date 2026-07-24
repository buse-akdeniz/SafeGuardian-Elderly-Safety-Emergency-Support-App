using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ilk_projem.Models.Persistence;

[Table("Subscriptions")]
public class StoredSubscription
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ElderlyId { get; set; } = "";

    /// <summary>free | premium</summary>
    [Required]
    public string PlanId { get; set; } = "free";

    /// <summary>RevenueCat entitlement identifier.</summary>
    public string EntitlementId { get; set; } = "";

    /// <summary>Apple/Google product identifier.</summary>
    public string ProductId { get; set; } = "";

    public string Platform { get; set; } = ""; // "ios" | "android"

    public string TransactionId { get; set; } = "";
    public string OriginalTransactionId { get; set; } = "";
    public string Environment { get; set; } = "";

    public DateTime ExpiresAt { get; set; } = DateTime.MinValue;
    public DateTime? AdUnlockUntil { get; set; }
    public bool IsActive { get; set; }
    public bool WillRenew { get; set; }
    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
