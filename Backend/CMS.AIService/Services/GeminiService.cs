using System.Net.Http.Json;
using System.Text.Json;
using CMS.AIService.Models;

namespace CMS.AIService.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ActionExecutorService _actionExecutor;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly Tool _tools;

    public GeminiService(
        IHttpClientFactory factory,
        ActionExecutorService actionExecutor,
        IConfiguration config,
        ILogger<GeminiService> logger)
    {
        _httpClient = factory.CreateClient();
        _actionExecutor = actionExecutor;
        _logger = logger;
        _apiKey = config["GeminiAI:ApiKey"] ?? throw new Exception("Gemini API key not configured");
        _tools = FunctionDefinitions.GetAllFunctions();
    }

    public async Task<(string response, bool requiresConfirmation, string? actionId)> GetResponseAsync(
        string userMessage,
        string userId,
        List<ChatMessage>? conversationHistory = null)
    {
        try
        {
            // Build conversation contents
            var contents = BuildConversationContents(userMessage, conversationHistory);

            // Call Gemini API with function calling
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents,
                tools = new[] { _tools },
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = GetSystemInstruction()
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API Error: {Error}", error);
                return ("I'm having trouble connecting to the AI service. Please try again later.", false, null);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            // Check if response contains function call
            var candidate = result.GetProperty("candidates")[0];
            var content = candidate.GetProperty("content");
            var parts = content.GetProperty("parts");

            // Check for function call in the first part
            if (parts[0].TryGetProperty("functionCall", out var functionCallElement))
            {
                var functionCall = ParseFunctionCall(functionCallElement);
                _logger.LogInformation("AI requested function call: {FunctionName}", functionCall.Name);

                // Execute the function
                var (functionResult, requiresConfirmation, actionId) = await _actionExecutor.ExecuteFunctionAsync(functionCall, userId);

                if (requiresConfirmation)
                {
                    // Return confirmation request to user
                    return ($"⚠️ **Confirmation Required**\n\n{functionResult}\n\nDo you want to proceed?", true, actionId);
                }

                // If no confirmation needed, send function result back to Gemini for natural language response
                var naturalResponse = await GetNaturalResponseAsync(userMessage, functionCall, functionResult, conversationHistory);
                return (naturalResponse, false, null);
            }

            // No function call - direct text response
            if (parts[0].TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString();
                return (text ?? "I couldn't generate a response.", false, null);
            }

            return ("I couldn't generate a response. Please try rephrasing your question.", false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return ($"Error: {ex.Message}", false, null);
        }
    }

    public async Task<string> GetConfirmedActionResponseAsync(
        string actionId,
        string originalMessage,
        List<ChatMessage>? conversationHistory = null)
    {
        try
        {
            // Execute the confirmed action
            var functionResult = await _actionExecutor.ConfirmAndExecuteAsync(actionId);

            // Get natural language response
            var response = await GetFinalResponseAsync(originalMessage, functionResult, conversationHistory);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing confirmed action");
            return $"Error executing action: {ex.Message}";
        }
    }

    private async Task<string> GetNaturalResponseAsync(
        string userMessage,
        FunctionCall functionCall,
        string functionResult,
        List<ChatMessage>? conversationHistory)
    {
        try
        {
            var contents = new List<object>();

            // Add conversation history if exists
            if (conversationHistory != null)
            {
                foreach (var msg in conversationHistory.TakeLast(5)) // Keep last 5 messages
                {
                    contents.Add(new
                    {
                        role = msg.Role == "assistant" ? "model" : "user",
                        parts = new[] { new { text = msg.Content } }
                    });
                }
            }

            // Add user message
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            // Add model's function call
            contents.Add(new
            {
                role = "model",
                parts = new[]
                {
                    new
                    {
                        functionCall = new
                        {
                            name = functionCall.Name,
                            args = functionCall.Args
                        }
                    }
                }
            });

            // Add function response
            contents.Add(new
            {
                role = "function",
                parts = new[]
                {
                    new
                    {
                        functionResponse = new
                        {
                            name = functionCall.Name,
                            response = new
                            {
                                content = functionResult
                            }
                        }
                    }
                }
            });

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents,
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = "Based on the function result, provide a clear, friendly, and concise response to the user. Format numbers and data in a readable way." }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            var text = result
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? functionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting natural response");
            return functionResult; // Fallback to raw function result
        }
    }

    private async Task<string> GetFinalResponseAsync(
        string originalMessage,
        string functionResult,
        List<ChatMessage>? conversationHistory)
    {
        try
        {
            var contents = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = $"{originalMessage}\n\nAction completed. Result: {functionResult}" } }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents,
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = "The action has been completed successfully. Provide a brief, friendly confirmation message to the user." }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            var text = result
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "✓ Action completed successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting final response");
            return "✓ Action completed successfully!";
        }
    }

    private List<object> BuildConversationContents(string userMessage, List<ChatMessage>? conversationHistory)
    {
        var contents = new List<object>();

        // Add recent conversation history (last 5 messages for context)
        if (conversationHistory != null)
        {
            foreach (var msg in conversationHistory.TakeLast(5))
            {
                contents.Add(new
                {
                    role = msg.Role == "assistant" ? "model" : "user",
                    parts = new[] { new { text = msg.Content } }
                });
            }
        }

        // Add current message
        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = userMessage } }
        });

        return contents;
    }

    private FunctionCall ParseFunctionCall(JsonElement functionCallElement)
    {
        var name = functionCallElement.GetProperty("name").GetString() ?? "";
        var args = new Dictionary<string, object>();

        if (functionCallElement.TryGetProperty("args", out var argsElement))
        {
            foreach (var property in argsElement.EnumerateObject())
            {
                args[property.Name] = property.Value;
            }
        }

        return new FunctionCall
        {
            Name = name,
            Args = args
        };
    }

    private string GetSystemInstruction()
    {
        return @"You are an intelligent AI assistant for a College Management System.

**Your Capabilities:**
- Query and retrieve information about students, courses, enrollments, fees, and attendance
- Create, update, and delete records across all modules
- Provide insights and analytics based on the data

**Instructions:**
1. When users ask questions, use the available functions to fetch accurate data
2. Always provide clear, concise, and friendly responses
3. For data queries, present information in an organized manner
4. When performing create/update/delete operations, confirm the action clearly
5. If information is missing or unclear, ask the user for clarification
6. Format numerical data (counts, amounts) in a readable way

**Important:**
- Use functions when the user's request involves data or actions
- Don't make up data - always use the actual system data via functions
- Be helpful and professional in all interactions";
    }
}
