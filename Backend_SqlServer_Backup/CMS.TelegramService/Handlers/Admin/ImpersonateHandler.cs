using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class ImpersonateHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;
    public ImpersonateHandler(ITelegramBotClient bot, SessionService sessions, ApiService api) { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? ""; 
        var userId = query.From.Id; 

        if (data == "admin_impersonate") { await ShowMenu(query); return; }
        if (data.StartsWith("imp_sel_role_")) { await SelectRole(query, data.Replace("imp_sel_role_", ""), userId); return; }
        if (data.StartsWith("imp_sel_dept_")) { await ListUsers(query, data.Replace("imp_sel_dept_", ""), userId); return; }
        if (data.StartsWith("imp_do_")) { await PerformLogin(query, userId); return; }
        if (data == "impersonate_stop") { await StopImpersonate(query, userId); return; }
    }

    public Task HandleState(Message msg, string state)
    {
        return Task.CompletedTask;
    }

    private async Task ShowMenu(CallbackQuery query)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("�‍🏫 Login as Teacher", "imp_sel_role_Teacher") },
            new[] { InlineKeyboardButton.WithCallbackData("� Login as Student", "imp_sel_role_Student") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Menu", "main_menu") }
        });
        await _bot.SendMessage(query.Message!.Chat.Id,
            "🎭 <b>Login As (Impersonation)</b>\n\n" +
            "Select a role to impersonate. You will browse and select a specific user.",
            parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task SelectRole(CallbackQuery query, string role, long userId)
    {
        _sessions.SetData(userId, "imp_role", role);
        
        var (resp, _) = await _api.GetAsync(userId, "/api/department");
        var depts = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("data", out var d)) depts = d;

        var kb = new List<InlineKeyboardButton[]>();
        var row = new List<InlineKeyboardButton>();

        if (depts.ValueKind == JsonValueKind.Array)
        {
            foreach (var dept in depts.EnumerateArray())
            {
                var did = dept.Str("departmentId", dept.Str("id"));
                var name = dept.Str("name");
                row.Add(InlineKeyboardButton.WithCallbackData(name, $"imp_sel_dept_{did}"));
                if (row.Count == 2) { kb.Add(row.ToArray()); row.Clear(); }
            }
        }
        if (row.Count > 0) kb.Add(row.ToArray());
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "admin_impersonate") });

        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"🏢 <b>Select Department</b> for {role}:", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ListUsers(CallbackQuery query, string deptId, long userId)
    {
        var role = _sessions.GetData<string>(userId, "imp_role") ?? "Student";
        
        var (dResp, _) = await _api.GetAsync(userId, "/api/department");
        var dList = dResp?.ValueKind == JsonValueKind.Array ? dResp.Value : default;
        if (dResp?.ValueKind == JsonValueKind.Object && dResp.Value.TryGetProperty("data", out var dData)) dList = dData;
        string deptName = "";
        if (dList.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in dList.EnumerateArray())
            {
                if (d.Str("departmentId", d.Str("id")) == deptId) { deptName = d.Str("name"); break; }
            }
        }

        string endpoint = role == "Student" ? $"/api/student?DepartmentId={deptId}&PageSize=20" : $"/api/teacher?Department={deptName}&PageSize=20";
        var (resp, _) = await _api.GetAsync(userId, endpoint);
        
        var users = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("data", out var uData)) users = uData;
        else if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("items", out var iData)) users = iData;

        var kb = new List<InlineKeyboardButton[]>();
        if (users.ValueKind == JsonValueKind.Array)
        {
            int count = 0;
            foreach (var u in users.EnumerateArray())
            {
                if (count++ >= 20) break;
                string fname = u.Str("firstName");
                string lname = u.Str("lastName");
                string name = $"{fname} {lname}".Trim();
                string uid = role == "Student" ? u.Str("studentId", u.Str("id")) : u.Str("teacherId", u.Str("id"));

                kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"� {name}", $"imp_do_{role}_{uid}") });
            }
        }
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", $"imp_sel_role_{role}") });

        string msg = kb.Count == 1 ? $"🚫 No {role}s found in <b>{(string.IsNullOrEmpty(deptName)? deptId : deptName)}</b>." : $"👤 <b>Select {role}</b> from {(string.IsNullOrEmpty(deptName)? deptId : deptName)}:";
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, msg, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task PerformLogin(CallbackQuery query, long userId)
    {
        var parts = query.Data!.Split('_'); // imp_do_{role}_{uid}
        var role = parts[2];
        var uid = parts[3];

        var endpoint = role == "Student" ? $"/api/student/{uid}" : $"/api/teacher/{uid}";
        var (resp, err) = await _api.GetAsync(userId, endpoint);
        
        if (err != null || !resp.HasValue || resp.Value.ValueKind != JsonValueKind.Object)
        {
            await _bot.AnswerCallbackQuery(query.Id, "❌ Error: Could not fetch valid user data.", showAlert: true);
            return;
        }

        var data = resp.Value;
        
        _sessions.Impersonate(userId, data);
        var fn = data.Str("firstName", role);

        await _bot.AnswerCallbackQuery(query.Id, $"✅ Logged in as {fn}", showAlert: true);
        
        try { await query.Message!.Delete(_bot); } catch { }
        
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🛑 Stop Impersonating", "impersonate_stop") },
            new[] { InlineKeyboardButton.WithCallbackData("� Go to Main Menu", "main_menu") }
        });

        await _bot.SendMessage(query.Message!.Chat.Id, $"🎭 <b>Now viewing as: {fn}</b>\n<i>Token remains Admin-level. You can now access their dashboard via Menu.</i>", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task StopImpersonate(CallbackQuery query, long userId)
    {
        _sessions.ClearSession(userId);
        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, "✅ Impersonation stopped. Please /start to re-login as Admin.");
    }
}
