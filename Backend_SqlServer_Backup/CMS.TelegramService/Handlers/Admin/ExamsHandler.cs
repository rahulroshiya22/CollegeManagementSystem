using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class ExamsHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;
    
    public ExamsHandler(ITelegramBotClient bot, SessionService sessions, ApiService api) 
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        
        if (data == "admin_exams") { await ListExamsDashboard(query); return; }
        if (data == "admin_exam_depts") { await ListExamDepartments(query); return; }
        if (data.StartsWith("admin_exam_dept_")) { await ListExamCourses(query, data.Replace("admin_exam_dept_", "")); return; }
        if (data.StartsWith("admin_exam_list_")) { await ListCourseExams(query, data.Replace("admin_exam_list_", "")); return; }
        if (data.StartsWith("view_exam_results_")) { await ViewExamResults(query, data.Replace("view_exam_results_", "")); return; }
        if (data.StartsWith("view_exam_")) { await ViewExamDetail(query, data.Replace("view_exam_", "")); return; }
        if (data.StartsWith("pub_exam_")) { await PublishExamResults(query, data.Replace("pub_exam_", "")); return; }
        
        // Basic list views for Upcoming/Past (simplified fallback)
        if (data == "admin_exams_upcoming" || data == "admin_exams_past") { await ListExamsFiltered(query, data == "admin_exams_upcoming"); return; }
    }

    private async Task ListExamsDashboard(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
        var (resp, err) = await _api.GetAsync(userId, "/api/exam");

        var exams = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        int total = 0, upcoming = 0, past = 0, published = 0;
        var now = DateTime.UtcNow.Date;

        if (exams.ValueKind == JsonValueKind.Array)
        {
            total = exams.GetArrayLength();
            foreach (var e in exams.EnumerateArray())
            {
                var dateStr = e.Str("scheduledDate", "-");
                var isPub = e.Bool("isPublished");
                if (isPub) published++;
                
                if (DateTime.TryParse(dateStr, out var d))
                {
                    if (d.Date >= now) upcoming++;
                    else past++;
                }
                else upcoming++; // default
            }
        }

        var text = $"🏠 <b>Home</b> > 👨‍💼 <b>Admin Dash</b> > 📝 <b>Exam Management</b>\n━━━━━━━━━━━━━━━━━━━━\nManage schedules, results, and view exam details.\n\n" +
                   $"📊 <b>Overview</b>\n🔹 Total Exams: <b>{total}</b>\n🗓️ Upcoming: <b>{upcoming}</b>\n" +
                   $"🏁 Completed: <b>{past}</b>\n📢 Published: <b>{published}</b>\n\n👇 <b>Select an option:</b>{FormattingUtils.Footer}";

        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🏢 Filter by Department", "admin_exam_depts") },
            new[] { InlineKeyboardButton.WithCallbackData("🗓️ Upcoming Exams", "admin_exams_upcoming") },
            new[] { InlineKeyboardButton.WithCallbackData("🏁 Past Exams", "admin_exams_past") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Main Menu", "main_menu") }
        });

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ListExamDepartments(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
        var (resp, _) = await _api.GetAsync(userId, "/api/department");

        var departments = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        var kb = new List<InlineKeyboardButton[]>();
        
        if (departments.ValueKind == JsonValueKind.Array)
        {
            var row = new List<InlineKeyboardButton>();
            foreach (var d in departments.EnumerateArray())
            {
                var name = d.Str("name", "Unknown");
                var did = d.Str("departmentId", d.Str("id"));
                if (string.IsNullOrEmpty(did)) continue;
                row.Add(InlineKeyboardButton.WithCallbackData($"{name}", $"admin_exam_dept_{did}"));
                if (row.Count == 2) { kb.Add(row.ToArray()); row.Clear(); }
            }
            if (row.Count > 0) kb.Add(row.ToArray());
        }

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "admin_exams"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") });
        await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, $"🏠 <b>Home</b> > 📝 <b>Exams</b> > 🏢 <b>Select Department:</b>{FormattingUtils.Footer}", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ListExamCourses(CallbackQuery query, string deptId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
        var (cResp, _) = await _api.GetAsync(userId, $"/api/course/department/{deptId}");
        
        var courses = cResp?.ValueKind == JsonValueKind.Array ? cResp.Value : default;
        if (cResp?.ValueKind == JsonValueKind.Object && cResp.Value.TryGetProperty("data", out var cData)) courses = cData;

        if (courses.ValueKind != JsonValueKind.Array || courses.GetArrayLength() == 0)
        {
            await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, $"❌ <b>No courses found</b> for Dept #{deptId}.{FormattingUtils.Footer}", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_exam_depts"));
            return;
        }

        var kb = new List<InlineKeyboardButton[]>();
        foreach (var c in courses.EnumerateArray())
        {
            var cid = c.Str("courseId", c.Str("id"));
            var code = c.Str("courseCode", c.Str("code"));
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"{code}", $"admin_exam_list_{cid}") });
        }
        
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "admin_exam_depts"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") });
        await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, $"🏠 <b>Home</b> > 📝 <b>Exams</b> > 📚 <b>Select Course:</b>{FormattingUtils.Footer}", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ListCourseExams(CallbackQuery query, string courseId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (eResp, _) = await _api.GetAsync(userId, $"/api/exam?courseId={courseId}");
        var exams = eResp?.ValueKind == JsonValueKind.Array ? eResp.Value : default;

        var (cResp, _) = await _api.GetAsync(userId, $"/api/course/{courseId}");
        var cName = cResp?.ValueKind == JsonValueKind.Object ? cResp.Value.Str("courseName", $"Course #{courseId}") : $"Course #{courseId}";

        if (exams.ValueKind != JsonValueKind.Array || exams.GetArrayLength() == 0)
        {
            var bkKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "admin_exam_depts") }, new[] { InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") } });
            await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, $"🚫 No exams found for <b>{cName}</b>.{FormattingUtils.Footer}", parseMode: ParseMode.Html, replyMarkup: bkKb);
            return;
        }

        var kb = new List<InlineKeyboardButton[]>();
        foreach (var ex in exams.EnumerateArray())
        {
            var eid = ex.Str("examId");
            var title = ex.Str("title");
            var date = ex.Str("scheduledDate", "TBD").Split('T')[0];
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"📅 {date} - {title}", $"view_exam_{eid}") });
        }

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "admin_exam_depts"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") });
        await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, $"🏠 <b>Home</b> > 📝 <b>Exams</b> > 📚 <b>{cName}</b>\n━━━━━━━━━━━━━━━━━━━━\nSelect an exam to view details:{FormattingUtils.Footer}", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ListExamsFiltered(CallbackQuery query, bool isUpcoming)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (resp, _) = await _api.GetAsync(userId, "/api/exam");
        var exams = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;

        var text = isUpcoming ? $"🏠 <b>Home</b> > 📝 <b>Exams</b> > 🗓️ <b>Upcoming Exams</b>\n━━━━━━━━━━━━━━━━━━━━\n" : $"🏠 <b>Home</b> > 📝 <b>Exams</b> > 🏁 <b>Past Exams</b>\n━━━━━━━━━━━━━━━━━━━━\n";
        var kb = new List<InlineKeyboardButton[]>();
        var now = DateTime.UtcNow.Date;
        
        if (exams.ValueKind == JsonValueKind.Array)
        {
            foreach (var ex in exams.EnumerateArray())
            {
                var eid = ex.Str("examId");
                var title = ex.Str("title");
                var dateStr = ex.Str("scheduledDate", "TBD").Split('T')[0];
                var d = DateTime.TryParse(dateStr, out var p) ? p.Date : now;
                
                if (isUpcoming && d >= now) kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"📅 {dateStr} - {title}", $"view_exam_{eid}") });
                else if (!isUpcoming && d < now) kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"📅 {dateStr} - {title}", $"view_exam_{eid}") });
            }
        }

        if (kb.Count == 0) text += "\n🚫 No exams found.";
        text += FormattingUtils.Footer;
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "admin_exams"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") });
        await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ViewExamDetail(CallbackQuery query, string examId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (eResp, err) = await _api.GetAsync(userId, $"/api/exam/{examId}");
        if (err != null) { await _bot.EditMessageText(chatId, query.Message!.MessageId, "❌ Exam not found.", replyMarkup: MenuHandler.BackButton()); return; }

        var exam = eResp!.Value;
        var cid = exam.Int("courseId");
        var (cResp, _) = await _api.GetAsync(userId, $"/api/course/{cid}");
        var cName = cResp?.ValueKind == JsonValueKind.Object ? cResp.Value.Str("courseName", $"Course #{cid}") : $"Course #{cid}";

        var title = exam.Str("title");
        var desc = exam.Str("description", "No description");
        var dateVal = exam.Str("scheduledDate", "TBD").Replace("T", " ");
        var date = dateVal.Length > 16 ? dateVal.Substring(0, 16) : dateVal;
        var duration = exam.Str("duration", "00:00:00");
        var marks = exam.Dec("totalMarks", 100);
        var passMarks = exam.Dec("passingMarks", 40);
        var isPub = exam.Bool("isPublished");
        var statusIcon = isPub ? "🟢 Published" : "🔴 Draft/Hidden";

        var text = $"🏠 <b>Home</b> > 📝 <b>Exams</b> > 🔍 <b>Exam Details</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                   $"📘 <b>Course:</b> {cName}\n📌 <b>Title:</b> {title}\n📅 <b>Date:</b> {date}\n" +
                   $"⏳ <b>Duration:</b> {duration}\n\n🔢 <b>Marks:</b> {passMarks}/{marks}\n" +
                   $"📢 <b>Status:</b> {statusIcon}\n📝 <b>Description:</b>\n<blockquote expandable>{desc}</blockquote>{FormattingUtils.Footer}";

        var kb = new List<InlineKeyboardButton[]>();
        if (!isPub) kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🚀 Publish Results", $"pub_exam_{examId}") });
        else kb.Add(new[] { InlineKeyboardButton.WithCallbackData("📊 View Results", $"view_exam_results_{examId}") });
        
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", $"admin_exam_list_{cid}"), InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") });

        await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task PublishExamResults(CallbackQuery query, string examId)
    {
        var userId = query.From.Id;
        var (resp, err) = await _api.PostAsync(userId, $"/api/exam/{examId}/publish", new { });

        if (err == null)
        {
            await _bot.AnswerCallbackQuery(query.Id, "✅ Exam Results Published!", showAlert: true);
            query.Data = $"view_exam_{examId}";
            await ViewExamDetail(query, examId);
        }
        else
        {
            await _bot.AnswerCallbackQuery(query.Id, $"Failed: {err}", showAlert: true);
        }
    }

    private async Task ViewExamResults(CallbackQuery query, string examId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (data, error) = await _api.GetAsync(userId, $"/api/exam/result?examId={examId}");
        if (error != null) { await _bot.SendMessage(chatId, $"❌ {error}", replyMarkup: MenuHandler.BackButton($"view_exam_{examId}")); return; }
        
        var sb = new StringBuilder($"🏠 <b>Home</b> > 📝 <b>Exams</b> > 📊 <b>Results</b>\n━━━━━━━━━━━━━━━━━━━━\n");
        if (data?.ValueKind == JsonValueKind.Array)
        {
            int i = 1;
            foreach (var r in data.Value.EnumerateArray())
            {
                var sn = r.Str("studentName");
                var marks = r.Dec("marksObtained", r.Dec("marks"));
                var total = r.Dec("totalMarks", 100);
                var grade = r.Str("grade", "-");
                var pct = (double)(marks / (total == 0 ? 1 : total)) * 100;
                var dot = FormattingUtils.GetStatusDot(pct);
                sb.AppendLine($"{i++}. {dot} <b>{sn}</b> — {marks}/{total} ({grade})");
            }
            if (i == 1) sb.AppendLine("📭 <i>No results found yet.</i>");
        }
        else sb.AppendLine("📭 <i>No results found yet.</i>");
        
        sb.Append(FormattingUtils.Footer);
        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", $"view_exam_{examId}") }, new[] { InlineKeyboardButton.WithCallbackData("🏠 Home", "main_menu") } });
        await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: kb);
    }

    public Task HandleState(Message msg, string state) => Task.CompletedTask;
}
