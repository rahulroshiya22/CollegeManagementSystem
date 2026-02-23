from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, CallbackQueryHandler
from services.api_client import api_client
from utils.keyboards import back_button_keyboard

async def list_fees(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """List fee records."""
    query = update.callback_query
    response = api_client.get("/api/fee")
    
    if isinstance(response, dict) and "error" in response:
        await query.edit_message_text(f"❌ Error: {response['error']}", reply_markup=back_button_keyboard())
        return
        
    fees = response if isinstance(response, list) else response.get("data", [])
    if isinstance(fees, dict): fees = []
    msg = "💰 **Fee Records**\n\n"
    for f in fees[:5]:
        status_icon = "✅" if f.get("isPaid") else "❌"
        msg += f"{status_icon} **Amount: {f.get('amount')}**\nStudent ID: {f.get('studentId')}\nDue: {f.get('dueDate')}\n---\n"
        
    keyboard = [[InlineKeyboardButton("🔙 Back to Menu", callback_data="menu_main")]]
    await query.edit_message_text(msg, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="Markdown")
