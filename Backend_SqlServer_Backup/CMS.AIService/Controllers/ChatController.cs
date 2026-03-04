using Microsoft.AspNetCore.Mvc;
using CMS.AIService.Services;
using CMS.AIService.Models;

namespace CMS.AIService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly GeminiService _geminiService;
    private readonly ActionExecutorService _actionExecutor;
    private readonly ILogger<ChatController> _logger;
    private static Dictionary<string, List<ChatMessage>> _chatHistory = new();
    private static Dictionary<string, string> _pendingActionMessages = new(); // Store original messages for confirmed actions

    public ChatController(
        GeminiService geminiService,
        ActionExecutorService actionExecutor,
        ILogger<ChatController> logger)
    {
        _geminiService = geminiService;
        _actionExecutor = actionExecutor;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Received chat message from user {UserId}", request.UserId);

            // Get conversation history for context
            var history = _chatHistory.ContainsKey(request.UserId)
                ? _chatHistory[request.UserId]
                : null;

            // Get AI response with function calling
            var (response, requiresConfirmation, actionId) = await _geminiService.GetResponseAsync(
                request.Message,
                request.UserId,
                history
            );

            // Store in chat history
            if (!_chatHistory.ContainsKey(request.UserId))
                _chatHistory[request.UserId] = new List<ChatMessage>();

            _chatHistory[request.UserId].Add(new ChatMessage
            {
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            });

            _chatHistory[request.UserId].Add(new ChatMessage
            {
                Role = "assistant",
                Content = response,
                Timestamp = DateTime.UtcNow
            });

            // If confirmation required, store original message
            if (requiresConfirmation && actionId != null)
            {
                _pendingActionMessages[actionId] = request.Message;
            }

            return Ok(new
            {
                response,
                requiresConfirmation,
                actionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = $"AI Error: {ex.Message}" });
        }
    }

    [HttpPost("confirm/{actionId}")]
    public async Task<IActionResult> ConfirmAction(string actionId, [FromBody] ConfirmActionRequest request)
    {
        try
        {
            if (!request.Confirmed)
            {
                // User cancelled the action
                _actionExecutor.CancelAction(actionId);
                _pendingActionMessages.Remove(actionId);
                
                return Ok(new { response = "❌ Action cancelled." });
            }

            // Get original message
            if (!_pendingActionMessages.TryGetValue(actionId, out var originalMessage))
            {
                return BadRequest(new { error = "Action not found or expired" });
            }

            // Execute the confirmed action
            var response = await _geminiService.GetConfirmedActionResponseAsync(
                actionId,
                originalMessage,
                _chatHistory.ContainsKey(request.UserId) ? _chatHistory[request.UserId] : null
            );

            // Update chat history
            if (_chatHistory.ContainsKey(request.UserId))
            {
                _chatHistory[request.UserId].Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = response,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Cleanup
            _pendingActionMessages.Remove(actionId);

            return Ok(new { response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming action");
            return StatusCode(500, new { error = $"Error: {ex.Message}" });
        }
    }

    [HttpPost("cancel/{actionId}")]
    public IActionResult CancelAction(string actionId)
    {
        _actionExecutor.CancelAction(actionId);
        _pendingActionMessages.Remove(actionId);
        return Ok(new { message = "Action cancelled successfully" });
    }

    [HttpGet("history/{userId}")]
    public IActionResult GetHistory(string userId)
    {
        if (_chatHistory.ContainsKey(userId))
            return Ok(_chatHistory[userId]);
        
        return Ok(new List<ChatMessage>());
    }

    [HttpDelete("history/{userId}")]
    public IActionResult ClearHistory(string userId)
    {
        if (_chatHistory.ContainsKey(userId))
            _chatHistory.Remove(userId);
        
        return Ok(new { message = "Chat history cleared successfully" });
    }
}

// Update ConfirmActionRequest model
public class ConfirmActionRequest
{
    public string UserId { get; set; } = string.Empty;
    public bool Confirmed { get; set; }
}
