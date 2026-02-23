namespace CMS.AIAssistantService.DTOs;

public class ChatResponseDto
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ServiceCalled { get; set; }
}
