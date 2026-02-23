from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from handlers.menu import get_back_button
from datetime import datetime
from utils.formatting import html_bold, html_code, esc, html_italic

async def view_results(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Placeholder for Results
    msg_text = (
        f"🏠 Home > 📊 {html_bold('Results')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n\n"
        f"🚫 {html_italic('No results published yet.')}"
    )

    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(
            chat_id=query.message.chat_id,
            text=msg_text,
            parse_mode="HTML", 
            reply_markup=get_back_button()
        )
    else:
        await query.edit_message_text(msg_text, parse_mode="HTML", reply_markup=get_back_button())

async def view_timetable(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    api = APIClient(update.effective_user.id)
    # GET /api/timeslot
    resp = api.get("/api/timeslot")
    slots = resp if isinstance(resp, list) else resp.get("data", [])
    
    # Filter for my courses? Or just show all for now?
    # Simple View: Show all slots or filer by day
    # Fetch courses for mapping
    c_resp = api.get("/api/course")
    courses = c_resp if isinstance(c_resp, list) else c_resp.get("data", [])
    course_map = {c.get('courseId'): c.get('courseCode') or c.get('name') for c in courses}
    
    today = datetime.now().strftime("%A") # e.g. "Monday"
    
    today_slots = [s for s in slots if s.get('dayOfWeek') == today]
    today_slots.sort(key=lambda x: x.get('startTime', ''))
    
    if not today_slots:
        text = (
            f"🏠 Home > 📅 {html_bold('Timetable')}\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
            f"🗓️ {html_bold(today)}\n\n"
            f"🚫 {html_italic('No classes scheduled for today.')}"
        )
    else:
        text = (
            f"🏠 Home > 📅 {html_bold('Timetable')}\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
            f"🗓️ {html_bold(today)}\n\n"
        )
        for s in today_slots:
            time = f"{s.get('startTime')[:5]} - {s.get('endTime')[:5]}"
            cid = s.get('courseId')
            cname = course_map.get(cid, f"Course {cid}")
            room = s.get('room', 'TBA')
            text += f"⏰ {html_bold(time)}\n📘 {esc(cname)} | 📍 {html_code(esc(room))}\n━━━━━━━━━━━━━━━━━━━━\n"

    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(
            chat_id=query.message.chat_id,
            text=text,
            parse_mode="HTML", 
            reply_markup=get_back_button()
        )
    else:
        await query.edit_message_text(text, parse_mode="HTML", reply_markup=get_back_button())
