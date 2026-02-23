from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api_client import api_client
from utils.keyboards import back_button_keyboard, course_action_keyboard

# States
COURSE_TITLE, COURSE_CODE, COURSE_CREDITS, COURSE_DEPT, COURSE_SEM = range(5)

async def list_courses(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Fetches and displays a list of courses."""
    query = update.callback_query
    
    response = api_client.get("/api/course")
    
    if isinstance(response, dict) and "error" in response:
        await query.edit_message_text(f"❌ Error: {response['error']}", reply_markup=back_button_keyboard())
        return

    # Handle List vs Paginated Dict
    if isinstance(response, list):
        courses = response
    elif isinstance(response, dict) and "data" in response:
        courses = response["data"]
    else:
        courses = []
    if not courses:
        await query.edit_message_text("No courses found.", reply_markup=back_button_keyboard())
        return

    message_text = "📚 **Course List**\n\n"
    keyboard = []
    
    for course in courses[:5]:
        c_id = course.get('courseId')
        message_text += f"📘 **{course.get('title')}** ({course.get('courseCode')})\n"
        message_text += f"Credits: {course.get('credits')} | Sem: {course.get('semester')}\nID: {c_id}\n\n"
        
    keyboard.append([InlineKeyboardButton("➕ Add Course", callback_data="add_course")])
    
    if courses:
        first_id = courses[0].get('courseId')
        message_text += f"👇 **Actions for first course (ID: {first_id})**"
        keyboard.extend(course_action_keyboard(first_id).inline_keyboard)
    else:
        keyboard.append([InlineKeyboardButton("🔙 Back to Menu", callback_data="menu_main")])
        
    await query.edit_message_text(text=message_text, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="Markdown")

async def course_action_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    data = query.data
    parts = data.split("_")
    action = parts[1]
    course_id = parts[2]

    if action == "delete":
        await query.message.reply_text(f"⚠️ Confirm Delete Course {course_id}?", 
             reply_markup=InlineKeyboardMarkup([
                 [InlineKeyboardButton("Yes", callback_data=f"confirm_del_course_{course_id}")],
                 [InlineKeyboardButton("Cancel", callback_data="menu_courses")]
             ]))

async def confirm_delete_course(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    course_id = query.data.split("_")[3]
    response = api_client.delete(f"/api/course/{course_id}")
    if "error" in response:
         await query.edit_message_text(f"❌ Error: {response['error']}")
    else:
         await query.edit_message_text("✅ Course Deleted!")

# --- Add Course Conversation ---
async def add_course_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.message.reply_text("📝 **Add Course**\n\nEnter **Course Title**:")
    return COURSE_TITLE

async def course_title(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["c_title"] = update.message.text
    await update.message.reply_text("Enter **Course Code** (e.g., CS101):")
    return COURSE_CODE

async def course_code(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["c_code"] = update.message.text
    await update.message.reply_text("Enter **Credits** (1-5):")
    return COURSE_CREDITS

async def course_credits(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["c_credits"] = update.message.text
    await update.message.reply_text("Enter **Department ID** (Integer):")
    return COURSE_DEPT

async def course_dept(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["c_dept"] = update.message.text
    await update.message.reply_text("Enter **Semester** (Integer):")
    return COURSE_SEM

async def course_sem(update: Update, context: ContextTypes.DEFAULT_TYPE):
    sem = update.message.text
    try:
        data = {
            "title": context.user_data["c_title"],
            "courseCode": context.user_data["c_code"],
            "credits": int(context.user_data["c_credits"]),
            "departmentId": int(context.user_data["c_dept"]),
            "semester": int(sem)
        }
        response = api_client.post("/api/course", data)
        if "error" in response:
             await update.message.reply_text(f"❌ Error: {response['error']}")
        else:
             await update.message.reply_text("✅ Course Added!")
    except ValueError:
        await update.message.reply_text("❌ Invalid number format. Start over.")
        
    return ConversationHandler.END

async def cancel_course(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await update.message.reply_text("Cancelled.")
    return ConversationHandler.END
