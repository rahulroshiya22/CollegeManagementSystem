from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from utils.formatting import html_bold

# States
WIKI_SEARCH = range(1)
TIMER_SET = range(1)

async def utils_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    keyboard = [
        [InlineKeyboardButton("⏱️ Study Timer", callback_data="extras_util_timer"),
         InlineKeyboardButton("📖 Wiki Search", callback_data="extras_util_wiki")],
        [InlineKeyboardButton("🔙 Back to Hub", callback_data="student_hub")]
    ]
    
    await query.edit_message_text(
        f"🛠️ {html_bold('Utilities')}\n\nSelect a tool:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

# --- Wiki Search ---
async def start_wiki(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"📖 Enter topic to search on {html_bold('Wikipedia')}:", parse_mode="HTML")
    return WIKI_SEARCH

async def process_wiki(update: Update, context: ContextTypes.DEFAULT_TYPE):
    topic = update.message.text
    # Mock result (no external lib dependency for now to avoid install issues)
    summary = f"Wikipedia summary for '{topic}':\n\nThis is a simulated summary. In a real deployment, we would use the `wikipedia` library to fetch the first paragraph."
    
    await update.message.reply_text(summary)
    return ConversationHandler.END

wiki_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(start_wiki, pattern="^extras_util_wiki$")],
    states={WIKI_SEARCH: [MessageHandler(filters.TEXT & ~filters.COMMAND, process_wiki)]},
    fallbacks=[]
)

# --- Study Timer ---
async def start_timer(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"⏱️ Enter duration in minutes (e.g. 25):", parse_mode="HTML")
    return TIMER_SET

async def set_timer(update: Update, context: ContextTypes.DEFAULT_TYPE):
    try:
        minutes = int(update.message.text)
        seconds = minutes * 60
        
        chat_id = update.effective_chat.id
        # Use JobQueue
        context.job_queue.run_once(alarm, seconds, chat_id=chat_id, name=str(chat_id), data=minutes)
        
        await update.message.reply_text(f"✅ Timer set for {minutes} minutes! Focus now. 🧠")
        return ConversationHandler.END
    except ValueError:
        await update.message.reply_text("Please enter a valid number.")
        return TIMER_SET

async def alarm(context: ContextTypes.DEFAULT_TYPE):
    job = context.job
    await context.bot.send_message(job.chat_id, text=f"⏰ {html_bold('Time Up!')}\n\n{job.data} minutes completed. Take a break! ☕", parse_mode="HTML")

timer_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(start_timer, pattern="^extras_util_timer$")],
    states={TIMER_SET: [MessageHandler(filters.TEXT & ~filters.COMMAND, set_timer)]},
    fallbacks=[]
)

utils_router = CallbackQueryHandler(utils_menu, pattern="^extras_utils$")
utils_handlers = [
    utils_router,
    wiki_conv,
    timer_conv
]
