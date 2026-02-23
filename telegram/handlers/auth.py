from telegram import Update, ReplyKeyboardMarkup, ReplyKeyboardRemove, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from services.session import session_manager

EMAIL, PASSWORD = range(2)

async def start_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    # Check if already logged in
    user_id = update.effective_user.id
    token = session_manager.get_user_token(user_id)
    
    # Image URL
    IMG_URL = "https://i.ibb.co/jZPS8PfB/16852bce-e5a1-44d9-8e45-dc281923dd58-0-1.jpg"
    
    if token:
        from handlers.menu import show_dashboard
        await show_dashboard(update, context)
        return ConversationHandler.END

    # Safe Chat ID Retrieval
    chat_id = update.effective_chat.id
    
    # Welcome Message for New Users
    welcome_text = (
        f"🤖 <b>College Management Bot</b>\n"
        f"✨ <b>Built by Rahul Roshiya</b>\n"
        f"━━━━━━━━━━━━━━━━━━━━\n\n"
        f"🚀 <b>Smart Features:</b>\n"
        f"� <b>Real-time Attendance</b>\n"
        f"� <b>Exam Results & Grades</b>\n"
        f"📢 <b>Instant Notices</b>\n"
        f"💳 <b>Fee Status & Receipts</b>\n\n"
        f"🔐 <i>Secure Login Required to access features.</i>\n"
        f"👇 <b>Click 'Login' to get started!</b>"
    )
    
    keyboard = [[InlineKeyboardButton("🔐 Login Now", callback_data="start_login")]]
    
    try:
        await context.bot.send_photo(
            chat_id=chat_id,
            photo=IMG_URL, 
            caption=welcome_text, 
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup(keyboard)
        )
    except Exception as e:
        # Fallback if image fails or any other error
        await context.bot.send_message(
            chat_id=chat_id,
            text=welcome_text,
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup(keyboard)
        )
        
    return ConversationHandler.END

async def login_start_callback(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    await query.edit_message_caption(
        caption=(
            f"🔐 <b>Login Portal</b>\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
            f"👋 Welcome! Let's get you signed in.\n\n"
            f"📧 <b>Step 1:</b> Please enter your <b>Email ID</b>.\n"
            f"<i>(e.g., student@cms.com)</i>"
        ),
        parse_mode="HTML"
    )
    # Track Message IDs for cleanup
    context.user_data['login_msg_ids'] = [query.message.message_id]
    
    return EMAIL

async def receive_email(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['email'] = update.message.text
    
    # Track User Email Msg
    if 'login_msg_ids' not in context.user_data: context.user_data['login_msg_ids'] = []
    context.user_data['login_msg_ids'].append(update.message.message_id)
    
    msg = (
        f"🔐 <b>Login Portal</b>\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"✅ Email accepted.\n\n"
        f"🔑 <b>Step 2:</b> Please enter your <b>Password</b>.\n"
    )
    
    m = await update.message.reply_text(msg, parse_mode="HTML")
    
    # Track Bot Password Prompt Msg
    context.user_data['login_msg_ids'].append(m.message_id)
    
    return PASSWORD

async def receive_password(update: Update, context: ContextTypes.DEFAULT_TYPE):
    email = context.user_data['email']
    password = update.message.text
    user_id = update.effective_user.id

    msg = await update.message.reply_text("🔄 Verifying credentials...")

    # API Request for Login
    print(f"Attempting Login for: {email}")
    api = APIClient(user_id) 
    response = api.post("/api/auth/login", {"email": email, "password": password})
    print(f"DEBUG LOGIN RESPONSE: {response}")
    
    if response and ("token" in response or "accessToken" in response):
        token = response.get("token") or response.get("accessToken")
        
        # User details are now directly in the login response
        user_role = response.get("role", "Student") 
        user_name = response.get("firstName", "User") 
        user_db_id = response.get("userId", 0)
        user_email = response.get("email", email)

        # Save Persistent Session
        session_manager.save_user_session(user_id, token, {
            "role": user_role,
            "email": user_email,
            "firstName": user_name,
            "userId": user_db_id
        })

        # Auto-Redirect to Menu
        # We need to import menu_handler or simulate it. 
        # Since menu_handler is in main.py (circular import issue), we can return a special flag or use a helper.
        # Better approach here: Send the menu DIRECTLY from here to avoid circular imports.
        
        from handlers.menu import show_dashboard
        
        await msg.delete() # Remove "Verifying..."
        
        # --- CLEANUP CHAT HISTORY ---
        # User wants only Dashboard to be visible.
        chat_id = update.effective_chat.id
        # Add current password message to list
        if 'login_msg_ids' in context.user_data:
            context.user_data['login_msg_ids'].append(update.message.message_id)
            
            for mid in context.user_data['login_msg_ids']:
                try:
                    await context.bot.delete_message(chat_id=chat_id, message_id=mid)
                except Exception as e:
                    print(f"Failed to delete msg {mid}: {e}")
            
            # Clear list
            context.user_data['login_msg_ids'] = []

        # --- INSTANT TRANSITION ---
        # Direct Dashboard Load (No Success Message, No Delay)
        await show_dashboard(update, context)
        
        return ConversationHandler.END

async def cancel(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await update.message.reply_text("🚫 Login cancelled.")
    return ConversationHandler.END

async def logout_confirm(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Step 1: Ask for confirmation before logging out.
    """
    query = update.callback_query
    await query.answer()
    
    keyboard = [
        [InlineKeyboardButton("✅ Yes, Logout", callback_data="perform_logout")],
        [InlineKeyboardButton("❌ Cancel", callback_data="main_menu")]
    ]
    
    await query.edit_message_caption(
        caption=(
            f"🚪 <b>Logout Confirmation</b>\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
            f"Are you sure you want to sign out?\n"
            f"Allowed sessions will be terminated."
        ),
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def logout(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Step 2: Perform the actual logout.
    """
    # Clear session first
    session_manager.clear_session(update.effective_user.id)
    
    # Determine if called via command or callback
    if update.callback_query:
        query = update.callback_query
        await query.answer("Logged out successfully!")
        # Delete the Dashboard (Menu)
        try:
            await query.message.delete()
        except:
            pass
    
    # Show Login Screen (Recycle start_handler)
    await start_handler(update, context)

# Handler definition
login_conv_handler = ConversationHandler(
    entry_points=[
        CommandHandler('login', start_handler),
        CommandHandler('start', start_handler),
        CallbackQueryHandler(login_start_callback, pattern="^start_login$")
    ],
    states={
        EMAIL: [MessageHandler(filters.TEXT & ~filters.COMMAND, receive_email)],
        PASSWORD: [MessageHandler(filters.TEXT & ~filters.COMMAND, receive_password)],
    },
    fallbacks=[CommandHandler('cancel', cancel)]
)
