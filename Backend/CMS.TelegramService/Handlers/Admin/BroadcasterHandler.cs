using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class BroadcasterHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public BroadcasterHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? ""; var userId = query.From.Id;

        // In Python, admin_post_management directly starts the broadcast.
        // But keeping the submenu is helpful for viewing groups. We'll start broadcast directly on broadcast_new.
        if (data == "admin_post_management") { await ShowBroadcastMenu(query); return; }
        if (data == "bc_start") { await StartBroadcast(query, userId); return; }
        if (data == "bc_list") { await ShowRegisteredGroups(query); return; }
        
        if (data.StartsWith("bc_dept_")) { await AskSemester(query, data.Replace("bc_dept_", ""), userId); return; }
        if (data.StartsWith("bc_sem_")) { await AskCategory(query, data.Replace("bc_sem_", ""), userId); return; }
        if (data.StartsWith("bc_cat_")) { await PrepareMessage(query, data.Replace("bc_cat_", ""), userId); return; }
        
        if (data.StartsWith("bc_send_")) { await ExecuteBroadcast(query, data.Replace("bc_send_", ""), userId); return; }
        if (data == "bc_cancel") { await CancelBroadcast(query, userId); return; }
    }

    private async Task ShowBroadcastMenu(CallbackQuery query)
    {
        var groups = GroupDb.GetAll();
        try { await query.Message!.Delete(_bot); } catch { }
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📢 New Broadcast", "bc_start") },
            new[] { InlineKeyboardButton.WithCallbackData($"📋 Registered Groups ({groups.Count})", "bc_list") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Back", "main_menu") }
        });
        await _bot.SendMessage(query.Message!.Chat.Id, "📢 <b>Post Management</b>\n━━━━━━━━━━━━━━━━━━━━", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ShowRegisteredGroups(CallbackQuery query)
    {
        var groups = GroupDb.GetAll();
        try { await query.Message!.Delete(_bot); } catch { }
        var sb = new StringBuilder("📋 <b>Registered Groups</b>\n━━━━━━━━━━━━━━━━━━━━\n");
        if (groups.Count == 0) sb.AppendLine("No groups registered yet. Use /register_group in a group.");
        else
            foreach (var kv in groups)
                sb.AppendLine($"• <b>{kv.Value.Title}</b> [{kv.Value.Department} | {kv.Value.Semester} | {kv.Value.Category}]");
        await _bot.SendMessage(query.Message!.Chat.Id, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_post_management"));
    }

    private async Task StartBroadcast(CallbackQuery query, long userId)
    {
        var groups = GroupDb.GetAll();
        try { await query.Message!.Delete(_bot); } catch { }
        if (groups.Count == 0)
        {
            await _bot.SendMessage(query.Message!.Chat.Id, "⚠️ <b>No Groups Registered!</b>\n\nPlease add the bot to a group and type <code>/register_group</code> before broadcasting.", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_post_management"));
            return;
        }
        
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🌐 All Departments", "bc_dept_All") },
            new[] { InlineKeyboardButton.WithCallbackData("BCA", "bc_dept_BCA"), InlineKeyboardButton.WithCallbackData("BBA", "bc_dept_BBA") },
            new[] { InlineKeyboardButton.WithCallbackData("B.Tech", "bc_dept_B.Tech"), InlineKeyboardButton.WithCallbackData("B.Com", "bc_dept_B.Com") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", "bc_cancel") }
        });
        await _bot.SendMessage(query.Message!.Chat.Id, "📢 <b>Post Management Engine</b>\n\nLet's create a targeted broadcast.\nStep 1: Which <b>Department</b> do you want to target?", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task AskSemester(CallbackQuery query, string dept, long userId)
    {
        _sessions.SetData(userId, "bc_dept", dept);
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("All Semesters", "bc_sem_All") },
            new[] { InlineKeyboardButton.WithCallbackData("Sem 1", "bc_sem_Sem 1"), InlineKeyboardButton.WithCallbackData("Sem 2", "bc_sem_Sem 2") },
            new[] { InlineKeyboardButton.WithCallbackData("Sem 3", "bc_sem_Sem 3"), InlineKeyboardButton.WithCallbackData("Sem 4", "bc_sem_Sem 4") },
            new[] { InlineKeyboardButton.WithCallbackData("Sem 5", "bc_sem_Sem 5"), InlineKeyboardButton.WithCallbackData("Sem 6", "bc_sem_Sem 6") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", "bc_cancel") }
        });
        await query.Message!.EditMessageText(_bot, $"✅ Targeted Department: <b>{dept}</b>\n\nStep 2: Which <b>Semester</b> do you want to target?", ParseMode.Html, kb);
    }

    private async Task AskCategory(CallbackQuery query, string sem, long userId)
    {
        _sessions.SetData(userId, "bc_sem", sem);
        var dept = _sessions.GetData<string>(userId, "bc_dept") ?? "All";
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("All (Mixed)", "bc_cat_All") },
            new[] { InlineKeyboardButton.WithCallbackData("Boys Only", "bc_cat_Boys"), InlineKeyboardButton.WithCallbackData("Girls Only", "bc_cat_Girls") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", "bc_cancel") }
        });
        await query.Message!.EditMessageText(_bot, $"✅ Targeted Department: <b>{dept}</b>\n✅ Targeted Semester: <b>{sem}</b>\n\nStep 3: Which <b>Category</b> do you want to target?", ParseMode.Html, kb);
    }

    private async Task PrepareMessage(CallbackQuery query, string cat, long userId)
    {
        _sessions.SetData(userId, "bc_cat", cat);
        var dept = _sessions.GetData<string>(userId, "bc_dept") ?? "All";
        var sem = _sessions.GetData<string>(userId, "bc_sem") ?? "All";

        var targets = GroupDb.GetFiltered(dept, sem, cat);
        _sessions.SetData(userId, "bc_targets", JsonSerializer.Serialize(targets));

        if (targets.Count == 0)
        {
            await query.Message!.EditMessageText(_bot, $"⚠️ <b>NO GROUPS MATCH YOUR FILTER.</b>\nDept: {dept} | Sem: {sem} | Cat: {cat}\n\nBroadcast canceled.", ParseMode.Html, MenuHandler.BackButton("admin_post_management"));
            _sessions.ClearState(userId);
            return;
        }

        _sessions.SetState(userId, "bc_awaiting_message");
        await query.Message!.EditMessageText(_bot, $"🎯 <b>Targets Acquired:</b> {targets.Count} Groups found.\n\nStep 4: <b>Send your message now.</b>\n\n<i>Tip: You can send Text, a Photo (with caption), a Video, or a Document.</i>", ParseMode.Html);
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id;
        
        if (state == "bc_awaiting_message")
        {
            _sessions.SetData(userId, "bc_msg_id", msg.MessageId.ToString());
            _sessions.SetData(userId, "bc_chat_id", chatId.ToString());
            _sessions.SetState(userId, "bc_awaiting_button");

            var text = "✅ Message saved.\n\nStep 5 (Optional): Do you want to add an <b>Inline Button</b> to the bottom of the message?\n\nReply with the format: <code>Button Text - https://link.com</code>\nOr just type <code>skip</code> to continue without a button.";
            await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html);
        }
        else if (state == "bc_awaiting_button")
        {
            var btnText = msg.Text?.Trim() ?? "";
            
            if (btnText.Equals("skip", StringComparison.OrdinalIgnoreCase))
            {
                _sessions.SetData(userId, "bc_button", "");
            }
            else
            {
                var parts = btnText.Split('-');
                if (parts.Length < 2)
                {
                    await _bot.SendMessage(chatId, "❌ Invalid format. Please use `Button Name - Link`, or type `skip`.");
                    return;
                }
                var label = parts[0].Trim();
                var url = btnText.Substring(parts[0].Length + 1).Trim();
                if (!url.StartsWith("http")) url = "https://" + url;
                
                var btnData = new { lbl = label, u = url };
                _sessions.SetData(userId, "bc_button", JsonSerializer.Serialize(btnData));
            }
            
            _sessions.ClearState(userId);
            await ShowPreview(chatId, userId);
        }
    }

    private async Task ShowPreview(long chatId, long userId)
    {
        await _bot.SendMessage(chatId, "👀 <b>Here is a preview of your broadcast:</b>", parseMode: ParseMode.Html);
        
        var msgIdStr = _sessions.GetData<string>(userId, "bc_msg_id");
        var chatIdStr = _sessions.GetData<string>(userId, "bc_chat_id");
        int msgId = int.Parse(msgIdStr);
        long fromChatId = long.Parse(chatIdStr);
        
        InlineKeyboardMarkup? markup = null;
        var btnDataStr = _sessions.GetData<string>(userId, "bc_button");
        if (!string.IsNullOrEmpty(btnDataStr))
        {
            var btnData = JsonSerializer.Deserialize<JsonElement>(btnDataStr);
            var lbl = btnData.GetProperty("lbl").GetString()!;
            var u = btnData.GetProperty("u").GetString()!;
            markup = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithUrl(lbl, u) } });
        }

        try
        {
            await _bot.CopyMessage(chatId, fromChatId, msgId, replyMarkup: markup);
        }
        catch (Exception ex)
        {
            await _bot.SendMessage(chatId, $"⚠️ Error rendering preview: {ex.Message}");
        }

        var tList = _sessions.GetData<string>(userId, "bc_targets");
        var targets = JsonSerializer.Deserialize<List<string>>(tList ?? "[]");
        
        var dept = _sessions.GetData<string>(userId, "bc_dept") ?? "All";
        var sem = _sessions.GetData<string>(userId, "bc_sem") ?? "All";
        var cat = _sessions.GetData<string>(userId, "bc_cat") ?? "All";

        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🚀 SEND NOW", "bc_send_normal") },
            new[] { InlineKeyboardButton.WithCallbackData("🔕 Send Silently", "bc_send_silent") },
            new[] { InlineKeyboardButton.WithCallbackData("📌 Send & Pin", "bc_send_pin") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", "bc_cancel") }
        });
        
        var confirmText = $"🎯 <b>Target Match:</b> {targets?.Count ?? 0} Groups\n📊 <b>Filters:</b> Dept: {dept} | Sem: {sem} | Cat: {cat}\n\nReady to blast?";
        await _bot.SendMessage(chatId, confirmText, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ExecuteBroadcast(CallbackQuery query, string mode, long userId)
    {
        await query.Message!.EditMessageText(_bot, "🔄 <b>Broadcasting... Please wait.</b>", ParseMode.Html, null);
        
        var tList = _sessions.GetData<string>(userId, "bc_targets");
        var targets = JsonSerializer.Deserialize<List<string>>(tList ?? "[]") ?? new List<string>();
        
        var msgIdStr = _sessions.GetData<string>(userId, "bc_msg_id");
        var chatIdStr = _sessions.GetData<string>(userId, "bc_chat_id");
        if (string.IsNullOrEmpty(msgIdStr)) { await CancelBroadcast(query, userId); return; }
        
        int msgId = int.Parse(msgIdStr);
        long fromChatId = long.Parse(chatIdStr);
        
        InlineKeyboardMarkup? markup = null;
        var btnDataStr = _sessions.GetData<string>(userId, "bc_button");
        if (!string.IsNullOrEmpty(btnDataStr))
        {
            var btnData = JsonSerializer.Deserialize<JsonElement>(btnDataStr);
            var lbl = btnData.GetProperty("lbl").GetString()!;
            var u = btnData.GetProperty("u").GetString()!;
            markup = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithUrl(lbl, u) } });
        }

        int success = 0, failed = 0;
        bool silent = mode == "silent";
        bool pin = mode == "pin";

        foreach (var tChat in targets)
        {
            try
            {
                long targetChatId = long.Parse(tChat);
                var sentMsg = await _bot.CopyMessage(targetChatId, fromChatId, msgId, replyMarkup: markup, disableNotification: silent);
                success++;
                
                if (pin)
                {
                    try { await _bot.PinChatMessage(targetChatId, sentMsg.Id); } catch { }
                }
            }
            catch { failed++; }
        }

        var report = $"✅ <b>Broadcast Complete!</b>\n\n🟢 Delivered: <b>{success}</b>\n🔴 Failed: <b>{failed}</b>\n";
        if (failed > 0) report += "\n<i>Common fail reason: Bot kicked/banned from group.</i>";

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, report, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_post_management"));
        _sessions.ClearState(userId);
    }

    private async Task CancelBroadcast(CallbackQuery query, long userId)
    {
        _sessions.ClearState(userId);
        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, "❌ Broadcast canceled.", replyMarkup: MenuHandler.BackButton("admin_post_management"));
    }
}

internal static class MessageEditExtension
{
    internal static async Task EditMessageText(this Message msg, ITelegramBotClient bot, string text, ParseMode mode, InlineKeyboardMarkup kb = null)
    {
        try { await bot.EditMessageText(msg.Chat.Id, msg.MessageId, text, parseMode: mode, replyMarkup: kb); }
        catch
        {
            await msg.Delete(bot);
            await bot.SendMessage(msg.Chat.Id, text, parseMode: mode, replyMarkup: kb);
        }
    }
    internal static Task Delete(this Message msg, ITelegramBotClient bot)
        => bot.DeleteMessage(msg.Chat.Id, msg.MessageId);
}

