from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, CallbackQueryHandler
from services.api_client import api_client
from utils.keyboards import back_button_keyboard

async def academics_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Sub-menu for Academic Actions."""
    query = update.callback_query
    
    keyboard = [
        [InlineKeyboardButton("📢 Announcements", callback_data="acad_announcements")],
        [InlineKeyboardButton("📝 Exams", callback_data="acad_exams")],
        [InlineKeyboardButton("🔙 Main Menu", callback_data="menu_main")]
    ]
    
    await query.edit_message_text(
        "📅 **Academic Management**\nSelect an option:",
        reply_markup=InlineKeyboardMarkup(keyboard),
        parse_mode="Markdown"
    )

async def list_announcements(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """List recent announcements."""
    query = update.callback_query
    response = api_client.get("/api/announcement")
    
    if isinstance(response, dict) and "error" in response:
        await query.edit_message_text(f"❌ Error: {response['error']}", reply_markup=back_button_keyboard())
        return
        
    anns = response if isinstance(response, list) else response.get("data", [])
    if isinstance(anns, dict): anns = [] # Fallback if API returns weird dict
    msg = "📢 **Recent Announcements**\n\n"
    for a in anns[:3]:
        msg += f"📌 **{a.get('title')}**\n{a.get('content')}\n---\n"
        
    keyboard = [[InlineKeyboardButton("🔙 Academics Menu", callback_data="menu_academics")]]
    await query.edit_message_text(msg, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="Markdown")

async def list_exams(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """List scheduled exams."""
    query = update.callback_query
    response = api_client.get("/api/exam")
    
    if isinstance(response, dict) and "error" in response:
        await query.edit_message_text(f"❌ Error: {response['error']}", reply_markup=back_button_keyboard())
        return
        
    exams = response if isinstance(response, list) else response.get("data", [])
    if isinstance(exams, dict): exams = []
    msg = "📝 **Scheduled Exams**\n\n"
    for e in exams[:5]:
        msg += f"📄 **{e.get('title')}**\nDate: {e.get('scheduledDate')}\nMarks: {e.get('totalMarks')}\n---\n"
        
    keyboard = [[InlineKeyboardButton("🔙 Academics Menu", callback_data="menu_academics")]]
    await query.edit_message_text(msg, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="Markdown")
