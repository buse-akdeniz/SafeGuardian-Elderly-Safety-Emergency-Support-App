namespace ilk_projem.Models.Persistence;

public class StoredMoodRecord
{
    public int Id { get; set; }
    public string ElderlyId { get; set; } = "";
    public int MoodScore { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "manual";
}
