from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from handlers.menu import get_back_button
from utils.formatting import html_bold, html_code, html_italic, esc

# States
E_TITLE, E_COURSE, E_DATE, E_MARKS = range(4)

async def start_exam_creation(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    msg_text = (
        f"🏠 Home > 📝 {html_bold('Create Exam')}\n━━━━━━━━━━━━━━━━━━━━\n\n"
        f"Enter the {html_bold('Exam Title')} (e.g. Mid-Term):"
    )
    
    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(
            chat_id=query.message.chat_id,
            text=msg_text,
            parse_mode="HTML", 
            reply_markup=get_back_button(callback_data="main_menu")
        )
    else:
        await query.edit_message_text(
            msg_text, 
            parse_mode="HTML", 
            reply_markup=get_back_button(callback_data="main_menu")
        )
    return E_TITLE

async def e_Title(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_exam_title'] = update.message.text
    
    # Fetch courses
    api = APIClient(update.effective_user.id)
    resp = api.get("/api/course")
    courses = resp if isinstance(resp, list) else resp.get("data", [])
    
    keyboard = []
    # Show active courses
    for c in courses[:10]:
        cname = c.get('courseCode') or c.get('name')
        cid = c.get('courseId') or c.get('id')
        keyboard.append([InlineKeyboardButton(f"📘 {cname}", callback_data=f"exam_sel_course_{cid}")])
        
    keyboard.append([InlineKeyboardButton("❌ Cancel", callback_data="cancel_exam")])
        
    await update.message.reply_text(
        f"📝 {html_bold('Select Course')}\nChoose the course for this exam:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )
    return E_COURSE

async def e_Course_Select(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    cid = query.data.split("_")[3]
    context.user_data['new_exam_course'] = cid
    
    await query.edit_message_text(f"✅ Selected Course ID: {html_code(cid)}\n\nNow enter {html_bold('Date')} (YYYY-MM-DD):", parse_mode="HTML")
    return E_DATE

async def e_Date(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_exam_date'] = update.message.text
    await update.message.reply_text("Enter **Total Marks** (e.g. 100):")
    return E_MARKS

async def e_Marks(update: Update, context: ContextTypes.DEFAULT_TYPE):
    marks = update.message.text
    
    payload = {
        "title": context.user_data['new_exam_title'],
        "courseId": int(context.user_data['new_exam_course']),
        "scheduledDate": context.user_data['new_exam_date'],
        "totalMarks": int(marks),
        "durationMinutes": 60, # Default
        "isPublished": False
    }
    
    api = APIClient(update.effective_user.id)
    resp = api.post("/api/exam", payload)
    
    if resp and "error" not in resp:
        await update.message.reply_text("✅ Exam Created Successfully!")
    else:
        await update.message.reply_text(f"❌ Failed: {resp.get('error')}")
        
    return ConversationHandler.END

async def cancel(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await update.message.reply_text("❌ Exam creation cancelled.")
    return ConversationHandler.END

exam_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(start_exam_creation, pattern='^teacher_exams$')],
    states={
        E_TITLE: [MessageHandler(filters.TEXT, e_Title)],
        E_COURSE: [CallbackQueryHandler(e_Course_Select, pattern="^exam_sel_course_"), CallbackQueryHandler(cancel, pattern="^cancel_exam$")],
        E_DATE: [MessageHandler(filters.TEXT, e_Date)],
        E_MARKS: [MessageHandler(filters.TEXT, e_Marks)],
    },
    fallbacks=[CommandHandler('cancel', cancel), CallbackQueryHandler(cancel, pattern="^cancel_exam$")]
)
