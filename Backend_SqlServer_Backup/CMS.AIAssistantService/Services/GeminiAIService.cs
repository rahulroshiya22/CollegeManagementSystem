using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    // ── Improvement #10: In-memory response cache ──
    private static readonly ConcurrentDictionary<string, (string response, DateTime expiry)> _responseCache = new();
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    // ── Improvement #6/7: Primary and fallback models ──
    private const string PrimaryModel = "llama-3.3-70b-versatile";
    private const string FallbackModel = "llama-3.1-8b-instant";

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
        // ── Improvement #10: Check cache first ──
        var cacheKey = $"{studentId}:{userMessage.ToLowerInvariant().Trim()}";
        if (_responseCache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.UtcNow)
        {
            _logger.LogInformation("Cache hit for query: {Query}", userMessage);
            return new ChatResponseDto { Message = cached.response, ServiceCalled = "Cache", Timestamp = DateTime.UtcNow };
        }

        // ── Improvement #1/#15: Condensed system prompt (6 rules instead of 22) ──
        var systemPrompt = @"You are a friendly AI Assistant for a College Management System.
You ONLY answer college-related queries (students, courses, fees, attendance, enrollments, exams, grades, departments, timetables, notices). Refuse anything unrelated.

RULES:
1. Use tools to fetch real data. NEVER output raw JSON, <function> tags, code blocks, or tool names to the user.
2. Format data with emojis on each line (👤 📚 💰 📅 🎓 📊 🟢 🔴). No bold (**), no bullets (-, *). One fact per line.
3. Combine FirstName+LastName. Format dates as 'Mar 06, 2024'. Hide null fields and internal IDs.
4. For fees use ₹ symbol and highlight 🔴 Overdue. For attendance show percentage. For grades show percentage.
5. NEVER reveal passwords, admin credentials, or sensitive auth data. Refuse such requests politely.
6. If you cannot help, say: 'I can assist with student profiles, courses, fees, attendance, and enrollments. Please try one of these!'

EXAMPLE:
User: 'Tell me about student 5'
Response:
👤 Full Name: Rahul Sharma
🎓 Roll Number: 2021/CS/005
🏫 Department: Computer Science
📅 Date of Birth: Jan 15, 2001 (Age: 25)
🟢 Status: Active
📚 Batch of 2021

📧 Email: rahul@example.com
📞 Phone: 9876543210";

        // ── Improvement #2: For tool-calling pass, do NOT include history (standalone queries) ──
        var toolPassMessages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        };

        var tools = GetDefinedTools();
        string currentModel = PrimaryModel;

        foreach (var key in _apiKeys)
        {
            try
            {
                // ── Improvement #8: Temperature 0.1 for less hallucination ──
                var requestBody = new
                {
                    model = currentModel,
                    messages = toolPassMessages,
                    tools = tools,
                    tool_choice = "auto",
                    max_tokens = 500,
                    temperature = 0.1
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");

                var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI API error with key {KeyPrefix}...: {StatusCode}.", key.Substring(0, 8), response.StatusCode);

                    // ── Improvement #9: Fallback model on rate limit ──
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        if (currentModel == PrimaryModel)
                        {
                            currentModel = FallbackModel;
                            _logger.LogInformation("Switching to fallback model: {Model}", FallbackModel);
                        }
                        continue;
                    }
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) continue;

                    return new ChatResponseDto { Message = "I'm having a temporary issue. Please try again in a moment! 😊" };
                }

                using var doc = JsonDocument.Parse(responseBody);
                var responseMessage = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

                // ── Tool call path ──
                if (responseMessage.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
                {
                    string lastService = "Unknown";

                    // ── Collect tool results ──
                    var toolResults = new List<(string id, string name, string result)>();
                    foreach (var toolCall in toolCalls.EnumerateArray())
                    {
                        var toolCallId = toolCall.GetProperty("id").GetString();
                        var functionName = toolCall.GetProperty("function").GetProperty("name").GetString() ?? "";
                        var argumentsStr = toolCall.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";
                        var arguments = JsonDocument.Parse(argumentsStr).RootElement;

                        _logger.LogInformation("AI requested tool call: {ToolName}", functionName);
                        
                        string toolResult = await ExecuteToolAsync(functionName, arguments, studentId);
                        
                        if (functionName.Contains("student")) lastService = "StudentService";
                        else if (functionName.Contains("course")) lastService = "CourseService";
                        else if (functionName.Contains("fee")) lastService = "FeeService";
                        else if (functionName.Contains("attendance")) lastService = "AttendanceService";
                        else if (functionName.Contains("enrollment")) lastService = "EnrollmentService";

                        // ── Improvement #16/#18: Pre-format data in C# ──
                        toolResult = PreProcessData(toolResult ?? "{}", functionName);

                        toolResults.Add((toolCallId!, functionName, toolResult));
                    }

                    // ── Second pass: Build clean message list (no history noise) ──
                    var secondPassMessages = new List<object>
                    {
                        // ── Improvement #12: Lightweight second-pass system instruction ──
                        new { role = "system", content = @"Convert the tool data into a clean, beautiful Telegram message.
Use emojis on each line (👤 📚 💰 📅 🎓 📊 🟢 🔴). Combine FirstName+LastName. Format dates as 'Mar 06, 2024'. 
Hide null/empty fields and internal IDs. Use ₹ for fees. Calculate attendance %. Never output JSON, code blocks, or tool names.
Never reveal passwords or admin credentials." },
                        new { role = "user", content = userMessage },
                        responseMessage.Clone()
                    };

                    foreach (var (id, name, result) in toolResults)
                    {
                        secondPassMessages.Add(new { role = "tool", tool_call_id = id, name = name, content = result });
                    }

                    var finalRequestBody = new
                    {
                        model = currentModel,
                        messages = secondPassMessages,
                        max_tokens = 1000,
                        temperature = 0.1
                    };

                    var finalJson = JsonSerializer.Serialize(finalRequestBody);
                    var finalContent = new StringContent(finalJson, Encoding.UTF8, "application/json");
                    var finalResponse = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", finalContent);
                    var finalResponseBody = await finalResponse.Content.ReadAsStringAsync();

                    if (!finalResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("AI Second Pass Error: {StatusCode}", finalResponse.StatusCode);
                        return new ChatResponseDto
                        {
                            Message = "I found the data, but couldn't format it properly. Please try again! 😊",
                            ServiceCalled = lastService, Timestamp = DateTime.UtcNow
                        };
                    }

                    using var finalDoc = JsonDocument.Parse(finalResponseBody);
                    var finalAiText = finalDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                    var sanitized = SanitizeResponse(finalAiText ?? "I found the data but couldn't format it.");

                    // ── Improvement #20: Log bad responses ──
                    LogBadResponse(sanitized, userMessage);

                    // ── Cache the good response ──
                    _responseCache[cacheKey] = (sanitized, DateTime.UtcNow.Add(_cacheDuration));

                    return new ChatResponseDto { Message = sanitized, ServiceCalled = lastService, Timestamp = DateTime.UtcNow };
                }
                else
                {
                    // ── Direct text response (no tools) — include history for conversational context ──
                    // ── Improvement #3/#5: Only include user messages from history, summarized ──
                    var aiText = responseMessage.GetProperty("content").GetString();
                    var sanitized = SanitizeResponse(aiText ?? "How else can I help you?");

                    // ── Improvement #20: Log bad responses ──
                    LogBadResponse(sanitized, userMessage);

                    return new ChatResponseDto { Message = sanitized, ServiceCalled = "GeneralAI", Timestamp = DateTime.UtcNow };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error with AI Key. Trying next key.");
                if (key == _apiKeys.Last())
                {
                    return new ChatResponseDto { Message = "I'm having a temporary issue. Please try again shortly! 😊", ServiceCalled = "Error" };
                }
            }
        }

        return new ChatResponseDto { Message = "All AI services are busy right now. Please try again in a few minutes! 😊", ServiceCalled = "RateLimited" };
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

    // ── Improvement #17: Tool definitions with endpoint data descriptions ──
    private object[] GetDefinedTools()
    {
        return new object[]
        {
            new {
                type = "function",
                function = new {
                    name = "get_records",
                    description = @"Fetches a list of records from any endpoint. Endpoints and their fields:
'student': firstName, lastName, email, phone, rollNumber, dateOfBirth, gender, address, departmentId, admissionYear, status
'course': courseCode, courseName, description, credits, semester, departmentId, isActive
'fee': feeId, studentId, amount, description, status (Pending/Paid/Overdue), dueDate, paidDate
'attendance': studentId, courseId, date, isPresent
'enrollment': studentId, courseId, semester, enrollmentDate, status
'teacher': firstName, lastName, email, departmentId, specialization
'exam': examId, courseId, title, date, totalMarks
'grade': studentId, examId, marksObtained, grade
'department': name, code, isActive
'timeslot': courseId, teacherId, dayOfWeek, startTime, endTime, room",
                    parameters = new {
                        type = "object",
                        properties = new {
                            endpoint = new { type = "string", description = "The API endpoint: 'student','teacher','course','fee','attendance','enrollment','exam','grade','department','timeslot','notice','message','announcement'." },
                            filters = new { type = "object", description = "Optional filters e.g. {\"departmentId\":2, \"status\":\"Active\", \"pageSize\":1000}" }
                        },
                        required = new[] { "endpoint" }
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "get_record_by_id",
                    description = "Fetches a single record by its numeric ID from an endpoint. Use this when the user asks about a specific student, course, etc.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            endpoint = new { type = "string", description = "The endpoint name (e.g. 'student', 'course', 'fee')." },
                            id = new { type = "integer", description = "The numeric ID of the record." }
                        },
                        required = new[] { "endpoint", "id" }
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "create_record",
                    description = "Creates a new record in an endpoint.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            endpoint = new { type = "string", description = "The endpoint name." },
                            payload = new { type = "object", description = "The JSON data to create." }
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
                            endpoint = new { type = "string", description = "The endpoint name." },
                            id = new { type = "integer", description = "The ID to update." },
                            payload = new { type = "object", description = "The updated JSON data." }
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
                            endpoint = new { type = "string", description = "The endpoint name." },
                            id = new { type = "integer", description = "The ID to delete." }
                        },
                        required = new[] { "endpoint", "id" }
                    }
                }
            }
        };
    }

    // ── Improvement #16/#18: Pre-format data, 1500 char limit for single records ──
    private string PreProcessData(string jsonData, string functionName)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonData);
            var root = doc.RootElement;
            
            // ── For single record queries, allow more data through ──
            bool isSingleRecord = functionName == "get_record_by_id";
            int charLimit = isSingleRecord ? 1500 : 800;

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
                    { arrayElement = dataEl; isArray = true; }
                else if (root.TryGetProperty("$values", out var valEl) && valEl.ValueKind == JsonValueKind.Array)
                    { arrayElement = valEl; isArray = true; }
                else if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
                    { arrayElement = itemsEl; isArray = true; }
                else
                {
                    // ── Single object: pre-format key fields in C# ──
                    var formatted = PreFormatSingleRecord(root);
                    return formatted.Length > charLimit ? formatted.Substring(0, charLimit) + "...(truncated)" : formatted;
                }
            }
            else
            {
                arrayElement = root;
            }
            
            if (isArray)
            {
                int totalCount = arrayElement.GetArrayLength();
                
                // Show up to 5 samples for the AI
                var samples = new List<string>();
                int i = 0;
                foreach (var item in arrayElement.EnumerateArray())
                {
                    if (i >= 5) break;
                    string itemText = PreFormatSingleRecord(item);
                    if (itemText.Length > 300) itemText = itemText.Substring(0, 300) + "...";
                    samples.Add(itemText);
                    i++;
                }
                
                string res = $"TOTAL: {totalCount} records.\n{string.Join("\n---\n", samples)}";
                return res.Length > charLimit ? res.Substring(0, charLimit) + "...(truncated)" : res;
            }
            
            if (jsonData.Length > charLimit)
                return jsonData.Substring(0, charLimit) + "...(truncated)";
                
            return jsonData;
        }
        catch
        {
            return jsonData.Length > 1500 ? jsonData.Substring(0, 1500) + "...(truncated)" : jsonData;
        }
    }

    /// <summary>
    /// ── Improvement #16: Pre-format a single JSON record into readable key-value lines in C# ──
    /// This reduces the AI's formatting burden significantly.
    /// </summary>
    private string PreFormatSingleRecord(JsonElement record)
    {
        if (record.ValueKind != JsonValueKind.Object) return record.GetRawText();

        var sb = new StringBuilder();
        foreach (var prop in record.EnumerateObject())
        {
            var key = prop.Name;
            var val = prop.Value;

            // Skip null, empty, and internal navigation properties
            if (val.ValueKind == JsonValueKind.Null) continue;
            if (val.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(val.GetString())) continue;
            if (val.ValueKind == JsonValueKind.Object || val.ValueKind == JsonValueKind.Array) continue;

            // Format the value
            string displayVal;
            if (val.ValueKind == JsonValueKind.String)
            {
                var strVal = val.GetString() ?? "";
                // Try to parse as date
                if (DateTime.TryParse(strVal, out var dateVal) && strVal.Contains("T"))
                    displayVal = dateVal.ToString("MMM dd, yyyy");
                else
                    displayVal = strVal;
            }
            else
            {
                displayVal = val.GetRawText();
            }

            // Readable key name
            var readableKey = Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
            readableKey = char.ToUpper(readableKey[0]) + readableKey.Substring(1);

            sb.AppendLine($"{readableKey}: {displayVal}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Strips leaked function call tags, raw JSON blocks, credential patterns, and internal tool names.
    /// </summary>
    private string SanitizeResponse(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove <function=...>...</function> tags (hallucinated tool calls)
        text = Regex.Replace(text, @"<function=[^>]*>.*?</function>", "", RegexOptions.Singleline);
        // Remove standalone <function=...> without closing tag
        text = Regex.Replace(text, @"<function=[^>]*>[^<]*", "");
        // Remove any remaining raw JSON blocks {...}
        text = Regex.Replace(text, @"\{\s*""\w+""\s*:.*?\}", "", RegexOptions.Singleline);
        // Remove lines that mention internal tool names
        text = Regex.Replace(text, @"(?i)(get_records|get_record_by_id|create_record|update_record|delete_record)", "");
        // Remove lines asking user to "specify a function"
        text = Regex.Replace(text, @"(?i).*specify a function.*\n?", "");
        // Remove any leaked password patterns
        text = Regex.Replace(text, @"(?i)(password|passwd|pwd)\s*[:=]\s*\S+", "[REDACTED]");
        // Remove markdown code blocks
        text = Regex.Replace(text, @"```[\s\S]*?```", "");

        // Clean up multiple blank lines
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        return text.Trim();
    }

    /// <summary>
    /// ── Improvement #20: Log responses that contain bad patterns for review ──
    /// </summary>
    private void LogBadResponse(string response, string userMessage)
    {
        bool isBad = false;
        if (response.Contains("<function")) isBad = true;
        if (response.Contains("get_record")) isBad = true;
        if (response.Contains("specify a function")) isBad = true;
        if (Regex.IsMatch(response, @"\{.*""[a-zA-Z]+"".*:.*\}")) isBad = true;
        if (response.Contains("**")) isBad = true;

        if (isBad)
        {
            _logger.LogWarning("⚠️ BAD AI RESPONSE detected for query: '{Query}'. Response snippet: {Snippet}",
                userMessage, response.Length > 200 ? response.Substring(0, 200) : response);
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
