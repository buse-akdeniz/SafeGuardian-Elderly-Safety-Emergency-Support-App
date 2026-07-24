namespace ilk_projem.Models.Persistence;

public sealed class StoredNotification
{
    public long Id { get; set; }
    public string ElderlyId { get; set; } = "";
    public string Type { get; set; } = "info";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "normal";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
