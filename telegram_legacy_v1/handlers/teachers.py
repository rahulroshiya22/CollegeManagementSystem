from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api_client import api_client
from utils.keyboards import back_button_keyboard, teacher_action_keyboard

# States for Add Teacher Conversation
TEACHER_NAME, TEACHER_LASTNAME, TEACHER_EMAIL, TEACHER_DEPT, TEACHER_QUAL = range(5)

async def list_teachers(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Fetches and displays a list of teachers."""
    query = update.callback_query
    
    response = api_client.get("/api/teacher")
    
    if isinstance(response, dict) and "error" in response:
        await query.edit_message_text(f"❌ Error: {response['error']}", reply_markup=back_button_keyboard())
        return

    # Handle List vs Paginated Dict
    if isinstance(response, list):
        teachers = response
    elif isinstance(response, dict) and "data" in response:
        teachers = response["data"]
    else:
        teachers = []
        
    if not teachers:
        await query.edit_message_text("No teachers found.", reply_markup=back_button_keyboard())
        return

    message_text = "👨‍🏫 **Teacher List**\n\n"
    keyboard = []
    
    for teacher in teachers[:5]:
        t_id = teacher.get('teacherId')
        message_text += f"👤 {teacher.get('firstName')} {teacher.get('lastName')} (ID: {t_id})\n"
        message_text += f"📚 {teacher.get('department')} - {teacher.get('qualification')}\n\n"
    
    # Actions for first teacher or general Add button
    keyboard.append([InlineKeyboardButton("➕ Add Teacher", callback_data="add_teacher")])
    
    if teachers:
        first_id = teachers[0].get('teacherId')
        message_text += f"👇 **Actions for first teacher (ID: {first_id})**"
        keyboard.extend(teacher_action_keyboard(first_id).inline_keyboard)
    else:
        keyboard.append([InlineKeyboardButton("🔙 Back", callback_data="menu_main")])

    await query.edit_message_text(text=message_text, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="Markdown")

async def teacher_action_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Handles Edit/Delete actions for teachers."""
    query = update.callback_query
    data = query.data
    parts = data.split("_")
    action = parts[1]
    teacher_id = parts[2]

    if action == "delete":
        confirm = await query.message.reply_text(f"⚠️ Confirm Delete Teacher {teacher_id}?", 
                                                 reply_markup=InlineKeyboardMarkup([
                                                     [InlineKeyboardButton("Yes, Delete", callback_data=f"confirm_del_teacher_{teacher_id}")],
                                                     [InlineKeyboardButton("Cancel", callback_data="menu_teachers")]
                                                 ]))
    elif action == "edit":
         await query.answer("Edit feature coming soon!")

async def confirm_delete_teacher(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    teacher_id = query.data.split("_")[3]
    
    response = api_client.delete(f"/api/teacher/{teacher_id}")
    if "error" in response:
        await query.edit_message_text(f"❌ Failed: {response['error']}", reply_markup=back_button_keyboard())
    else:
        await query.edit_message_text("✅ Teacher Deleted Successfully!", reply_markup=back_button_keyboard())

# --- Add Teacher Flow ---

async def add_teacher_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Starts the add teacher conversation."""
    query = update.callback_query
    await query.answer()
    await query.message.reply_text("📝 **Add New Teacher**\n\nEnter **First Name**:")
    return TEACHER_NAME

async def teacher_name_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["t_firstname"] = update.message.text
    await update.message.reply_text("Enter **Last Name**:")
    return TEACHER_LASTNAME

async def teacher_lastname_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["t_lastname"] = update.message.text
    await update.message.reply_text("Enter **Email**:")
    return TEACHER_EMAIL

async def teacher_email_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["t_email"] = update.message.text
    await update.message.reply_text("Enter **Department** (e.g., Computer Science):")
    return TEACHER_DEPT

async def teacher_dept_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["t_dept"] = update.message.text
    await update.message.reply_text("Enter **Qualification**:")
    return TEACHER_QUAL

async def teacher_qual_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    qualification = update.message.text
    data = {
        "firstName": context.user_data["t_firstname"],
        "lastName": context.user_data["t_lastname"],
        "email": context.user_data["t_email"],
        "department": context.user_data["t_dept"],
        "qualification": qualification
    }
    
    # Call API
    response = api_client.post("/api/teacher", data)
    
    if "error" in response:
        await update.message.reply_text(f"❌ Failed to add teacher: {response['error']}")
    else:
        await update.message.reply_text("✅ Teacher added successfully!")
        
    return ConversationHandler.END

async def cancel_add(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await update.message.reply_text("Action cancelled.")
    return ConversationHandler.END
