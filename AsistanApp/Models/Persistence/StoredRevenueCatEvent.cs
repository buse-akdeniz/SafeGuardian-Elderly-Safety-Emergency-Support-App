using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ilk_projem.Models.Persistence;

[Table("RevenueCatEvents")]
public sealed class StoredRevenueCatEvent
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string EventId { get; set; } = "";

    [Required]
    public string EventType { get; set; } = "";

    public string AppUserId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
