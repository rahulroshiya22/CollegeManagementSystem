using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Admin;

public class NoticesHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;
    public NoticesHandler(ITelegramBotClient bot, SessionService sessions, ApiService api) { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? ""; 
        var userId = query.From.Id;
        
        if (data == "admin_notices") { await ListNotices(query); return; }
        
        if (data.StartsWith("del_notice_")) { await DeleteConfirm(query, data.Replace("del_notice_", "")); return; }
        if (data.StartsWith("confirm_del_notice_")) { await DeleteFinal(query, data.Replace("confirm_del_notice_", ""), userId); return; }
        
        if (data == "add_notice") { await AddNoticeStart(query); return; }
        if (data.StartsWith("role_")) { await SelectRole(query, data.Replace("role_", ""), userId); return; }
        if (data.StartsWith("cat_")) { await SelectCategory(query, data.Replace("cat_", ""), userId); return; }
        
        if (data == "confirm_send_notice") { await ConfirmNotice(query, userId); return; }
        if (data == "cancel_notice") { await CancelNotice(query, userId); return; }
    }

    private async Task ListNotices(CallbackQuery query)
    {
        var userId = query.From.Id;
        var (resp, _) = await _api.GetAsync(userId, "/api/notice");
        var notices = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("data", out var d)) notices = d;
        
        var list = new List<JsonElement>();
        if (notices.ValueKind == JsonValueKind.Array) foreach (var n in notices.EnumerateArray()) list.Add(n);
        list.Sort((a, b) => string.Compare(b.Str("createdAt"), a.Str("createdAt"))); // Descending
        
        var sb = new StringBuilder("🏠 Home > 📢 <b>Notice Board</b>\n━━━━━━━━━━━━━━━━━━━━\n");
        var kb = new List<InlineKeyboardButton[]>();
        
        if (list.Count == 0) sb.Append("🚫 <i>No active notices.</i>");
        else
        {
            for (int i = 0; i < Math.Min(5, list.Count); i++)
            {
                var n = list[i];
                var nid = n.Str("noticeId", n.Str("id"));
                var title = n.Str("title", "No Title");
                var content = n.Str("content");
                var role = n.Str("targetRole"); if (string.IsNullOrEmpty(role)) role = "Everyone";
                var date = n.Str("createdAt").Split('T')[0];
                var category = n.Str("category", "General");
                
                var icon = role == "Student" ? "🎓" : role == "Teacher" ? "👨‍🏫" : "📢";
                
                sb.AppendLine($"{icon} <b>{title}</b> ({category})");
                sb.AppendLine($"   └ 📅 {date} | 👥 {role}");
                sb.AppendLine($"<blockquote expandable>{content}</blockquote>\n");
                
                kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"🗑️ Delete: {(title.Length > 15 ? title.Substring(0, 15) + "..." : title)}", $"del_notice_{nid}") });
            }
        }
        
        kb.Insert(0, new[] { InlineKeyboardButton.WithCallbackData("➕ Post New Notice", "add_notice") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Main Menu", "main_menu") });

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task DeleteConfirm(CallbackQuery query, string nid)
    {
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Yes, Delete Forever", $"confirm_del_notice_{nid}") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", "admin_notices") }
        });
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, "⚠️ <b>Delete Notice?</b>\n\nAre you sure you want to delete this notice?\nThis action <b>cannot</b> be undone.", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task DeleteFinal(CallbackQuery query, string nid, long userId)
    {
        var (_, err) = await _api.DeleteAsync(userId, $"/api/notice/{nid}");
        if (err == null) await _bot.AnswerCallbackQuery(query.Id, "✅ Notice deleted!", showAlert: true);
        else await _bot.AnswerCallbackQuery(query.Id, "❌ Failed to delete notice.", showAlert: true);
        query.Data = "admin_notices";
        await ListNotices(query);
    }

    private async Task AddNoticeStart(CallbackQuery query)
    {
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("👥 Everyone", "role_All") },
            new[] { InlineKeyboardButton.WithCallbackData("🎓 Students Only", "role_Student") },
            new[] { InlineKeyboardButton.WithCallbackData("👨‍🏫 Teachers Only", "role_Teacher") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Cancel", "cancel_notice") }
        });
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, "📢 <b>Post New Notice</b>\n\nWho is this notice for?", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task SelectRole(CallbackQuery query, string role, long userId)
    {
        _sessions.SetData(userId, "notice_role", role == "All" ? "" : role);
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📌 General", "cat_General") },
            new[] { InlineKeyboardButton.WithCallbackData("🎓 Academic", "cat_Academic") },
            new[] { InlineKeyboardButton.WithCallbackData("📝 Exam", "cat_Exam") },
            new[] { InlineKeyboardButton.WithCallbackData("🎉 Event", "cat_Event") }
        });
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"Selected Audience: <b>{role}</b>\n\nNow select a <b>Category</b>:", parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task SelectCategory(CallbackQuery query, string cat, long userId)
    {
        _sessions.SetData(userId, "notice_cat", cat);
        _sessions.SetState(userId, "notice_add_title");
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"Selected Category: <b>{cat}</b>\n\nPlease type the <b>Title</b> of the notice:", parseMode: ParseMode.Html);
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id; var text = msg.Text ?? "";
        
        if (state == "notice_add_title")
        {
            _sessions.SetData(userId, "notice_title", text);
            _sessions.SetState(userId, "notice_add_content");
            await _bot.SendMessage(chatId, $"📝 Title: <b>{text}</b>\n\nNow type the <b>Content/Body</b> of the notice:", parseMode: ParseMode.Html);
        }
        else if (state == "notice_add_content")
        {
            _sessions.SetData(userId, "notice_content", text);
            _sessions.ClearState(userId);
            
            var role = _sessions.GetData<string>(userId, "notice_role"); if (string.IsNullOrEmpty(role)) role = "Everyone";
            var cat = _sessions.GetData<string>(userId, "notice_cat");
            var title = _sessions.GetData<string>(userId, "notice_title");
            
            var preview = $"📢 <b>Preview Notice</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                          $"👥 <b>To:</b> {role}\n🏷️ <b>Category:</b> {cat}\n📌 <b>Title:</b> {title}\n" +
                          $"📝 <b>Content:</b>\n<blockquote expandable>{text}</blockquote>\n\nSend this notice?";
            
            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✅ Send Notice", "confirm_send_notice") },
                new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", "cancel_notice") }
            });
            await _bot.SendMessage(chatId, preview, parseMode: ParseMode.Html, replyMarkup: kb);
        }
    }

    private async Task ConfirmNotice(CallbackQuery query, long userId)
    {
        var payload = new
        {
            title = _sessions.GetData<string>(userId, "notice_title"),
            content = _sessions.GetData<string>(userId, "notice_content"),
            category = _sessions.GetData<string>(userId, "notice_cat"),
            targetRole = _sessions.GetData<string>(userId, "notice_role"),
            isActive = true
        };
        
        var (_, err) = await _api.PostAsync(userId, "/api/notice", payload);
        if (err == null) await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, "✅ <b>Notice Posted Successfully!</b>", parseMode: ParseMode.Html);
        else await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, $"❌ Failed to post notice: {err}");
        
        // Return to admin notices
        query.Data = "admin_notices";
        await ListNotices(query);
    }

    private async Task CancelNotice(CallbackQuery query, long userId)
    {
        _sessions.ClearState(userId);
        await _bot.EditMessageText(query.Message!.Chat.Id, query.Message!.MessageId, "❌ Action Cancelled.", replyMarkup: MenuHandler.BackButton("admin_notices"));
    }
}
