using CMS.AIAssistantService.Data;
using CMS.AIAssistantService.DTOs;
using CMS.AIAssistantService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.AIAssistantService.Services;

public class ChatHistoryService
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ChatHistoryService> _logger;

    public ChatHistoryService(ChatDbContext context, ILogger<ChatHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Conversation> GetOrCreateConversationAsync(int studentId)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.Timestamp))
            .FirstOrDefaultAsync(c => c.StudentId == studentId);

        if (conversation == null)
        {
            conversation = new Conversation
            {
                StudentId = studentId,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created new conversation for student {StudentId}", studentId);
        }

        return conversation;
    }

    public async Task AddMessageAsync(int conversationId, string role, string message, string? serviceCalled = null)
    {
        var chatMessage = new ChatMessage
        {
            ConversationId = conversationId,
            Role = role,
            Message = message,
            Timestamp = DateTime.UtcNow,
            ServiceCalled = serviceCalled
        };

        _context.ChatMessages.Add(chatMessage);

        // Update last message time
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.LastMessageAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Added message to conversation {ConversationId}", conversationId);
    }

    public async Task<List<MessageHistoryDto>> GetMessageHistoryAsync(int studentId, int limit = 50)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.StudentId == studentId);

        if (conversation == null)
        {
            return new List<MessageHistoryDto>();
        }

        return conversation.Messages
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .OrderBy(m => m.Timestamp)
            .Select(m => new MessageHistoryDto
            {
                Role = m.Role,
                Message = m.Message,
                Timestamp = m.Timestamp
            })
            .ToList();
    }

    public async Task<bool> ClearHistoryAsync(int studentId)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.StudentId == studentId);

        if (conversation != null)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleared conversation history for student {StudentId}", studentId);
            return true;
        }

        return false;
    }

    public List<string> FormatHistoryForAI(List<MessageHistoryDto> history)
    {
        return history.Select(m => $"{m.Role}: {m.Message}").ToList();
    }
}
