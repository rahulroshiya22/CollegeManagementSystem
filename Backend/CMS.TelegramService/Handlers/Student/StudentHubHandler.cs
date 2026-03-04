using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Student;

public class StudentHubHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public StudentHubHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        if (data == "extras_timetable_weekly") { await ShowTimetable(query); return; }
        if (data == "student_hub" || data == "student_timetable" || data.StartsWith("extras_")) { await ExtrasMenu(query); return; }
    }

    private async Task ShowTimetable(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        await _bot.SendChatActionAsync(chatId, ChatAction.Typing);

        Message waitMsg;
        try { waitMsg = await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, "🔄 Loading your schedule..."); }
        catch { waitMsg = await _bot.SendMessage(chatId, "🔄 Loading your schedule..."); }

        var session = _sessions.Get(userId);
        var sid = session?.UserId ?? "";

        var (eResp, err1) = await _api.GetAsync(userId, "/api/enrollment?PageSize=100");
        var (tResp, err2) = await _api.GetAsync(userId, "/api/timeslot?PageSize=1000");
        var (cResp, _) = await _api.GetAsync(userId, "/api/course?PageSize=100");

        await _bot.DeleteMessage(chatId, waitMsg.MessageId);
        
        if (err1 != null || err2 != null || string.IsNullOrEmpty(sid))
        {
            var errTxt = $"❌ <b>Error loading schedule.</b>{FormattingUtils.Footer}";
            try { await _bot.EditMessageTextAsync(chatId, waitMsg.MessageId, errTxt, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("student_hub")); }
            catch { await _bot.SendMessage(chatId, errTxt, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("student_hub")); }
            return;
        }

        var enrollments = eResp?.ValueKind == JsonValueKind.Object && eResp.Value.TryGetProperty("data", out var d) ? d : eResp;
        var timeslots = tResp?.ValueKind == JsonValueKind.Object && tResp.Value.TryGetProperty("data", out var td) ? td : tResp;
        var coursesData = cResp?.ValueKind == JsonValueKind.Object && cResp.Value.TryGetProperty("data", out var cd) ? cd : cResp;

        var myCourseIds = new HashSet<string>();
        if (enrollments?.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in enrollments.Value.EnumerateArray())
            {
                if (e.Str("studentId") == sid)
                    myCourseIds.Add(e.Str("courseId"));
            }
        }

        var cMap = new Dictionary<string, string>();
        if (coursesData?.ValueKind == JsonValueKind.Array)
            foreach (var c in coursesData.Value.EnumerateArray())
                cMap[c.Str("courseId", c.Str("id"))] = c.Str("courseCode", c.Str("name"));

        var schedule = new Dictionary<string, List<JsonElement>>();
        string[] days = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
        foreach (var day in days) schedule[day] = new List<JsonElement>();

        if (timeslots?.ValueKind == JsonValueKind.Array)
        {
            foreach (var ts in timeslots.Value.EnumerateArray())
            {
                var cid = ts.Str("courseId");
                var dOfWeek = ts.Str("dayOfWeek");
                if (myCourseIds.Contains(cid) && schedule.ContainsKey(dOfWeek))
                    schedule[dOfWeek].Add(ts);
            }
        }

        var text = $"🏠 <b>Home</b> > 👨‍🎓 <b>Student Hub</b> > 📅 <b>My Weekly Schedule</b>\n━━━━━━━━━━━━━━━━━━━━\n";
        bool hasClasses = false;

        foreach (var day in days)
        {
            var daySlots = schedule[day];
            if (daySlots.Count == 0) continue;
            
            hasClasses = true;
            daySlots.Sort((a, b) => string.Compare(a.Str("startTime"), b.Str("startTime")));
            
            text += $"\n🗓 <b>{day}</b>\n";
            foreach (var s in daySlots)
            {
                var start = s.Str("startTime").Length >= 5 ? s.Str("startTime").Substring(0, 5) : s.Str("startTime");
                var end = s.Str("endTime").Length >= 5 ? s.Str("endTime").Substring(0, 5) : s.Str("endTime");
                var room = s.Str("room");
                var cName = cMap.TryGetValue(s.Str("courseId"), out var n) ? n : "Class";
                var badge = FormattingUtils.GetSubjectBadge(cName);
                
                text += $" ⏰ {start}-{end} | {badge} {cName} (Rm: {room})\n";
            }
        }

        if (!hasClasses) text += "\n📭 <i>No classes found for your enrolled courses.</i>";
        text += FormattingUtils.Footer;

        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Student Hub", "student_hub") }, new[] { InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") } });
        
        try { await _bot.EditMessageTextAsync(chatId, waitMsg.MessageId, text, parseMode: ParseMode.Html, replyMarkup: kb); }
        catch { await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: kb); }
    }

    private async Task ExtrasMenu(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id;

        bool examMode = _sessions.GetData<bool>(query.From.Id, "tt_exam_mode");
        if (query.Data == "extras_tt_toggle_mode") 
        { 
            examMode = !examMode; 
            _sessions.SetData(query.From.Id, "tt_exam_mode", examMode); 
        }

        string modeText = examMode ? "Exam Mode: ON" : "Exam Mode: OFF";
        var toggleBtn = InlineKeyboardButton.WithCallbackData($"🔄 {modeText}", "extras_tt_toggle_mode");

        List<InlineKeyboardButton[]> kb;
        string text;

        if (examMode)
        {
            kb = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("📅 Exam Schedule", "noop"), InlineKeyboardButton.WithWebApp("💺 Seating Plan", new WebAppInfo { Url = "https://raised-inexpensive-wage-wheels.trycloudflare.com/index.html" }) },
                new[] { toggleBtn },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "main_menu"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") }
            };
            text = $"🏠 <b>Home</b> > 📝 <b>Exam Dashboard</b>\n\nGood luck with your preparations! 🍀{FormattingUtils.Footer}";
        }
        else
        {
            kb = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("📍 Where am I?", "noop"), InlineKeyboardButton.WithCallbackData("⏩ Next Class", "noop") },
                new[] { InlineKeyboardButton.WithCallbackData("📅 Day View", "noop"), InlineKeyboardButton.WithWebApp("🗓️ Web Timetable", new WebAppInfo { Url = "https://raised-inexpensive-wage-wheels.trycloudflare.com/index.html" }) },
                new[] { InlineKeyboardButton.WithCallbackData("🕒 Gaps & Breaks", "noop"), InlineKeyboardButton.WithCallbackData("🔔 Set Reminder", "noop") },
                new[] { InlineKeyboardButton.WithCallbackData("🏃 I'm Late", "noop"), InlineKeyboardButton.WithCallbackData("👥 Study Buddy", "noop") },
                new[] { toggleBtn },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "main_menu"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") }
            };
            text = $"🏠 <b>Home</b> > 🚀 <b>Student Hub</b>\n━━━━━━━━━━━━━━━━━━━━\nManage your schedule smarter.{FormattingUtils.Footer}";
        }

        try { await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb)); }
        catch { await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb)); }
    }
}
