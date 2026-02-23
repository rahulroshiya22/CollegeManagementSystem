from telegram import Update
from telegram.ext import (
    ApplicationBuilder, CommandHandler, ContextTypes, CallbackQueryHandler, 
    MessageHandler, filters, ChatMemberHandler
)
from config import BOT_TOKEN
from services.session import session_manager
from handlers import auth

import logging
logging.basicConfig(format='%(asctime)s - %(name)s - %(levelname)s - %(message)s', level=logging.INFO)

def main():
    app = ApplicationBuilder().token(BOT_TOKEN).build()

    # --- Handlers ---
    # Login & Start are now handled by auth.login_conv_handler
    app.add_handler(auth.login_conv_handler)
    app.add_handler(CommandHandler("logout", auth.logout))

    # --- Feature Modules ---
    from handlers.admin import students, teachers, courses, fees, attendance, exams, notices
    from handlers.teacher import attendance as teacher_attendance
    from handlers.teacher import exams as teacher_exams
    from handlers.student import profile, academics
    from handlers.menu import get_main_menu_keyboard

    # 1. Admin
    # 1. Admin
    # Initial Menu: List Departments
    app.add_handler(CallbackQueryHandler(students.list_departments, pattern="^admin_students$"))
    # Filtered List: List Students by Dept
    app.add_handler(CallbackQueryHandler(students.list_students, pattern="^admin_students_dept_"))
    
    app.add_handler(students.search_student_conv)
    app.add_handler(students.edit_student_conv)
    app.add_handler(students.dm_student_conv)
    app.add_handler(students.add_student_conv)
    
    app.add_handler(CallbackQueryHandler(students.reset_password_confirm, pattern="^reset_pass_"))
    app.add_handler(CallbackQueryHandler(students.do_reset_password, pattern="^do_reset_"))
    
    app.add_handler(CallbackQueryHandler(students.set_status, pattern="^set_status_"))
    
    app.add_handler(CallbackQueryHandler(students.manage_enrollments, pattern="^manage_enrollments_"))
    app.add_handler(CallbackQueryHandler(students.unenroll_confirm, pattern="^unenroll_"))
    app.add_handler(CallbackQueryHandler(students.enroll_course_list, pattern="^enroll_new_"))
    app.add_handler(CallbackQueryHandler(students.perform_enroll, pattern="^do_enroll_"))

    app.add_handler(CallbackQueryHandler(students.view_student, pattern="^view_student_"))
    # app.add_handler(CallbackQueryHandler(students.delete_student, pattern="^delete_student_")) # MOVED TO MAIN HANDLERS


    # Fees - Specific handlers MUST come before generic if using prefix, or use strict regex
    app.add_handler(fees.add_fee_conv)
    app.add_handler(fees.search_fee_conv)
    
    app.add_handler(CallbackQueryHandler(fees.view_filtered_fees, pattern="^admin_fees_filter_"))
    app.add_handler(CallbackQueryHandler(fees.view_fee_detail, pattern="^view_fee_"))
    app.add_handler(CallbackQueryHandler(fees.mark_fee_paid, pattern="^pay_fee_"))
    app.add_handler(CallbackQueryHandler(fees.download_receipt, pattern="^receipt_fee_"))
    app.add_handler(CallbackQueryHandler(fees.view_fee_detail, pattern="^remind_fee_"))
    
    app.add_handler(CallbackQueryHandler(fees.delete_fee_confirm, pattern="^delete_fee_"))
    app.add_handler(CallbackQueryHandler(fees.delete_fee, pattern="^confirm_del_fee_"))
    
    app.add_handler(CallbackQueryHandler(fees.list_fees, pattern="^admin_fees$"))
    
    # Attendance
    app.add_handler(attendance.search_att_conv)
    
    # 1. List Departments -> Calls list_att_departments
    app.add_handler(CallbackQueryHandler(attendance.list_att_departments, pattern="^admin_att_depts$"))
    
    # 2. Select Action (Stats or Students) -> Calls select_dept_action
    app.add_handler(CallbackQueryHandler(attendance.select_dept_action, pattern="^admin_att_action_"))
    
    # 3. List Courses -> Calls list_att_courses
    app.add_handler(CallbackQueryHandler(attendance.list_att_courses, pattern="^admin_att_courses_"))
    
    # 4. List Students -> Calls list_att_dept_students
    app.add_handler(CallbackQueryHandler(attendance.list_att_dept_students, pattern="^admin_att_students_"))
    
    # 5. Views
    app.add_handler(CallbackQueryHandler(attendance.view_course_att_stats, pattern="^view_att_course_"))
    app.add_handler(CallbackQueryHandler(attendance.view_student_att_detail, pattern="^view_att_student_"))
    
    app.add_handler(CallbackQueryHandler(attendance.view_attendance_dashboard, pattern="^admin_attendance"))
    
    app.add_handler(CallbackQueryHandler(exams.list_exams, pattern="^admin_exams$"))
    
    # Notices
    app.add_handler(notices.add_notice_conv)
    app.add_handler(CallbackQueryHandler(notices.delete_notice_confirm, pattern="^del_notice_"))
    app.add_handler(CallbackQueryHandler(notices.delete_notice, pattern="^confirm_del_notice_"))
    app.add_handler(CallbackQueryHandler(notices.list_notices, pattern="^admin_notices"))

    # Impersonation
    from handlers.admin import impersonate
    app.add_handler(CallbackQueryHandler(impersonate.impersonate_menu, pattern="^admin_impersonate"))
    app.add_handler(CallbackQueryHandler(impersonate.imp_sel_role, pattern="^imp_sel_role_"))
    app.add_handler(CallbackQueryHandler(impersonate.imp_list_users, pattern="^imp_sel_dept_"))
    app.add_handler(CallbackQueryHandler(impersonate.imp_perform_login, pattern="^imp_do_"))

    # Timetable
    from handlers.admin import timetable
    app.add_handler(timetable.tt_add_conv)
    app.add_handler(timetable.tt_edit_conv)
    app.add_handler(CallbackQueryHandler(timetable.timetable_menu, pattern="^admin_timetable"))
    app.add_handler(CallbackQueryHandler(timetable.tt_show_filter_list, pattern="^tt_filter_"))
    app.add_handler(CallbackQueryHandler(timetable.view_timetable_day, pattern="^tt_view_day_"))
    app.add_handler(CallbackQueryHandler(timetable.tt_delete_confirm, pattern="^tt_del_confirm_"))
    app.add_handler(CallbackQueryHandler(timetable.tt_delete_final, pattern="^tt_del_final_"))

    app.add_handler(teachers.search_teacher_conv)
    app.add_handler(teachers.edit_teacher_conv)
    app.add_handler(teachers.dm_teacher_conv)
    app.add_handler(teachers.add_teacher_conv)
    app.add_handler(teachers.reset_teacher_pass_conv)
    app.add_handler(CallbackQueryHandler(teachers.list_departments, pattern="^admin_teachers$"))
    app.add_handler(CallbackQueryHandler(teachers.view_teacher_schedule, pattern="^view_tschedule_"))
    app.add_handler(CallbackQueryHandler(teachers.view_teacher_stats, pattern="^view_tstats_"))
    app.add_handler(CallbackQueryHandler(teachers.list_teachers, pattern="^admin_teachers_dept_"))
    
    app.add_handler(teachers.reset_teacher_pass_conv)
    app.add_handler(CallbackQueryHandler(teachers.list_teachers, pattern="^admin_teachers_dept_"))
    
    app.add_handler(CallbackQueryHandler(teachers.view_teacher, pattern="^view_teacher_"))
    app.add_handler(CallbackQueryHandler(teachers.delete_teacher_confirm, pattern="^delete_teacher_"))
    app.add_handler(CallbackQueryHandler(teachers.delete_teacher, pattern="^confirm_del_teacher_"))

    app.add_handler(courses.add_course_conv)
    app.add_handler(courses.search_course_conv)
    
    # Course Routes
    app.add_handler(CallbackQueryHandler(courses.list_departments, pattern="^admin_courses$"))
    app.add_handler(CallbackQueryHandler(courses.list_courses, pattern="^admin_courses_dept_"))
    app.add_handler(CallbackQueryHandler(courses.view_course, pattern="^view_course_"))
    app.add_handler(CallbackQueryHandler(courses.delete_course_confirm, pattern="^delete_course_"))
    app.add_handler(CallbackQueryHandler(courses.delete_course, pattern="^confirm_del_course_"))

    # 2. Teacher
    app.add_handler(teacher_attendance.attendance_conv)
    app.add_handler(teacher_exams.exam_conv)
    app.add_handler(CallbackQueryHandler(teacher_attendance.view_my_classes, pattern="^teacher_classes$"))
    
    # 3. Student
    app.add_handler(CallbackQueryHandler(profile.view_profile, pattern="^my_profile$"))
    app.add_handler(CallbackQueryHandler(profile.view_attendance_stats, pattern="^student_attendance$"))
    app.add_handler(CallbackQueryHandler(profile.view_fees, pattern="^student_fees$"))
    app.add_handler(CallbackQueryHandler(academics.view_results, pattern="^student_results$"))
    app.add_handler(CallbackQueryHandler(academics.view_timetable, pattern="^my_timetable$"))

    # 4. Student Extras (Hub)
    from handlers.student import extras
    from handlers.student.extras import analytics, timetable as tt_extras, fun, utils as util_extras
    
    app.add_handler(extras.extras_menu_handler)
    
    # Analytics
    for h in analytics.analytics_handlers: app.add_handler(h)
    
    # Timetable Tools
    for h in tt_extras.timetable_handlers: app.add_handler(h)
    
    # Fun
    for h in fun.fun_handlers: app.add_handler(h)
    
    # Utilities
    for h in util_extras.utils_handlers: app.add_handler(h)

    # Main Menu
    from handlers.menu import show_dashboard

    async def menu_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
        await show_dashboard(update, context)

    # Handlers
    app.add_handler(CommandHandler("menu", menu_handler))
    app.add_handler(CallbackQueryHandler(menu_handler, pattern="^main_menu$"))
    app.add_handler(CallbackQueryHandler(auth.logout, pattern="^auth_logout$"))

    # Broadcasting & Group Management
    from handlers.admin import broadcaster
    from handlers.admin.group import registration
    from utils.group_db import track_chat, untrack_chat
    
    async def track_chats(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
        """Tracks when the bot is added or removed from a group/channel."""
        result = update.my_chat_member
        if not result: return
        
        chat = result.chat
        new_status = result.new_chat_member.status
        
        if new_status in ["member", "administrator"]:
            # Bot was added
            track_chat(chat.id, chat.title or "Unknown", chat.type)
        elif new_status in ["left", "kicked"]:
            # Bot was removed
            untrack_chat(chat.id)

    app.add_handler(ChatMemberHandler(track_chats, ChatMemberHandler.MY_CHAT_MEMBER))
    app.add_handler(broadcaster.broadcaster_conv_handler)
    app.add_handler(registration.group_registration_handler)
    app.add_handler(registration.unregister_cmd_handler)

    # -- Error Handler --
    async def error_handler(update: object, context: ContextTypes.DEFAULT_TYPE) -> None:
        logging.error(msg="Exception while handling an update:", exc_info=context.error)
        if isinstance(update, Update) and update.effective_message:
            await update.effective_message.reply_text(f"⚠️ <b>System Error:</b>\n<pre>{context.error}</pre>", parse_mode="HTML")

    app.add_error_handler(error_handler)

    print("Bot is running...")
    app.run_polling()

if __name__ == '__main__':
    main()
