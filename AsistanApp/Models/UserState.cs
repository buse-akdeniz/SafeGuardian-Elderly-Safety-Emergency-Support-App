namespace ilk_projem.Models;

public class UserState
{
    public string ElderlyId { get; set; } = "";
    public string CurrentContext { get; set; } = "home";
    public string ActiveTaskId { get; set; } = "";
    public string ScreenPriority { get; set; } = "normal";
    public bool IsAssistantActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
