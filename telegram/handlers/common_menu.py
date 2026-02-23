from telegram import Update
from telegram.ext import ContextTypes
from services.session import session_manager
from handlers.menu import get_main_menu_keyboard

async def show_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.effective_user.id
    token = session_manager.get_user_token(user_id)
    
    if not token:
        await update.message.reply_text("❌ You are not logged in. Use /start to login.")
        return

    role = session_manager.get_user_role(user_id)
    keyboard = get_main_menu_keyboard(role)
    
    await update.message.reply_text(
        "📱 <b>Main Menu</b>\nSelect an option:",
        reply_markup=keyboard,
        parse_mode="HTML"
    )
