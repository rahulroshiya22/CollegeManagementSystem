namespace CMS.TelegramService.Utils;

public static class FormattingUtils
{
    // Footer Branding
    public const string Footer = "\n━━━━━━━━━━━━━━━━━━━━\n🎓 <i>CMS Bot v2.0</i>";
    public const string SignatureWatermark = "\n━━━━━━━━━━━━━━━━━━━━\n✅ <i>Verified by CMS Automated System</i>";

    // Format Currency
    public static string FormatCurrency(decimal amount)
    {
        return $"₹{amount:N2}";
    }
    
    // ASCII Progress Bar
    public static string GetProgressBar(double current, double max, int length)
    {
        if (max == 0) return new string('⬜', length);
        int filledLen = (int)Math.Round(length * current / max);
        if (filledLen > length) filledLen = length;
        if (filledLen < 0) filledLen = 0;
        return string.Concat(Enumerable.Repeat("🟩", filledLen)) + string.Concat(Enumerable.Repeat("⬜", length - filledLen));
    }

    // Color-coded status dots
    public static string GetStatusDot(double percentage)
    {
        if (percentage >= 80) return "🟢";
        if (percentage >= 50) return "🟡";
        return "🔴";
    }

    // Trend indicators
    public static string GetTrend(double current, double previous)
    {
        if (current > previous) return "📈";
        if (current < previous) return "📉";
        return "➖";
    }

    // Subject Badges
    public static string GetSubjectBadge(string subjectName)
    {
        var sn = subjectName.ToLower();
        if (sn.Contains("math")) return "📐";
        if (sn.Contains("sci") || sn.Contains("phys") || sn.Contains("chem")) return "🧬";
        if (sn.Contains("comp") || sn.Contains("prog") || sn.Contains("cs")) return "💻";
        if (sn.Contains("eng") || sn.Contains("lit") || sn.Contains("hist")) return "📚";
        if (sn.Contains("art") || sn.Contains("draw")) return "🎨";
        return "📘";
    }

    // Action Confirmation Emoji mapping
    public const string AddEmoji = "➕";
    public const string EditEmoji = "✏️";
    public const string DeleteEmoji = "🗑️";
    public const string SearchEmoji = "🔍";
    public const string ViewEmoji = "👁️";
}

