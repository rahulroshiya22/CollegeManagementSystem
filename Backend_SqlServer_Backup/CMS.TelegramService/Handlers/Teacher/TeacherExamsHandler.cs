using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Teacher;

public class TeacherExamsHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public TeacherExamsHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        if (data == "teacher_exams") { await StartExamCreation(query); return; }
        if (data.StartsWith("tch_exam_course_")) { await SelectCourse(query, data.Replace("tch_exam_course_", "")); return; }
        if (data == "cancel_tch_exam") { await Cancel(query); return; }
    }

    private async Task StartExamCreation(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        try { await query.Message!.Delete(_bot); } catch { }
        
        _sessions.SetState(userId, "tch_exam_title");
        await _bot.SendMessage(chatId, $"🏠 Home > 📝 <b>Create Exam</b>\n━━━━━━━━━━━━━━━━━━━━\n\nEnter the <b>Exam Title</b> (e.g. Mid-Term):", 
            parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton());
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id; var text = msg.Text ?? "";
        switch (state)
        {
            case "tch_exam_title":
                _sessions.SetData(userId, "new_exam_title", text);
                _sessions.ClearState(userId); // Use callback now
                
                var wait = await _bot.SendMessage(chatId, "🔄 Loading courses...");
                var (coursesData, err) = await _api.GetAsync(userId, "/api/course");
                await _bot.DeleteMessage(chatId, wait.MessageId);

                if (err != null) { await _bot.SendMessage(chatId, $"❌ {err}", replyMarkup: MenuHandler.BackButton()); return; }

                var kb = new List<InlineKeyboardButton[]>();
                if (coursesData?.ValueKind == JsonValueKind.Array)
                {
                    int counter = 0;
                    foreach (var c in coursesData.Value.EnumerateArray())
                    {
                        if (counter++ >= 10) break; // limit to 10
                        var cname = c.Str("courseCode", c.Str("name"));
                        var cid = c.Str("courseId", c.Str("id"));
                        if (!string.IsNullOrEmpty(cid))
                            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"📘 {cname}", $"tch_exam_course_{cid}") });
                    }
                }
                
                kb.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", "cancel_tch_exam") });
                await _bot.SendMessage(chatId, $"📝 <b>Select Course</b>\nChoose the course for this exam:", 
                    parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
                break;
                
            case "tch_exam_date":
                _sessions.SetData(userId, "new_exam_date", text);
                _sessions.SetState(userId, "tch_exam_marks");
                await _bot.SendMessage(chatId, "Enter <b>Total Marks</b> (e.g. 100):", parseMode: ParseMode.Html);
                break;

            case "tch_exam_marks":
                _sessions.ClearState(userId);
                if (!int.TryParse(text, out var marks)) marks = 100;
                
                var payload = new
                {
                    title = _sessions.GetData<string>(userId, "new_exam_title"),
                    courseId = int.Parse(_sessions.GetData<string>(userId, "new_exam_course") ?? "0"),
                    scheduledDate = _sessions.GetData<string>(userId, "new_exam_date"),
                    totalMarks = marks,
                    durationMinutes = 60,
                    isPublished = false
                };
                
                var wait2 = await _bot.SendMessage(chatId, "🔄 Creating exam...");
                var (_, saveErr) = await _api.PostAsync(userId, "/api/exam", payload);
                await _bot.DeleteMessage(chatId, wait2.MessageId);
                
                if (saveErr != null)
                    await _bot.SendMessage(chatId, $"❌ Failed: {saveErr}", replyMarkup: MenuHandler.BackButton());
                else
                    await _bot.SendMessage(chatId, "✅ <b>Exam Created Successfully!</b>", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton());
                break;
        }
    }

    private async Task SelectCourse(CallbackQuery query, string cid)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        _sessions.SetData(userId, "new_exam_course", cid);
        _sessions.SetState(userId, "tch_exam_date");
        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(chatId, $"✅ Selected Course ID: <code>{cid}</code>\n\nNow enter <b>Date</b> (YYYY-MM-DD):", parseMode: ParseMode.Html);
    }

    private async Task Cancel(CallbackQuery query)
    {
        _sessions.ClearState(query.From.Id);
        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(query.Message!.Chat.Id, "❌ Exam creation cancelled.", replyMarkup: MenuHandler.BackButton());
    }
}
