from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CallbackQueryHandler, MessageHandler, filters
from utils.formatting import html_bold, html_italic
from handlers.menu import get_back_button

# States for Extras Conversation
SELECT_FEATURE = range(1)

async def show_student_hub(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    keyboard = [
        [InlineKeyboardButton("📊 Analytics & Stats", callback_data="extras_analytics")],
        [InlineKeyboardButton("🎮 Fun Zone", callback_data="extras_fun"),
         InlineKeyboardButton("🛠️ Utilities", callback_data="extras_utils")],
        [InlineKeyboardButton("🔙 Back to Main Menu", callback_data="main_menu")]
    ]
    
    text = (
        f"🚀 {html_bold('Student Hub')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Explore extra features, calculators, and fun tools!\n\n"
        f"👇 Select a category:"
    )
    
    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(
            chat_id=query.message.chat_id,
            text=text,
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup(keyboard)
        )
    else:
        await query.edit_message_text(
            text=text,
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup(keyboard)
        )
    return SELECT_FEATURE

# Placeholder handlers for sub-menus (will be imported from respective files)
# We will register them in main.py individually or as a group.
# For now, let's define the ConversationHandler here or in main.py?
# Better to have a unified entry point or separate handlers.
# Given the complexity, separate handlers usually work better, but a ConversationHandler 
# is good for managing state if we have inputs (like calculators).

# Let's import the sub-handlers
from .analytics import analytics_menu, analytics_router
from .timetable import timetable_menu, timetable_router
from .fun import fun_menu, fun_router
from .utils import utils_menu, utils_router

# We'll need a way to route callbacks.
# A ConversationHandler might be too rigid if we want free navigation.
# Let's use simple CallbackQueryHandlers for the menus, and specific ConversationHandlers for complex inputs.

extras_menu_handler = CallbackQueryHandler(show_student_hub, pattern="^student_hub$")

# We will export a list of handlers to add to the application
def get_extras_handlers():
    return [
        extras_menu_handler,
        # We will add other handlers here as we create them
    ]
