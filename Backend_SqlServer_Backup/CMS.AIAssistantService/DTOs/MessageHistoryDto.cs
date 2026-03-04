namespace CMS.AIAssistantService.DTOs;

public class MessageHistoryDto
{
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
