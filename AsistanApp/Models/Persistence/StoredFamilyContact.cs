using System.ComponentModel.DataAnnotations;

namespace ilk_projem.Models.Persistence;

public class StoredFamilyContact
{
    [Key]
    public string ElderlyId { get; set; } = "";
    public DateTime LastContactAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReminderSentAt { get; set; }
}
