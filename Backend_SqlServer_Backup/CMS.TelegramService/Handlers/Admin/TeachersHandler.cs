using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class TeachersHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public TeachersHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        var userId = query.From.Id;
        
        if (data == "admin_teachers") { await ListDepartments(query); return; }
        if (data.StartsWith("admin_teachers_dept_")) { await ListTeachers(query); return; }
        if (data == "add_teacher_start") { await StartAdd(query, userId); return; }
        if (data == "search_teacher_start") { await StartSearch(query, userId); return; }
        if (data.StartsWith("view_teacher_")) { await ViewTeacher(query, data.Replace("view_teacher_", "")); return; }
        if (data.StartsWith("view_tschedule_")) { await ViewTeacherSchedule(query, data.Replace("view_tschedule_", "")); return; }
        if (data.StartsWith("view_tstats_")) { await ViewTeacherStats(query, data.Replace("view_tstats_", "")); return; }
        if (data.StartsWith("delete_teacher_")) { await DeleteTeacherConfirm(query, data.Replace("delete_teacher_", "")); return; }
        if (data.StartsWith("confirm_del_teacher_")) { await DeleteTeacher(query, data.Replace("confirm_del_teacher_", "")); return; }
    }

    private async Task ListDepartments(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (resp, err) = await _api.GetAsync(userId, "/api/department");

        if (err != null)
        {
            try { await query.Message.Delete(_bot); } catch { }
            await _bot.SendMessage(chatId, $"❌ Error fetching departments: {err}", replyMarkup: MenuHandler.BackButton());
            return;
        }

        var departments = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        var kb = new List<InlineKeyboardButton[]>();
        
        if (departments.ValueKind == JsonValueKind.Array && departments.GetArrayLength() > 0)
        {
            var row = new List<InlineKeyboardButton>();
            foreach (var d in departments.EnumerateArray())
            {
                var name = d.Str("name", "Unknown");
                var did = d.Str("departmentId", d.Str("id"));
                row.Add(InlineKeyboardButton.WithCallbackData($"🎓 {name}", $"admin_teachers_dept_{did}_page_1"));
                if (row.Count == 2) { kb.Add(row.ToArray()); row.Clear(); }
            }
            if (row.Count > 0) kb.Add(row.ToArray());
        }
        else
        {
            try { await query.Message.Delete(_bot); } catch { }
            await _bot.SendMessage(chatId, "🏢 No Departments found.", replyMarkup: MenuHandler.BackButton());
            return;
        }

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔍 Search Teacher", "search_teacher_start") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Add New Teacher", "add_teacher_start") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Menu", "main_menu") });

        var msg = $"🏫 <b>Select Department</b>\n━━━━━━━━━━━━━━━━━━━━\nSelect a department to view teachers.";

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(chatId, msg, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ListTeachers(CallbackQuery query)
    {
        var dataParts = query.Data!.Split('_');
        int deptId = 0, page = 1;
        try { deptId = int.Parse(dataParts[3]); page = int.Parse(dataParts[5]); } catch { }

        var userId = query.From.Id;
        string? deptNameFilter = null;

        if (deptId > 0)
        {
            var (dResp, _) = await _api.GetAsync(userId, $"/api/department/{deptId}");
            deptNameFilter = dResp?.ValueKind == JsonValueKind.Object ? dResp.Value.Str("name") : null;
        }

        var endpoint = string.IsNullOrEmpty(deptNameFilter) 
            ? $"/api/teacher?Page={page}&PageSize=10" 
            : $"/api/teacher?Department={deptNameFilter}&Page={page}&PageSize=10";

        var (resp, err) = await _api.GetRawAsync(userId, endpoint);

        if (err != null)
        {
            await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, $"❌ Error: {err}", replyMarkup: MenuHandler.BackButton());
            return;
        }

        var root = resp!.Value;
        var teachers = root.TryGetProperty("data", out var d) ? d : root;
        var totalPages = root.Int("totalPages", 1);

        if (teachers.ValueKind != JsonValueKind.Array || teachers.GetArrayLength() == 0)
        {
            var backKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Departments", "admin_teachers") } });
            await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, "👨‍🏫 No Teachers found in this department.", replyMarkup: backKb);
            return;
        }

        var text = $"👨‍🏫 <b>Teacher Directory</b> (Page {page}/{totalPages})\n\n";
        var kb = new List<InlineKeyboardButton[]>();

        foreach (var t in teachers.EnumerateArray())
        {
            var fn = t.Str("firstName"); var ln = t.Str("lastName");
            var name = string.IsNullOrWhiteSpace(fn + ln) ? t.Str("name", "Unknown") : $"{fn} {ln}";
            var tid = t.Str("teacherId", t.Str("id"));
            var dept = t.Str("department", "N/A");
            var qual = t.Str("qualification", "N/A");
            var statusIcon = t.Bool("isActive", true) ? "🟢" : "🔴";

            text += $"{statusIcon} <b>{name}</b>\n   └ 🎓 {dept} | 📜 {qual}\n\n";

            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"👁️ View {name}", $"view_teacher_{tid}") });
        }

        var navRow = new List<InlineKeyboardButton>();
        if (page > 1) navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Prev", $"admin_teachers_dept_{deptId}_page_{page - 1}"));
        if (page < totalPages) navRow.Add(InlineKeyboardButton.WithCallbackData("Next ➡️", $"admin_teachers_dept_{deptId}_page_{page + 1}"));
        if (navRow.Count > 0) kb.Add(navRow.ToArray());

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Switch Department", "admin_teachers") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Main Menu", "main_menu") });

        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ViewTeacher(CallbackQuery query, string teacherId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (tData, err) = await _api.GetAsync(userId, $"/api/teacher/{teacherId}");
        if (err != null) { await _bot.EditMessageText(chatId, query.Message.MessageId, "❌ Teacher not found.", replyMarkup: MenuHandler.BackButton()); return; }

        var t = tData!.Value;
        var name = $"{t.Str("firstName")} {t.Str("lastName")}".Trim();
        var dept = t.Str("department", "N/A");
        var qual = t.Str("qualification", "N/A");
        var exp = t.Int("experience");
        var phone = $"<code>{t.Str("phoneNumber", "N/A")}</code>";
        var email = $"<code>{t.Str("email", "N/A")}</code>";
        var statusVal = t.Bool("isActive", true) ? "Active" : "Inactive";
        var statusIcon = statusVal == "Active" ? "🟢" : "🔴";
        var tidCode = $"<code>{teacherId}</code>";

        var info = $"👨‍🏫 <b>TEACHER PROFILE</b>\n━━━━━━━━━━━━━━━━━━━━\n\n" +
                   $"👤 <b>{name}</b>\n🆔 ID: {tidCode}\n\n" +
                   $"📋 <b><u>Professional Details</u></b>\n├ 🏛 <b>Dept:</b> {dept}\n├ 📜 <b>Qual:</b> {qual}\n" +
                   $"├ ⏳ <b>Exp:</b> {exp} Years\n└ {statusIcon} <b>Status:</b> {statusVal}\n\n" +
                   $"📞 <b><u>Contact Info</u></b>\n├ 📧 {email}\n└ 📱 {phone}\n━━━━━━━━━━━━━━━━━━━━";

        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📅 Schedule", $"view_tschedule_{teacherId}"), InlineKeyboardButton.WithCallbackData("📊 Stats", $"view_tstats_{teacherId}") },
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Delete Teacher", $"delete_teacher_{teacherId}") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to List", "admin_teachers") }
        });

        await _bot.EditMessageText(chatId, query.Message!.MessageId, info, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ViewTeacherSchedule(CallbackQuery query, string teacherId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        
        var (tData, _) = await _api.GetAsync(userId, $"/api/teacher/{teacherId}");
        var tName = tData?.ValueKind == JsonValueKind.Object ? $"{tData.Value.Str("firstName")} {tData.Value.Str("lastName")}".Trim() : "Teacher";

        var (sData, err) = await _api.GetAsync(userId, $"/api/timeslot/teacher/{teacherId}");
        
        if (err != null || (sData?.ValueKind == JsonValueKind.Array && sData.Value.GetArrayLength() == 0))
        {
            var msg = $"📅 <b>Schedule for {tName}</b>\n━━━━━━━━━━━━━━━━━━━━\n\n<i>No classes assigned yet.</i>";
            var kbEmpty = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Profile", $"view_teacher_{teacherId}") } });
            await _bot.EditMessageText(chatId, query.Message.MessageId, msg, parseMode: ParseMode.Html, replyMarkup: kbEmpty);
            return;
        }

        var courseMap = new Dictionary<int, string>();
        var (cData, _) = await _api.GetAsync(userId, "/api/course?PageSize=100");
        if (cData?.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in cData.Value.EnumerateArray())
            {
                var cid = c.TryGetProperty("courseId", out var cObj) ? cObj.GetInt32() : c.Int("id");
                var cname = c.Str("name", c.Str("courseName"));
                if (cid > 0 && !string.IsNullOrEmpty(cname)) courseMap[cid] = cname;
            }
        }

        var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        var grouped = days.ToDictionary(d => d, _ => new List<JsonElement>());

        foreach (var s in sData!.Value.EnumerateArray())
        {
            var day = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.Str("dayOfWeek", "Monday").ToLower());
            if (grouped.ContainsKey(day)) grouped[day].Add(s);
        }

        var text = $"🗓 <b>Weekly Timetable</b>\n👤 <i>{tName}</i>\n━━━━━━━━━━━━━━━━━━━━\n";
        bool hasClasses = false;

        foreach (var day in days)
        {
            var daySlots = grouped[day];
            if (daySlots.Count == 0) continue;
            hasClasses = true;
            
            daySlots.Sort((a, b) => string.Compare(a.Str("startTime", "00:00:00"), b.Str("startTime", "00:00:00")));
            text += $"\n📅 <b>{day}</b>\n";

            foreach (var s in daySlots)
            {
                var start = s.Str("startTime", "00:00:00").Substring(0, 5);
                var end = s.Str("endTime", "00:00:00").Substring(0, 5);
                var room = s.Str("room", "N/A");
                var cid = s.Int("courseId");
                var cName = courseMap.TryGetValue(cid, out var n) ? n : $"Course #{cid}";

                text += $"⏰ <code>{start}</code> - <code>{end}</code>  📍 <code>{room}</code>\n📚 <b>{cName}</b>\n〰️〰️〰️〰️〰️〰️\n";
            }
        }

        if (!hasClasses) text += "\n<i>No classes scheduled.</i>";

        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Profile", $"view_teacher_{teacherId}") } });
        await _bot.EditMessageText(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ViewTeacherStats(CallbackQuery query, string teacherId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        
        var (tData, _) = await _api.GetAsync(userId, $"/api/teacher/{teacherId}");
        var tName = tData?.ValueKind == JsonValueKind.Object ? $"{tData.Value.Str("firstName")} {tData.Value.Str("lastName")}".Trim() : "Teacher";

        var (sData, _) = await _api.GetAsync(userId, $"/api/timeslot/teacher/{teacherId}");
        
        int totalClasses = 0;
        var uniqueCourses = new HashSet<int>();
        int totalMinutes = 0;

        if (sData?.ValueKind == JsonValueKind.Array)
        {
            totalClasses = sData.Value.GetArrayLength();
            foreach (var s in sData.Value.EnumerateArray())
            {
                uniqueCourses.Add(s.Int("courseId"));
                var start = s.Str("startTime", "00:00:00");
                var end = s.Str("endTime", "00:00:00");
                try
                {
                    TimeSpan ts = TimeSpan.Parse(end) - TimeSpan.Parse(start);
                    totalMinutes += (int)ts.TotalMinutes;
                }
                catch { }
            }
        }

        double totalHours = totalMinutes / 60.0;
        
        var text = $"📊 <b>Teacher Statistics</b>\n━━━━━━━━━━━━━━━━━━━━\n👤 <b>{tName}</b>\n\n" +
                   $"📚 <b><u>Workload Analysis</u></b>\n├ 🏫 <b>Courses:</b> {uniqueCourses.Count}\n├ 🗓 <b>Weekly Classes:</b> {totalClasses}\n" +
                   $"└ ⏱ <b>Total Hours:</b> {totalHours:F1} hrs/week\n\n<i>(Note: Attendance stats not available yet)</i>";

        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Profile", $"view_teacher_{teacherId}") } });
        await _bot.EditMessageText(chatId, query.Message.MessageId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task StartSearch(CallbackQuery query, long userId)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        _sessions.SetState(userId, "teacher_adm_search");
        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Menu", "admin_teachers") } });
        await _bot.SendMessage(query.Message!.Chat.Id, "🔍 <b>Search Teacher</b>\n\nEnter Name or Department:", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task StartAdd(CallbackQuery query, long userId)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        _sessions.SetState(userId, "teacher_adm_add_firstname");
        await _bot.SendMessage(query.Message!.Chat.Id, "➕ <b>Add Teacher</b>\n\nEnter <b>First Name</b>:", parseMode: ParseMode.Html);
    }

    private async Task DeleteTeacherConfirm(CallbackQuery query, string teacherId)
    {
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Yes, Delete Forever", $"confirm_del_teacher_{teacherId}") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", $"view_teacher_{teacherId}") }
        });
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, $"⚠️ <b>Delete Teacher?</b>\n\nAre you sure you want to delete this teacher?\nThis action <b>cannot</b> be undone.", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task DeleteTeacher(CallbackQuery query, string teacherId)
    {
        var userId = query.From.Id;
        var (_, err) = await _api.DeleteAsync(userId, $"/api/teacher/{teacherId}");
        
        if (err == null)
        {
            await _bot.AnswerCallbackQuery(query.Id, "Teacher Deleted!", showAlert: true);
            await ListDepartments(query);
        }
        else
        {
            await _bot.AnswerCallbackQuery(query.Id, $"Failed: {err}", showAlert: true);
            await ListDepartments(query);
        }
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id; var text = msg.Text ?? "";
        switch (state)
        {
            case "teacher_adm_search":
                _sessions.ClearState(userId);
                var wait = await _bot.SendMessage(chatId, "🔍 Searching...");
                var (data, error) = await _api.GetAsync(userId, $"/api/teacher?SearchQuery={text}&PageSize=10");
                await _bot.DeleteMessage(chatId, wait.MessageId);
                await ShowSearchResults(chatId, data, error, text);
                break;
            case "teacher_adm_add_firstname": _sessions.SetData(userId, "add_teacher_fn", text); _sessions.SetState(userId, "teacher_adm_add_email"); await _bot.SendMessage(chatId, "Enter <b>Email Address</b>:", parseMode: ParseMode.Html); break;
            case "teacher_adm_add_email": _sessions.SetData(userId, "add_teacher_email", text); _sessions.SetState(userId, "teacher_adm_add_dept"); await _bot.SendMessage(chatId, "Enter <b>Department</b>:", parseMode: ParseMode.Html); break;
            case "teacher_adm_add_dept": _sessions.SetData(userId, "add_teacher_dept", text); _sessions.SetState(userId, "teacher_adm_add_qual"); await _bot.SendMessage(chatId, "Enter <b>Qualification</b> (e.g. PhD, M.Tech):", parseMode: ParseMode.Html); break;
            case "teacher_adm_add_qual": _sessions.SetData(userId, "add_teacher_qual", text); _sessions.SetState(userId, "teacher_adm_add_password"); await _bot.SendMessage(chatId, "Enter <b>Initial Password</b>:", parseMode: ParseMode.Html); break;
            case "teacher_adm_add_password": await FinishAdd(chatId, userId, text); break;
        }
    }

    private async Task ShowSearchResults(long chatId, JsonElement? data, string? error, string query)
    {
        if (error != null) { await _bot.SendMessage(chatId, $"❌ {error}", replyMarkup: MenuHandler.BackButton("admin_teachers")); return; }
        
        var teachers = data?.ValueKind == JsonValueKind.Array ? data.Value : default;
        if (teachers.ValueKind != JsonValueKind.Array || teachers.GetArrayLength() == 0)
        {
            await _bot.SendMessage(chatId, $"❌ No teachers found for '{query}'", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_teachers"));
            return;
        }

        var sb = new StringBuilder($"🔍 <b>Results for {query}</b>\n━━━━━━━━━━━━━━━━━━━━\n\n");
        var kb = new List<InlineKeyboardButton[]>();
        
        foreach (var t in teachers.EnumerateArray())
        {
            var name = $"{t.Str("firstName")} {t.Str("lastName")}".Trim();
            var tid = t.Str("teacherId", t.Str("id"));
            var dept = t.Str("department");
            var status = t.Bool("isActive", true) ? "🟢" : "🔴";
            
            sb.AppendLine($"{status} <b>{name}</b>\n   └ 🏛 {dept}\n");
            if (!string.IsNullOrEmpty(tid)) kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"👁️ View {name}", $"view_teacher_{tid}") });
        }
        
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to List", "admin_teachers") });
        await _bot.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task FinishAdd(long chatId, long userId, string password)
    {
        var payload = new
        {
            firstName = _sessions.GetData<string>(userId, "add_teacher_fn"),
            lastName = ".",
            email = _sessions.GetData<string>(userId, "add_teacher_email"),
            department = _sessions.GetData<string>(userId, "add_teacher_dept"),
            qualification = _sessions.GetData<string>(userId, "add_teacher_qual"),
            role = "Teacher",
            experience = 0,
            password
        };
        _sessions.ClearState(userId);
        var (_, err) = await _api.PostAsync(userId, "/api/teacher", payload);
        await _bot.SendMessage(chatId, err != null ? $"❌ Failed: {err}" : "✅ <b>Teacher Created Successfully!</b>", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_teachers"));
    }
}
