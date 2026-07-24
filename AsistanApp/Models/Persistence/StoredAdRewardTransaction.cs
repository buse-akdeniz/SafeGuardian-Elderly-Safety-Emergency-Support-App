using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ilk_projem.Models.Persistence;

[Table("AdRewardTransactions")]
public sealed class StoredAdRewardTransaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string TransactionId { get; set; } = "";

    [Required]
    public string ElderlyId { get; set; } = "";

    public string AdUnitId { get; set; } = "";
    public long RewardAmount { get; set; }
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
}
