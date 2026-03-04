namespace CMS.TelegramService.Models;

public class UserSession
{
    public string Token { get; set; } = "";
    public string Role { get; set; } = "Student";
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsImpersonating { get; set; } = false;

    // Conversation state management
    public string? ConversationState { get; set; }
    public Dictionary<string, object> ConversationData { get; set; } = new();
}
