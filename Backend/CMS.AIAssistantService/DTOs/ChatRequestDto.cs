namespace CMS.AIAssistantService.DTOs;

public class ChatRequestDto
{
    public int StudentId { get; set; }
    public string Message { get; set; } = string.Empty;
}
