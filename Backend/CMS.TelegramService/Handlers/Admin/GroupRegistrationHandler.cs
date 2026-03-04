using System.Text;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class GroupRegistrationHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;
    public GroupRegistrationHandler(ITelegramBotClient bot, SessionService sessions, ApiService api) { _bot = bot; _sessions = sessions; _api = api; }

    public async Task StartRegistration(Message msg)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id;
        if (msg.Chat.Type == ChatType.Private)
            await ShowTrackedChatsMenu(chatId, userId, null);
        else
        {
            // In-group: register the current group
            var member = await _bot.GetChatMember(chatId, userId);
            if (member.Status is not ChatMemberStatus.Administrator and not ChatMemberStatus.Creator)
            { await _bot.SendMessage(chatId, "❌ Only Group Admins can register this group."); return; }
            _sessions.SetData(userId, "reg_chat_id", chatId.ToString());
            _sessions.SetData(userId, "reg_title", msg.Chat.Title ?? "Group");
            await AskDept(chatId, userId, null);
        }
    }

    private async Task ShowTrackedChatsMenu(long chatId, long userId, CallbackQuery? query)
    {
        var tracked = GroupDb.GetTracked();
        if (query != null) { try { await query.Message!.Delete(_bot); } catch { } }

        if (tracked.Count == 0)
        {
            await _bot.SendMessage(chatId, "⚠️ I am not inside any Groups or Channels yet!\nPlease add me to a group as an Admin first.", replyMarkup: MenuHandler.BackButton());
            return;
        }

        var rows = new List<InlineKeyboardButton[]>();
        foreach (var kv in tracked)
            rows.Add([InlineKeyboardButton.WithCallbackData($"📢 {kv.Value.Title} ({kv.Value.Type})", $"reg_sel_{kv.Key}")]);
        rows.Add([InlineKeyboardButton.WithCallbackData("❌ Cancel", "reg_cancel")]);

        await _bot.SendMessage(chatId,
            "⚙️ <b>Remote Group/Channel Registration</b>\n\nSelect a group/channel you want to configure:",
            parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(rows));
    }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? ""; var userId = query.From.Id; var chatId = query.Message!.Chat.Id;

        if (data == "admin_add_group") { await ShowTrackedChatsMenu(chatId, userId, query); return; }
        if (data == "reg_cancel") { _sessions.ClearState(userId); try { await query.Message.Delete(_bot); } catch { } await _bot.SendMessage(chatId, "❌ Registration canceled.", replyMarkup: MenuHandler.BackButton("admin_post_management")); return; }
        if (data.StartsWith("reg_sel_")) { await SelectGroup(query, data.Replace("reg_sel_", ""), userId); return; }
        if (data.StartsWith("reg_dept_")) { _sessions.SetData(userId, "reg_dept", data.Replace("reg_dept_", "")); await AskSem(query, userId); return; }
        if (data.StartsWith("reg_sem_")) { _sessions.SetData(userId, "reg_sem", data.Replace("reg_sem_", "")); await AskCat(query, userId); return; }
        if (data.StartsWith("reg_cat_")) { await FinishRegistration(query, data.Replace("reg_cat_", ""), userId); return; }
    }

    private async Task SelectGroup(CallbackQuery query, string chatId, long userId)
    {
        _sessions.SetData(userId, "reg_chat_id", chatId);
        var tracked = GroupDb.GetTracked();
        _sessions.SetData(userId, "reg_title", tracked.TryGetValue(chatId, out var t) ? t.Title : "Unknown Group");
        await AskDept(query.Message!.Chat.Id, userId, query);
    }

    private async Task AskDept(long chatId, long userId, CallbackQuery? query)
    {
        var title = _sessions.GetData<string>(userId, "reg_title");
        var text = $"Step 1: Which <b>Department</b> does this group/channel belong to?\nTarget: <i>{title}</i>";
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("All Departments", "reg_dept_All") },
            new[] { InlineKeyboardButton.WithCallbackData("BCA", "reg_dept_BCA"), InlineKeyboardButton.WithCallbackData("BBA", "reg_dept_BBA") },
            new[] { InlineKeyboardButton.WithCallbackData("B.Tech", "reg_dept_B.Tech"), InlineKeyboardButton.WithCallbackData("B.Com", "reg_dept_B.Com") },
            new[] { InlineKeyboardButton.WithCallbackData("Cancel", "reg_cancel") }
        });
        if (query != null) { try { await query.Message!.Delete(_bot); } catch { } }
        await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task AskSem(CallbackQuery query, long userId)
    {
        var dept = _sessions.GetData<string>(userId, "reg_dept");
        var text = $"✅ Tagged Department: <b>{dept}</b>\n\nStep 2: Which <b>Semester</b>?";
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("All Semesters", "reg_sem_All") },
            new[] { InlineKeyboardButton.WithCallbackData("Sem 1", "reg_sem_Sem 1"), InlineKeyboardButton.WithCallbackData("Sem 2", "reg_sem_Sem 2") },
            new[] { InlineKeyboardButton.WithCallbackData("Sem 3", "reg_sem_Sem 3"), InlineKeyboardButton.WithCallbackData("Sem 4", "reg_sem_Sem 4") },
            new[] { InlineKeyboardButton.WithCallbackData("Sem 5", "reg_sem_Sem 5"), InlineKeyboardButton.WithCallbackData("Sem 6", "reg_sem_Sem 6") },
            new[] { InlineKeyboardButton.WithCallbackData("Cancel", "reg_cancel") }
        });
        await query.Message!.EditMessageText(_bot, text, ParseMode.Html, kb);
    }

    private async Task AskCat(CallbackQuery query, long userId)
    {
        var sem = _sessions.GetData<string>(userId, "reg_sem");
        var text = $"✅ Tagged Semester: <b>{sem}</b>\n\nStep 3: Which <b>Category (Gender)</b>?";
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("All (Mixed)", "reg_cat_All") },
            new[] { InlineKeyboardButton.WithCallbackData("Boys Only", "reg_cat_Boys"), InlineKeyboardButton.WithCallbackData("Girls Only", "reg_cat_Girls") },
            new[] { InlineKeyboardButton.WithCallbackData("Cancel", "reg_cancel") }
        });
        await query.Message!.EditMessageText(_bot, text, ParseMode.Html, kb);
    }

    private async Task FinishRegistration(CallbackQuery query, string cat, long userId)
    {
        var chatId = _sessions.GetData<string>(userId, "reg_chat_id") ?? "";
        var title = _sessions.GetData<string>(userId, "reg_title") ?? "";
        var dept = _sessions.GetData<string>(userId, "reg_dept") ?? "All";
        var sem = _sessions.GetData<string>(userId, "reg_sem") ?? "All";
        _sessions.ClearState(userId);

        GroupDb.Save(chatId, title, dept, sem, cat, userId);

        var text = $"🎉 <b>GROUP REGISTRATION SUCCESSFUL!</b>\n\n" +
                   $"Title: <i>{title}</i>\n" +
                   $"Tags: [{dept}] - [{sem}] - [{cat}]\n\n" +
                   $"This group will now receive targeted broadcasts.";
                   
        await query.Message!.EditMessageText(_bot, text, ParseMode.Html);
        
        // Let's also ensure menu is brought back if done via UI callback (not direct cmd in grp)
        if (query.Message.Chat.Type == ChatType.Private)
        {
            await _bot.SendMessage(query.Message.Chat.Id, "You can manage more groups below:", replyMarkup: MenuHandler.BackButton("admin_post_management"));
        }
    }
    
    public async Task UnregisterGroup(Message msg)
    {
        var chatId = msg.Chat.Id;
        if (msg.Chat.Type == ChatType.Private) return;

        try
        {
            var member = await _bot.GetChatMember(chatId, msg.From!.Id);
            if (member.Status is not ChatMemberStatus.Administrator and not ChatMemberStatus.Creator)
            {
                await _bot.SendMessage(chatId, "❌ Only Group Admins can unregister this group.");
                return;
            }
        }
        catch { return; }

        var deleted = GroupDb.Delete(chatId.ToString());
        if (deleted) await _bot.SendMessage(chatId, "✅ Group unregistered. You will no longer receive broadcasts.");
        else await _bot.SendMessage(chatId, "⚠️ This group was not registered.");
    }
}
