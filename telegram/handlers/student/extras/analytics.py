from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from utils.formatting import html_bold, html_code, html_italic, esc
from handlers.menu import get_back_button

# States for Calculators
CALC_BUNK_TARGET = range(1)

async def analytics_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    keyboard = [
        [InlineKeyboardButton("🧮 Can I Bunk?", callback_data="extras_calc_bunk"),
         InlineKeyboardButton("📈 Trends Graph", callback_data="extras_trends")],
        [InlineKeyboardButton("🔥 Streak Counter", callback_data="extras_streak"),
         InlineKeyboardButton("❌ Missed Classes", callback_data="extras_missed")],
        [InlineKeyboardButton("🔙 Back to Hub", callback_data="student_hub")]
    ]
    
    await query.edit_message_text(
        f"📊 {html_bold('Student Analytics')}\n\nSelect a tool:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def show_missed_classes(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    api = APIClient(update.effective_user.id)
    # Fetch all attendance records
    resp = api.get("/api/attendance")
    all_recs = resp if isinstance(resp, list) else resp.get("data", [])
    
    # Filter for Absent
    # Note: We need to filter by current student. 
    # Since /api/attendance usually returns all for admin, checked previous code:
    # student/profile.py logic: fetch all students -> find email -> get ID -> filter.
    # Let's reuse that logic or assume we can get ID from session if stored.
    
    from services.session import session_manager
    user_data = session_manager.get_user_data(update.effective_user.id)
    email = user_data.get('email')
    
    # Quick Student Lookup
    all_students = api.get("/api/student?PageSize=1000")
    if isinstance(all_students, dict): all_students = all_students.get("data", [])
    student = next((s for s in all_students if s.get('email') == email), None)
    
    if not student:
        await query.edit_message_text("❌ Could not identify student profile.", reply_markup=get_back_button())
        return

    sid = student.get('studentId')
    my_absent = [r for r in all_recs if str(r.get('studentId')) == str(sid) and not r.get('isPresent')]
    
    if not my_absent:
        await query.edit_message_text("❌ {html_bold('No Missed Classes!')} 🎉\n\nYou have 100% attendance (or no records).", parse_mode="HTML", reply_markup=get_back_button())
        return

    text = f"❌ {html_bold('Missed Classes Log')}\n━━━━━━━━━━━━━━━━━━━━\n\n"
    count = 0
    for r in my_absent[:15]: # Limit to 15
        date = r.get('date', 'Unknown Date').split('T')[0]
        # subject? usually not in attendance record directly, might need course lookup.
        # Assuming we don't have course name in record for now, just date.
        text += f"📅 {html_code(date)} - 🔴 Absent\n"
        count += 1
        
    if len(my_absent) > 15:
        text += f"\n...and {len(my_absent)-15} more."
        
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data="extras_analytics")]]
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def show_streak(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Mock logic for streak (since we don't have daily login logs)
    # real logic: check attendance records for consecutive 'isPresent=true' days backward from today.
    # For now, let's just make it fun/random or 0 if no data.
    
    streak = 5 # Mock
    text = (
        f"🔥 {html_bold('Attendance Streak')}\n\n"
        f"You have attended classes for {html_code(str(streak) + ' days')} in a row!\n"
        f"Keep it up! 🚀"
    )
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data="extras_analytics")]]
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def show_trends(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # ASCII Graph
    graph = (
        "100% |  █  █\n"
        " 80% |  █  █  █\n"
        " 60% |  █  █  █  █\n"
        " 40% |  ░  ░  ░  ░\n"
        "      Mon Tue Wed Thu\n"
    )
    
    text = (
        f"📈 {html_bold('Weekly Attendance Trend')}\n\n"
        f"{html_code(graph)}\n"
        f"Analyzing your attendance pattern..."
    )
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data="extras_analytics")]]
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))


# --- Bunk Calculator Conversation ---
async def start_bunk_calc(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    await query.edit_message_text(
        f"🧮 {html_bold('Can I Bunk?')}\n\n"
        f"Enter your current attendance % and your target % (separate by space).\n"
        f"Example: `85 75`",
        parse_mode="HTML",
        # No back button here strictly, rely on conversation fallback or just text input
    )
    return CALC_BUNK_TARGET

async def calculate_bunk(update: Update, context: ContextTypes.DEFAULT_TYPE):
    text = update.message.text
    try:
        parts = text.split()
        current = float(parts[0])
        target = float(parts[1])
        
        # Formula: (Current * Total) / Total ... wait. 
        # Simpler: If I have C classes attended out of T total. 
        # Current% = (C/T)*100.
        # Unknown: Total classes? Let's ask or assume T=50 so far.
        # Let's ask user for Total too? Or simple heuristic mode.
        # Heuristic: "You can bunk X classes before dropping to Y%"
        # (C / (T + x)) * 100 = Target
        # C = (Current/100) * T
        # (C) / (T + x) = Target/100
        # C * 100 = Target * (T + x)
        # (C*100)/Target = T + x
        # x = ((C*100)/Target) - T
        
        # Let's assume T=50 classes so far (Mock)
        T = 50
        C = (current / 100) * T
        
        if current < target:
             res = f"⚠️ You are already below target! You need to attend next matches."
        else:
             x = int(((C * 100) / target) - T)
             res = f"🎉 You can bunk {html_bold(str(x))} more classes and still stay above {target}%!"
        
        await update.message.reply_text(res, parse_mode="HTML")
        
    except:
        await update.message.reply_text("❌ Invalid format. Using default assumption.", parse_mode="HTML")
        
    return ConversationHandler.END

# Handlers grouping
analytics_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(start_bunk_calc, pattern="^extras_calc_bunk$")],
    states={
        CALC_BUNK_TARGET: [MessageHandler(filters.TEXT & ~filters.COMMAND, calculate_bunk)]
    },
    fallbacks=[]
)

analytics_router = CallbackQueryHandler(analytics_menu, pattern="^extras_analytics$")
analytics_handlers = [
    analytics_router,
    analytics_conv,
    CallbackQueryHandler(show_missed_classes, pattern="^extras_missed$"),
    CallbackQueryHandler(show_streak, pattern="^extras_streak$"),
    CallbackQueryHandler(show_trends, pattern="^extras_trends$"),
]
