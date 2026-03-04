using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class CoursesHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public CoursesHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        var userId = query.From.Id;
        
        if (data == "admin_courses") { await ListDepartments(query); return; }
        if (data.StartsWith("admin_courses_dept_")) { await ListCourses(query); return; }
        if (data == "add_course_start") { await StartAdd(query, userId); return; }
        if (data == "search_course_start") { await StartSearch(query, userId); return; }
        if (data.StartsWith("view_course_")) { await ViewCourse(query, data.Replace("view_course_", "")); return; }
        if (data.StartsWith("delete_course_")) { await DeleteCourseConfirm(query, data.Replace("delete_course_", "")); return; }
        if (data.StartsWith("confirm_del_course_")) { await DeleteCourse(query, data.Replace("confirm_del_course_", "")); return; }
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
                row.Add(InlineKeyboardButton.WithCallbackData($"📚 {name}", $"admin_courses_dept_{did}_page_1"));
                if (row.Count == 2) { kb.Add(row.ToArray()); row.Clear(); }
            }
            if (row.Count > 0) kb.Add(row.ToArray());
        }

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🌐 All Courses", "admin_courses_dept_0_page_1") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔍 Search Course", "search_course_start") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Add New Course", "add_course_start") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Menu", "main_menu") });

        var msg = $"🏫 <b>Course Management</b>\n━━━━━━━━━━━━━━━━━━━━\nSelect a department to view courses:";

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(chatId, msg, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ListCourses(CallbackQuery query)
    {
        var dataParts = query.Data!.Split('_');
        int deptId = 0, page = 1;
        try { deptId = int.Parse(dataParts[3]); page = int.Parse(dataParts[5]); } catch { }

        var userId = query.From.Id;
        var endpoint = deptId > 0 ? $"/api/course/department/{deptId}" : $"/api/course?Page={page}&PageSize=10";
        var (resp, err) = await _api.GetRawAsync(userId, endpoint);

        if (err != null)
        {
            await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, $"❌ Error: {err}", replyMarkup: MenuHandler.BackButton());
            return;
        }

        var root = resp!.Value;
        JsonElement courses = default;
        int totalPages = 1;

        if (root.ValueKind == JsonValueKind.Array) // Manual pagination for department filtering
        {
            int totalItems = root.GetArrayLength();
            int itemsPerPage = 10;
            totalPages = (totalItems + itemsPerPage - 1) / itemsPerPage;
            if (totalPages == 0) totalPages = 1;

            int start = (page - 1) * itemsPerPage;
            int count = Math.Min(itemsPerPage, totalItems - start);
            
            if (start < totalItems)
            {
                var pageItems = new List<JsonElement>();
                int end = start + count;
                int i = 0;
                foreach (var item in root.EnumerateArray())
                {
                    if (i >= start && i < end) pageItems.Add(item);
                    i++;
                }
                courses = JsonSerializer.SerializeToElement(pageItems);
            }
            else
            {
                courses = JsonSerializer.SerializeToElement(new List<JsonElement>());
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            courses = root.TryGetProperty("data", out var d) ? d : root;
            totalPages = root.Int("totalPages", 1);
        }

        if (courses.ValueKind != JsonValueKind.Array || courses.GetArrayLength() == 0)
        {
            var backKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Departments", "admin_courses") } });
            await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, "📚 No Courses found in this department.", replyMarkup: backKb);
            return;
        }

        var text = $"📚 <b>Course Catalog</b> (Page {page}/{totalPages})\n━━━━━━━━━━━━━━━━━━━━\n\n";
        var kb = new List<InlineKeyboardButton[]>();

        foreach (var c in courses.EnumerateArray())
        {
            var name = c.Str("courseName", c.Str("name"));
            var code = c.Str("courseCode", c.Str("code", "N/A"));
            var cid = c.Str("courseId", c.Str("id"));
            var credits = c.Int("credits");
            var sem = c.Int("semester");

            text += $"📖 <b>{name}</b>\n   └ <code>{code}</code> | Sem {sem} | ⭐️ {credits} Cr\n\n";
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"👁️ View {code}", $"view_course_{cid}") });
        }

        var navRow = new List<InlineKeyboardButton>();
        if (page > 1) navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Prev", $"admin_courses_dept_{deptId}_page_{page - 1}"));
        if (page < totalPages) navRow.Add(InlineKeyboardButton.WithCallbackData("Next ➡️", $"admin_courses_dept_{deptId}_page_{page + 1}"));
        if (navRow.Count > 0) kb.Add(navRow.ToArray());

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Switch Department", "admin_courses") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Main Menu", "main_menu") });

        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ViewCourse(CallbackQuery query, string courseId)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (cData, err) = await _api.GetAsync(userId, $"/api/course/{courseId}");
        if (err != null) { await _bot.EditMessageText(chatId, query.Message.MessageId, "❌ Course not found.", replyMarkup: MenuHandler.BackButton()); return; }

        var c = cData!.Value;
        var name = c.Str("courseName", c.Str("name"));
        var code = c.Str("courseCode", c.Str("code"));
        var desc = c.Str("description", "No description available.");
        var credits = c.Int("credits");
        var sem = c.Int("semester");
        var deptId = c.Int("departmentId");

        var deptName = "Unknown Dept";
        if (deptId > 0)
        {
            var (dData, _) = await _api.GetAsync(userId, $"/api/department/{deptId}");
            if (dData?.ValueKind == JsonValueKind.Object) deptName = dData.Value.Str("name", "Unknown Dept");
        }

        var instructorsText = "\n👨‍🏫 <b>Instructors:</b> <i>None assigned</i>\n";
        var (sData, _) = await _api.GetAsync(userId, $"/api/timeslot/course/{courseId}");
        
        if (sData?.ValueKind == JsonValueKind.Array)
        {
            var uniqueTids = new HashSet<int>();
            foreach (var slot in sData.Value.EnumerateArray())
            {
                var tid = slot.Int("teacherId");
                if (tid > 0) uniqueTids.Add(tid);
            }

            if (uniqueTids.Count > 0)
            {
                instructorsText = "\n👨‍🏫 <b>Instructors:</b>\n";
                foreach (var tid in uniqueTids)
                {
                    var (tData, _) = await _api.GetAsync(userId, $"/api/teacher/{tid}");
                    if (tData?.ValueKind == JsonValueKind.Object)
                    {
                        var tName = $"{tData.Value.Str("firstName")} {tData.Value.Str("lastName")}".Trim();
                        instructorsText += $"   • {tName}\n";
                    }
                }
            }
        }

        var info = $"📘 <b>COURSE DETAILS</b>\n━━━━━━━━━━━━━━━━━━━━\n\n" +
                   $"📖 <b>{name}</b>\n🔖 Code: <code>{code}</code>\n\n" +
                   $"🏛 <b>Department:</b> {deptName}\n📅 <b>Semester:</b> {sem}\n⭐️ <b>Credits:</b> {credits}\n" +
                   $"{instructorsText}━━━━━━━━━━━━━━━━━━━━\n" +
                   $"📝 <b>Description:</b>\n<blockquote expandable>{desc}</blockquote>\n" +
                   $"━━━━━━━━━━━━━━━━━━━━";

        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Delete Course", $"delete_course_{courseId}") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to List", "admin_courses") }
        });

        await _bot.EditMessageText(chatId, query.Message!.MessageId, info, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task StartSearch(CallbackQuery query, long userId)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        _sessions.SetState(userId, "course_adm_search");
        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Menu", "admin_courses") } });
        await _bot.SendMessage(query.Message!.Chat.Id, "🔍 <b>Search Course</b>\n\nEnter Course Name or Code:", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task StartAdd(CallbackQuery query, long userId)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        _sessions.SetState(userId, "course_adm_add_name");
        await _bot.SendMessage(query.Message!.Chat.Id, "➕ <b>Add Course</b>\n\nEnter <b>Course Name</b>:", parseMode: ParseMode.Html);
    }

    private async Task DeleteCourseConfirm(CallbackQuery query, string courseId)
    {
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Yes, Delete Forever", $"confirm_del_course_{courseId}") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", $"view_course_{courseId}") }
        });
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message.MessageId, $"⚠️ <b>Delete Course?</b>\n\nAre you sure you want to delete this course?\nThis action <b>cannot</b> be undone.", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task DeleteCourse(CallbackQuery query, string courseId)
    {
        var userId = query.From.Id;
        var (_, err) = await _api.DeleteAsync(userId, $"/api/course/{courseId}");
        
        if (err == null)
        {
            await _bot.AnswerCallbackQuery(query.Id, "Course Deleted!", showAlert: true);
            await ListDepartments(query);
        }
        else
        {
            await _bot.AnswerCallbackQuery(query.Id, $"Failed: {err}", showAlert: true);
        }
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id; var text = msg.Text ?? "";
        switch (state)
        {
            case "course_adm_search":
                _sessions.ClearState(userId);
                var wait = await _bot.SendMessage(chatId, "🔍 Searching...");
                var (data, error) = await _api.GetAsync(userId, "/api/course");
                await _bot.DeleteMessage(chatId, wait.MessageId);
                await ShowSearchResults(chatId, data, error, text);
                break;
            case "course_adm_add_name": _sessions.SetData(userId, "add_course_name", text); _sessions.SetState(userId, "course_adm_add_code"); await _bot.SendMessage(chatId, "Enter <b>Course Code</b> (e.g. CS101):", parseMode: ParseMode.Html); break;
            case "course_adm_add_code": _sessions.SetData(userId, "add_course_code", text); _sessions.SetState(userId, "course_adm_add_credits"); await _bot.SendMessage(chatId, "Enter <b>Credits</b> (1-5):", parseMode: ParseMode.Html); break;
            case "course_adm_add_credits": _sessions.SetData(userId, "add_course_credits", text); _sessions.SetState(userId, "course_adm_add_sem"); await _bot.SendMessage(chatId, "Enter <b>Semester</b> (1-8):", parseMode: ParseMode.Html); break;
            case "course_adm_add_sem": _sessions.SetData(userId, "add_course_sem", text); _sessions.SetState(userId, "course_adm_add_dept"); await _bot.SendMessage(chatId, "Enter <b>Department ID</b> (e.g. 1):", parseMode: ParseMode.Html); break;
            case "course_adm_add_dept": await FinishAdd(chatId, userId, text); break;
        }
    }

    private async Task ShowSearchResults(long chatId, JsonElement? data, string? error, string query)
    {
        if (error != null) { await _bot.SendMessage(chatId, $"❌ {error}", replyMarkup: MenuHandler.BackButton("admin_courses")); return; }
        
        var courses = data?.ValueKind == JsonValueKind.Array ? data.Value : default;
        if (courses.ValueKind != JsonValueKind.Array || courses.GetArrayLength() == 0)
        {
            await _bot.SendMessage(chatId, $"❌ No courses found.", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_courses"));
            return;
        }

        var matches = new List<JsonElement>();
        var qLower = query.ToLower();
        foreach (var c in courses.EnumerateArray())
        {
            var name = c.Str("courseName", c.Str("name")).ToLower();
            var code = c.Str("courseCode", c.Str("code")).ToLower();
            if (name.Contains(qLower) || code.Contains(qLower)) matches.Add(c);
        }

        if (matches.Count == 0)
        {
            await _bot.SendMessage(chatId, $"❌ No courses found for '{query}'", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_courses"));
            return;
        }

        var sb = new StringBuilder($"🔍 <b>Results for \"{query}\"</b>\n━━━━━━━━━━━━━━━━━━━━\n\n");
        var kb = new List<InlineKeyboardButton[]>();
        
        foreach (var c in matches.Take(5))
        {
            var name = c.Str("courseName", c.Str("name"));
            var code = c.Str("courseCode", c.Str("code"));
            var cid = c.Str("courseId", c.Str("id"));
            
            sb.AppendLine($"📖 {code} - {name}");
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"👁️ View {code}", $"view_course_{cid}") });
        }
        
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to List", "admin_courses") });
        await _bot.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task FinishAdd(long chatId, long userId, string deptId)
    {
        var payload = new
        {
            courseName = _sessions.GetData<string>(userId, "add_course_name"),
            courseCode = _sessions.GetData<string>(userId, "add_course_code"),
            credits = int.TryParse(_sessions.GetData<string>(userId, "add_course_credits"), out var c) ? c : 3,
            semester = int.TryParse(_sessions.GetData<string>(userId, "add_course_sem"), out var s) ? s : 1,
            departmentId = int.TryParse(deptId, out var did) ? did : 1,
            description = "Added via Telegram Bot"
        };
        _sessions.ClearState(userId);
        var (_, err) = await _api.PostAsync(userId, "/api/course", payload);
        await _bot.SendMessage(chatId, err != null ? $"❌ Failed: {err}" : "✅ <b>Course Created Successfully!</b>", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_courses"));
    }
}
