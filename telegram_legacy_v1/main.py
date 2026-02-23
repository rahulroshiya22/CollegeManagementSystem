import logging
from telegram import Update
from telegram.ext import ApplicationBuilder, CommandHandler, CallbackQueryHandler, ConversationHandler, MessageHandler, filters
from config import BOT_TOKEN
from handlers.auth import auth_conversation, logout
from handlers.menu import menu_handler
from handlers.users import list_users, user_action_handler
from handlers.teachers import list_teachers, add_teacher_start, teacher_name_handler, teacher_lastname_handler, teacher_email_handler, teacher_dept_handler, teacher_qual_handler, cancel_add, teacher_action_handler, confirm_delete_teacher, TEACHER_NAME, TEACHER_LASTNAME, TEACHER_EMAIL, TEACHER_DEPT, TEACHER_QUAL
from handlers.courses import list_courses, add_course_start, course_title, course_code, course_credits, course_dept, course_sem, cancel_course, course_action_handler, confirm_delete_course, COURSE_TITLE, COURSE_CODE, COURSE_CREDITS, COURSE_DEPT, COURSE_SEM
from handlers.academics import academics_menu, list_announcements, list_exams
from handlers.finance import list_fees
from handlers.finance import list_fees
from handlers.students import list_students, view_student_profile, student_action_handler, view_enrollments, view_fees, view_timetable, add_student_start, st_firstname, st_lastname, st_email, st_phone, st_dob, st_gender, st_address, cancel_op, confirm_delete_student
from handlers.students import ST_FIRSTNAME, ST_LASTNAME, ST_EMAIL, ST_PHONE, ST_DOB, ST_GENDER, ST_ADDRESS
from handlers.academics import academics_menu, list_announcements, list_exams
from handlers.finance import list_fees

# Logging setup
logging.basicConfig(
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    level=logging.INFO
)

if __name__ == '__main__':
    if not BOT_TOKEN or BOT_TOKEN == "YOUR_BOT_TOKEN_HERE":
        print("❌ Error: BOT_TOKEN is missing or default in .env file.")
        exit(1)

    application = ApplicationBuilder().token(BOT_TOKEN).build()

    # Register Handlers
    
    application.add_handler(auth_conversation)
    application.add_handler(CommandHandler("logout", logout))
    
    # Register Feature Handlers FIRST (Specific Patterns)
    
    # Student Handlers & Conversation
    application.add_handler(CallbackQueryHandler(list_users, pattern="^menu_users$"))
    application.add_handler(CallbackQueryHandler(user_action_handler, pattern="^user_"))
    
    # Student Handlers & Conversation
    application.add_handler(CallbackQueryHandler(list_students, pattern="^menu_students$"))
    application.add_handler(CallbackQueryHandler(list_students, pattern="^student_list_page_")) # Pagination
    application.add_handler(CallbackQueryHandler(view_student_profile, pattern="^st_profile_"))
    
    # Student Sub-Menus & Features
    application.add_handler(CallbackQueryHandler(student_action_handler, pattern="^st_(acad|fin|att)_"))
    application.add_handler(CallbackQueryHandler(view_enrollments, pattern="^st_enrolls_"))
    application.add_handler(CallbackQueryHandler(view_fees, pattern="^st_fees_"))
    application.add_handler(CallbackQueryHandler(view_timetable, pattern="^st_timetable_"))
    
    application.add_handler(CallbackQueryHandler(student_action_handler, pattern="^student_edit_")) # Placeholder
    application.add_handler(CallbackQueryHandler(student_action_handler, pattern="^student_delete_")) # Delegate to confirm delete
    application.add_handler(CallbackQueryHandler(confirm_delete_student, pattern="^confirm_del_student_"))
    
    student_conv = ConversationHandler(
        entry_points=[CallbackQueryHandler(add_student_start, pattern="^add_student$")],
        states={
            ST_FIRSTNAME: [MessageHandler(filters.TEXT & ~filters.COMMAND, st_firstname)],
            ST_LASTNAME: [MessageHandler(filters.TEXT & ~filters.COMMAND, st_lastname)],
            ST_EMAIL: [MessageHandler(filters.TEXT & ~filters.COMMAND, st_email)],
            ST_PHONE: [MessageHandler(filters.TEXT & ~filters.COMMAND, st_phone)],
            ST_DOB: [MessageHandler(filters.TEXT & ~filters.COMMAND, st_dob)],
            ST_GENDER: [MessageHandler(filters.TEXT & ~filters.COMMAND, st_gender)],
            ST_ADDRESS: [MessageHandler(filters.TEXT & ~filters.COMMAND, st_address)],
        },
        fallbacks=[CommandHandler("cancel", cancel_op)]
    )
    application.add_handler(student_conv)

    # Teacher Handlers & Conversation
    application.add_handler(CallbackQueryHandler(list_teachers, pattern="^menu_teachers$"))
    application.add_handler(CallbackQueryHandler(teacher_action_handler, pattern="^teacher_"))
    application.add_handler(CallbackQueryHandler(confirm_delete_teacher, pattern="^confirm_del_teacher_"))

    teacher_conv = ConversationHandler(
        entry_points=[CallbackQueryHandler(add_teacher_start, pattern="^add_teacher$")],
        states={
            TEACHER_NAME: [MessageHandler(filters.TEXT & ~filters.COMMAND, teacher_name_handler)],
            TEACHER_LASTNAME: [MessageHandler(filters.TEXT & ~filters.COMMAND, teacher_lastname_handler)],
            TEACHER_EMAIL: [MessageHandler(filters.TEXT & ~filters.COMMAND, teacher_email_handler)],
            TEACHER_DEPT: [MessageHandler(filters.TEXT & ~filters.COMMAND, teacher_dept_handler)],
            TEACHER_QUAL: [MessageHandler(filters.TEXT & ~filters.COMMAND, teacher_qual_handler)],
        },
        fallbacks=[CommandHandler("cancel", cancel_add)]
    )
    application.add_handler(teacher_conv)
    
    # Course Handlers & Conversation
    application.add_handler(CallbackQueryHandler(list_courses, pattern="^menu_courses$"))
    application.add_handler(CallbackQueryHandler(course_action_handler, pattern="^course_"))
    application.add_handler(CallbackQueryHandler(confirm_delete_course, pattern="^confirm_del_course_"))

    course_conv = ConversationHandler(
        entry_points=[CallbackQueryHandler(add_course_start, pattern="^add_course$")],
        states={
            COURSE_TITLE: [MessageHandler(filters.TEXT & ~filters.COMMAND, course_title)],
            COURSE_CODE: [MessageHandler(filters.TEXT & ~filters.COMMAND, course_code)],
            COURSE_CREDITS: [MessageHandler(filters.TEXT & ~filters.COMMAND, course_credits)],
            COURSE_DEPT: [MessageHandler(filters.TEXT & ~filters.COMMAND, course_dept)],
            COURSE_SEM: [MessageHandler(filters.TEXT & ~filters.COMMAND, course_sem)],
        },
        fallbacks=[CommandHandler("cancel", cancel_course)]
    )
    application.add_handler(course_conv)

    # Academic Handlers
    application.add_handler(CallbackQueryHandler(academics_menu, pattern="^menu_academics$"))
    application.add_handler(CallbackQueryHandler(list_announcements, pattern="^acad_announcements$"))
    application.add_handler(CallbackQueryHandler(list_exams, pattern="^acad_exams$"))

    # Finance Handlers
    application.add_handler(CallbackQueryHandler(list_fees, pattern="^menu_fees$"))

    # Generic Menu Handler (Last fallback for other menu_ items)
    application.add_handler(CallbackQueryHandler(menu_handler, pattern="^menu_"))

    print("Admin Bot is starting...")
    application.run_polling()
