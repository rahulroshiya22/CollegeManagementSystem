using System.Text.Json;
using CMS.TelegramService.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using CMS.TelegramService.Utils;

namespace CMS.TelegramService.Handlers;

public class MenuHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;
    private const string ImgUrl = "https://i.ibb.co/jZPS8PfB/16852bce-e5a1-44d9-8e45-dc281923dd58-0-1.jpg";

    public MenuHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    {
        _bot = bot; _sessions = sessions; _api = api;
    }

    private static string GetGreeting()
    {
        var h = DateTime.Now.Hour;
        return h < 12 ? "Good Morning ☀️" : h < 17 ? "Good Afternoon 🌤️" : h < 21 ? "Good Evening 🌆" : "Hello 👋";
    }

    private static string GetRandomQuote()
    {
        var quotes = new[] { 
            "Education is the most powerful weapon which you can use to change the world.",
            "The beautiful thing about learning is that no one can take it away from you.",
            "Live as if you were to die tomorrow. Learn as if you were to live forever.",
            "The future belongs to those who believe in the beauty of their dreams."
        };
        return quotes[new Random().Next(quotes.Length)];
    }

    private static string FooterHtml => FormattingUtils.Footer;

    public async Task ShowDashboard(long chatId, long userId)
    {
        var role = _sessions.GetRole(userId) ?? "Student";
        var session = _sessions.Get(userId);
        var name = session?.Name ?? "User";
        var greeting = GetGreeting();
        var date = DateTime.Now.ToString("dd MMM yyyy");
        var quote = $"<blockquote expandable>💡 {GetRandomQuote()}</blockquote>";
        var kb = BuildMainKeyboard(role);
        string caption;

        if (role == "Admin")
        {
            try
            {
                var (sDataResp, _) = await _api.GetAsync(userId, "/api/student?PageSize=1");
                var (tDataResp, _) = await _api.GetAsync(userId, "/api/teacher?PageSize=1");
                var (cDataResp, _) = await _api.GetAsync(userId, "/api/course");
                
                var sData = sDataResp?.ValueKind == JsonValueKind.Object && sDataResp.Value.TryGetProperty("data", out var sd) ? sd : sDataResp;
                var tData = tDataResp?.ValueKind == JsonValueKind.Object && tDataResp.Value.TryGetProperty("data", out var td) ? td : tDataResp;
                var cData = cDataResp?.ValueKind == JsonValueKind.Object && cDataResp.Value.TryGetProperty("data", out var cd) ? cd : cDataResp;

                var sCount = sDataResp?.ValueKind == JsonValueKind.Object && sDataResp.Value.TryGetProperty("totalRecords", out var sr) ? sr.GetInt32() : (sData?.ValueKind == JsonValueKind.Array ? sData.Value.GetArrayLength() : 0);
                var tCount = tDataResp?.ValueKind == JsonValueKind.Object && tDataResp.Value.TryGetProperty("totalRecords", out var tr) ? tr.GetInt32() : (tData?.ValueKind == JsonValueKind.Array ? tData.Value.GetArrayLength() : 0);
                var cCount = cData?.ValueKind == JsonValueKind.Array ? cData.Value.GetArrayLength() : 0;

                var (fDataResp, _) = await _api.GetAsync(userId, "/api/fee?PageSize=1000");
                var fData = fDataResp?.ValueKind == JsonValueKind.Object && fDataResp.Value.TryGetProperty("data", out var fd) ? fd : fDataResp;
                decimal collected = 0, pending = 0;
                if (fData?.ValueKind == JsonValueKind.Array)
                {
                    foreach (var f in fData.Value.EnumerateArray())
                    {
                        var amt = f.TryGetProperty("amount", out var a) ? a.GetDecimal() : 0m;
                        var isPaid = f.TryGetProperty("isPaid", out var p) ? p.GetBoolean() : (f.TryGetProperty("status", out var s) && s.GetString() == "Paid");
                        if (isPaid) collected += amt; else pending += amt;
                    }
                }

                var (nDataResp, _) = await _api.GetAsync(userId, "/api/notice");
                var nData = nDataResp?.ValueKind == JsonValueKind.Object && nDataResp.Value.TryGetProperty("data", out var nd) ? nd : nDataResp;
                var activeNotices = nData?.ValueKind == JsonValueKind.Array ? nData.Value.GetArrayLength() : 0;

                caption = $"🏫 <b>College Admin Dashboard</b>\n📅 <i>{date}</i>\n━━━━━━━━━━━━━━━━━━━━\n" +
                          $"{greeting}, <b>Admin!</b>\n{quote}\n\n" +
                          $"📊 <b><u>System Status:</u></b>\n" +
                          $"👥 <b>Users:</b> <code>{sCount}</code> Students | <code>{tCount}</code> Teachers\n" +
                          $"⚖️ <b>Demographics:</b> <code>♂️ 55% | ♀️ 45%</code>\n" +
                          $"📚 <b>Courses:</b> <code>{cCount}</code> Active\n\n" +
                          $"💰 <b>Financials:</b>\n" +
                          $"✅ Collected: <b>{FormattingUtils.FormatCurrency(collected)}</b>\n" +
                          $"⏳ Pending: <b>{FormattingUtils.FormatCurrency(pending)}</b>\n\n" +
                          $"📢 <b>Updates:</b>\n" +
                          $"🔹 Active Notices: <code>{activeNotices}</code>\n" +
                          $"⚠️ Low Attendance: <code>5</code> Students\n\n" +
                          $"<i>🟢 System Online</i>{FooterHtml}";
            }
            catch
            {
                caption = $"🏫 <b>College Admin Dashboard</b>\n📅 <i>{date}</i>\n━━━━━━━━━━━━━━━━━━━━\n" +
                          $"{greeting}, <b>Admin!</b>\n<i>(Stats unavailable - Check Logs)</i>\n\n" +
                          $"🔴 <i>System Offline</i>{FooterHtml}";
            }
        }
        else if (role == "Teacher")
        {
            caption = $"👨‍🏫 <b>Teacher Dashboard</b>\n📅 <i>{date}</i>\n━━━━━━━━━━━━━━━━━━━━\n" +
                      $"{greeting}, <b>{name}!</b>\n{quote}\n\n" +
                      $"📋 <b><u>Today's Overview:</u></b>\n" +
                      $"🏫 <b>Classes:</b> <code>3</code> Scheduled\n" +
                      $"📝 <b>Pending Reviews:</b> <code>2</code> Exams{FooterHtml}";
        }
        else // Student
        {
            caption = $"🎓 <b>Student Dashboard</b>\n📅 <i>{date}</i>\n━━━━━━━━━━━━━━━━━━━━\n" +
                      $"{greeting}, <b>{name}!</b>\n{quote}\n\n" +
                      $"📈 <b><u>Your Status:</u></b>\n" +
                      $"✅ <b>Attendance:</b> <code>87% 🟢</code>\n" +
                      $"💰 <b>Fees:</b> <code>Paid ✅</code>\n\n" +
                      $"🔜 <b>Next Class:</b>\n" +
                      $"📚 <i>Mathematics (10:00 AM)</i>{FooterHtml}";
        }

        try { await _bot.SendPhoto(chatId, ImgUrl, caption: caption, parseMode: ParseMode.Html, replyMarkup: kb); }
        catch { await _bot.SendMessage(chatId, caption, parseMode: ParseMode.Html, replyMarkup: kb); }
    }

    public async Task ShowProfile(CallbackQuery query)
    {
        var userId = query.From.Id;
        var session = _sessions.Get(userId);
        if (session == null) { await _bot.SendMessage(query.Message!.Chat.Id, "Not logged in."); return; }

        var text = $"👤 <b>My Profile</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                   $"👤 <b>Name:</b> {session.Name}\n📧 <b>Email:</b> {session.Email}\n" +
                   $"🎭 <b>Role:</b> {session.Role}\n🆔 <b>User ID:</b> <code>{session.UserId}</code>";
        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "main_menu") } });
        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    public static InlineKeyboardMarkup BuildMainKeyboard(string role)
    {
        var rows = new List<InlineKeyboardButton[]>();

        if (role == "Admin")
        {
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("👥 Students", "admin_students"), InlineKeyboardButton.WithCallbackData("👨‍🏫 Teachers", "admin_teachers") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📚 Courses", "admin_courses"), InlineKeyboardButton.WithCallbackData("💰 Fees", "admin_fees") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("✅ Attendance", "admin_attendance"), InlineKeyboardButton.WithCallbackData("🗓️ Timetable", "admin_timetable") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📝 Exams", "admin_exams"), InlineKeyboardButton.WithCallbackData("📢 Notices", "admin_notices") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📢 Broadcasts", "admin_post_management"), InlineKeyboardButton.WithCallbackData("➕ Groups", "admin_add_group") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🎭 Impersonate", "admin_impersonate"), InlineKeyboardButton.WithWebApp("🌐 Admin WebApp", new WebAppInfo { Url = "https://raised-inexpensive-wage-wheels.trycloudflare.com/index.html" }) });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("👤 My Profile", "student_profile") });
        }
        else if (role == "Teacher")
        {
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📅 My Classes", "teacher_classes"), InlineKeyboardButton.WithCallbackData("✅ Take Attendance", "teacher_attendance") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📝 Create Exam", "teacher_exams"), InlineKeyboardButton.WithCallbackData("🗓️ Timetable", "my_timetable") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("👤 My Profile", "student_profile") });
        }
        else // Student
        {
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("👤 My Profile", "student_profile"), InlineKeyboardButton.WithCallbackData("📊 My Results", "student_results") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📅 My Attendance", "student_attendance"), InlineKeyboardButton.WithCallbackData("🗓️ Timetable", "student_timetable") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("💰 My Fees", "student_fees"), InlineKeyboardButton.WithCallbackData("🚀 Student Hub", "student_hub") });
        }

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔄 Refresh Dashboard", "main_menu") });
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🚪 Logout", "auth_logout") });

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup PaginationKeyboard(int page, int totalPages, string prefix)
    {
        var row = new List<InlineKeyboardButton>();
        if (page > 1) row.Add(InlineKeyboardButton.WithCallbackData("⬅️ Prev", $"{prefix}_page_{page - 1}"));
        row.Add(InlineKeyboardButton.WithCallbackData($"📄 {page}/{totalPages}", "noop"));
        if (page < totalPages) row.Add(InlineKeyboardButton.WithCallbackData("Next ➡️", $"{prefix}_page_{page + 1}"));
        return new InlineKeyboardMarkup(new[] { row.ToArray(), new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Menu", "main_menu") }, new[] { InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") } });
    }

    public static InlineKeyboardMarkup BackButton(string callbackData = "main_menu")
        => new(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", callbackData) }, new[] { InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") } });
}
