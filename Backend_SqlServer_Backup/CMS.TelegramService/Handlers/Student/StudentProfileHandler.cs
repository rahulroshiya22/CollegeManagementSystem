using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CMS.TelegramService.Handlers.Student;

public class StudentProfileHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public StudentProfileHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        if (data == "student_profile") { await ViewProfile(query); return; }
    }

    private async Task ViewProfile(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id;
        var telegramId = query.From.Id;
        var backendUserId = _sessions.Get(telegramId)?.UserId;

        try { await query.Message.Delete(_bot); } catch { }
        var wait = await _bot.SendMessage(chatId, "🔄 Loading profile...");

        // 1. Fetch Student Info
        var (sData, error) = await _api.GetAsync(telegramId, "/api/student?PageSize=1000");
        if (error != null) { await ShowError(chatId, wait.MessageId, error); return; }

        JsonElement? student = null;
        if (sData?.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in sData.Value.EnumerateArray())
            {
                if (s.Str("userId") == backendUserId)
                {
                    student = s;
                    break;
                }
            }
        }

        if (student == null)
        {
            await ShowError(chatId, wait.MessageId, "Profile not found.");
            return;
        }

        var sObj = student.Value;
        var sid = sObj.Str("studentId", sObj.Str("id"));
        var name = $"{sObj.Str("firstName")} {sObj.Str("lastName")}";
        var deptName = "N/A";
        if (sObj.TryGetProperty("department", out var dept))
            deptName = dept.ValueKind == JsonValueKind.Object ? dept.Str("name", "N/A") : dept.Str("");

        var dob = sObj.Date("dateOfBirth");
        if (string.IsNullOrEmpty(dob)) dob = "N/A";

        var status = sObj.Str("status", "Active");
        var statusIcon = status == "Active" ? "🟢" : "🔴";

        // 2. Attendance Summary
        var (attData, _) = await _api.GetAsync(telegramId, $"/api/attendance?studentId={sid}");
        string attText = "N/A";
        if (attData?.ValueKind == JsonValueKind.Array)
        {
            int total = 0, present = 0;
            foreach (var r in attData.Value.EnumerateArray())
            {
                total++;
                if (r.Bool("isPresent") || r.Str("status") == "Present") present++;
            }
            attText = total > 0 ? $"{present}/{total} ({(present * 100.0 / total):F1}%)" : "0% (No Records)";
        }

        // 3. Enrollments Summary
        var (enrollData, _) = await _api.GetAsync(telegramId, $"/api/enrollment?studentId={sid}");
        int activeCourses = 0;
        var coursesText = "<i>No active enrollments found.</i>";
        
        if (enrollData?.ValueKind == JsonValueKind.Array)
        {
            var courses = new List<string>();
            foreach (var e in enrollData.Value.EnumerateArray())
            {
                activeCourses++;
                if (courses.Count < 5) // Add first 5
                {
                    var c = e.TryGetProperty("course", out var ce) ? ce : default;
                    if (c.ValueKind != JsonValueKind.Undefined)
                        courses.Add($"📘 <code>{c.Str("courseCode", "?")}</code>: {c.Str("title", c.Str("name", "?"))}");
                    else
                        courses.Add($"📘 Course ID: <code>{e.Str("courseId", "?")}</code>");
                }
            }
            if (courses.Count > 0)
            {
                coursesText = string.Join("\n", courses);
                if (activeCourses > 5)
                    coursesText += $"\n<i>...and {activeCourses - 5} more</i>";
            }
        }

        // 4. Build message
        var sb = new StringBuilder();
        sb.AppendLine($"🎓 <b>STUDENT PROFILE</b>\n━━━━━━━━━━━━━━━━━━━━━━\n");
        sb.AppendLine($"👤 <b>{name}</b>");
        sb.AppendLine($"🆔 <code>{sid}</code> | 📜 <code>{sObj.Str("rollNumber", "N/A")}</code>");
        sb.AppendLine($"{statusIcon} <b>Status:</b> {status} | 🎂 {dob}\n");
        
        sb.AppendLine($"🏫 <b>ACADEMIC DETAILS</b>");
        sb.AppendLine($"├ 🏛 <b>Dept:</b> {deptName}");
        sb.AppendLine($"├ 📅 <b>Batch:</b> <code>{sObj.Str("admissionYear", "N/A")}</code>");
        sb.AppendLine($"└ ⚧ <b>Gender:</b> {sObj.Str("gender", "N/A")}\n");
        
        sb.AppendLine($"📊 <b>PERFORMANCE</b>");
        sb.AppendLine($"├ 🙋‍♂️ <b>Attendance:</b> <code>{attText}</code>");
        sb.AppendLine($"└ 📚 <b>Courses:</b> {activeCourses} Active\n");
        
        sb.AppendLine($"📝 <b>ENROLLED COURSES</b>\n{coursesText}\n");
        
        sb.AppendLine($"📞 <b>CONTACT INFO</b>");
        sb.AppendLine($"📧 <code>{sObj.Str("email", "N/A")}</code>");
        sb.AppendLine($"📱 <code>{sObj.Str("phone", "N/A")}</code>");

        string rawAddr = sObj.Str("address", "N/A");
        if (rawAddr.Length > 100) rawAddr = rawAddr.Substring(0, 100) + "...";
        sb.AppendLine($"📍 {rawAddr}\n━━━━━━━━━━━━━━━━━━━━━━");

        await _bot.DeleteMessage(chatId, wait.MessageId);
        await _bot.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton());
    }

    private async Task ShowError(long chatId, int msgId, string text)
    {
        try { await _bot.DeleteMessage(chatId, msgId); } catch { }
        await _bot.SendMessage(chatId, $"❌ {text}", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton());
    }
}
