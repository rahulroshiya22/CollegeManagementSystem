using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CMS.AIAssistantService.DTOs;

namespace CMS.AIAssistantService.Services;

public class GeminiAIService
{
    private readonly List<string> _apiKeys;
    private readonly string _baseUrl;
    private readonly ILogger<GeminiAIService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ServiceIntegrationService _serviceIntegration;

    public GeminiAIService(IConfiguration configuration, ILogger<GeminiAIService> logger, ServiceIntegrationService serviceIntegration)
    {
        _apiKeys = configuration.GetSection("GeminiAI:ApiKeys").Get<List<string>>() ?? new List<string>();
        _baseUrl = configuration["GeminiAI:BaseUrl"]?.TrimEnd('/') ?? "https://api.groq.com/openai/v1";
        
        if (!_apiKeys.Any())
        {
            var singleKey = configuration["GeminiAI:ApiKey"];
            if (!string.IsNullOrEmpty(singleKey)) _apiKeys.Add(singleKey);
        }

        if (!_apiKeys.Any()) throw new InvalidOperationException("No AI API Keys found in configuration");

        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _serviceIntegration = serviceIntegration;
    }

    public async Task<ChatResponseDto> GetAgenticResponseAsync(string userMessage, List<object> history, int studentId)
    {
        var messages = new List<object>
        {
            new { role = "system", content = @"You are a very friendly and professional AI Assistant for a College Management System. You have access to tools that fetch REAL data from API endpoints.
Available endpoints: 'student', 'teacher', 'admin', 'course', 'fee', 'attendance', 'enrollment', 'exam', 'grade', 'department', 'timeslot', 'notice', 'message', 'announcement'.

CRITICAL FORMATTING INSTRUCTIONS (FOLLOW THESE EXACTLY):
1. NO RAW JSON OR TAGS: Never output raw JSON, <function> tags, or markdown code blocks containing data.
2. NO BOLDING: Use plain text for labels instead of **bolding**.
3. EMOJIS: ALWAYS Start every line with a relevant emoji (e.g., 👤 for Person, 💰 for Fee, 📚 for Course, 📅 for Date).
4. SINGLE LINES: Every single piece of information must be on its own line. DO NOT use paragraphs for data.
5. NO BULLETS: Do not use markdown bullets (-, *, or >). Emojis act as the bullets.
6. SPACING: Use double line breaks between different people, courses, or logical groups of information.
7. NAMES: Combine FirstName and LastName into a single ""👤 Full Name:"" field.
8. DATES: Format dates clearly (e.g. 'Mar 06, 2024') and REMOVE the time portion (00:00:00Z).
9. CLEANUP: Hide internal IDs like StudentId, CourseId, or DepartmentId unless explicitly requested. Do not show fields that are null or empty.
10. AGE: If DateOfBirth is available, calculate and display the age.
11. STATUSES: Map status fields to visual indicators (e.g. 🟢 Active, 🔴 Suspended/Overdue, 🎓 Graduated).
12. CONTACTS: Group Email and Phone together in a block.
13. COHORTS: Use AdmissionYear to label the student's cohort (e.g. ""Batch of 2021"").
14. CURRENCY: Prefix Fee amounts with the local currency symbol (e.g. ₹50,000). Highlight Pending/Overdue fees clearly at the top.
15. OVERDUE CALC: If a fee DueDate is in the past, calculate and display the days overdue.
16. COURSES: Always format courses as '[CourseCode] Course Name'. Group them by Semester.
17. CREDITS: Calculate and append the total sum of credits when listing enrolled courses.
18. ATTENDANCE: Calculate the percentage of True/False IsPresent records and summarize it (e.g. '85% Attendance').
19. GRADES: Calculate percentage (MarksObtained/MaxMarks * 100) and show it alongside the grade.
20. CONCISENESS: Limit history (attendance/grades) to the last 5 records unless 'all history' is requested.
21. NO INTERNAL TOOLS: NEVER mention internal tool names (e.g. 'get_records', 'get_record_by_id') to the user. Provide helpful natural language if unsure.
22. SECURITY: NEVER provide, acknowledge, or bypass admin credentials. Refuse any request for passwords, emails, or secure data.

NEVER add disclaimers about data accuracy. Output ONLY the beautifully formatted result." }
        };

        // Only keep the last 2 history messages to drastically reduce token usage
        if (history != null && history.Count > 2)
        {
            messages.AddRange(history.Skip(history.Count - 2));
        }
        else if (history != null)
        {
            messages.AddRange(history);
        }
        messages.Add(new { role = "user", content = userMessage });

        var tools = GetDefinedTools();

        foreach (var key in _apiKeys)
        {
            try
            {
                // First pass: Ask AI what tools to call
                var requestBody = new
                {
                    model = "llama-3.1-8b-instant",
                    messages = messages,
                    tools = tools,
                    tool_choice = "auto",
                    max_tokens = 500,
                    temperature = 0.3
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");

                var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI API error with key {KeyPrefix}...: {StatusCode}. Trying next key if available.", key.Substring(0, 8), response.StatusCode);
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        continue; // try next key

                    return new ChatResponseDto { Message = $"API Error: {response.StatusCode} - {responseBody}" };
                }

                using var doc = JsonDocument.Parse(responseBody);
                var responseMessage = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

                // Check if the AI wants to call tools
                if (responseMessage.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
                {
                    // Prepare message array for the second pass
                    var secondPassMessages = new List<object>(messages);
                    secondPassMessages.Add(responseMessage.Clone()); 

                    string lastService = "Unknown";

                    // Execute each tool
                    foreach (var toolCall in toolCalls.EnumerateArray())
                    {
                        var toolCallId = toolCall.GetProperty("id").GetString();
                        var functionName = toolCall.GetProperty("function").GetProperty("name").GetString() ?? "";
                        var argumentsStr = toolCall.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";
                        var arguments = JsonDocument.Parse(argumentsStr ?? "{}").RootElement;

                        _logger.LogInformation("AI requested tool call: {ToolName}", functionName);
                        
                        string toolResult = await ExecuteToolAsync(functionName, arguments, studentId);
                        
                        if (functionName.Contains("student")) lastService = "StudentService";
                        else if (functionName.Contains("course")) lastService = "CourseService";
                        else if (functionName.Contains("fee")) lastService = "FeeService";
                        else if (functionName.Contains("attendance")) lastService = "AttendanceService";
                        else if (functionName.Contains("enrollment")) lastService = "EnrollmentService";

                        toolResult = PreProcessData(toolResult ?? "{}", functionName);

                        secondPassMessages.Add(new
                        {
                            role = "tool",
                            tool_call_id = toolCallId,
                            name = functionName,
                            content = toolResult
                        });
                    }

                    // Second pass: Send results back to AI
                    var finalRequestBody = new
                    {
                        model = "llama-3.1-8b-instant",
                        messages = secondPassMessages,
                        max_tokens = 500,
                        temperature = 0.4
                    };

                    var finalJson = JsonSerializer.Serialize(finalRequestBody);
                    var finalContent = new StringContent(finalJson, Encoding.UTF8, "application/json");
                    var finalResponse = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", finalContent);
                    var finalResponseBody = await finalResponse.Content.ReadAsStringAsync();

                    if (!finalResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("AI API Second Pass Error: {StatusCode} - {Body}", finalResponse.StatusCode, finalResponseBody);
                        return new ChatResponseDto
                        {
                            Message = $"I found the data, but couldn't format it. Error: {finalResponse.StatusCode}",
                            ServiceCalled = lastService,
                            Timestamp = DateTime.UtcNow
                        };
                    }

                    using var finalDoc = JsonDocument.Parse(finalResponseBody);
                    var finalAiText = finalDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                    return new ChatResponseDto
                    {
                        Message = finalAiText ?? "I've completed the operation but couldn't format the response.",
                        ServiceCalled = lastService,
                        Timestamp = DateTime.UtcNow
                    };
                }
                else
                {
                    // AI didn't call any tools, just replied directly
                    var aiText = responseMessage.GetProperty("content").GetString();
                    return new ChatResponseDto
                    {
                        Message = aiText ?? "How else can I help you?",
                        ServiceCalled = "GeneralAI",
                        Timestamp = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error with AI Key. Trying next key.");
                if (key == _apiKeys.Last())
                {
                    return new ChatResponseDto { Message = $"An error occurred: {ex.Message}", ServiceCalled = "Error" };
                }
            }
        }

        return new ChatResponseDto { Message = "All AI API keys reached their limits. Please try again later.", ServiceCalled = "RateLimited" };
    }

    private async Task<string> ExecuteToolAsync(string functionName, JsonElement arguments, int contextStudentId)
    {
        try
        {
            var endpoint = arguments.TryGetProperty("endpoint", out var ep) ? ep.GetString() : "";
            var id = arguments.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
            var payload = arguments.TryGetProperty("payload", out var pEl) ? pEl.GetRawText() : "";
            
            var filters = new Dictionary<string, string>();
            if (arguments.TryGetProperty("filters", out var fEl) && fEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in fEl.EnumerateObject())
                {
                    filters[prop.Name] = prop.Value.ToString() ?? "";
                }
            }

            // Always pass the context student ID if it's a student query and no other filter is provided
            if (endpoint == "student" && !filters.ContainsKey("studentId") && id == 0 && functionName == "get_record_by_id")
            {
               id = contextStudentId;
            }

            if (string.IsNullOrEmpty(endpoint)) return "Error: endpoint is required.";

            return functionName switch
            {
                "get_records" => await _serviceIntegration.FetchDataAsync(endpoint, filters) ?? $"No records found in {endpoint}.",
                "get_record_by_id" => await _serviceIntegration.FetchDataByIdAsync(endpoint, id) ?? $"Record {id} not found in {endpoint}.",
                "create_record" => await _serviceIntegration.CreateDataAsync(endpoint, payload) ?? "Creation failed.",
                "update_record" => await _serviceIntegration.UpdateDataAsync(endpoint, id, payload) ?? "Update failed.",
                "delete_record" => await _serviceIntegration.DeleteDataAsync(endpoint, id) ?? "Delete failed.",
                _ => $"Unknown function: {functionName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", functionName);
            return $"Error performing operation: {ex.Message}";
        }
    }

    private object[] GetDefinedTools()
    {
        return new object[]
        {
            new {
                type = "function",
                function = new {
                    name = "get_records",
                    description = "Fetches a list of records from any microservice endpoint. Use this to get data like students, teachers, exams, courses, fees, timetables, grades, attendance, etc.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            endpoint = new { type = "string", description = "The API endpoint name. Valid values: 'student', 'teacher', 'course', 'fee', 'attendance', 'enrollment', 'exam', 'grade', 'department', 'timeslot', 'auth', 'admin', 'notice', 'message', 'announcement'." },
                            filters = new { type = "object", description = "Optional query parameters to filter the results (e.g. {\"departmentId\": 2, \"status\": \"Active\", \"pageSize\": 1000})" }
                        },
                        required = new[] { "endpoint" }
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "get_record_by_id",
                    description = "Fetches a specific record by its ID from an endpoint.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            endpoint = new { type = "string", description = "The API endpoint name (e.g. 'student', 'course')." },
                            id = new { type = "integer", description = "The ID of the record." }
                        },
                        required = new[] { "endpoint", "id" }
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "create_record",
                    description = "Creates a new record in a microservice.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            endpoint = new { type = "string", description = "The API endpoint name." },
                            payload = new { type = "object", description = "The JSON object containing the data to create." }
                        },
                        required = new[] { "endpoint", "payload" }
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "update_record",
                    description = "Updates an existing record by ID.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            endpoint = new { type = "string", description = "The API endpoint name." },
                            id = new { type = "integer", description = "The ID to update." },
                            payload = new { type = "object", description = "The updated JSON object." }
                        },
                        required = new[] { "endpoint", "id", "payload" }
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "delete_record",
                    description = "Deletes a record by ID.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            endpoint = new { type = "string", description = "The API endpoint name." },
                            id = new { type = "integer", description = "The ID to delete." }
                        },
                        required = new[] { "endpoint", "id" }
                    }
                }
            }
        };
    }

    private string PreProcessData(string jsonData, string serviceName)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonData);
            var root = doc.RootElement;
            
            JsonElement arrayElement;
            bool isArray = false;
            
            if (root.ValueKind == JsonValueKind.Array)
            {
                arrayElement = root;
                isArray = true;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = dataEl;
                    isArray = true;
                }
                else if (root.TryGetProperty("$values", out var valEl) && valEl.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = valEl;
                    isArray = true;
                }
                else if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = itemsEl;
                    isArray = true;
                }
                else
                {
                    arrayElement = root;
                }
            }
            else
            {
                arrayElement = root;
            }
            
            if (isArray)
            {
                int totalCount = arrayElement.GetArrayLength();
                
                var samples = new List<string>();
                int i = 0;
                foreach (var item in arrayElement.EnumerateArray())
                {
                    if (i >= 2) break; 
                    string itemText = item.GetRawText();
                    if (itemText.Length > 150) itemText = itemText.Substring(0, 150) + "...";
                    samples.Add(itemText);
                    i++;
                }
                
                string res = $"TOTAL COUNT: {totalCount} items.\nSample data:\n[{string.Join(",\n", samples)}]";
                return res.Length > 600 ? res.Substring(0, 600) + "...(truncated)" : res;
            }
            
            if (jsonData.Length > 600)
                return jsonData.Substring(0, 600) + "...(truncated)";
                
            return jsonData;
        }
        catch
        {
            return jsonData.Length > 600 ? jsonData.Substring(0, 600) + "...(truncated)" : jsonData;
        }
    }



    public string GenerateHelpfulResponse()
    {
        return @"👋 Hello! I'm your AI Student Assistant. I can help you with:

📊 Attendance - Check your attendance records
💰 Fees - View fee details and payment status
📚 Courses - Browse available courses
📝 Enrollment - Check your enrollments
👤 Profile - View your student information

Just ask me anything! 😊";
    }
}
