namespace CMS.AIAssistantService.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ServiceCalled { get; set; } // Which microservice was called (optional)
    
    public Conversation Conversation { get; set; } = null!;
}
