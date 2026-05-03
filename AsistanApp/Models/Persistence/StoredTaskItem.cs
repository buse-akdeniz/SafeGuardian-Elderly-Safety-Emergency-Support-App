namespace ilk_projem.Models.Persistence;

public class StoredTaskItem
{
    public string Id { get; set; } = "";
    public string ElderlyId { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime ScheduledTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public bool IsCompleted { get; set; }
    public string CompletionMethod { get; set; } = "";
}
