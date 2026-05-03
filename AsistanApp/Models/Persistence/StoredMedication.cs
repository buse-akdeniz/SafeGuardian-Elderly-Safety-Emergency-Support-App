namespace ilk_projem.Models.Persistence;

public class StoredMedication
{
    public int Id { get; set; }
    public string ElderlyId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public string ScheduleTimesJson { get; set; } = "[]";
    public int? StockCount { get; set; }
    public DateTime? LastTakenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
