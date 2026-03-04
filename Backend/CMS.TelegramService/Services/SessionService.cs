using System.Collections.Concurrent;
using System.Text.Json;
using CMS.TelegramService.Models;

namespace CMS.TelegramService.Services;

public class SessionService
{
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();
    private readonly string _sessionFile = "bot_sessions.json";

    public SessionService()
    {
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_sessionFile)) return;
        try
        {
            var json = File.ReadAllText(_sessionFile);
            var data = JsonSerializer.Deserialize<Dictionary<long, UserSession>>(json);
            if (data != null)
                foreach (var kv in data)
                    _sessions[kv.Key] = kv.Value;
        }
        catch { /* ignore corrupt file */ }
    }

    private void Save()
    {
        try { File.WriteAllText(_sessionFile, JsonSerializer.Serialize(_sessions)); }
        catch { }
    }

    public UserSession? Get(long telegramId) =>
        _sessions.TryGetValue(telegramId, out var s) ? s : null;

    public string? GetToken(long telegramId) => Get(telegramId)?.Token;
    public string? GetRole(long telegramId) => Get(telegramId)?.Role;
    public bool IsLoggedIn(long telegramId) => _sessions.ContainsKey(telegramId) && !string.IsNullOrEmpty(GetToken(telegramId));

    public void SaveSession(long telegramId, string token, dynamic userData)
    {
        var session = new UserSession
        {
            Token = token,
            Role = userData.GetProperty("role").GetString() ?? "Student",
            UserId = userData.GetProperty("userId").GetString() ?? "",
            Email = userData.GetProperty("email").GetString() ?? "",
            Name = userData.GetProperty("firstName").GetString() ?? "User"
        };
        _sessions[telegramId] = session;
        Save();
    }

    public void SaveSessionFromElement(long telegramId, string token, JsonElement userData)
    {
        _sessions[telegramId] = new UserSession
        {
            Token = token,
            Role = SafeGetString(userData, "role") ?? "Student",
            UserId = SafeGetString(userData, "userId") ?? "",
            Email = SafeGetString(userData, "email") ?? "",
            Name = SafeGetString(userData, "firstName") ?? "User"
        };
        Save();
    }

    /// <summary>Safely reads a JSON property as string, handling both "string" and number types.</summary>
    private static string? SafeGetString(JsonElement elem, string property)
    {
        if (!elem.TryGetProperty(property, out var val)) return null;
        return val.ValueKind switch
        {
            JsonValueKind.String => val.GetString(),
            JsonValueKind.Number => val.GetRawText(), // e.g. userId = 42 → "42"
            JsonValueKind.True   => "true",
            JsonValueKind.False  => "false",
            JsonValueKind.Null   => null,
            _ => val.GetRawText()
        };
    }

    public void ClearSession(long telegramId)
    {
        _sessions.TryRemove(telegramId, out _);
        Save();
    }

    // Conversation state helpers — works even BEFORE the user is logged in
    public void SetState(long telegramId, string state)
    {
        // Create a minimal entry if none exists (e.g., during login flow)
        var session = _sessions.GetOrAdd(telegramId, _ => new UserSession());
        session.ConversationState = state;
    }

    public string? GetState(long telegramId) =>
        _sessions.TryGetValue(telegramId, out var s) ? s.ConversationState : null;

    public void ClearState(long telegramId)
    {
        if (_sessions.TryGetValue(telegramId, out var s))
        {
            s.ConversationState = null;
            s.ConversationData.Clear();
        }
    }

    public void SetData(long telegramId, string key, object value)
    {
        // Create a minimal entry if none exists (e.g., during login flow)
        var session = _sessions.GetOrAdd(telegramId, _ => new UserSession());
        session.ConversationData[key] = value;
    }

    public T? GetData<T>(long telegramId, string key)
    {
        if (_sessions.TryGetValue(telegramId, out var s) && s.ConversationData.TryGetValue(key, out var val))
        {
            if (val is JsonElement je) return JsonSerializer.Deserialize<T>(je.GetRawText());
            return (T)val;
        }
        return default;
    }

    // Reverse lookup: Find Telegram ID by backend userId
    public long? GetTelegramIdByUserId(string userId)
    {
        foreach (var kv in _sessions)
            if (kv.Value.UserId == userId)
                return kv.Key;
        return null;
    }

    public void Impersonate(long adminTgId, JsonElement targetUser)
    {
        if (!_sessions.TryGetValue(adminTgId, out var s)) return;
        s.Role = targetUser.TryGetProperty("role", out var r) ? r.GetString() ?? "Student" : "Student";
        s.UserId = targetUser.TryGetProperty("userId", out var uid) ? uid.GetString() ?? "" : "";
        s.Name = targetUser.TryGetProperty("firstName", out var fn) ? fn.GetString() ?? "User" : "User";
        s.Email = targetUser.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
        s.IsImpersonating = true;
        Save();
    }
}
