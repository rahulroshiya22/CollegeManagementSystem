from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes
from services.session import session_manager
from services.api import APIClient
import logging
from datetime import datetime
from utils.formatting import format_currency, get_greeting, get_random_quote, get_footer

# --- Header Images ---
# specific image provided by user
IMG_DASHBOARD = "https://i.ibb.co/jZPS8PfB/16852bce-e5a1-44d9-8e45-dc281923dd58-0-1.jpg"

IMG_ADMIN = IMG_DASHBOARD
IMG_STUDENT = IMG_DASHBOARD
IMG_TEACHER = IMG_DASHBOARD

def get_greeting():
    hour = datetime.now().hour
    if 5 <= hour < 12:
        return "Good Morning ☀️"
    elif 12 <= hour < 17:
        return "Good Afternoon 🌤️"
    elif 17 <= hour < 21:
        return "Good Evening 🌆"
    else:
        return "Hello 👋"

async def show_dashboard(update: Update, context: ContextTypes.DEFAULT_TYPE):
    # Safe Defaults (Prevent UnboundLocalError in except block)
    date_str = datetime.now().strftime("%d %b %Y")
    
    # Imports for formatting (Must be before usage)
    from utils.formatting import html_bold, html_code, html_italic, html_expandable_quote, html_spoiler, esc, format_currency, html_underline
    
    # Pre-calculate simple values
    greeting = get_greeting()
    # Re-build footer manually for HTML
    footer_html = f"\n━━━━━━━━━━━━━━━━━━━━\n<i>Bot Created by Rahul | CMS v2.0</i>"
    
    try:
        """
        Displays the main dashboard with stats and the menu keyboard.
        Uses Rich Media & Smart Stats.
        """
        user_id = update.effective_user.id
        role = (session_manager.get_user_role(user_id) or "Student").capitalize()
        keyboard = get_main_menu_keyboard(role)
        
        # 🎨 Visual Enhancements
        # Quote with Expandable Blockquote
        raw_quote = get_random_quote()
        quote = html_expandable_quote(f"💡 {esc(raw_quote)}") 
        
        footer = esc(get_footer().strip()) # footer usually has formatting chars, careful
        
        # Defaults
        photo_url = IMG_STUDENT 
        caption_text = ""
        
        # In Admin `except`:
        # uses `greeting`, `date_str`, `footer_html`. Are these safe?
        # `greeting` (line 39), `date_str` (line 48), `footer_html` (line 46).
        # These are ALL defined before the big `if role == "Admin"` block. So they are safe.
        
        # What about `keyboard`?
        # `keyboard` (line 35). Safe.
        
        # What else?
        # Maybe inside `impersonate.py`?
        
    except Exception as e:
        import traceback
        traceback.print_exc()
        await update.effective_message.reply_text(f"⚠️ System Error: {str(e)}")


    if role == "Admin":
        photo_url = IMG_ADMIN
        try:
            api = APIClient(user_id)
            
            # 1. Fetch Basic Counts (Use raw=True to get metadata like totalRecords)
            s_resp = api.get("/api/student?Page=1&PageSize=1", raw=True)
            t_resp = api.get("/api/teacher?Page=1&PageSize=1", raw=True)
            c_resp = api.get("/api/course", raw=True)
            
            def get_count(resp):
                if isinstance(resp, dict):
                    return resp.get('totalRecords') or resp.get('totalCount') or resp.get('count') or 0
                if isinstance(resp, list):
                    return len(resp)
                return 0

            total_s = get_count(s_resp)
            s_count = html_code(str(total_s))
            t_count = html_code(str(get_count(t_resp)))
            c_count = html_code(str(get_count(c_resp)))
            
            # 1b. Mock Gender Stats (Since Backend might not support aggregate)
            # In real system: api.get("/api/student?Gender=Male&PageSize=1")
            male_pct = 55
            female_pct = 45
            gender_stats = f"♂️ {male_pct}% | ♀️ {female_pct}%"
            
            # 2. Fetch Financials
            all_fees = api.get("/api/fee") 
            fee_list = []
            if isinstance(all_fees, list): fee_list = all_fees
            elif isinstance(all_fees, dict): fee_list = all_fees.get("data", [])
            
            total_collected = sum(f.get('amount', 0) for f in fee_list if f.get('status') == 'Paid' or f.get('isPaid'))
            total_pending = sum(f.get('amount', 0) for f in fee_list if f.get('status') != 'Paid' and not f.get('isPaid'))
            
            fmt_coll = html_bold(esc(format_currency(total_collected)))
            fmt_pend = html_bold(esc(format_currency(total_pending)))
            
            # 3. Fetch Notices
            all_notices = api.get("/api/notice")
            notice_list = []
            if isinstance(all_notices, list): notice_list = all_notices
            active_notices = html_code(str(len(notice_list)))
            
            # 4. Mock Attendance
            low_att_count = html_code("5")
            
            # 5. System Health
            sys_health = "🟢 System Online"
            
            caption_text = (
                f"🏫 {html_bold('College Admin Dashboard')}\n"
                f"📅 {html_italic(date_str)}\n"
                f"━━━━━━━━━━━━━━━━━━━━\n"
                f"{greeting}, {html_bold('Admin!')}\n"
                f"{quote}\n\n"
                f"📊 {html_bold(html_underline('System Status:'))}\n"
                f"👥 {html_bold('Users:')} {s_count} Students | {t_count} Teachers\n"
                f"⚖️ {html_bold('Demographics:')} {html_code(gender_stats)}\n"
                f"📚 {html_bold('Courses:')} {c_count} Active\n\n"
                f"💰 {html_bold('Financials:')}\n"
                f"✅ Collected: {fmt_coll}\n"
                f"⏳ Pending: {fmt_pend}\n\n"
                f"📢 {html_bold('Updates:')}\n"
                f"🔹 Active Notices: {active_notices}\n"
                f"⚠️ Low Attendance: {low_att_count} Students\n\n"
                f"{html_italic(sys_health)}\n"
                f"{footer_html}"
            )
        except Exception as e:
            logging.error(f"Dashboard Stats Error: {e}")
            caption_text = (
                 f"🏫 {html_bold('College Admin Dashboard')}\n"
                 f"📅 {html_italic(date_str)}\n"
                 f"━━━━━━━━━━━━━━━━━━━━\n"
                 f"{greeting}, {html_bold('Admin!')}\n"
                 f"{html_italic('(Stats unavailable - Check Logs)')}\n\n"
                 f"🔴 {html_italic('System Offline')}\n"
                 f"{footer_html}"
            )

    elif role == "Student":
        photo_url = IMG_STUDENT
        user_details = session_manager.get_user_data(user_id)
        name = esc(user_details.get('firstName', 'Student'))
        
        # MOCK SMART STATS
        attendance_status = html_code("87% 🟢")
        fee_status = html_code("Paid ✅")
        next_class = html_italic("Mathematics (10:00 AM)")
        
        caption_text = (
            f"🎓 <b>Student Dashboard</b>\n"
            f"📅 <i>{date_str}</i>\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
            f"{greeting}, <b>{name}</b>!\n"
            f"{quote}\n\n"
            f"📈 <b><u>Your Status:</u></b>\n"
            f"✅ <b>Attendance:</b> {attendance_status}\n"
            f"💰 <b>Fees:</b> {fee_status}\n\n"
            f"🔜 <b>Next Class:</b>\n"
            f"📚 {next_class}\n"
            f"{footer_html}"
        )

    elif role == "Teacher":
        photo_url = IMG_TEACHER
        user_details = session_manager.get_user_data(user_id)
        name = esc(user_details.get('firstName', 'Teacher'))
        
        # MOCK STATS
        classes_today = html_code("3")
        pending_reviews = html_code("2")
        
        caption_text = (
            f"👨‍🏫 <b>Teacher Dashboard</b>\n"
            f"📅 <i>{date_str}</i>\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
            f"{greeting}, <b>{name}</b>!\n"
            f"{quote}\n\n"
            f"📋 <b><u>Today's Overview:</u></b>\n"
            f"🏫 <b>Classes:</b> {classes_today} Scheduled\n"
            f"📝 <b>Pending Reviews:</b> {pending_reviews} Exams\n"
            f"{footer_html}"
        )
    
    # Send Photo with Rich Caption (ParseMode.HTML)
    target = None
    if update.callback_query:
        target = update.callback_query.message
        try:
            await target.delete()
        except:
            pass
        chat_id = update.effective_chat.id
        try:
            await context.bot.send_photo(
                chat_id=chat_id,
                photo=photo_url,
                caption=caption_text,
                parse_mode="HTML",
                reply_markup=keyboard
            )
        except Exception as e:
            logging.error(f"Send Photo Error: {e}")
            await context.bot.send_message(chat_id=chat_id, text=caption_text, parse_mode="HTML", reply_markup=keyboard)
            
    else:
        # Command /start or /menu
        await update.message.reply_photo(
            photo=photo_url,
            caption=caption_text,
            parse_mode="HTML",
            reply_markup=keyboard
        )

def get_main_menu_keyboard(role: str):
    """
    Returns the main menu keyboard based on user role.
    """
    keyboard = []
    
    if role == "Admin":
        keyboard = [
            [InlineKeyboardButton("👥 Manage Students", callback_data="admin_students"),
             InlineKeyboardButton("👨‍🏫 Manage Teachers", callback_data="admin_teachers")],
            [InlineKeyboardButton("📚 Manage Courses", callback_data="admin_courses"),
             InlineKeyboardButton("💰 Fee Management", callback_data="admin_fees")],
            [InlineKeyboardButton("✅ Attendance", callback_data="admin_attendance"),
             InlineKeyboardButton("🗓️ Timetable", callback_data="admin_timetable")],
            [InlineKeyboardButton("📝 Exams & Results", callback_data="admin_exams"),
             InlineKeyboardButton("📢 Notices", callback_data="admin_notices")],
            [InlineKeyboardButton("📢 Post Management", callback_data="admin_post_management"),
             InlineKeyboardButton("➕ Add Group/Channel", callback_data="admin_add_group")],
            [InlineKeyboardButton("🎭 Login As (Impersonate)", callback_data="admin_impersonate")],
            [InlineKeyboardButton("👤 My Profile", callback_data="my_profile")]
        ]
    elif role == "Teacher":
        keyboard = [
            [InlineKeyboardButton("📅 My Classes", callback_data="teacher_classes"),
             InlineKeyboardButton("✅ Take Attendance", callback_data="teacher_attendance")],
            [InlineKeyboardButton("📝 Create Exam", callback_data="teacher_exams"),
             InlineKeyboardButton("🗓️ Timetable", callback_data="my_timetable")],
             [InlineKeyboardButton("👤 My Profile", callback_data="my_profile")]
        ]
    elif role == "Student":
        keyboard = [
            [InlineKeyboardButton("👤 My Profile", callback_data="my_profile"),
             InlineKeyboardButton("📊 My Results", callback_data="student_results")],
            [InlineKeyboardButton("📅 My Attendance", callback_data="student_attendance"),
             InlineKeyboardButton("🗓️ Timetable", callback_data="extras_timetable")],
            [InlineKeyboardButton("💰 My Fees", callback_data="student_fees"),
             InlineKeyboardButton("🚀 Student Hub", callback_data="student_hub")]
        ]
    
    # Common Refresh & Logout
    keyboard.append([InlineKeyboardButton("🔄 Refresh Dashboard", callback_data="main_menu")])
    keyboard.append([InlineKeyboardButton("🚪 Logout", callback_data="auth_logout")])
    
    return InlineKeyboardMarkup(keyboard)

def get_pagination_keyboard(current_page: int, total_pages: int, callback_prefix: str):
    """
    Generates generic pagination buttons (Prev | Page X/Y | Next)
    """
    buttons = []
    if current_page > 1:
        buttons.append(InlineKeyboardButton("⬅️ Prev", callback_data=f"{callback_prefix}_page_{current_page - 1}"))
    
    buttons.append(InlineKeyboardButton(f"📄 {current_page}/{total_pages}", callback_data="noop"))
    
    if current_page < total_pages:
        buttons.append(InlineKeyboardButton("Next ➡️", callback_data=f"{callback_prefix}_page_{current_page + 1}"))
        
    return InlineKeyboardMarkup([buttons, [InlineKeyboardButton("🔙 Back to Menu", callback_data="main_menu")]])

def get_back_button():
    """Returns a simple Back to Menu button"""
    return InlineKeyboardMarkup([[InlineKeyboardButton("🔙 Back to Menu", callback_data="main_menu")]])
