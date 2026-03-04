using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class AttendanceHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public AttendanceHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        var userId = query.From.Id;
        
        if (data == "admin_attendance") { await ViewAttendanceDashboard(query); return; }
        if (data == "admin_att_depts") { await ListAttDepartments(query); return; }
        if (data.StartsWith("admin_att_action_")) { await SelectDeptAction(query, data.Replace("admin_att_action_", "")); return; }
        
        // Flow A: Course Stats
        if (data.StartsWith("admin_att_courses_")) { await ListAttCourses(query, data.Replace("admin_att_courses_", "")); return; }
        if (data.StartsWith("view_att_course_")) { await ViewCourseAttStats(query, data.Replace("view_att_course_", "")); return; }
        
        // Flow B: Student List & Detail
        if (data.StartsWith("admin_att_students_")) { await ListAttDeptStudents(query); return; }
        if (data.StartsWith("view_att_student_")) { await ViewStudentAttDetail(query); return; }
        
        // Search
        if (data == "search_att_start") { await SearchAttStart(query, userId); return; }
    }

    private async Task ViewAttendanceDashboard(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id;
        var text = $"🏠 Home > 📅 <b>Attendance</b>\n━━━━━━━━━━━━━━━━━━━━\nMonitor student attendance and class records.\n\n👇 <b>Select an option:</b>";
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🏢 View by Department", "admin_att_depts") },
            new[] { InlineKeyboardButton.WithCallbackData("🔍 Search Student Stats", "search_att_start") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Main Menu", "main_menu") }
        });
        
        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ListAttDepartments(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
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
                row.Add(InlineKeyboardButton.WithCallbackData($"{name}", $"admin_att_action_{did}"));
                if (row.Count == 2) { kb.Add(row.ToArray()); row.Clear(); }
            }
            if (row.Count > 0) kb.Add(row.ToArray());
        }

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "admin_attendance") });
        await _bot.EditMessageText(chatId, query.Message!.MessageId, $"🏢 <b>Select Department:</b>", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task SelectDeptAction(CallbackQuery query, string deptId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (dResp, _) = await _api.GetAsync(userId, $"/api/department/{deptId}");
        var dName = dResp?.ValueKind == JsonValueKind.Object ? dResp.Value.Str("name", $"Department #{deptId}") : $"Department #{deptId}";

        var text = $"🏢 <b>{dName}</b>\n━━━━━━━━━━━━━━━━━━━━\nSelect View Mode:";
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📊 Course Statistics", $"admin_att_courses_{deptId}") },
            new[] { InlineKeyboardButton.WithCallbackData("👥 Student Attendance List", $"admin_att_students_{deptId}_page_1") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Change Department", "admin_att_depts") }
        });

        await _bot.EditMessageText(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ListAttCourses(CallbackQuery query, string deptId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (cResp, _) = await _api.GetAsync(userId, $"/api/course/department/{deptId}");
        
        var courses = cResp?.ValueKind == JsonValueKind.Array ? cResp.Value : default;
        if (cResp?.ValueKind == JsonValueKind.Object && cResp.Value.TryGetProperty("data", out var cData)) courses = cData;

        if (courses.ValueKind != JsonValueKind.Array || courses.GetArrayLength() == 0)
        {
            await _bot.EditMessageText(chatId, query.Message!.MessageId, $"❌ No courses found for Dept #{deptId}.", replyMarkup: MenuHandler.BackButton($"admin_att_action_{deptId}"));
            return;
        }

        var kb = new List<InlineKeyboardButton[]>();
        foreach (var c in courses.EnumerateArray())
        {
            var cid = c.Str("courseId", c.Str("id"));
            var code = c.Str("courseCode", c.Str("code"));
            var name = c.Str("courseName", c.Str("name"));
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"{code} - {name}", $"view_att_course_{cid}") });
        }
        
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", $"admin_att_action_{deptId}") });
        await _bot.EditMessageText(chatId, query.Message!.MessageId, $"📚 <b>Select Course to View Stats:</b>", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ViewCourseAttStats(CallbackQuery query, string courseId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        
        var (cResp, _) = await _api.GetAsync(userId, $"/api/course/{courseId}");
        var cName = cResp?.ValueKind == JsonValueKind.Object ? cResp.Value.Str("courseName", cResp.Value.Str("name", "Unknown Course")) : "Unknown Course";
        var deptId = cResp?.ValueKind == JsonValueKind.Object ? cResp.Value.Int("departmentId") : 0;

        var (rResp, _) = await _api.GetAsync(userId, $"/api/attendance/course/{courseId}");
        var records = rResp?.ValueKind == JsonValueKind.Array ? rResp.Value : default;
        if (rResp?.ValueKind == JsonValueKind.Object && rResp.Value.TryGetProperty("data", out var rData)) records = rData;

        if (records.ValueKind != JsonValueKind.Array || records.GetArrayLength() == 0)
        {
            await _bot.EditMessageText(chatId, query.Message!.MessageId, $"📉 <b>{cName}</b>\n\nNo attendance records found.", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton($"admin_att_courses_{deptId}"));
            return;
        }

        int totalRecords = records.GetArrayLength();
        int presentCount = 0;
        var dates = new HashSet<string>();

        foreach (var r in records.EnumerateArray())
        {
            if (r.Bool("isPresent")) presentCount++;
            var dateStr = r.Str("date");
            if (!string.IsNullOrEmpty(dateStr)) dates.Add(dateStr.Split('T')[0]);
        }

        int totalClasses = dates.Count;
        double avgMeasure = totalRecords > 0 ? ((double)presentCount / totalRecords * 100) : 0;
        string bar = FormattingUtils.GetProgressBar(avgMeasure, 100, 10);

        var text = $"🏠 Home > 📅 Attd > 📊 <b>Stats</b>\n━━━━━━━━━━━━━━━━━━━━\n📘 <b>{cName}</b>\n\n" +
                   $"📅 <b>Total Classes Held:</b> {totalClasses}\n👥 <b>Total Records:</b> {totalRecords}\n" +
                   $"✅ <b>Overall Presence:</b> {avgMeasure:F1}%\n<code>{bar}</code>\n\n";

        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Courses", $"admin_att_courses_{deptId}") } });
        await _bot.EditMessageText(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ListAttDeptStudents(CallbackQuery query)
    {
        var dataParts = query.Data!.Split('_');
        string deptId = dataParts[3];
        int page = 1; try { page = int.Parse(dataParts[5]); } catch { }

        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var endpoint = $"/api/student?DepartmentId={deptId}&Page={page}&PageSize=10";
        var (resp, _) = await _api.GetAsync(userId, endpoint);

        JsonElement students = default;
        int totalPages = 1;

        if (resp?.ValueKind == JsonValueKind.Object)
        {
            students = resp.Value.TryGetProperty("data", out var d) ? d : default;
            totalPages = resp.Value.Int("totalPages", 1);
        }
        else if (resp?.ValueKind == JsonValueKind.Array)
        {
            students = resp.Value;
        }

        if (students.ValueKind != JsonValueKind.Array || students.GetArrayLength() == 0)
        {
            var bkKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", $"admin_att_action_{deptId}") } });
            await _bot.EditMessageText(chatId, query.Message!.MessageId, $"❌ No students found for Dept #{deptId}.", replyMarkup: bkKb);
            return;
        }

        var text = $"👥 <b>Student List</b> (Page {page}/{totalPages})\nSelect a student to view detailed attendance:\n";
        var kb = new List<InlineKeyboardButton[]>();

        foreach (var s in students.EnumerateArray())
        {
            var sid = s.Str("studentId", s.Str("id"));
            var name = $"{s.Str("firstName")} {s.Str("lastName")}".Trim();
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"👤 {name}", $"view_att_student_{sid}_{deptId}") });
        }

        var nav = new List<InlineKeyboardButton>();
        if (page > 1) nav.Add(InlineKeyboardButton.WithCallbackData("⬅️ Prev", $"admin_att_students_{deptId}_page_{page - 1}"));
        if (page < totalPages) nav.Add(InlineKeyboardButton.WithCallbackData("Next ➡️", $"admin_att_students_{deptId}_page_{page + 1}"));
        if (nav.Count > 0) kb.Add(nav.ToArray());

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Options", $"admin_att_action_{deptId}") });
        await _bot.EditMessageText(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ViewStudentAttDetail(CallbackQuery query)
    {
        var dataParts = query.Data!.Split('_');
        string sid = dataParts[3];
        string deptId = dataParts.Length > 4 ? dataParts[4] : "0";

        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (sResp, _) = await _api.GetAsync(userId, $"/api/student/{sid}");
        var name = sResp?.ValueKind == JsonValueKind.Object ? $"{sResp.Value.Str("firstName", "Student")} {sResp.Value.Str("lastName")}".Trim() : "Student";

        var (rResp, _) = await _api.GetAsync(userId, $"/api/attendance/student/{sid}");
        var records = rResp?.ValueKind == JsonValueKind.Array ? rResp.Value : default;
        if (rResp?.ValueKind == JsonValueKind.Object && rResp.Value.TryGetProperty("data", out var rData)) records = rData;

        if (records.ValueKind != JsonValueKind.Array || records.GetArrayLength() == 0)
        {
            var textNone = $"📊 <b>Attendance: {name}</b>\n\nNo attendance records found.";
            var kbNone = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to List", $"admin_att_students_{deptId}_page_1") } });
            await _bot.EditMessageText(chatId, query.Message!.MessageId, textNone, parseMode: ParseMode.Html, replyMarkup: kbNone);
            return;
        }

        int totalPresent = 0, totalLectures = 0;
        var courseStats = new Dictionary<int, (int present, int total)>();

        foreach (var r in records.EnumerateArray())
        {
            var cid = r.Int("courseId");
            if (!courseStats.ContainsKey(cid)) courseStats[cid] = (0, 0);
            
            var stats = courseStats[cid];
            stats.total++;
            totalLectures++;
            if (r.Bool("isPresent"))
            {
                stats.present++;
                totalPresent++;
            }
            courseStats[cid] = stats;
        }

        double overallPct = totalLectures > 0 ? ((double)totalPresent / totalLectures * 100) : 0;
        string overallBar = FormattingUtils.GetProgressBar(overallPct, 100, 10);

        var text = $"🏠 Home > 📅 Attd > 👤 <b>Detail</b>\n━━━━━━━━━━━━━━━━━━━━\n👤 <b>{name}</b>\n" +
                   $"🆔 Student ID: <code>{sid}</code>\n━━━━━━━━━━━━━━━━━━━━\n\n📊 <b>Overall Attendance</b>\n" +
                   $"<code>{overallBar}</code> <b>{overallPct:F1}%</b>\n🔹 Present: <b>{totalPresent}</b> / {totalLectures} Lectures\n\n" +
                   $"📚 <b>Subject Breakdown</b>\n────────────────────\n";

        foreach (var kvp in courseStats)
        {
            var cid = kvp.Key;
            var stats = kvp.Value;
            double pct = stats.total > 0 ? ((double)stats.present / stats.total * 100) : 0;
            
            string icon = pct >= 75 ? "🟢" : (pct >= 60 ? "🟡" : "🔴");
            string state = pct >= 75 ? "Excellent" : (pct >= 60 ? "Fair" : "Low");
            
            var (cResp, _) = await _api.GetAsync(userId, $"/api/course/{cid}");
            var cName = cResp?.ValueKind == JsonValueKind.Object ? cResp.Value.Str("courseName", $"Subject #{cid}") : $"Subject #{cid}";

            text += $"{icon} <b>{cName}</b>\n   └ 📅 {stats.present}/{stats.total}  |  📊 {pct:F0}% ({state})\n\n";
        }

        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to List", $"admin_att_students_{deptId}_page_1") } });
        await _bot.EditMessageText(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task SearchAttStart(CallbackQuery query, long userId)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        _sessions.SetState(userId, "search_att_student");
        await _bot.SendMessage(query.Message!.Chat.Id, "🔍 <b>Search Student Attendance</b>\n\nEnter Student Name:", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_attendance"));
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id; var text = msg.Text ?? "";
        if (state == "search_att_student")
        {
            _sessions.ClearState(userId);
            var qLower = text.ToLower();
            var (sResp, _) = await _api.GetAsync(userId, "/api/student?Page=1&PageSize=100");
            
            var students = sResp?.ValueKind == JsonValueKind.Array ? sResp.Value : default;
            if (sResp?.ValueKind == JsonValueKind.Object && sResp.Value.TryGetProperty("data", out var sData)) students = sData;

            string? targetSid = null;
            string targetName = "";

            if (students.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in students.EnumerateArray())
                {
                    var fullName = $"{s.Str("firstName")} {s.Str("lastName")}".Trim();
                    if (fullName.ToLower().Contains(qLower))
                    {
                        targetSid = s.Str("studentId", s.Str("id"));
                        targetName = fullName;
                        break;
                    }
                }
            }

            if (targetSid == null)
            {
                await _bot.SendMessage(chatId, "❌ Student not found.", replyMarkup: MenuHandler.BackButton("admin_attendance"));
                return;
            }

            var queryCb = new CallbackQuery { From = msg.From!, Message = await _bot.SendMessage(chatId, "Loading..."), Data = $"view_att_student_{targetSid}_0" };
            await ViewStudentAttDetail(queryCb);
        }
    }
}
