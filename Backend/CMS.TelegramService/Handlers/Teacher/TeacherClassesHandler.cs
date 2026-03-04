using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Teacher;

public class TeacherClassesHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public TeacherClassesHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        if (data == "teacher_classes" || data == "my_timetable") { await ViewMyClasses(query); return; }
    }

    private async Task ViewMyClasses(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id;
        var telegramId = query.From.Id;
        var backendUserId = _sessions.Get(telegramId)?.UserId;

        try { await query.Message.Delete(_bot); } catch { }
        var wait = await _bot.SendMessage(chatId, "🔄 Loading your classes...");

        // 1. Resolve Teacher ID
        var (tData, tErr) = await _api.GetAsync(telegramId, "/api/teacher?PageSize=1000");
        if (tErr != null) { await ShowError(chatId, wait.MessageId, tErr); return; }

        string? teacherId = null;
        if (tData?.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in tData.Value.EnumerateArray())
            {
                if (t.Str("userId") == backendUserId)
                {
                    teacherId = t.Str("teacherId", t.Str("id"));
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(teacherId))
        {
            await ShowError(chatId, wait.MessageId, "Teacher profile not found for your account.");
            return;
        }

        // 2. Fetch TimeSlots (Classes)
        var (slotsData, sErr) = await _api.GetAsync(telegramId, $"/api/timeslot/teacher/{teacherId}");
        if (sErr != null || slotsData?.ValueKind != JsonValueKind.Array)
        {
            await ShowError(chatId, wait.MessageId, sErr ?? "No classes scheduled yet.");
            return;
        }

        var uniqueCids = new HashSet<string>();
        foreach (var s in slotsData.Value.EnumerateArray())
            uniqueCids.Add(s.Str("courseId"));

        if (uniqueCids.Count == 0)
        {
            await _bot.DeleteMessage(chatId, wait.MessageId);
            await _bot.SendMessage(chatId, $"👨‍🏫 <b>My Classes</b>\n\n<i>No classes scheduled yet.</i>", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton());
            return;
        }

        // 3. Aggregate Courses
        var (cData, _) = await _api.GetAsync(telegramId, "/api/course?PageSize=1000");
        var sb = new StringBuilder($"👨‍🏫 <b>My Classes</b>\n━━━━━━━━━━━━━━━━━━━━\n\n");

        if (cData?.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in cData.Value.EnumerateArray())
            {
                if (uniqueCids.Contains(c.Str("courseId")))
                {
                    var code = c.Str("courseCode", "N/A");
                    var name = c.Str("courseName", c.Str("name"));
                    var credits = c.Int("credits");
                    sb.AppendLine($"📘 <b>{name}</b> (<code>{code}</code>)\n   ⭐️ {credits} Credits\n");
                }
            }
        }

        await _bot.DeleteMessage(chatId, wait.MessageId);
        await _bot.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton());
    }

    private async Task ShowError(long chatId, int waitMsgId, string err)
    {
        try { await _bot.DeleteMessage(chatId, waitMsgId); } catch { }
        await _bot.SendMessage(chatId, $"❌ {err}", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton());
    }
}
