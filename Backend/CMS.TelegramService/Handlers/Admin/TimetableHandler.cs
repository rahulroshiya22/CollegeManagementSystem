using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class TimetableHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;
    private readonly string[] _days = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    public TimetableHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        var userId = query.From.Id;

        if (data == "admin_timetable") { await ShowMenu(query); return; }
        if (data.StartsWith("tt_filter_")) { await ShowFilterList(query, data.Replace("tt_filter_", "")); return; }
        if (data.StartsWith("tt_view_day_")) { await ViewTimetableDay(query); return; }
        
        if (data == "tt_add_start") { await AddStart(query, userId); return; }
        if (data.StartsWith("tt_sel_course_")) { await AddSelectCourse(query, data.Replace("tt_sel_course_", ""), userId); return; }
        if (data.StartsWith("tt_sel_day_")) { await AddSelectDay(query, data.Replace("tt_sel_day_", ""), userId); return; }
        if (data == "tt_confirm_submit") { await AddSubmit(query, userId); return; }

        if (data.StartsWith("tt_edit_room_start")) { await EditRoomStart(query, userId); return; }
        if (data.StartsWith("tt_edit_")) { await EditMenu(query, data.Replace("tt_edit_", ""), userId); return; }
        
        if (data.StartsWith("tt_del_confirm_")) { await DeleteConfirm(query); return; }
        if (data.StartsWith("tt_del_final_")) { await DeleteFinal(query, userId); return; }
    }

    private async Task ShowMenu(CallbackQuery query)
    {
        var text = $"🏠 Home > 📅 <b>Timetable Management</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                   $"Manage class schedules, assign rooms, and resolving conflicts.\n\n" +
                   $"🔍 <b>Filtering:</b> Select a specific Teacher, Department, or Course to view their schedule.";

        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("➕ Add Time Slot", "tt_add_start") },
            new[] { InlineKeyboardButton.WithCallbackData("🗓️ Full Weekly View", "tt_view_day_0_all_0") },
            new[] { InlineKeyboardButton.WithCallbackData("👨‍🏫 By Teacher", "tt_filter_tea"), InlineKeyboardButton.WithCallbackData("🏢 By Dept", "tt_filter_dep") },
            new[] { InlineKeyboardButton.WithCallbackData("📚 By Course", "tt_filter_cou") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Main Menu", "main_menu") }
        });

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ShowFilterList(CallbackQuery query, string filterType)
    {
        var userId = query.From.Id;
        string endpoint = "", title = "";
        if (filterType == "tea") { title = "Select Teacher"; endpoint = "/api/teacher"; }
        else if (filterType == "dep") { title = "Select Department"; endpoint = "/api/department"; }
        else if (filterType == "cou") { title = "Select Course"; endpoint = "/api/course"; }

        var (resp, _) = await _api.GetAsync(userId, endpoint);
        var items = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("data", out var d)) items = d;

        var kb = new List<InlineKeyboardButton[]>();
        var row = new List<InlineKeyboardButton>();

        if (items.ValueKind == JsonValueKind.Array)
        {
            int count = 0;
            foreach (var item in items.EnumerateArray())
            {
                if (count++ >= 20) break; // Limit to 20
                string id = "", name = "";
                
                if (filterType == "tea") { id = item.Str("teacherId", item.Str("id")); name = $"{item.Str("firstName")} {item.Str("lastName")}".Trim(); }
                else if (filterType == "dep") { id = item.Str("departmentId", item.Str("id")); name = item.Str("name"); }
                else if (filterType == "cou") { id = item.Str("courseId", item.Str("id")); name = item.Str("courseCode", item.Str("name")); }

                row.Add(InlineKeyboardButton.WithCallbackData(name.Length > 30 ? name.Substring(0, 30) : name, $"tt_view_day_0_{filterType}_{id}"));
                if (row.Count == 2) { kb.Add(row.ToArray()); row.Clear(); }
            }
        }
        if (row.Count > 0) kb.Add(row.ToArray());
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "admin_timetable") });

        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"🔍 <b>{title}</b>\nSelect one to view timetable:", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ViewTimetableDay(CallbackQuery query)
    {
        var parts = query.Data!.Split('_'); // tt_view_day_{day_idx}_{type}_{id}
        int dayIdx = 0; int.TryParse(parts.Length > 3 ? parts[3] : "0", out dayIdx);
        string filterType = parts.Length > 4 ? parts[4] : "all";
        string filterId = parts.Length > 5 ? parts[5] : "0";

        var currentDay = _days[dayIdx];
        var userId = query.From.Id;
        
        var (slotsResp, _) = await _api.GetAsync(userId, "/api/timeslot");
        var slots = slotsResp?.ValueKind == JsonValueKind.Array ? slotsResp.Value : default;
        if (slotsResp?.ValueKind == JsonValueKind.Object && slotsResp.Value.TryGetProperty("data", out var d)) slots = d;

        var (coursesResp, _) = await _api.GetAsync(userId, "/api/course?PageSize=100");
        var cData = coursesResp?.ValueKind == JsonValueKind.Array ? coursesResp.Value : default;
        if (coursesResp?.ValueKind == JsonValueKind.Object && coursesResp.Value.TryGetProperty("data", out var cd)) cData = cd;
        var cMap = new Dictionary<string, JsonElement>();
        if (cData.ValueKind == JsonValueKind.Array) foreach (var c in cData.EnumerateArray()) cMap[c.Str("courseId", c.Str("id"))] = c;

        var (teachersResp, _) = await _api.GetAsync(userId, "/api/teacher?PageSize=100");
        var tData = teachersResp?.ValueKind == JsonValueKind.Array ? teachersResp.Value : default;
        if (teachersResp?.ValueKind == JsonValueKind.Object && teachersResp.Value.TryGetProperty("data", out var td)) tData = td;
        var tMap = new Dictionary<string, JsonElement>();
        if (tData.ValueKind == JsonValueKind.Array) foreach (var t in tData.EnumerateArray()) tMap[t.Str("teacherId", t.Str("id"))] = t;

        var filteredSlots = new List<JsonElement>();
        string filterTitle = "Full View";

        if (slots.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in slots.EnumerateArray())
            {
                if (!s.Str("dayOfWeek").Equals(currentDay, StringComparison.OrdinalIgnoreCase)) continue;

                var cid = s.Str("courseId");
                var tid = s.Str("teacherId");
                
                if (filterType == "all") { filteredSlots.Add(s); }
                else if (filterType == "tea")
                {
                    if (tid == filterId) filteredSlots.Add(s);
                    filterTitle = $"Teacher: {(tMap.TryGetValue(filterId, out var t) ? t.Str("firstName") : "Teacher")}";
                }
                else if (filterType == "dep")
                {
                    if (cMap.TryGetValue(cid, out var c) && c.Str("departmentId") == filterId) filteredSlots.Add(s);
                    filterTitle = "Department View";
                }
                else if (filterType == "cou")
                {
                    if (cid == filterId) filteredSlots.Add(s);
                    filterTitle = $"Course: {(cMap.TryGetValue(filterId, out var c) ? c.Str("courseCode", c.Str("name")) : "Course")}";
                }
            }
        }

        filteredSlots.Sort((a, b) => string.Compare(a.Str("startTime"), b.Str("startTime")));

        var text = $"🏠 Home > 📅 <b>Timetable</b> > 🗓 <b>{currentDay}</b>\n🔍 Filter: <i>{filterTitle}</i>\n━━━━━━━━━━━━━━━━━━━━\n";
        var kb = new List<InlineKeyboardButton[]>();

        if (filteredSlots.Count == 0) text += "\n🚫 <i>No classes found for this filter.</i>\n";
        else
        {
            foreach (var s in filteredSlots)
            {
                var sid = s.Str("timeSlotId", s.Str("id"));
                var start = s.Str("startTime").Length >= 5 ? s.Str("startTime").Substring(0, 5) : s.Str("startTime");
                var end = s.Str("endTime").Length >= 5 ? s.Str("endTime").Substring(0, 5) : s.Str("endTime");
                var room = s.Str("room");
                var cid = s.Str("courseId");
                var tid = s.Str("teacherId");

                var cName = cMap.TryGetValue(cid, out var c) ? c.Str("courseCode", c.Str("name")) : $"Unknown Course ({cid})";
                var tName = tMap.TryGetValue(tid, out var t) ? t.Str("firstName") : "N/A";

                text += $"⏰ <b>{start} - {end}</b>\n📍 Room: <code>{room}</code>\n📘 <b>{cName}</b> ({tName})\n〰️〰️〰️〰️〰️〰️\n";

                string ctxStr = $"{dayIdx}_{filterType}_{filterId}";
                kb.Add(new[] {
                    InlineKeyboardButton.WithCallbackData("✏️ Edit", $"tt_edit_{sid}"),
                    InlineKeyboardButton.WithCallbackData("🗑️ Del", $"tt_del_confirm_{sid}_{ctxStr}")
                });
            }
        }

        int prevIdx = (dayIdx - 1 + 7) % 7;
        int nextIdx = (dayIdx + 1) % 7;
        kb.Add(new[] {
            InlineKeyboardButton.WithCallbackData($"⬅️ {_days[prevIdx].Substring(0, 3)}", $"tt_view_day_{prevIdx}_{filterType}_{filterId}"),
            InlineKeyboardButton.WithCallbackData($"🗓 {currentDay}", "noop"),
            InlineKeyboardButton.WithCallbackData($"{_days[nextIdx].Substring(0, 3)} ➡️", $"tt_view_day_{nextIdx}_{filterType}_{filterId}")
        });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Menu", "admin_timetable") });

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task AddStart(CallbackQuery query, long userId)
    {
        var (resp, _) = await _api.GetAsync(userId, "/api/course");
        var courses = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("data", out var d)) courses = d;

        var kb = new List<InlineKeyboardButton[]>();
        if (courses.ValueKind == JsonValueKind.Array)
        {
            int count = 0;
            foreach (var c in courses.EnumerateArray())
            {
                if (count++ >= 10) break;
                kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"📘 {c.Str("courseCode", c.Str("name"))}", $"tt_sel_course_{c.Str("courseId", c.Str("id"))}") });
            }
        }
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Cancel", "admin_timetable") });

        var text = $"🏠 Home > 📅 Timetable > ➕ <b>Add Slot</b>\n━━━━━━━━━━━━━━━━━━━━\n<b>Step 1/5:</b> Select Course";
        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task AddSelectCourse(CallbackQuery query, string cid, long userId)
    {
        _sessions.SetData(userId, "tt_cid", cid);
        var kb = new List<InlineKeyboardButton[]>();
        var row = new List<InlineKeyboardButton>();
        for (int i = 0; i < 6; i++)
        {
            row.Add(InlineKeyboardButton.WithCallbackData(_days[i], $"tt_sel_day_{_days[i]}"));
            if (row.Count == 2) { kb.Add(row.ToArray()); row.Clear(); }
        }
        if (row.Count > 0) kb.Add(row.ToArray());
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Cancel", "admin_timetable") });

        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"🏠 Home > 📅 Timetable > ➕ <b>Add Slot</b>\n━━━━━━━━━━━━━━━━━━━━\n<b>Step 2/5:</b> Select Day of Week", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task AddSelectDay(CallbackQuery query, string day, long userId)
    {
        _sessions.SetData(userId, "tt_day", day);
        _sessions.SetState(userId, "tt_start");
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"🏠 Home > 📅 Timetable > ➕ <b>Add Slot</b>\n━━━━━━━━━━━━━━━━━━━━\n<b>Step 3/5:</b> Enter Start Time (24-hour format, e.g. <code>09:00</code>)", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_timetable"));
    }

    private async Task AddSubmit(CallbackQuery query, long userId)
    {
        var payload = new
        {
            courseId = int.Parse(_sessions.GetData<string>(userId, "tt_cid")),
            dayOfWeek = _sessions.GetData<string>(userId, "tt_day"),
            startTime = _sessions.GetData<string>(userId, "tt_start"),
            endTime = _sessions.GetData<string>(userId, "tt_end"),
            room = _sessions.GetData<string>(userId, "tt_room")
        };

        var (resp, err) = await _api.PostAsync(userId, "/api/timeslot", payload);
        if (err == null) await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, "✅ <b>Time Slot Added Successfully!</b>", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_timetable"));
        else await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"❌ Failed: {err}", replyMarkup: MenuHandler.BackButton("admin_timetable"));
    }

    private async Task EditMenu(CallbackQuery query, string tsid, long userId)
    {
        _sessions.SetData(userId, "edit_tt_id", tsid);
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📍 Change Room", "tt_edit_room_start") },
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Delete Slot", $"tt_del_confirm_{tsid}_0_all_0") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Timetable", "tt_view_day_0_all_0") }
        });
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"✏️ <b>Edit Time Slot</b>\n\nSelect a field to update:", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task EditRoomStart(CallbackQuery query, long userId)
    {
        _sessions.SetState(userId, "tt_edit_room");
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"📍 Enter new <b>Room Number</b>:", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("tt_view_day_0_all_0"));
    }

    private async Task DeleteConfirm(CallbackQuery query)
    {
        var parts = query.Data!.Split('_'); // tt_del_confirm_{sid}_{day}_{type}_{id}
        var tsid = parts[3];
        var ctx = parts.Length > 4 ? string.Join("_", parts.Skip(4)) : "0_all_0";

        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Yes, Delete", $"tt_del_final_{tsid}_{ctx}") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", $"tt_view_day_{ctx}") }
        });
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"⚠️ <b>Delete Time Slot?</b>\nAre you sure you want to remove this slot?", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task DeleteFinal(CallbackQuery query, long userId)
    {
        var parts = query.Data!.Split('_');
        var tsid = parts[3];
        var ctx = parts.Length > 4 ? string.Join("_", parts.Skip(4)) : "0_all_0";

        var (_, err) = await _api.DeleteAsync(userId, $"/api/timeslot/{tsid}");
        if (err == null) await _bot.AnswerCallbackQuery(query.Id, "✅ Deleted!", showAlert: true);
        else await _bot.AnswerCallbackQuery(query.Id, "❌ Failed to delete.", showAlert: true);

        query.Data = $"tt_view_day_{ctx}";
        await ViewTimetableDay(query);
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id; var text = msg.Text ?? "";
        
        if (state == "tt_start")
        {
            _sessions.SetData(userId, "tt_start", text);
            _sessions.SetState(userId, "tt_end");
            await _bot.SendMessage(chatId, $"<b>Step 4/5:</b> Enter <b>End Time</b> (e.g. <code>10:00</code>):", parseMode: ParseMode.Html);
        }
        else if (state == "tt_end")
        {
            _sessions.SetData(userId, "tt_end", text);
            _sessions.SetState(userId, "tt_room");
            await _bot.SendMessage(chatId, $"<b>Step 5/5:</b> Enter <b>Room Number</b> (e.g. <code>101</code> or <code>Lab A</code>):", parseMode: ParseMode.Html);
        }
        else if (state == "tt_room")
        {
            _sessions.SetData(userId, "tt_room", text);
            _sessions.ClearState(userId);

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✅ Confirm & Add", "tt_confirm_submit") },
                new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", "admin_timetable") }
            });

            var t = $"📝 <b>Confirm Details</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                    $"📘 <b>Course ID:</b> {_sessions.GetData<string>(userId, "tt_cid")}\n" +
                    $"🗓 <b>Day:</b> {_sessions.GetData<string>(userId, "tt_day")}\n" +
                    $"⏰ <b>Time:</b> {_sessions.GetData<string>(userId, "tt_start")} - {_sessions.GetData<string>(userId, "tt_end")}\n" +
                    $"📍 <b>Room:</b> {text}\n";

            await _bot.SendMessage(chatId, t, parseMode: ParseMode.Html, replyMarkup: kb);
        }
        else if (state == "tt_edit_room")
        {
            _sessions.ClearState(userId);
            var tsid = _sessions.GetData<string>(userId, "edit_tt_id");
            
            var (slotsResp, _) = await _api.GetAsync(userId, "/api/timeslot");
            var slots = slotsResp?.ValueKind == JsonValueKind.Array ? slotsResp.Value : default;
            if (slotsResp?.ValueKind == JsonValueKind.Object && slotsResp.Value.TryGetProperty("data", out var d)) slots = d;

            JsonElement? target = null;
            if (slots.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in slots.EnumerateArray())
                {
                    if (s.Str("timeSlotId", s.Str("id")) == tsid) { target = s; break; }
                }
            }

            if (!target.HasValue)
            {
                await _bot.SendMessage(chatId, "❌ Error: Slot not found.", replyMarkup: MenuHandler.BackButton("admin_timetable"));
                return;
            }

            var payload = new
            {
                courseId = target.Value.Int("courseId"),
                dayOfWeek = target.Value.Str("dayOfWeek"),
                startTime = target.Value.Str("startTime"),
                endTime = target.Value.Str("endTime"),
                room = text
            };

            var (_, err) = await _api.PutAsync(userId, $"/api/timeslot/{tsid}", payload);
            if (err == null) await _bot.SendMessage(chatId, "✅ Room Updated!", replyMarkup: MenuHandler.BackButton("tt_view_day_0_all_0"));
            else await _bot.SendMessage(chatId, $"❌ Failed: {err}", replyMarkup: MenuHandler.BackButton("tt_view_day_0_all_0"));
        }
    }
}
