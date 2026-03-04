using CMS.TelegramService;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var botToken = builder.Configuration["BotConfiguration:BotToken"]
    ?? throw new Exception("BotToken not configured in appsettings.json");

// --- Services ---
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.AuthHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.MenuHandler>();

// Register Admin handlers
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.StudentsHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.TeachersHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.CoursesHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.FeesHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.AttendanceHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.NoticesHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.ExamsHandler>();

builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.TimetableHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.BroadcasterHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.GroupRegistrationHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.ImpersonateHandler>();

// Register new Teacher handlers
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Teacher.TeacherClassesHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Teacher.TeacherAttendanceHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Teacher.TeacherExamsHandler>();

// Register new Student handlers
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Student.StudentAcademicsHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Student.StudentProfileHandler>();
builder.Services.AddSingleton<CMS.TelegramService.Handlers.Student.StudentHubHandler>();

builder.Services.AddHttpClient("api").ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true });
builder.Services.AddSingleton<ApiService>();
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton<UpdateHandler>();

var app = builder.Build();

app.UseStaticFiles(); // Serve the WebApp HTML files

// --- Start Long Polling ---
var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
var updateHandler = app.Services.GetRequiredService<UpdateHandler>();
var sessions = app.Services.GetRequiredService<SessionService>();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// Start receiving
Console.WriteLine("🤖 CMS Telegram Bot (.NET) starting...");
var me = await botClient.GetMeAsync(cts.Token);
Console.WriteLine($"✅ Bot is running as: @{me.Username}");

// Set up comprehensive command menu
var commands = new List<BotCommand>
{
    new BotCommand { Command = "menu", Description = "🏠 Open Main Dashboard" },
    new BotCommand { Command = "start", Description = "🚀 Start or Restart Bot" },
    new BotCommand { Command = "whatsnew", Description = "✨ View Latest Updates" },
    new BotCommand { Command = "cancel", Description = "❌ Cancel Current Action" }
};
await botClient.SetMyCommandsAsync(commands, cancellationToken: cts.Token);

botClient.StartReceiving(
    updateHandler: async (bot, update, ct) =>
    {
        try
        {
            if (update.Type == UpdateType.MyChatMember && update.MyChatMember != null)
            {
                var chat = update.MyChatMember.Chat;
                var newStatus = update.MyChatMember.NewChatMember.Status;
                if (newStatus is ChatMemberStatus.Member or ChatMemberStatus.Administrator)
                    GroupDb.TrackChat(chat.Id, chat.Title ?? "Unknown", chat.Type.ToString().ToLower());
                else if (newStatus is ChatMemberStatus.Left or ChatMemberStatus.Kicked)
                    GroupDb.UntrackChat(chat.Id);
                return;
            }
            await updateHandler.HandleUpdateAsync(update);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
        }
    },
    errorHandler: (bot, ex, src, ct) =>
    {
        Console.WriteLine($"[POLLING ERROR] {ex.Message}");
        return Task.CompletedTask;
    },
    cancellationToken: cts.Token
);

// Keep running until cancelled
Console.WriteLine("📡 Long polling and Web Server active. Press Ctrl+C to stop.");
try { await app.RunAsync(); } catch (OperationCanceledException) { }

Console.WriteLine("🛑 Bot stopped.");
