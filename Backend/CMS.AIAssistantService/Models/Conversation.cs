namespace CMS.AIAssistantService.Models;

public class Conversation
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
