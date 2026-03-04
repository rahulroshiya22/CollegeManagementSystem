using CMS.TelegramService.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers;

public class AuthHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    private const string ImgUrl = "https://i.ibb.co/jZPS8PfB/16852bce-e5a1-44d9-8e45-dc281923dd58-0-1.jpg";

    public AuthHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    {
        _bot = bot; _sessions = sessions; _api = api;
    }

    public async Task StartLogin(long chatId, long userId)
    {
        var welcome = "🤖 <b>College Management Bot</b>\n" +
                      "✨ <b>Built by Rahul Roshiya</b>\n" +
                      "━━━━━━━━━━━━━━━━━━━━\n\n" +
                      "🚀 <b>Smart Features:</b>\n" +
                      "📋 <b>Real-time Attendance</b>\n" +
                      "📝 <b>Exam Results &amp; Grades</b>\n" +
                      "📢 <b>Instant Notices</b>\n" +
                      "💳 <b>Fee Status &amp; Receipts</b>\n\n" +
                      "🔐 <i>Secure Login Required to access features.</i>\n" +
                      "👇 <b>Click 'Login' to get started!</b>";

        var kb = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🔐 Login Now", "start_login"));
        try
        {
            await _bot.SendPhoto(chatId, ImgUrl, caption: welcome, parseMode: ParseMode.Html, replyMarkup: kb);
        }
        catch
        {
            await _bot.SendMessage(chatId, welcome, parseMode: ParseMode.Html, replyMarkup: kb);
        }
    }

    public async Task HandleLoginStartCallback(CallbackQuery query)
    {
        var userId = query.From.Id;
        _sessions.SetState(userId, "auth_email");
        _sessions.SetData(userId, "login_msg_id", query.Message!.MessageId);

        try
        {
            await _bot.EditMessageCaption(query.Message.Chat.Id, query.Message.MessageId,
                "🔐 <b>Login Portal</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                "👋 Welcome! Let's get you signed in.\n\n" +
                "📧 <b>Step 1:</b> Please enter your <b>Email ID</b>.\n<i>(e.g., student@cms.com)</i>",
                parseMode: ParseMode.Html);
        }
        catch
        {
            await _bot.SendMessage(query.Message.Chat.Id,
                "📧 <b>Step 1:</b> Please enter your <b>Email ID</b>.",
                parseMode: ParseMode.Html);
        }
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id;
        var chatId = msg.Chat.Id;

        switch (state)
        {
            case "auth_email":
                _sessions.SetData(userId, "login_email", msg.Text!);
                _sessions.SetState(userId, "auth_password");
                var m = await _bot.SendMessage(chatId,
                    "🔐 <b>Login Portal</b>\n━━━━━━━━━━━━━━━━━━━━\n✅ Email accepted.\n\n🔑 <b>Step 2:</b> Please enter your <b>Password</b>.",
                    parseMode: ParseMode.Html);
                _sessions.SetData(userId, "pw_msg_id", m.MessageId);
                _sessions.SetData(userId, "email_msg_id", msg.MessageId);
                break;

            case "auth_password":
                await HandlePasswordAsync(msg, userId, chatId);
                break;
        }
    }

    private async Task HandlePasswordAsync(Message msg, long userId, long chatId)
    {
        var email = _sessions.GetData<string>(userId, "login_email") ?? "";
        var verifyMsg = await _bot.SendMessage(chatId, "🔄 Verifying credentials...");

        var (data, error) = await _api.LoginAsync(email, msg.Text!);

        await _bot.DeleteMessage(chatId, verifyMsg.MessageId);

        if (error != null || data == null)
        {
            await _bot.SendMessage(chatId, $"❌ <b>Login Failed:</b> {error ?? "Unknown error."}\n\nPlease try again. Send your email:", parseMode: ParseMode.Html);
            _sessions.SetState(userId, "auth_email");
            return;
        }

        // Extract token from response
        var resp = data.Value;
        string? token = null;
        if (resp.TryGetProperty("token", out var t)) token = t.GetString();
        if (token == null && resp.TryGetProperty("accessToken", out var at)) token = at.GetString();

        if (string.IsNullOrEmpty(token))
        {
            await _bot.SendMessage(chatId, "❌ Login failed — no token received. Try again. Send your email:", parseMode: ParseMode.Html);
            _sessions.SetState(userId, "auth_email");
            return;
        }

        _sessions.SaveSessionFromElement(userId, token, resp);
        _sessions.ClearState(userId);

        // Cleanup login messages
        foreach (var key in new[] { "login_msg_id", "email_msg_id", "pw_msg_id" })
        {
            var mid = _sessions.GetData<long>(userId, key);
            if (mid > 0) try { await _bot.DeleteMessage(chatId, (int)mid); } catch { }
        }
        try { await _bot.DeleteMessage(chatId, msg.MessageId); } catch { }

        // Go straight to dashboard
        await new MenuHandler(_bot, _sessions, _api).ShowDashboard(chatId, userId);
    }

    public async Task Logout(CallbackQuery query)
    {
        var userId = query.From.Id;
        _sessions.ClearSession(userId);
        _sessions.ClearState(userId);

        try { await query.Message!.Delete(_bot); } catch { }
        await StartLogin(query.Message!.Chat.Id, userId);
    }
}

internal static class MessageExtensions
{
    internal static Task Delete(this Message msg, ITelegramBotClient bot)
        => bot.DeleteMessage(msg.Chat.Id, msg.MessageId);
}
