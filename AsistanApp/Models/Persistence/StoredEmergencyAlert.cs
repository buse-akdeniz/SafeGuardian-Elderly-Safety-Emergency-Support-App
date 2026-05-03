namespace ilk_projem.Models.Persistence;

public class StoredEmergencyAlert
{
    public string Id { get; set; } = "";
    public string ElderlyId { get; set; } = "";
    public string AlertType { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = "";
    public bool IsResolved { get; set; }
}
