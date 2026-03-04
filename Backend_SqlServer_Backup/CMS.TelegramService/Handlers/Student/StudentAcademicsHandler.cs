using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Student;

public class StudentAcademicsHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public StudentAcademicsHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        if (data == "student_results") { await ViewResults(query); return; }
        if (data == "student_attendance") { await ViewAttendanceStats(query); return; }
        if (data == "student_fees") { await ViewFees(query); return; }
    }

    private async Task ViewResults(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id;
        await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
        
        string text = $"🏠 <b>Home</b> > 👨‍🎓 <b>Student Hub</b> > 📊 <b>Results</b>\n━━━━━━━━━━━━━━━━━━━━\n\n" +
                      $"📭 <i>Yay! No academic results published yet.</i>{FormattingUtils.Footer}";
        try { await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton()); }
        catch { await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton()); }
    }

    private async Task ViewFees(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id;
        await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
        
        string text = $"🏠 <b>Home</b> > 👨‍🎓 <b>Student Hub</b> > 💰 <b>Fee Status</b>\n━━━━━━━━━━━━━━━━━━━━\n\n" +
                      $"⏱️ <i>Feature coming soon!</i>{FormattingUtils.Footer}";
        try { await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton()); }
        catch { await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton()); }
    }

    private async Task ViewAttendanceStats(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id;
        var telegramId = query.From.Id;
        var backendUserId = _sessions.Get(telegramId)?.UserId;

        await _bot.SendChatActionAsync(chatId, ChatAction.Typing);

        // Edit existing message into loading state
        Message waitMsg;
        try { waitMsg = await _bot.EditMessageTextAsync(chatId, query.Message!.MessageId, "🔄 Loading attendance data..."); }
        catch { waitMsg = await _bot.SendMessage(chatId, "🔄 Loading attendance data..."); }

        // 1. Get My Student ID (matching logic used in TeacherClasses)
        var (sDataResp, _) = await _api.GetAsync(telegramId, "/api/student?PageSize=1000");
        var sData = sDataResp?.ValueKind == JsonValueKind.Object && sDataResp.Value.TryGetProperty("data", out var sd) ? sd : sDataResp;
        
        string? studentId = null;
        if (sData?.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in sData.Value.EnumerateArray())
                if (s.Str("userId") == backendUserId) { studentId = s.Str("id", s.Str("studentId")); break; }
        }

        if (string.IsNullOrEmpty(studentId))
        {
            await ShowError(chatId, waitMsg.MessageId, "Could not resolve student profile.");
            return;
        }

        // 2. Fetch my attendance
        var (attDataResp, err) = await _api.GetAsync(telegramId, "/api/attendance?PageSize=1000");
        if (err != null) { await ShowError(chatId, waitMsg.MessageId, err); return; }
        var attData = attDataResp?.ValueKind == JsonValueKind.Object && attDataResp.Value.TryGetProperty("data", out var ad) ? ad : attDataResp;

        int total = 0, present = 0;
        if (attData?.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in attData.Value.EnumerateArray())
            {
                if (r.Str("studentId") == studentId)
                {
                    total++;
                    if (r.Bool("isPresent") || r.Str("status") == "Present") present++;
                }
            }
        }

        if (total == 0)
        {
            var emptyTxt = $"🏠 <b>Home</b> > 👨‍🎓 <b>Student Hub</b> > 📅 <b>Attendance</b>\n━━━━━━━━━━━━━━━━━━━━\n\n" +
                           $"📭 <i>Yay! No attendance records found.</i>{FormattingUtils.Footer}";
            await _bot.EditMessageTextAsync(chatId, waitMsg.MessageId, emptyTxt, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton());
            return;
        }

        double pct = present * 100.0 / total;
        string bar = FormattingUtils.GetProgressBar(pct, 100, 10);
        string dot = FormattingUtils.GetStatusDot(pct);
        string statusMsg = pct < 75 ? $"🚨 <b>ACTION REQUIRED:</b> Low Attendance (<75%)" : $"✅ <b>Good Standing</b>";

        var sb = new StringBuilder($"🏠 <b>Home</b> > 👨‍🎓 <b>Student Hub</b> > 📅 <b>Attendance</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                                   $"📊 <b>Overall Performance</b>\n\n<code>{bar}</code>\n{dot} <b>{pct:F1}% Attendance</b>\n\n" +
                                   $"✅ Present: <b>{present}</b>\n❌ Absent: <b>{total - present}</b>\n📅 Total Classes: <b>{total}</b>\n\n" +
                                   $"{statusMsg}{FormattingUtils.SignatureWatermark}{FormattingUtils.Footer}");

        try { await _bot.EditMessageTextAsync(chatId, waitMsg.MessageId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton()); }
        catch { await _bot.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton()); }
    }

    private async Task ShowError(long chatId, int msgId, string text)
    {
        var errText = text.Contains("<") ? text : $"❌ <b>Action Failed</b>\n{text}{FormattingUtils.Footer}";
        try { await _bot.EditMessageTextAsync(chatId, msgId, errText, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton()); }
        catch { await _bot.SendMessage(chatId, errText, parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton()); }
    }
}
