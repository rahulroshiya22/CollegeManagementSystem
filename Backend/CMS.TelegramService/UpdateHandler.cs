using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Handlers;
using CMS.TelegramService.Handlers.Admin;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CMS.TelegramService;

public class UpdateHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    // Handler instances
    private readonly AuthHandler _auth;
    private readonly MenuHandler _menu;
    private readonly CMS.TelegramService.Handlers.Admin.StudentsHandler _adminStudents;
    private readonly CMS.TelegramService.Handlers.Admin.TeachersHandler _adminTeachers;
    private readonly CMS.TelegramService.Handlers.Admin.CoursesHandler _adminCourses;
    private readonly CMS.TelegramService.Handlers.Admin.FeesHandler _adminFees;
    private readonly CMS.TelegramService.Handlers.Admin.AttendanceHandler _adminAtt;
    private readonly CMS.TelegramService.Handlers.Admin.NoticesHandler _adminNotices;
    private readonly CMS.TelegramService.Handlers.Admin.ExamsHandler _adminExams;
    private readonly CMS.TelegramService.Handlers.Admin.TimetableHandler _adminTt;
    private readonly CMS.TelegramService.Handlers.Admin.BroadcasterHandler _adminBroadcast;
    private readonly CMS.TelegramService.Handlers.Admin.ImpersonateHandler _adminImpersonate;
    private readonly CMS.TelegramService.Handlers.Admin.GroupRegistrationHandler _adminGroupReg;

    // Teacher Handlers
    private readonly CMS.TelegramService.Handlers.Teacher.TeacherClassesHandler _teacherClasses;
    private readonly CMS.TelegramService.Handlers.Teacher.TeacherAttendanceHandler _teacherAtt;
    private readonly CMS.TelegramService.Handlers.Teacher.TeacherExamsHandler _teacherExams;

    // Student Handlers
    private readonly CMS.TelegramService.Handlers.Student.StudentAcademicsHandler _studentAcademics;
    private readonly CMS.TelegramService.Handlers.Student.StudentProfileHandler _studentProfile;
    private readonly CMS.TelegramService.Handlers.Student.StudentHubHandler _studentHub;

    public UpdateHandler(
        ITelegramBotClient bot,
        SessionService sessions,
        AuthHandler auth,
        MenuHandler menu,
        CMS.TelegramService.Handlers.Admin.StudentsHandler adminStudents,
        CMS.TelegramService.Handlers.Admin.TeachersHandler adminTeachers,
        CMS.TelegramService.Handlers.Admin.CoursesHandler adminCourses,
        CMS.TelegramService.Handlers.Admin.FeesHandler adminFees,
        CMS.TelegramService.Handlers.Admin.AttendanceHandler adminAtt,
        CMS.TelegramService.Handlers.Admin.NoticesHandler adminNotices,
        CMS.TelegramService.Handlers.Admin.ExamsHandler adminExams,
        CMS.TelegramService.Handlers.Admin.TimetableHandler adminTt,
        CMS.TelegramService.Handlers.Admin.BroadcasterHandler adminBroadcast,
        CMS.TelegramService.Handlers.Admin.ImpersonateHandler adminImpersonate,
        CMS.TelegramService.Handlers.Admin.GroupRegistrationHandler adminGroupReg,
        CMS.TelegramService.Handlers.Teacher.TeacherClassesHandler teacherClasses,
        CMS.TelegramService.Handlers.Teacher.TeacherAttendanceHandler teacherAtt,
        CMS.TelegramService.Handlers.Teacher.TeacherExamsHandler teacherExams,
        CMS.TelegramService.Handlers.Student.StudentAcademicsHandler studentAcademics,
        CMS.TelegramService.Handlers.Student.StudentProfileHandler studentProfile,
        CMS.TelegramService.Handlers.Student.StudentHubHandler studentHub)
    {
        _bot = bot; _sessions = sessions; _auth = auth; _menu = menu;
        _adminStudents = adminStudents; _adminTeachers = adminTeachers;
        _adminCourses = adminCourses; _adminFees = adminFees;
        _adminAtt = adminAtt; _adminNotices = adminNotices;
        _adminExams = adminExams; _adminTt = adminTt;
        _adminBroadcast = adminBroadcast; _adminImpersonate = adminImpersonate;
        _adminGroupReg = adminGroupReg;
        _teacherClasses = teacherClasses; _teacherAtt = teacherAtt; _teacherExams = teacherExams;
        _studentAcademics = studentAcademics; _studentProfile = studentProfile; _studentHub = studentHub;
    }

    public async Task HandleUpdateAsync(Update update)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
                await HandleMessageAsync(update.Message);
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                await HandleCallbackAsync(update.CallbackQuery);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            // Telegram API errors (message deleted, etc.) ¬log silently, don't send a message
            Console.WriteLine($"[TG API ERR] {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERR] {ex.GetType().Name}: {ex.Message}");
            // Only send user-facing error for truly unexpected exceptions
            var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
            if (chatId.HasValue)
            {
                try { await _bot.SendMessage(chatId.Value, $"⚠️ <b>Error:</b> {ex.Message}", parseMode: ParseMode.Html); }
                catch { /* ignore send failures */ }
            }
        }
    }

    private async Task HandleMessageAsync(Message msg)
    {
        var text = msg.Text ?? "";
        var userId = msg.From!.Id;
        var chatId = msg.Chat.Id;

        // Global Commands
        if (text == "/start" || text == "/menu")
        {
            if (!_sessions.IsLoggedIn(userId))
                await _auth.StartLogin(chatId, userId);
            else
                await _menu.ShowDashboard(chatId, userId);
            return;
        }

        if (text == "/whatsnew" || text == "/updates")
        {
            var updateMsg = $"🚀 <b>CMS Bot v2.1 Update Notes</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                            $"✨ <b>What's New:</b>\n" +
                            $"• <b>Student Timetable:</b> Added full weekly schedule view.\n" +
                            $"• <b>Fees Module:</b> Pending fee visual dashboard now displays correctly.\n" +
                            $"• <b>Attendance:</b> Added course-wise attendance stats and visual progress bars.\n" +
                            $"• <b>Dashboard:</b> Fixed 'System Offline' bug for Admins.\n\n" +
                            $"<i>Type /menu to explore the new features!</i>";
            await _bot.SendMessage(chatId, updateMsg, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
            return;
        }

        if (text == "/cancel") { _sessions.ClearState(userId); await _bot.SendMessage(msg.Chat.Id, "🚫 Current action cancelled. Use /menu."); return; }
        
        // Group Commands
        if (text == "/register_group") { await _adminGroupReg.StartRegistration(msg); return; }
        if (text == "/unregister_group") { await _adminGroupReg.UnregisterGroup(msg); return; }

        var state = _sessions.GetState(userId);
        if (string.IsNullOrEmpty(state))
        {
            await _bot.SendMessage(msg.Chat.Id, "Use /menu to see available options.");
            return;
        }

        // Route by state
        if (state.StartsWith("auth_")) await _auth.HandleState(msg, state);
        else if (state.StartsWith("student_adm_")) await _adminStudents.HandleState(msg, state);
        else if (state.StartsWith("teacher_adm_")) await _adminTeachers.HandleState(msg, state);
        else if (state.StartsWith("course_")) await _adminCourses.HandleState(msg, state);
        else if (state.StartsWith("fee_")) await _adminFees.HandleState(msg, state);
        else if (state.StartsWith("notice_")) await _adminNotices.HandleState(msg, state);
        else if (state.StartsWith("exam_")) await _adminExams.HandleState(msg, state);
        else if (state.StartsWith("tt_")) await _adminTt.HandleState(msg, state);
        else if (state.StartsWith("bc_")) await _adminBroadcast.HandleState(msg, state);
        else if (state.StartsWith("impersonate_")) await _adminImpersonate.HandleState(msg, state);
        else if (state.StartsWith("tch_exam_")) await _teacherExams.HandleState(msg, state);
        else await _bot.SendMessage(msg.Chat.Id, "I'm waiting for your input, but I lost track of what for. Try /cancel.");
    }

    private async Task HandleCallbackAsync(CallbackQuery query)
    {
        var data = query.Data ?? "";
        await _bot.AnswerCallbackQuery(query.Id);

        // Global Callbacks
        if (data == "start_login") { await _auth.HandleLoginStartCallback(query); return; }
        if (data == "auth_logout") { await _auth.Logout(query); return; }
        if (data == "noop") return; 

        if (data == "main_menu") { await _menu.ShowDashboard(query.Message!.Chat.Id, query.From.Id); return; }

        // Route by prefix/exact string
        if (data.StartsWith("admin_students") || data.StartsWith("view_student_") || data.StartsWith("delete_student_") || data.StartsWith("confirm_del_student_") || data.StartsWith("approve_student_") || data.StartsWith("reject_student_") || data.StartsWith("student_set_status_")) await _adminStudents.HandleCallback(query);
        else if (data.StartsWith("admin_teachers") || data.StartsWith("view_teacher_") || data.StartsWith("delete_teacher_") || data.StartsWith("confirm_del_teacher_") || data.StartsWith("approve_teacher_") || data.StartsWith("reject_teacher_") || data.StartsWith("teacher_set_status_")) await _adminTeachers.HandleCallback(query);
        else if (data.StartsWith("admin_courses") || data.StartsWith("view_course_") || data.StartsWith("delete_course_") || data.StartsWith("confirm_del_course_")) await _adminCourses.HandleCallback(query);
        else if (data.StartsWith("admin_fees") || data.StartsWith("fee_")) await _adminFees.HandleCallback(query);
        else if (data.StartsWith("admin_attendance") || data.StartsWith("att_student_")) await _adminAtt.HandleCallback(query);
        else if (data.StartsWith("admin_notices") || data.StartsWith("notice_")) await _adminNotices.HandleCallback(query);
        else if (data.StartsWith("admin_exams") || data.StartsWith("exam_")) await _adminExams.HandleCallback(query);
        else if (data.StartsWith("admin_timetable") || data.StartsWith("tt_")) await _adminTt.HandleCallback(query);
        else if (data == "admin_post_management" || data.StartsWith("bc_")) await _adminBroadcast.HandleCallback(query);
        else if (data == "admin_add_group" || data.StartsWith("reg_")) await _adminGroupReg.HandleCallback(query);
        else if (data.StartsWith("admin_impersonate") || data.StartsWith("imp_") || data == "impersonate_stop") await _adminImpersonate.HandleCallback(query);
        
        // Teacher Specific
        else if (data == "teacher_classes" || data == "my_timetable") await _teacherClasses.HandleCallback(query);
        else if (data == "teacher_attendance" || data.StartsWith("tch_att_") || data.StartsWith("tch_toggle_") || data.StartsWith("tch_submit_") || data == "cancel_tch_att") await _teacherAtt.HandleCallback(query);
        else if (data == "teacher_exams" || data.StartsWith("tch_exam_") || data == "cancel_tch_exam") await _teacherExams.HandleCallback(query);

        // Student Specific
        else if (data == "student_profile") await _studentProfile.HandleCallback(query);
        else if (data == "student_results" || data == "student_attendance" || data == "student_fees") await _studentAcademics.HandleCallback(query);
        else if (data == "student_hub" || data == "student_timetable" || data.StartsWith("extras_")) await _studentHub.HandleCallback(query);
    }
}
