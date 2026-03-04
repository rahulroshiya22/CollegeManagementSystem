using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Teacher;

public class TeacherAttendanceHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public TeacherAttendanceHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        if (data == "teacher_attendance") { await StartAttendance(query); return; }
        if (data.StartsWith("tch_att_course_")) { await SelectDate(query, data.Replace("tch_att_course_", "")); return; }
        if (data.StartsWith("tch_att_date_")) { await FetchStudentsForMarking(query, data.Replace("tch_att_date_", "")); return; }
        if (data.StartsWith("tch_toggle_att_")) { await ToggleStudentStatus(query, data.Replace("tch_toggle_att_", "")); return; }
        if (data == "tch_submit_att") { await SubmitAttendance(query); return; }
        if (data == "cancel_tch_att") { await CancelAtt(query); return; }
    }

    private async Task StartAttendance(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        try { await query.Message.Delete(_bot); } catch { }
        
        var (data, error) = await _api.GetAsync(userId, "/api/course");
        if (error != null) { await _bot.SendMessage(chatId, $"❌ {error}", replyMarkup: MenuHandler.BackButton()); return; }

        var kb = new List<InlineKeyboardButton[]>();
        if (data?.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in data.Value.EnumerateArray())
            {
                var cname = c.Str("courseCode", c.Str("name"));
                var cid = c.Str("courseId", c.Str("id"));
                var badge = FormattingUtils.GetSubjectBadge(cname);
                if (!string.IsNullOrEmpty(cid))
                    kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"{badge} {cname}", $"tch_att_course_{cid}") });
            }
        }

        if (kb.Count == 0)
        {
            await _bot.SendMessage(chatId, "🚫 No courses found to take attendance for.", replyMarkup: MenuHandler.BackButton());
            return;
        }

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Cancel", "main_menu"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") });
        await _bot.SendMessage(chatId, $"🏠 <b>Home</b> > 👨‍🏫 <b>Teacher Dash</b> > 📅 <b>Attendance</b>\n━━━━━━━━━━━━━━━━━━━━\nSelect a course to mark attendance:{FormattingUtils.Footer}", 
            parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task SelectDate(CallbackQuery query, string courseId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        _sessions.SetData(userId, "att_course_id", courseId);
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData($"📅 Today ({today})", $"tch_att_date_{today}") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Cancel", "cancel_tch_att"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") }
        });

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(chatId, $"🏠 <b>Home</b> > 👨‍🏫 <b>Teacher Dash</b> > 📅 <b>Select Date</b>\n━━━━━━━━━━━━━━━━━━━━\nWhich day are you marking attendance for?{FormattingUtils.Footer}", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task FetchStudentsForMarking(CallbackQuery query, string date)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
        _sessions.SetData(userId, "att_date", date);
        var courseId = _sessions.GetData<string>(userId, "att_course_id");

        Message wait;
        try { wait = await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, "🔄 Loading students..."); }
        catch { wait = await _bot.SendMessage(chatId, "🔄 Loading students..."); }

        // Optimized logic matching Python
        var (enrollData, err1) = await _api.GetAsync(userId, "/api/enrollment?PageSize=1000");
        var (studentData, err2) = await _api.GetAsync(userId, "/api/student?PageSize=1000");

        await _bot.DeleteMessage(chatId, wait.MessageId);

        if (err1 != null || err2 != null) 
        { 
            string errTxt = $"❌ <b>Action Failed</b>\nError loading data.{FormattingUtils.Footer}";
            await _bot.SendMessage(chatId, errTxt, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton()); 
            return; 
        }

        var studentMap = new Dictionary<string, string>();
        if (studentData?.ValueKind == JsonValueKind.Array)
            foreach (var s in studentData.Value.EnumerateArray())
                studentMap[s.Str("studentId", s.Str("id"))] = $"{s.Str("firstName")} {s.Str("lastName")}";

        var markingState = new Dictionary<string, bool>();
        if (enrollData?.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in enrollData.Value.EnumerateArray())
            {
                if (e.Str("courseId") == courseId)
                {
                    var sid = e.Str("studentId");
                    if (studentMap.ContainsKey(sid))
                        markingState[sid] = true; // Default present
                }
            }
        }

        if (markingState.Count == 0)
        {
            await _bot.SendMessage(chatId, "🚫 No students enrolled in this course.", replyMarkup: MenuHandler.BackButton());
            return;
        }

        // Convert the dictionaries to JSON strings to save them in session state
        _sessions.SetData(userId, "att_marking", JsonSerializer.Serialize(markingState));
        _sessions.SetData(userId, "att_names", JsonSerializer.Serialize(studentMap));

        await RenderMarkingList(chatId, userId);
    }

    private async Task RenderMarkingList(long chatId, long userId)
    {
        var courseId = _sessions.GetData<string>(userId, "att_course_id");
        var date = _sessions.GetData<string>(userId, "att_date");
        var markingRaw = _sessions.GetData<string>(userId, "att_marking") ?? "{}";
        var namesRaw = _sessions.GetData<string>(userId, "att_names") ?? "{}";

        var marking = JsonSerializer.Deserialize<Dictionary<string, bool>>(markingRaw)!;
        var names = JsonSerializer.Deserialize<Dictionary<string, string>>(namesRaw)!;

        int presentCount = marking.Values.Count(v => v);
        int absentCount = marking.Count - presentCount;

        var sb = new StringBuilder($"🏠 <b>Home</b> > 📅 <b>Attd</b> > 📝 <b>Marking</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                                   $"📘 <b>Course:</b> {courseId}\n📅 <b>Date:</b> {date}\n\n" +
                                   $"🟢 <b>Present:</b> {presentCount}  |  🔴 <b>Absent:</b> {absentCount}\n━━━━━━━━━━━━━━━━━━━━\n" +
                                   $"👇 Tap to toggle status:\n");

        var kb = new List<InlineKeyboardButton[]>();
        foreach (var kv in marking)
        {
            var sid = kv.Key;
            var isPresent = kv.Value;
            var statusIcon = isPresent ? "✅" : "❌";
            var name = names.TryGetValue(sid, out var n) ? n : $"Student {sid}";
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"{statusIcon} {name}", $"tch_toggle_att_{sid}") });
        }

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("💾 SUBMIT ATTENDANCE", "tch_submit_att") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Cancel", "cancel_tch_att"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") });

        // Try getting last msg id, if not possible just send new one
        var mId = _sessions.GetData<long>(userId, "tch_att_msg_id");
        if (mId > 0)
        {
            try { 
                var m = await _bot.EditMessageText(chatId, (int)mId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
                return;
            } catch { }
        }

        var newM = await _bot.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
        _sessions.SetData(userId, "tch_att_msg_id", newM.MessageId);
    }

    private async Task ToggleStudentStatus(CallbackQuery query, string sid)
    {
        var userId = query.From.Id;

        var markingRaw = _sessions.GetData<string>(userId, "att_marking") ?? "{}";
        var marking = JsonSerializer.Deserialize<Dictionary<string, bool>>(markingRaw)!;

        if (marking.ContainsKey(sid))
            marking[sid] = !marking[sid];

        _sessions.SetData(userId, "att_marking", JsonSerializer.Serialize(marking));
        
        // Show temporary toast via AnswerCallbackQuery without blocking chat
        var ns = marking[sid] ? "Present 🟢" : "Absent 🔴";
        await _bot.AnswerCallbackQuery(query.Id, $"Marked {ns}", showAlert: false);

        await RenderMarkingList(query.Message!.Chat.Id, userId);
    }

    private async Task SubmitAttendance(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        await _bot.AnswerCallbackQuery(query.Id, "Submitting...");

        var date = _sessions.GetData<string>(userId, "att_date");
        if (!int.TryParse(_sessions.GetData<string>(userId, "att_course_id"), out var courseId)) courseId = 0;

        var markingRaw = _sessions.GetData<string>(userId, "att_marking") ?? "{}";
        var marking = JsonSerializer.Deserialize<Dictionary<string, bool>>(markingRaw)!;

        int success = 0, errors = 0;
        foreach (var kv in marking)
        {
            if (!int.TryParse(kv.Key, out var sid)) continue;
            var payload = new { studentId = sid, courseId, date, isPresent = kv.Value, remarks = "" };
            var (_, err) = await _api.PostAsync(userId, "/api/attendance", payload);
            if (err == null) success++; else errors++;
        }

        try { await query.Message!.Delete(_bot); } catch { }
        string resText = $"✅ <b>Attendance Saved!</b>\nSuccess: {success}\nFailed: {errors}{FormattingUtils.Footer}";
        await _bot.SendMessage(chatId, resText, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton());
        
        // Self-Destructing Success Toast for immediate feedback
        await _bot.AnswerCallbackQuery(query.Id, $"Success: {success}, Failed: {errors}", showAlert: false);
        
        _sessions.SetData(userId, "tch_att_msg_id", 0);
    }

    private async Task CancelAtt(CallbackQuery query)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, "🚫 Attendance Cancelled.", replyMarkup: MenuHandler.BackButton());
        _sessions.SetData(query.From.Id, "tch_att_msg_id", 0);
    }
}
