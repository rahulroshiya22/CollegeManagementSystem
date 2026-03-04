using CMS.AIAssistantService.DTOs;
using CMS.AIAssistantService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.AIAssistantService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly GeminiAIService _geminiService;
    private readonly ChatHistoryService _chatHistoryService;
    private readonly ServiceIntegrationService _serviceIntegration;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        GeminiAIService geminiService,
        ChatHistoryService chatHistoryService,
        ServiceIntegrationService serviceIntegration,
        ILogger<ChatController> logger)
    {
        _geminiService = geminiService;
        _chatHistoryService = chatHistoryService;
        _serviceIntegration = serviceIntegration;
        _logger = logger;
    }

    /// <summary>
    /// Send a message and get AI response
    /// </summary>
    [HttpPost("message")]
public async Task<ActionResult<ChatResponseDto>> SendMessage([FromBody] ChatRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            // Get or create conversation
            var conversation = await _chatHistoryService.GetOrCreateConversationAsync(request.StudentId);

            // Get conversation history
            var history = await _chatHistoryService.GetMessageHistoryAsync(request.StudentId);
            var formattedHistory = _chatHistoryService.FormatHistoryForAI(history);

            // Save user message
            await _chatHistoryService.AddMessageAsync(conversation.Id, "user", request.Message);

            // Get AI response
            var (aiResponse, intent, serviceName) = await _geminiService.GetAIResponseAsync(
                request.Message, 
                formattedHistory);

            // If intent detected, try to call the service
            if (!string.IsNullOrEmpty(serviceName) && !string.IsNullOrEmpty(intent))
            {
                string? serviceData = null;

                switch (intent)
                {
                    case "student_query":
                        serviceData = await _serviceIntegration.GetStudentInfoAsync(request.StudentId);
                        break;
                    case "attendance_query":
                        serviceData = await _serviceIntegration.GetAttendanceInfoAsync(request.StudentId);
                        break;
                    case "fee_query":
                        serviceData = await _serviceIntegration.GetFeeInfoAsync(request.StudentId);
                        break;
                    case "course_query":
                        serviceData = await _serviceIntegration.GetCoursesAsync();
                        break;
                    case "enrollment_query":
                        serviceData = await _serviceIntegration.GetEnrollmentsAsync(request.StudentId);
                        break;
                }

                // If we got data from service, enhance the AI response
                if (!string.IsNullOrEmpty(serviceData))
                {
                    _logger.LogInformation("Retrieved data from {ServiceName}", serviceName);
                    // In a production scenario, you'd parse the JSON and format it nicely
                    // For now, the AI will provide a helpful response based on the query
                }
            }

            // If greeting or help request, provide helpful response
            if (intent == "greeting" || request.Message.ToLower().Contains("help"))
            {
                aiResponse = _geminiService.GenerateHelpfulResponse();
            }

            // Save AI response
            await _chatHistoryService.AddMessageAsync(conversation.Id, "assistant", aiResponse, serviceName);

            return Ok(new ChatResponseDto
            {
                Message = aiResponse,
                Timestamp = DateTime.UtcNow,
                ServiceCalled = serviceName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = "An error occurred while processing your message" });
        }
    }

    /// <summary>
    /// Get conversation history for a student
    /// </summary>
    [HttpGet("history/{studentId}")]
    public async Task<ActionResult<List<MessageHistoryDto>>> GetHistory(int studentId)
    {
        try
        {
            var history = await _chatHistoryService.GetMessageHistoryAsync(studentId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history for student {StudentId}", studentId);
            return StatusCode(500, new { error = "An error occurred while retrieving chat history" });
        }
    }

    /// <summary>
    /// Clear conversation history for a student
    /// </summary>
    [HttpDelete("history/{studentId}")]
    public async Task<ActionResult> ClearHistory(int studentId)
    {
        try
        {
            var result = await _chatHistoryService.ClearHistoryAsync(studentId);
            if (result)
            {
                return Ok(new { message = "Conversation history cleared successfully" });
            }
            return NotFound(new { error = "No conversation found for this student" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing chat history for student {StudentId}", studentId);
            return StatusCode(500, new { error = "An error occurred while clearing chat history" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "AI Assistant Service", timestamp = DateTime.UtcNow });
    }
}
