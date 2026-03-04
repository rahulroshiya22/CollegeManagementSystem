using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class StudentsHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public StudentsHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        var userId = query.From.Id;
        
        if (data == "admin_students") { await ListDepartments(query); return; }
        if (data.StartsWith("admin_students_dept_")) { await ListStudents(query); return; }
        if (data == "add_student_start") { await StartAdd(query, userId); return; }
        if (data == "search_student_start") { await StartSearch(query, userId); return; }
        if (data.StartsWith("view_student_")) { await ViewStudent(query, data.Replace("view_student_", "")); return; }
        if (data.StartsWith("delete_student_")) { await DeleteStudentConfirm(query, data.Replace("delete_student_", "")); return; }
        if (data.StartsWith("confirm_del_student_")) { await DeleteStudent(query, data.Replace("confirm_del_student_", "")); return; }
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
                row.Add(InlineKeyboardButton.WithCallbackData($"🎓 {name}", $"admin_students_dept_{did}_page_1"));
                if (row.Count == 2) { kb.Add(row.ToArray()); row.Clear(); }
            }
            if (row.Count > 0) kb.Add(row.ToArray());
        }
        else
        {
            try { await query.Message.Delete(_bot); } catch { }
            await _bot.SendMessage(chatId, "🏢 No Departments found. Please add departments first.", replyMarkup: MenuHandler.BackButton());
            return;
        }

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔍 Search Student", "search_student_start") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Add New Student", "add_student_start") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Menu", "main_menu") });

        var msg = $"🏫 <b>Select Department</b>\n━━━━━━━━━━━━━━━━━━━━\nPlease select a department to view students.\nThis helps load the list faster!";

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(chatId, msg, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ListStudents(CallbackQuery query)
    {
        var dataParts = query.Data!.Split('_');
        int deptId = 0, page = 1;
        try { deptId = int.Parse(dataParts[3]); page = int.Parse(dataParts[5]); } catch { }

        var userId = query.From.Id;
        var endpoint = deptId == 0 ? $"/api/student?Page={page}&PageSize=10" : $"/api/student?DepartmentId={deptId}&Page={page}&PageSize=10";
        var (resp, err) = await _api.GetRawAsync(userId, endpoint);

        if (err != null)
        {
            await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, $"❌ Error: {err}", replyMarkup: MenuHandler.BackButton());
            return;
        }

        var root = resp!.Value;
        var students = root.TryGetProperty("data", out var d) ? d : root;
        var totalPages = root.Int("totalPages", 1);

        if (students.ValueKind != JsonValueKind.Array || students.GetArrayLength() == 0)
        {
            var backKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Departments", "admin_students") } });
            await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, "👥 No Students found in this department.", replyMarkup: backKb);
            return;
        }

        var text = $"🛡️ <b>Student Directory</b>\nPage {page}/{totalPages}\n━━━━━━━━━━━━━━━━━━━━\n\n";
        var kb = new List<InlineKeyboardButton[]>();

        foreach (var s in students.EnumerateArray())
        {
            var fn = s.Str("firstName"); var ln = s.Str("lastName");
            var name = string.IsNullOrWhiteSpace(fn + ln) ? s.Str("email", "Unknown") : $"{fn} {ln}";
            var roll = $"<code>{s.Str("rollNumber", "N/A")}</code>";
            var statusIcon = s.Bool("isActive", true) ? "🟢" : "🔴";
            var sid = s.Str("studentId", s.Str("id"));

            text += $"{statusIcon} <b>{name}</b>\n🆔 {roll}\n━━━━━━━━━━━━━━━━━━━━\n";

            kb.Add(new[] {
                InlineKeyboardButton.WithCallbackData("👁️ View", $"view_student_{sid}"),
                InlineKeyboardButton.WithCallbackData("🗑️ Delete", $"delete_student_{sid}")
            });
        }

        var navRow = new List<InlineKeyboardButton>();
        if (page > 1) navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Prev", $"admin_students_dept_{deptId}_page_{page - 1}"));
        if (page < totalPages) navRow.Add(InlineKeyboardButton.WithCallbackData("Next ➡️", $"admin_students_dept_{deptId}_page_{page + 1}"));
        if (navRow.Count > 0) kb.Add(navRow.ToArray());

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Switch Department", "admin_students") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Main Menu", "main_menu") });

        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ViewStudent(CallbackQuery query, string studentId)
    {
        var userId = query.From.Id; var chatId = query.Message!.Chat.Id;
        var (sData, err) = await _api.GetAsync(userId, $"/api/student/{studentId}");
        if (err != null) { await _bot.EditMessageText(chatId, query.Message.MessageId, "❌ Student not found.", replyMarkup: MenuHandler.BackButton()); return; }

        var student = sData!.Value;
        var name = $"{student.Str("firstName")} {student.Str("lastName")}";
        var deptName = "N/A";
        if (student.TryGetProperty("department", out var dept)) deptName = dept.ValueKind == JsonValueKind.Object ? dept.Str("name", "N/A") : dept.Str("");
        
        var phone = $"<code>{student.Str("phone", student.Str("phoneNumber", "N/A"))}</code>";
        var batch = student.Str("admissionYear", "N/A");
        var gender = student.Str("gender", "N/A");
        var status = student.Str("status", "Active");
        var statusIcon = status == "Active" ? "🟢" : "🔴";
        
        var (attResp, _) = await _api.GetAsync(userId, $"/api/attendance/student/{studentId}");
        string attSummary = "<code>N/A</code>";
        if (attResp?.ValueKind == JsonValueKind.Array)
        {
            int total = 0, present = 0;
            foreach (var a in attResp.Value.EnumerateArray())
            {
                total++; if (a.Bool("isPresent")) present++;
            }
            attSummary = total > 0 ? $"<code>{(present * 100.0 / total):F1}% ({present}/{total})</code>" : "<code>0% (No Records)</code>";
        }

        var (enrollResp, _) = await _api.GetAsync(userId, $"/api/enrollment/student/{studentId}");
        var courseListStr = "None";
        int coursesCount = 0;
        if (enrollResp?.ValueKind == JsonValueKind.Array && enrollResp.Value.GetArrayLength() > 0)
        {
            var cList = new List<string>();
            foreach (var e in enrollResp.Value.EnumerateArray())
            {
                coursesCount++;
                if (cList.Count < 7)
                {
                    var cName = $"Course {e.Str("courseId")}";
                    if (e.TryGetProperty("course", out var cObj) && cObj.ValueKind == JsonValueKind.Object)
                        cName = cObj.Str("title", cObj.Str("name", cName));
                    cList.Add($"• <code>{e.Str("courseId")}</code> {cName}");
                }
            }
            if (coursesCount > 7) cList.Add("... (more)");
            courseListStr = string.Join("\n", cList);
        }

        var addr = $"<blockquote expandable>{student.Str("address", "N/A")}</blockquote>";
        var email = $"<code>{student.Str("email", "N/A")}</code>";
        var dob = student.Date("dateOfBirth");
        var roll = $"<code>{student.Str("rollNumber", "N/A")}</code>";
        var sidCode = $"<code>{student.Str("studentId", studentId)}</code>";

        var info = $"🎓 <b>STUDENT REPORT CARD</b>\n━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                   $"👤 <b>{name}</b>\n🆔 ID: {sidCode}\n\n" +
                   $"📋 <b><u>Academic Details</u></b>\n├ 🏛 <b>Dept:</b> {deptName}\n├ 📜 <b>Roll No:</b> {roll}\n" +
                   $"├ 📅 <b>Batch:</b> {batch}\n└ {statusIcon} <b>Status:</b> {status}\n\n" +
                   $"📊 <b><u>Performance</u></b>\n├ 🙋‍♂️ <b>Attendance:</b> {attSummary}\n" +
                   $"└ 🔢 <b>Courses:</b> {coursesCount}\n\n" +
                   $"📚 <b><u>Enrolled Courses</u></b>\n{courseListStr}\n\n" +
                   $"📞 <b><u>Contact Info</u></b>\n├ 📧 {email}\n├ 📱 {phone}\n└ 📍 {addr}\n\n" +
                   $"📝 <b><u>Personal Info</u></b>\n├ 🎂 <b>DOB:</b> {dob}\n└ ⚧ <b>Gender:</b> {gender}\n━━━━━━━━━━━━━━━━━━━━━━";

        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Delete Student", $"delete_student_{studentId}") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to List", "admin_students") }
        });

        await _bot.EditMessageText(chatId, query.Message!.MessageId, info, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task StartSearch(CallbackQuery query, long userId)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        _sessions.SetState(userId, "student_adm_search");
        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Menu", "admin_students") } });
        await _bot.SendMessage(query.Message!.Chat.Id, "🔍 <b>Search Student</b>\n\nEnter the <b>Name</b> (First/Last) or <b>Roll No</b> to search:", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task StartAdd(CallbackQuery query, long userId)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        _sessions.SetState(userId, "student_adm_add_firstname");
        await _bot.SendMessage(query.Message!.Chat.Id, "➕ <b>Add New Student</b>\nStep 1: Enter <b>First Name</b>:", parseMode: ParseMode.Html);
    }

    private async Task DeleteStudentConfirm(CallbackQuery query, string studentId)
    {
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Yes, Delete Forever", $"confirm_del_student_{studentId}") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", $"view_student_{studentId}") }
        });
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, $"⚠️ <b>Delete Student?</b>\n\nAre you sure you want to delete this student?\nThis action <b>cannot</b> be undone.", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task DeleteStudent(CallbackQuery query, string studentId)
    {
        var userId = query.From.Id;
        var (_, err) = await _api.DeleteAsync(userId, $"/api/student/{studentId}");
        if (err == null)
        {
            await _bot.AnswerCallbackQuery(query.Id, "Student Deleted!", showAlert: true);
            await ListDepartments(query);
        }
        else
        {
            await _bot.AnswerCallbackQuery(query.Id, $"Failed to delete: {err}", showAlert: true);
        }
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id; var text = msg.Text ?? "";
        switch (state)
        {
            case "student_adm_search":
                _sessions.ClearState(userId);
                var wait = await _bot.SendMessage(chatId, "🔍 Searching...");
                var (data, error) = await _api.GetAsync(userId, $"/api/student?SearchQuery={text}&PageSize=10");
                await _bot.DeleteMessage(chatId, wait.MessageId);
                await ShowSearchResults(chatId, data, error, text);
                break;
            case "student_adm_add_firstname": _sessions.SetData(userId, "add_s_fn", text); _sessions.SetState(userId, "student_adm_add_lastname"); await _bot.SendMessage(chatId, "Step 2: Enter <b>Last Name</b>:", parseMode: ParseMode.Html); break;
            case "student_adm_add_lastname": _sessions.SetData(userId, "add_s_ln", text); _sessions.SetState(userId, "student_adm_add_email"); await _bot.SendMessage(chatId, "Step 3: Enter <b>Email</b>:", parseMode: ParseMode.Html); break;
            case "student_adm_add_email": _sessions.SetData(userId, "add_s_email", text); _sessions.SetState(userId, "student_adm_add_phone"); await _bot.SendMessage(chatId, "Step 4: Enter <b>Phone</b>:", parseMode: ParseMode.Html); break;
            case "student_adm_add_phone": _sessions.SetData(userId, "add_s_phone", text); _sessions.SetState(userId, "student_adm_add_password"); await _bot.SendMessage(chatId, "Step 5: Enter <b>Password</b>:", parseMode: ParseMode.Html); break;
            case "student_adm_add_password": await FinishAdd(chatId, userId, text); break;
        }
    }

    private async Task ShowSearchResults(long chatId, JsonElement? data, string? error, string query)
    {
        if (error != null) { await _bot.SendMessage(chatId, $"❌ {error}", replyMarkup: MenuHandler.BackButton("admin_students")); return; }
        
        var students = data?.ValueKind == JsonValueKind.Array ? data.Value : default;
        if (students.ValueKind != JsonValueKind.Array || students.GetArrayLength() == 0)
        {
            await _bot.SendMessage(chatId, $"❌ No students found matching <b>{query}</b>.", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_students"));
            return;
        }

        var sb = new StringBuilder($"🔍 <b>Search Results for {query}</b>\n━━━━━━━━━━━━━━━━━━━━\n\n");
        var kb = new List<InlineKeyboardButton[]>();
        
        foreach (var s in students.EnumerateArray())
        {
            var fn = s.Str("firstName"); var ln = s.Str("lastName");
            var name = string.IsNullOrWhiteSpace(fn + ln) ? s.Str("email", "Unknown") : $"{fn} {ln}";
            var id = s.Str("studentId", s.Str("id"));
            sb.AppendLine($"• <b>{name}</b>");
            if (!string.IsNullOrEmpty(id)) kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"👁 {name}", $"view_student_{id}") });
        }
        
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Search", "search_student_start") });
        await _bot.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task FinishAdd(long chatId, long userId, string password)
    {
        var payload = new
        {
            firstName = _sessions.GetData<string>(userId, "add_s_fn"),
            lastName = _sessions.GetData<string>(userId, "add_s_ln"),
            email = _sessions.GetData<string>(userId, "add_s_email"),
            phone = _sessions.GetData<string>(userId, "add_s_phone"),
            password
        };
        _sessions.ClearState(userId);
        var (_, err) = await _api.PostAsync(userId, "/api/student", payload);
        await _bot.SendMessage(chatId, err != null ? $"❌ {err}" : "✅ <b>Student added!</b>", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_students"));
    }
}
