from telegram import Update, ReplyKeyboardRemove, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api_client import api_client
from utils.keyboards import main_menu_keyboard
from config import BOT_IMAGE_URL

# States
EMAIL, PASSWORD = range(2)

async def start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Entry point for the bot."""
    print(f"Received /start from {update.effective_user.id}") # DEBUG LOG
    
    # If called via Callback (Re-login)
    if update.callback_query:
        query = update.callback_query
        await query.answer()
        if query.data == "start_login":
             await query.edit_message_caption("👋 **Welcome!**\n\nPlease enter your **Email** to login:", parse_mode="Markdown")
             return EMAIL
    
    if api_client.token:
        await update.message.reply_photo(
            photo=BOT_IMAGE_URL,
            caption="🎓 **College Management Bot**\n\n✅ You are already logged in.",
            reply_markup=main_menu_keyboard(),
            parse_mode="Markdown"
        )
        return ConversationHandler.END

    # Initial Welcome Message
    keyboard = [[InlineKeyboardButton("🔐 Login to Dashboard", callback_data="start_login")]]
    await update.message.reply_photo(
        photo=BOT_IMAGE_URL,
        caption=(
            "🎓 **College Management System**\n"
            "━━━━━━━━━━━━━━━━━━━━━━\n\n"
            "👋 **Hello! I am your AI Admin Assistant.**\n\n"
            "I can help you manage students, teachers, courses, and more.\n"
            "Tap the button below to access your dashboard 🚀"
        ),
        reply_markup=InlineKeyboardMarkup(keyboard),
        parse_mode="Markdown"
    )
    return ConversationHandler.END # End initially, wait for button click

async def email_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Stores the email and asks for password."""
    context.user_data["email"] = update.message.text.strip()
    await update.message.reply_text(
        "Thanks! Now please enter your **Password**:\n"
        "||(Hidden for security)||",
        parse_mode="Markdown"
    )
    return PASSWORD

async def password_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Authenticates with the backend."""
    email = context.user_data["email"]
    password = update.message.text.strip()
    
    # Attempt login
    print("DEBUG: Calling api_client.login...") # DEBUG
    success, message = api_client.login(email, password)
    print(f"DEBUG: Login result: {success}, {message}") # DEBUG
    
    if success:
        await update.message.reply_photo(
            photo=BOT_IMAGE_URL,
            caption=f"✅ **Login Successful!**\n\nLogged in as: `{email}`\n\nSelect an option from the menu below:",
            reply_markup=main_menu_keyboard(),
            parse_mode="Markdown"
        )
        # Clear sensitive data
        context.user_data.pop("email", None)
        context.user_data.pop("password", None)
        return ConversationHandler.END
    else:
        await update.message.reply_text(
            f"❌ Login Failed: {message}\n\nPlease try again.\n"
            "Enter your **Email**:"
        )
        return EMAIL  # Loop back to email if failed

async def cancel(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Cancels the login flow."""
    await update.message.reply_text(
        "Login cancelled. Type /login to start again.",
        reply_markup=ReplyKeyboardRemove()
    )
    return ConversationHandler.END

async def logout(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Logs out the user."""
    api_client.token = None
    await update.message.reply_text(
        "🔒 You have been logged out.",
        reply_markup=ReplyKeyboardRemove()
    )

# Conversation Handler
auth_conversation = ConversationHandler(
    entry_points=[
        CommandHandler("start", start), 
        CommandHandler("login", start),
        CallbackQueryHandler(start, pattern="^start_login$")
    ],
    states={
        EMAIL: [MessageHandler(filters.TEXT & ~filters.COMMAND, email_handler)],
        PASSWORD: [MessageHandler(filters.TEXT & ~filters.COMMAND, password_handler)],
    },
    fallbacks=[CommandHandler("cancel", cancel)],
)
