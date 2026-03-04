using System.Text.Json;

namespace CMS.TelegramService.Utils;

public class GroupEntry
{
    public string Title { get; set; } = "";
    public string Department { get; set; } = "All";
    public string Semester { get; set; } = "All";
    public string Category { get; set; } = "All";
    public long AddedBy { get; set; }
}

public static class GroupDb
{
    private static readonly string DbFile = Path.Combine(AppContext.BaseDirectory, "data", "registered_groups.json");
    private static readonly string TrackFile = Path.Combine(AppContext.BaseDirectory, "data", "tracked_chats.json");

    private static void EnsureDir() => Directory.CreateDirectory(Path.GetDirectoryName(DbFile)!);

    public static Dictionary<string, GroupEntry> GetAll()
    {
        EnsureDir();
        if (!File.Exists(DbFile)) return new();
        try { return JsonSerializer.Deserialize<Dictionary<string, GroupEntry>>(File.ReadAllText(DbFile)) ?? new(); }
        catch { return new(); }
    }

    public static void Save(string chatId, string title, string dept, string sem, string cat, long addedBy)
    {
        var db = GetAll();
        db[chatId] = new GroupEntry { Title = title, Department = dept, Semester = sem, Category = cat, AddedBy = addedBy };
        EnsureDir();
        File.WriteAllText(DbFile, JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static bool Delete(string chatId)
    {
        var db = GetAll();
        bool removed = db.Remove(chatId);
        if (removed) File.WriteAllText(DbFile, JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true }));
        return removed;
    }

    public static List<string> GetFiltered(string dept, string sem, string cat)
    {
        return GetAll()
            .Where(kv =>
                (dept == "All" || kv.Value.Department == "All" || kv.Value.Department == dept) &&
                (sem == "All" || kv.Value.Semester == "All" || kv.Value.Semester == sem) &&
                (cat == "All" || kv.Value.Category == "All" || kv.Value.Category == cat))
            .Select(kv => kv.Key)
            .ToList();
    }

    // Tracked chats (all groups bot is in)
    public static Dictionary<string, TrackedChat> GetTracked()
    {
        EnsureDir();
        if (!File.Exists(TrackFile)) return new();
        try { return JsonSerializer.Deserialize<Dictionary<string, TrackedChat>>(File.ReadAllText(TrackFile)) ?? new(); }
        catch { return new(); }
    }

    public static void TrackChat(long chatId, string title, string type)
    {
        var db = GetTracked();
        db[chatId.ToString()] = new TrackedChat { Title = title, Type = type };
        EnsureDir();
        File.WriteAllText(TrackFile, JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void UntrackChat(long chatId)
    {
        var db = GetTracked();
        db.Remove(chatId.ToString());
        File.WriteAllText(TrackFile, JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public class TrackedChat
{
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
}
