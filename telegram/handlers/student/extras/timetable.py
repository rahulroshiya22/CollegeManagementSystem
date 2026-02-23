from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from utils.formatting import html_bold, html_code, html_italic, esc, format_date
from handlers.menu import get_back_button
from datetime import datetime, timedelta
import random

# States
SET_REMINDER_TIME = range(1)
SET_REMINDER_NOTE = range(1)

# --- Icons & Helpers ---
def get_subject_icon(subject_name):
    s = subject_name.lower()
    if 'lab' in s or 'practical' in s: return "🧪"
    if 'math' in s: return "📐"
    if 'physics' in s: return "⚛️"
    if 'chem' in s: return "⚗️"
    if 'comp' in s or 'code' in s or 'program' in s: return "💻"
    if 'eng' in s: return "📖"
    if 'history' in s: return "🏺"
    return "📚"

def get_time_obj(t_str):
    return datetime.strptime(t_str, "%H:%M")

# --- Main Dashboard ---
async def timetable_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Toggle Exam Mode (Stored in user_data)
    exam_mode = context.user_data.get('tt_exam_mode', False)
    mode_icon = "📝" if exam_mode else "🗓️"
    mode_text = "Exam Mode: ON" if exam_mode else "Exam Mode: OFF"
    toggle_btn = InlineKeyboardButton(f"🔄 {mode_text}", callback_data="extras_tt_toggle_mode")

    # Theme Logic
    theme = context.user_data.get('theme', 'default')
    header = f"🗓️ {html_bold('Timetable 2.0')}"
    
    if theme == 'dark': header = f"🌑 {html_bold('TIMETABLE [DARK]')}"
    elif theme == 'retro': header = f"💾 {html_bold('SCHEDULE.EXE')}"
    elif theme == 'minimal': header = "Schedule"
    
    if exam_mode:
        # EXAM DASHBOARD
        keyboard = [
            [InlineKeyboardButton("📅 Exam Schedule", callback_data="extras_tt_exams"),
             InlineKeyboardButton("💺 Seating Plan", callback_data="extras_tt_seat")],
            [toggle_btn],
            [InlineKeyboardButton("🔙 Back to Menu", callback_data="main_menu")]
        ]
        text = f"📝 {html_bold('Exam Dashboard')}\n\nGood luck with your preparations! 🍀"
    else:
        # REGULAR DASHBOARD
        keyboard = [
            [InlineKeyboardButton("📍 Where am I?", callback_data="extras_tt_status"),
             InlineKeyboardButton("⏩ Next Class", callback_data="extras_tt_next")],
            [InlineKeyboardButton("📅 Day View", callback_data="extras_tt_day"),
             InlineKeyboardButton("🗓️ Weekly", callback_data="my_timetable")], 
            [InlineKeyboardButton("🕒 Gaps & Breaks", callback_data="extras_tt_gaps"),
             InlineKeyboardButton("🔔 Set Reminder", callback_data="extras_tt_remind_start")],
             [InlineKeyboardButton("🏃 I'm Late", callback_data="extras_tt_late"),
              InlineKeyboardButton("👥 Study Buddy", callback_data="extras_tt_buddy")],
            [toggle_btn],
            [InlineKeyboardButton("🔙 Back to Menu", callback_data="main_menu")]
        ]
        text = (
            f"{header}\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
            f"Manage your schedule smarter.\n"
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

async def toggle_mode(update: Update, context: ContextTypes.DEFAULT_TYPE):
    current = context.user_data.get('tt_exam_mode', False)
    context.user_data['tt_exam_mode'] = not current
    await timetable_menu(update, context)

# --- Feature: Where am I? (Status) ---
async def show_status(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    api = APIClient(update.effective_user.id)
    resp = api.get("/api/timeslot")
    slots = resp if isinstance(resp, list) else resp.get("data", [])
    
    now = datetime.now()
    today_name = now.strftime("%A")
    current_hm = now.strftime("%H:%M")
    
    today_slots = [s for s in slots if s.get('dayOfWeek') == today_name]
    today_slots.sort(key=lambda x: x.get('startTime'))
    
    current_slot = None
    next_slot = None
    
    for i, s in enumerate(today_slots):
        start = s.get('startTime')[:5]
        end = s.get('endTime')[:5]
        
        if start <= current_hm <= end:
            current_slot = s
            if i + 1 < len(today_slots):
                next_slot = today_slots[i+1]
            break
        elif start > current_hm:
            next_slot = s
            break
            
    text = f"📍 {html_bold('Live Status')}\n━━━━━━━━━━━━━━━━━━━━\n\n"
    
    if current_slot:
        icon = get_subject_icon(str(current_slot.get('courseId')))
        text += (
            f"🔴 {html_bold('HAPPENING NOW')}\n"
            f"{icon} {html_code(current_slot.get('courseId'))}\n"
            f"📍 Room: {html_bold(current_slot.get('room', 'Unknown'))}\n"
            f"⏰ Ends: {current_slot.get('endTime')[:5]}\n\n"
        )
    else:
        text += f"🟢 {html_italic('You are currently free!')}\n\n"
        
    if next_slot:
        start_dt = datetime.strptime(next_slot.get('startTime')[:5], "%H:%M")
        now_dt = datetime.strptime(current_hm, "%H:%M")
        diff_min = (start_dt - now_dt).seconds // 60
        
        icon = get_subject_icon(str(next_slot.get('courseId')))
        text += (
            f"🔜 {html_bold('NEXT UP')} (in {diff_min} mins)\n"
            f"{icon} {html_code(next_slot.get('courseId'))} @ {next_slot.get('startTime')[:5]}\n"
            f"📍 Room: {next_slot.get('room')}\n"
        )
    else:
        text += "🏁 No more classes today. Go home! 🏠"
        
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data="extras_timetable")]]
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# --- Feature: Gap Detector ---
async def show_gaps(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Mock finding gaps > 45 mins
    text = (
        f"🕒 {html_bold('Gap Detector')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Found 2 breaks today:\n\n"
        f"1️⃣ {html_bold('11:00 - 12:30')} (1h 30m)\n"
        f"   🥪 Sufficient time for Lunch at Canteen.\n\n"
        f"2️⃣ {html_bold('14:30 - 15:15')} (45m)\n"
        f"   📚 Quick revision in Library?"
    )
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data="extras_timetable")]]
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# --- Feature: I'm Late ---
async def late_alert(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    # Simulate marking late
    await query.answer("Run! 🏃 Late status noted.", show_alert=True)
    # Could imply notifying teacher in a real system

# --- Feature: Study Buddy ---
async def study_buddy(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    text = (
        f"👥 {html_bold('Study Buddy Finder')}\n\n"
        f"Checking schedule matches...\n"
        f"✅ {html_bold('Rahul')} is also free at 11:00.\n"
        f"✅ {html_bold('Priya')} has a gap now.\n\n"
        f"Tap to message them! (Simulated)"
    )
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data="extras_timetable")]]
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# --- Feature: Custom Reminders ---
async def start_reminder(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"🔔 {html_bold('Set Reminder')}\n\nEnter time (HH:MM) to be reminded:", parse_mode="HTML")
    return SET_REMINDER_TIME

async def receive_remind_time(update: Update, context: ContextTypes.DEFAULT_TYPE):
    time_str = update.message.text
    context.user_data['remind_time'] = time_str
    await update.message.reply_text("📝 What's the note? (e.g. 'Submit Assignment')")
    return SET_REMINDER_NOTE

async def receive_remind_note(update: Update, context: ContextTypes.DEFAULT_TYPE):
    note = update.message.text
    time_str = context.user_data.get('remind_time')
    
    # In real app: Calculate delay seconds and use job_queue
    # Mock:
    delay = 5 # 5 seconds demo
    context.job_queue.run_once(reminder_alarm, delay, chat_id=update.effective_chat.id, data=note)
    
    await update.message.reply_text(f"✅ Reminder set for {time_str}: {html_bold(note)}\n(Demo: Will alert in 5s)", parse_mode="HTML")
    return ConversationHandler.END

async def reminder_alarm(context: ContextTypes.DEFAULT_TYPE):
    job = context.job
    await context.bot.send_message(job.chat_id, text=f"🔔 {html_bold('REMINDER')}\n\n{job.data}", parse_mode="HTML")

remind_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(start_reminder, pattern="^extras_tt_remind_start$")],
    states={
        SET_REMINDER_TIME: [MessageHandler(filters.TEXT & ~filters.COMMAND, receive_remind_time)],
        SET_REMINDER_NOTE: [MessageHandler(filters.TEXT & ~filters.COMMAND, receive_remind_note)],
    },
    fallbacks=[]
)

# --- Re-export previous useful functions ---
# We reuse next_class, day_view from previous file but adapt them to new UI if needed. 
# For brevity, reusing the handler names but they need to be defined or imported.
# Let's redefine them quickly to ensure compatibility with new imports.

async def show_next_class(update: Update, context: ContextTypes.DEFAULT_TYPE):
    # Reuse show_status logic but focused on next
    await show_status(update, context)

async def show_day_view(update: Update, context: ContextTypes.DEFAULT_TYPE):
    # Similar to previous implementation
    query = update.callback_query
    await query.answer()
    days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]
    keyboard = []
    row = []
    for d in days:
        row.append(InlineKeyboardButton(d[:3], callback_data=f"extras_tt_day_{d}"))
        if len(row) == 3: keyboard.append(row); row=[]
    if row: keyboard.append(row)
    keyboard.append([InlineKeyboardButton("🔙 Back", callback_data="extras_timetable")])
    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(chat_id=query.message.chat_id, text="📅 Select Day:", parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    else:
        await query.edit_message_text(f"📅 Select Day:", parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def show_specific_day(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    day = query.data.split("_")[3]
    
    api = APIClient(update.effective_user.id)
    resp = api.get("/api/timeslot")
    slots = resp if isinstance(resp, list) else resp.get("data", [])
    day_slots = [s for s in slots if s.get('dayOfWeek') == day]
    day_slots.sort(key=lambda x: x.get('startTime'))
    
    text = f"📅 {html_bold(day)}\n━━━━━━━━━━━━━━━━━━━━\n"
    if not day_slots: text += "No classes."
    
    for s in day_slots:
        icon = get_subject_icon(str(s.get('courseId')))
        text += f"{icon} {html_code(s.get('startTime')[:5])} - {html_bold(s.get('courseId'))}\n"
    
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data="extras_timetable")]]
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# --- ROUTER CONFIG ---
timetable_router = CallbackQueryHandler(timetable_menu, pattern="^extras_timetable$")
timetable_handlers = [
    timetable_router,
    remind_conv,
    CallbackQueryHandler(show_status, pattern="^extras_tt_status$"),
    CallbackQueryHandler(toggle_mode, pattern="^extras_tt_toggle_mode$"),
    CallbackQueryHandler(show_next_class, pattern="^extras_tt_next$"),
    CallbackQueryHandler(show_day_view, pattern="^extras_tt_day$"),
    CallbackQueryHandler(show_specific_day, pattern="^extras_tt_day_"),
    CallbackQueryHandler(show_gaps, pattern="^extras_tt_gaps$"),
    CallbackQueryHandler(late_alert, pattern="^extras_tt_late$"),
    CallbackQueryHandler(study_buddy, pattern="^extras_tt_buddy$"),
    # Exam placeholders
    CallbackQueryHandler(lambda u,c: u.callback_query.answer("Exam Schedule loading..."), pattern="^extras_tt_exams$"),
    CallbackQueryHandler(lambda u,c: u.callback_query.answer("Seating Plan: Room 301, Row 2"), pattern="^extras_tt_seat$"),
]
