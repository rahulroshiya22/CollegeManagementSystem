using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CMS.TelegramService.Services;

/// <summary>
/// HTTP client wrapper that mirrors Python's APIClient.
/// Automatically injects the current user's JWT token.
/// </summary>
public class ApiService
{
    private readonly IHttpClientFactory _factory;
    private readonly SessionService _sessions;
    private readonly string _baseUrl;

    public ApiService(IHttpClientFactory factory, SessionService sessions, IConfiguration config)
    {
        _factory = factory;
        _sessions = sessions;
        _baseUrl = config["BotConfiguration:ApiBaseUrl"] ?? "https://localhost:7000";
    }

    private HttpClient CreateClient(long telegramId)
    {
        var client = _factory.CreateClient("api");
        client.BaseAddress = new Uri(_baseUrl);
        var token = _sessions.GetToken(telegramId);
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<(JsonElement? data, string? error)> HandleResponseAsync(HttpResponseMessage resp, long telegramId, bool raw = false)
    {
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _sessions.ClearSession(telegramId);
            return (null, "Unauthorized. Please /start and login again.");
        }
        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent)
            return (null, null);

        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            try
            {
                var err = JsonDocument.Parse(body).RootElement;
                var msg = err.TryGetProperty("message", out var m) ? m.GetString()
                        : err.TryGetProperty("error", out var e) ? e.GetString()
                        : $"Error {(int)resp.StatusCode}";
                return (null, msg);
            }
            catch { return (null, $"Error {(int)resp.StatusCode}: {body[..Math.Min(100, body.Length)]}"); }
        }

        if (string.IsNullOrWhiteSpace(body)) return (null, null);

        try
        {
            var doc = JsonDocument.Parse(body).RootElement;
            return (doc, null);
        }
        catch { return (null, $"Invalid response: {body[..Math.Min(80, body.Length)]}"); }
    }

    public async Task<(JsonElement? data, string? error)> GetAsync(long telegramId, string endpoint, Dictionary<string, string>? query = null)
    {
        var client = CreateClient(telegramId);
        var url = endpoint;
        if (query?.Count > 0)
            url += "?" + string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        try
        {
            var resp = await client.GetAsync(url);
            return await HandleResponseAsync(resp, telegramId);
        }
        catch (Exception ex) { return (null, $"Connection failed: {ex.Message}"); }
    }

    public async Task<(JsonElement? data, string? error)> GetRawAsync(long telegramId, string endpoint, Dictionary<string, string>? query = null)
    {
        var client = CreateClient(telegramId);
        var url = endpoint;
        if (query?.Count > 0)
            url += "?" + string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        try
        {
            var resp = await client.GetAsync(url);
            return await HandleResponseAsync(resp, telegramId, raw: true);
        }
        catch (Exception ex) { return (null, $"Connection failed: {ex.Message}"); }
    }

    public async Task<(JsonElement? data, string? error)> PostAsync(long telegramId, string endpoint, object payload)
    {
        var client = CreateClient(telegramId);
        var json = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        try
        {
            var resp = await client.PostAsync(endpoint, json);
            return await HandleResponseAsync(resp, telegramId);
        }
        catch (Exception ex) { return (null, $"Connection failed: {ex.Message}"); }
    }

    public async Task<(JsonElement? data, string? error)> PutAsync(long telegramId, string endpoint, object payload)
    {
        var client = CreateClient(telegramId);
        var json = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        try
        {
            var resp = await client.PutAsync(endpoint, json);
            return await HandleResponseAsync(resp, telegramId);
        }
        catch (Exception ex) { return (null, $"Connection failed: {ex.Message}"); }
    }

    public async Task<(JsonElement? data, string? error)> DeleteAsync(long telegramId, string endpoint)
    {
        var client = CreateClient(telegramId);
        try
        {
            var resp = await client.DeleteAsync(endpoint);
            return await HandleResponseAsync(resp, telegramId);
        }
        catch (Exception ex) { return (null, $"Connection failed: {ex.Message}"); }
    }

    /// <summary>Auth login — no JWT needed, returns token + user object</summary>
    public async Task<(JsonElement? data, string? error)> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient("api");
        client.BaseAddress = new Uri(_baseUrl);
        var payload = new StringContent(JsonSerializer.Serialize(new { email, password }), Encoding.UTF8, "application/json");
        try
        {
            var resp = await client.PostAsync("/api/auth/login", payload);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                try { var e = JsonDocument.Parse(body).RootElement; return (null, e.TryGetProperty("message", out var m) ? m.GetString() : "Login failed."); }
                catch { return (null, "Login failed."); }
            }
            return (JsonDocument.Parse(body).RootElement, null);
        }
        catch (Exception ex) { return (null, $"Connection failed: {ex.Message}"); }
    }
}
