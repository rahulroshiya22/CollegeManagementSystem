from telegram import Update
from telegram.ext import ContextTypes, CallbackQueryHandler
from utils.keyboards import main_menu_keyboard
from utils.helpers import edit_response

async def menu_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Handles main menu navigation."""
    query = update.callback_query
    await query.answer()

    data = query.data
    
    if data == "menu_main":
        await edit_response(
            query,
            "🏠 **Main Menu**\nSelect an option below:",
            main_menu_keyboard()
        )
    # menu_users is now handled by handlers/users.py
    elif data == "menu_teachers":
        await edit_response(query, "👨‍🏫 **Teacher Management** feature coming soon!")
    elif data == "menu_courses":
        await query.edit_message_text(text="📚 **Course Management** feature coming soon!")
    elif data == "menu_academics":
        await query.edit_message_text(text="📅 **Academic Management** feature coming soon!")
    elif data == "menu_fees":
        await query.edit_message_text(text="💰 **Fee Management** feature coming soon!")
    elif data == "menu_settings":
        await query.edit_message_text(text="⚙️ **Settings**\nBot Version: 1.0\nBackend: .NET 8 Microservices")

