using System.ComponentModel.DataAnnotations;

namespace ilk_projem.Models.Persistence;

public sealed class StoredUserState
{
    [Key]
    public string ElderlyId { get; set; } = "";
    public string CurrentContext { get; set; } = "home";
    public string ActiveTaskId { get; set; } = "";
    public string ScreenPriority { get; set; } = "normal";
    public bool IsAssistantActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
