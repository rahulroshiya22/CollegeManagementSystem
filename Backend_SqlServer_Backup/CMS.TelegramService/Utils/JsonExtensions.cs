using System.Text.Json;

namespace CMS.TelegramService.Utils;

/// <summary>
/// Extension methods on JsonElement to safely read values regardless
/// of whether the API returns them as strings or numbers.
/// This fixes the "element type 'String' but target is 'Number'" error.
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// Safely get a string value from a JsonElement property.
    /// Works whether the value is a JSON string OR a number.
    /// </summary>
    public static string Str(this JsonElement elem, string property, string fallback = "")
    {
        if (elem.ValueKind != JsonValueKind.Object) return fallback;
        if (!elem.TryGetProperty(property, out var val)) return fallback;
        return val.ValueKind switch
        {
            JsonValueKind.String => val.GetString() ?? fallback,
            JsonValueKind.Number => val.GetRawText(),       // 42 → "42"
            JsonValueKind.True   => "true",
            JsonValueKind.False  => "false",
            JsonValueKind.Null   => fallback,
            _                   => val.GetRawText()
        };
    }

    /// <summary>Get a decimal property (handles both string and number JSON types).</summary>
    public static decimal Dec(this JsonElement elem, string property, decimal fallback = 0)
    {
        if (elem.ValueKind != JsonValueKind.Object) return fallback;
        if (!elem.TryGetProperty(property, out var val)) return fallback;
        if (val.ValueKind == JsonValueKind.Number) return val.GetDecimal();
        if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out var d)) return d;
        return fallback;
    }

    /// <summary>Get an int property (handles both string and number JSON types).</summary>
    public static int Int(this JsonElement elem, string property, int fallback = 0)
    {
        if (elem.ValueKind != JsonValueKind.Object) return fallback;
        if (!elem.TryGetProperty(property, out var val)) return fallback;
        if (val.ValueKind == JsonValueKind.Number) return val.GetInt32();
        if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var i)) return i;
        return fallback;
    }

    /// <summary>Get a bool property safely.</summary>
    public static bool Bool(this JsonElement elem, string property, bool fallback = false)
    {
        if (elem.ValueKind != JsonValueKind.Object) return fallback;
        if (!elem.TryGetProperty(property, out var val)) return fallback;
        if (val.ValueKind == JsonValueKind.True) return true;
        if (val.ValueKind == JsonValueKind.False) return false;
        if (val.ValueKind == JsonValueKind.String)
        {
            var s = val.GetString() ?? "";
            return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";
        }
        if (val.ValueKind == JsonValueKind.Number) return val.GetInt32() != 0;
        return fallback;
    }

    /// <summary>Trim a date string from "2024-01-15T00:00:00" to "2024-01-15".</summary>
    public static string Date(this JsonElement elem, string property)
    {
        var raw = elem.Str(property);
        return raw.Contains('T') ? raw.Split('T')[0] : raw;
    }
}
