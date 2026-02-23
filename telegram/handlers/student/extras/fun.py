from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from utils.formatting import html_bold, html_code, html_italic, esc, get_random_quote as get_rnd_quote_util
import random

# States
SET_NICKNAME = range(1)
GAME_GUESS_NUM = range(1)

async def fun_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    keyboard = [
        [InlineKeyboardButton("🎲 Roll Dice", callback_data="extras_fun_dice"),
         InlineKeyboardButton("🪙 Coin Flip", callback_data="extras_fun_coin")],
        [InlineKeyboardButton("🔢 Guess Number", callback_data="extras_fun_guess_start"),
         InlineKeyboardButton("🤡 Joke", callback_data="extras_fun_joke")],
         [InlineKeyboardButton("🧩 Riddle", callback_data="extras_fun_riddle"),
          InlineKeyboardButton("💡 Quote", callback_data="extras_fun_quote")],
        [InlineKeyboardButton("🏷️ Set Nickname", callback_data="extras_fun_nick_start"),
         InlineKeyboardButton("🎨 Theme", callback_data="extras_fun_theme")],
        [InlineKeyboardButton("🎉 Confetti", callback_data="extras_fun_confetti"),
         InlineKeyboardButton("❓ Feeling Lucky", callback_data="extras_fun_lucky")],
        [InlineKeyboardButton("🔙 Back to Hub", callback_data="student_hub")]
    ]
    
    await query.edit_message_text(
        f"🎮 {html_bold('Fun Zone & Personalization')}\n\nSelect an activity:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

# --- Games ---
async def roll_dice(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    res = random.randint(1, 6)
    dice_map = {1: "⚀", 2: "⚁", 3: "⚂", 4: "⚃", 5: "⚄", 6: "⚅"}
    await query.edit_message_text(f"🎲 Rolling... {html_bold(str(res))} {dice_map[res]}", parse_mode="HTML", reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("Again", callback_data="extras_fun_dice"), InlineKeyboardButton("Back", callback_data="extras_fun")]]))

async def coin_flip(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    res = random.choice(["Heads 🦅", "Tails 🪙"])
    await query.edit_message_text(f"🪙 Flipping... {html_bold(res)}", parse_mode="HTML", reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("Again", callback_data="extras_fun_coin"), InlineKeyboardButton("Back", callback_data="extras_fun")]]))

# --- Guess Number ---
async def start_guess_game(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    target = random.randint(1, 10)
    context.user_data['guess_target'] = target
    await query.edit_message_text(f"🔢 {html_bold('Guess the Number')}\n\nI'm thinking of a number between 1 and 10.\nType your guess below!", parse_mode="HTML")
    return GAME_GUESS_NUM

async def process_guess(update: Update, context: ContextTypes.DEFAULT_TYPE):
    try:
        guess = int(update.message.text)
        target = context.user_data.get('guess_target')
        
        if guess == target:
            await update.message.reply_text("🎉 Correct! You guessed it!", parse_mode="HTML")
            return ConversationHandler.END
        else:
            await update.message.reply_text("❌ Wrong! Try again or Type /cancel to exit.")
            return GAME_GUESS_NUM
    except:
        await update.message.reply_text("Please enter a number.")
        return GAME_GUESS_NUM

guess_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(start_guess_game, pattern="^extras_fun_guess_start$")],
    states={GAME_GUESS_NUM: [MessageHandler(filters.TEXT & ~filters.COMMAND, process_guess)]},
    fallbacks=[CommandHandler("cancel", fun_menu)]
)

# --- Personalization ---
async def start_set_nickname(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"🏷️ Enter your new {html_bold('Nickname')}:", parse_mode="HTML")
    return SET_NICKNAME

async def save_nickname(update: Update, context: ContextTypes.DEFAULT_TYPE):
    nick = update.message.text
    context.user_data['nickname'] = nick
    await update.message.reply_text(f"✅ Nickname set to {html_bold(nick)}!", parse_mode="HTML")
    return ConversationHandler.END

nick_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(start_set_nickname, pattern="^extras_fun_nick_start$")],
    states={SET_NICKNAME: [MessageHandler(filters.TEXT & ~filters.COMMAND, save_nickname)]},
    fallbacks=[]
)

# --- Content ---
async def show_joke(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    jokes = [
        "Why do programmers prefer dark mode?\nBecause light attracts bugs. 🐛",
        "How many programmers does it take to change a light bulb?\nNone, that's a hardware problem.",
        "A SQL query walks into a bar, walks up to two tables and asks...\n'Can I join you?'"
    ]
    await query.edit_message_text(f"🤡 {html_italic(random.choice(jokes))}", parse_mode="HTML", reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("More", callback_data="extras_fun_joke"), InlineKeyboardButton("Back", callback_data="extras_fun")]]))

async def show_riddle(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    riddles = [
        ("I speak without a mouth and hear without ears. I have no body, but I come alive with wind. What am I?", "An Echo"),
        ("The more of this there is, the less you see. What is it?", "Darkness")
    ]
    r, a = random.choice(riddles)
    await query.edit_message_text(f"🧩 {html_bold('Riddle:')}\n{r}\n\n{html_italic('Answer: ||' + a + '||')}", parse_mode="MarkdownV2", reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("More", callback_data="extras_fun_riddle"), InlineKeyboardButton("Back", callback_data="extras_fun")]]))
    
async def show_quote(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    q = get_rnd_quote_util()
    await query.edit_message_text(f"💡 {esc(q)}", parse_mode="HTML", reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("More", callback_data="extras_fun_quote"), InlineKeyboardButton("Back", callback_data="extras_fun")]]))

async def show_confetti(update: Update, context: ContextTypes.DEFAULT_TYPE):
    # Send animated sticker (confetti)
    await update.callback_query.message.reply_sticker("CAACAgIAAxkBAAELxJll_...") # Placeholder ID
    await update.callback_query.answer("🎉 Party time!")

async def show_lucky(update: Update, context: ContextTypes.DEFAULT_TYPE):
    facts = ["Did you know? The first computer bug was an actual moth.", "Tip: Drink water before exams."]
    await update.callback_query.edit_message_text(f"🍀 {random.choice(facts)}", reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("Back", callback_data="extras_fun")]]))

# --- Themes ---
async def show_theme_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    current = context.user_data.get('theme', 'default')
    
    def btn(label, code):
        icon = "✅ " if current == code else ""
        return InlineKeyboardButton(f"{icon}{label}", callback_data=f"set_theme_{code}")
    
    keyboard = [
        [btn("Standard 🗓️", "default"), btn("Dark Mode 🌑", "dark")],
        [btn("Retro 💾", "retro"), btn("Minimal ⚪", "minimal")],
        [InlineKeyboardButton("🔙 Back", callback_data="extras_fun")]
    ]
    
    await query.edit_message_text(
        f"🎨 {html_bold('Choose Theme')}\n\nSelect a visual style for your dashboard:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def set_theme(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    theme_code = query.data.split("_")[2]
    context.user_data['theme'] = theme_code
    await query.answer(f"Theme set to {theme_code.title()}!")
    await show_theme_menu(update, context)

fun_router = CallbackQueryHandler(fun_menu, pattern="^extras_fun$")
fun_handlers = [
    fun_router,
    guess_conv,
    nick_conv,
    CallbackQueryHandler(roll_dice, pattern="^extras_fun_dice$"),
    CallbackQueryHandler(coin_flip, pattern="^extras_fun_coin$"),
    CallbackQueryHandler(show_joke, pattern="^extras_fun_joke$"),
    CallbackQueryHandler(show_riddle, pattern="^extras_fun_riddle$"),
    CallbackQueryHandler(show_quote, pattern="^extras_fun_quote$"),
    CallbackQueryHandler(show_confetti, pattern="^extras_fun_confetti$"),
    CallbackQueryHandler(show_lucky, pattern="^extras_fun_lucky$"),
    CallbackQueryHandler(show_theme_menu, pattern="^extras_fun_theme$"),
    CallbackQueryHandler(set_theme, pattern="^set_theme_")
]
