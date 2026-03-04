using Mscc.GenerativeAI;

namespace CMS.AIAssistantService.Services;

public class GeminiAIService
{
    private readonly string _apiKey;
    private readonly ILogger<GeminiAIService> _logger;
    private readonly GoogleAI _googleAI;

    public GeminiAIService(IConfiguration configuration, ILogger<GeminiAIService> logger)
    {
        _apiKey = configuration["GeminiAI:ApiKey"] ?? throw new InvalidOperationException("Gemini API Key not found in configuration");
        _logger = logger;
        _googleAI = new GoogleAI(apiKey: _apiKey);
    }

    public async Task<(string response, string? intent, string? serviceName)> GetAIResponseAsync(
        string userMessage, 
        List<string> conversationHistory)
    {
        try
        {
            // Build system prompt with context
            var systemPrompt = @"You are an intelligent student assistant for a College Management System. 
You help students with queries about their courses, fees, attendance, and enrollments.

When responding:
1. Be friendly, concise, and helpful
2. Use emojis appropriately 😊
3. If the query is about attendance, fees, courses, or enrollment, indicate that in your response
4. Format numbers and data clearly

Available information domains:
- ATTENDANCE: Student attendance records and percentages
- FEE: Fee details, payments, and balances
- COURSE: Course information, schedules, and details
- ENROLLMENT: Student enrollments and course registrations
- STUDENT: Student profile and personal information
- GENERAL: General queries, greetings, help

Detect the intent from the user's message and respond accordingly.";

            // Add conversation history
            var fullPrompt = systemPrompt + "\n\nConversation History:\n";
            foreach (var msg in conversationHistory.TakeLast(5)) // Last 5 messages for context
            {
                fullPrompt += msg + "\n";
            }
            fullPrompt += $"\nStudent: {userMessage}\nAssistant:";

            // Generate response using GoogleAI
            var model = _googleAI.GenerativeModel(model: "gemini-2.0-flash-exp");
            var response = await model.GenerateContent(fullPrompt);
            var aiResponse = response?.Text ?? "I apologize, but I couldn't process that request. Could you please try again?";


            // Detect intent
            var (intent, serviceName) = DetectIntent(userMessage, aiResponse);

            _logger.LogInformation("AI Response generated. Intent: {Intent}, Service: {Service}", intent, serviceName);

            return (aiResponse, intent, serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response");
            return ("I'm sorry, I'm having trouble connecting right now. Please try again later. 😔", null, null);
        }
    }

    private (string? intent, string? serviceName) DetectIntent(string userMessage, string aiResponse)
    {
        var lowerMessage = userMessage.ToLower();

        // Detect intent based on keywords
        if (lowerMessage.Contains("attendance") || lowerMessage.Contains("present") || 
            lowerMessage.Contains("absent") || lowerMessage.Contains("class"))
        {
            return ("attendance_query", "AttendanceService");
        }
        else if (lowerMessage.Contains("fee") || lowerMessage.Contains("payment") || 
                 lowerMessage.Contains("pay") || lowerMessage.Contains("owe") || 
                 lowerMessage.Contains("dues"))
        {
            return ("fee_query", "FeeService");
        }
        else if (lowerMessage.Contains("course") || lowerMessage.Contains("subject") || 
                 lowerMessage.Contains("class") || lowerMessage.Contains("schedule"))
        {
            return ("course_query", "CourseService");
        }
        else if (lowerMessage.Contains("enroll") || lowerMessage.Contains("registration") || 
                 lowerMessage.Contains("register"))
        {
            return ("enrollment_query", "EnrollmentService");
        }
        else if (lowerMessage.Contains("profile") || lowerMessage.Contains("student") || 
                 lowerMessage.Contains("details") || lowerMessage.Contains("information"))
        {
            return ("student_query", "StudentService");
        }
        else if (lowerMessage.Contains("hello") || lowerMessage.Contains("hi") || 
                 lowerMessage.Contains("help") || lowerMessage.Contains("hey"))
        {
            return ("greeting", null);
        }

        return (null, null);
    }

    public string GenerateHelpfulResponse()
    {
        return @"👋 Hello! I'm your AI Student Assistant. I can help you with:

📊 Attendance - Check your attendance records
💰 Fees - View fee details and payment status
📚 Courses - Browse available courses
📝 Enrollment - Check your enrollments
👤 Profile - View your student information

Just ask me anything! For example:
• ""What's my attendance?""
• ""How much do I owe in fees?""
• ""Show me available courses""

How can I assist you today? 😊";
    }
}
