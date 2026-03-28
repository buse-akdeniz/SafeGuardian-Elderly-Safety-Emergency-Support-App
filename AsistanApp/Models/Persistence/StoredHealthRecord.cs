namespace ilk_projem.Models.Persistence;

public class StoredHealthRecord
{
    public int Id { get; set; }
    public string ElderlyId { get; set; } = "";
    public string MetricType { get; set; } = "";
    public double Value { get; set; }
    public int? Systolic { get; set; }
    public int? Diastolic { get; set; }
    public int? Glucose { get; set; }
    public int? HeartRate { get; set; }
    public string HealthStatus { get; set; } = "normal";
    public string Notes { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
