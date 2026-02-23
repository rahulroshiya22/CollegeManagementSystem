from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from services.session import session_manager
from handlers.menu import get_pagination_keyboard, get_back_button
from utils.formatting import get_role_badge, get_status_emoji, html_bold, html_code, html_italic, esc
import logging

# States
T_ADD_NAME, T_ADD_EMAIL, T_ADD_DEPT, T_ADD_QUAL, T_ADD_PASS = range(5)
SEARCH_QUERY = range(1)
EDIT_PHONE, EDIT_QUAL = range(2)
DM_MESSAGE = range(1)

# -------------------------------------------------------------------------
#  Teacher List
# -------------------------------------------------------------------------

# -------------------------------------------------------------------------
#  Department Selection & Teacher List
# -------------------------------------------------------------------------

async def list_departments(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Step 1: Show list of Departments to filter teachers.
    """
    query = update.callback_query
    await query.answer()
    
    api = APIClient(update.effective_user.id)
    logging.info("Fetching Departments for Teacher Filter...")
    
    # Fetch all departments
    response = api.get("/api/department")
    
    departments = []
    if isinstance(response, list):
        departments = response
    elif isinstance(response, dict):
        departments = response.get("data") or response.get("items") or response.get("value") or []

    if not departments:
        await query.edit_message_text("🏢 No Departments found.", reply_markup=get_back_button())
        return

    # Build Grid Keyboard (2 cols)
    keyboard = []
    row = []
    for dept in departments:
        name = dept.get('name', 'Unknown')
        did = dept.get('departmentId') or dept.get('id')
        row.append(InlineKeyboardButton(f"🎓 {name}", callback_data=f"admin_teachers_dept_{did}_page_1"))
        
        if len(row) == 2:
            keyboard.append(row)
            row = []
    
    if row:
        keyboard.append(row)

    # Search Button
    keyboard.append([InlineKeyboardButton("🔍 Search Teacher", callback_data="search_teacher_start")])
    keyboard.append([InlineKeyboardButton("➕ Add New Teacher", callback_data="add_teacher_start")])
    keyboard.append([InlineKeyboardButton("🔙 Back to Menu", callback_data="main_menu")])

    msg = (
        f"🏫 <b>Select Department</b>\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Select a department to view teachers."
    )
    
    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(
            chat_id=query.message.chat_id,
            text=msg,
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup(keyboard)
        )
    else:
        await query.edit_message_text(msg, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))


async def list_teachers(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Step 2: List Teachers for a specific Department.
    """
    query = update.callback_query
    await query.answer()
    
    # Fallback to defaults
    dept_id = 0
    page = 1
    
    # Parse Callback Data
    parts = query.data.split("_")
    if "dept" in parts:
        try:
             dept_index = parts.index("dept")
             dept_id = int(parts[dept_index + 1])
             
             if "page" in parts:
                 page_index = parts.index("page")
                 page = int(parts[page_index + 1])
        except:
             pass
    
    api = APIClient(update.effective_user.id)
    
    # Resolving Department Name if ID is provided
    dept_name_filter = None
    if dept_id > 0:
        dept_info = api.get(f"/api/department/{dept_id}")
        if dept_info and not "error" in dept_info:
            dept_name_filter = dept_info.get("name")
    
    # Filter by Department Name if available
    endpoint = f"/api/teacher?Page={page}&PageSize=10"
    if dept_name_filter:
        endpoint = f"/api/teacher?Department={dept_name_filter}&Page={page}&PageSize=10"

    response = api.get(endpoint)
    
    if not response or (isinstance(response, dict) and "error" in response):
        err = response.get('error') if response else 'Unknown'
        await query.edit_message_text(f"❌ Error: {err}", reply_markup=get_back_button())
        return

    teachers = []
    total_pages = 1
    
    if isinstance(response, list):
        teachers = response
    elif isinstance(response, dict):
        teachers = response.get("data") or response.get("items") or response.get("value") or []
        total_pages = response.get("totalPages", 1)

    if not teachers:
        keyboard = [[InlineKeyboardButton("🔙 Back to Departments", callback_data="admin_teachers")]]
        await query.edit_message_text("👨‍🏫 No Teachers found in this department.", reply_markup=InlineKeyboardMarkup(keyboard))
        return

    text = f"{get_role_badge('Teacher')} <b>Teacher Directory</b> (Page {page}/{total_pages})\n\n"
    keyboard = []
    
    for t in teachers:
        raw_name = f"{t.get('firstName', '')} {t.get('lastName', '')}".strip() or t.get('name')
        name = esc(raw_name)
        tid = t.get('teacherId') or t.get('id')
        dept = esc(t.get('department', 'N/A'))
        qual = esc(t.get('qualification', 'N/A'))
        
        # Status
        status_icon = get_status_emoji(t.get('isActive', True))
        
        text += f"{status_icon} <b>{name}</b>\n   └ 🎓 {dept} | 📜 {qual}\n\n"
        
        keyboard.append([
            InlineKeyboardButton(f"👁️ View {name}", callback_data=f"view_teacher_{tid}")
        ])

    # Pagination
    nav_buttons = []
    if page > 1:
        nav_buttons.append(InlineKeyboardButton("⬅️ Prev", callback_data=f"admin_teachers_dept_{dept_id}_page_{page - 1}"))
    if page < total_pages:
        nav_buttons.append(InlineKeyboardButton("Next ➡️", callback_data=f"admin_teachers_dept_{dept_id}_page_{page + 1}"))
    if nav_buttons: keyboard.append(nav_buttons)
        
    keyboard.append([InlineKeyboardButton("🔙 Switch Department", callback_data="admin_teachers")])
    keyboard.append([InlineKeyboardButton("🔙 Main Menu", callback_data="main_menu")])

    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# -------------------------------------------------------------------------
#  View Teacher Profile
# -------------------------------------------------------------------------

async def view_teacher(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    tid = query.data.split("_")[2]
    api = APIClient(update.effective_user.id)
    teacher = api.get(f"/api/teacher/{tid}")
    
    if not teacher or "error" in teacher:
        await query.edit_message_text("❌ Teacher not found.", reply_markup=get_back_button())
        return

    # Details
    fname = teacher.get('firstName', '')
    lname = teacher.get('lastName', '')
    name = esc(f"{fname} {lname}".strip())
    
    dept = esc(teacher.get('department', 'N/A'))
    qual = esc(teacher.get('qualification', 'N/A'))
    exp = teacher.get('experience', 0)
    phone = html_code(esc(teacher.get('phoneNumber', 'N/A')))
    email = html_code(esc(teacher.get('email', 'N/A')))
    status_val = "Active" if teacher.get('isActive', True) else "Inactive"
    status_icon = "🟢" if status_val == "Active" else "🔴"
    
    info = (
        f"{get_role_badge('Teacher')} <b>TEACHER PROFILE</b>\n"
        f"━━━━━━━━━━━━━━━━━━━━\n\n"
        f"👤 <b>{name}</b>\n"
        f"🆔 ID: {html_code(tid)}\n\n"
        
        f"📋 <b><u>Professional Details</u></b>\n"
        f"├ 🏛 <b>Dept:</b> {dept}\n"
        f"├ 📜 <b>Qual:</b> {qual}\n"
        f"├ ⏳ <b>Exp:</b> {exp} Years\n"
        f"└ {status_icon} <b>Status:</b> {status_val}\n\n"
        
        f"📞 <b><u>Contact Info</u></b>\n"
        f"├ 📧 {email}\n"
        f"└ 📱 {phone}\n"
        f"━━━━━━━━━━━━━━━━━━━━"
    )
    
    keyboard = [
        [InlineKeyboardButton("📅 Schedule", callback_data=f"view_tschedule_{tid}"),
         InlineKeyboardButton("📊 Stats", callback_data=f"view_tstats_{tid}")],
        [InlineKeyboardButton("🔑 Reset Pass", callback_data=f"reset_tpass_{tid}")],
        [InlineKeyboardButton("✏️ Edit Details", callback_data=f"edit_teacher_{tid}"),
         InlineKeyboardButton("📩 Message", callback_data=f"dm_teacher_{tid}")],
        [InlineKeyboardButton("🗑️ Delete Teacher", callback_data=f"delete_teacher_{tid}")],
        [InlineKeyboardButton("🔙 Back to List", callback_data="admin_teachers")]
    ]
    
    try:
        await query.edit_message_text(info, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    except Exception as e:
        logging.error(f"Render Error: {e}")
        await query.edit_message_text(f"❌ Render Error: {str(e)[:100]}", reply_markup=get_back_button())

async def delete_teacher_confirm(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    tid = query.data.split("_")[2]
    
    keyboard = [
        [InlineKeyboardButton("🗑️ Yes, Delete Forever", callback_data=f"confirm_del_teacher_{tid}")],
        [InlineKeyboardButton("❌ Cancel", callback_data=f"view_teacher_{tid}")]
    ]
    
    await query.edit_message_text(
        f"⚠️ {html_bold('Delete Teacher?')}\n\n"
        f"Are you sure you want to delete this teacher?\n"
        f"This action {html_bold('cannot')} be undone.",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def delete_teacher(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    tid = query.data.split("_")[3] # confirm_del_teacher_{id}
    
    api = APIClient(update.effective_user.id)
    resp = api.delete(f"/api/teacher/{tid}")
    
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await query.answer("Teacher Deleted!", show_alert=True)
        await list_teachers(update, context)
    else:
        err = resp.get("error") if isinstance(resp, dict) else "Unknown"
        await query.answer(f"Failed: {err}", show_alert=True)
        await list_teachers(update, context)

# -------------------------------------------------------------------------
#  Search Teacher
# -------------------------------------------------------------------------

async def search_teacher_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(
        "🔍 **Search Teacher**\n\nEnter Name or Department:",
        reply_markup=get_back_button()
    )
    return SEARCH_QUERY

async def perform_search(update: Update, context: ContextTypes.DEFAULT_TYPE):
    q = update.message.text
    api = APIClient(update.effective_user.id)
    
    await update.message.chat.send_action(action="typing")
    # Backend TeacherQueryDto has SearchQuery
    resp = api.get(f"/api/teacher?SearchQuery={q}&PageSize=10")
    
    teachers = []
    if isinstance(resp, list): teachers = resp
    elif isinstance(resp, dict): teachers = resp.get("data") or []
    
    if not teachers:
        await update.message.reply_text(f"❌ No teachers found for '{q}'", reply_markup=get_back_button())
        return ConversationHandler.END
        
    text = f"🔍 {html_bold(f'Results for {esc(q)}')}\n━━━━━━━━━━━━━━━━━━━━\n\n"
    keyboard = []
    
    for t in teachers:
        name = f"{t.get('firstName')} {t.get('lastName')}".strip()
        tid = t.get('teacherId')
        dept = t.get('department')
        status = "🟢" if t.get('isActive', True) else "🔴"
        
        text += f"{status} {html_bold(esc(name))}\n   └ 🏛 {esc(str(dept))}\n\n"
        keyboard.append([InlineKeyboardButton(f"👁️ View {name}", callback_data=f"view_teacher_{tid}")])
        
    keyboard.append([InlineKeyboardButton("🔙 Back to List", callback_data="admin_teachers")])
    await update.message.reply_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    return ConversationHandler.END

search_teacher_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(search_teacher_start, pattern='^search_teacher_start$')],
    states={SEARCH_QUERY: [MessageHandler(filters.TEXT, perform_search)]},
    fallbacks=[CallbackQueryHandler(list_teachers, pattern='^admin_teachers')]
)

# -------------------------------------------------------------------------
#  Edit Teacher
# -------------------------------------------------------------------------

async def edit_teacher_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    tid = query.data.split("_")[2]
    context.user_data['edit_teacher_id'] = tid
    
    keyboard = [
        [InlineKeyboardButton("📱 Change Phone", callback_data="edit_t_phone"),
         InlineKeyboardButton("📜 Change Qualification", callback_data="edit_t_qual")],
        [InlineKeyboardButton("🔙 Back to Profile", callback_data=f"view_teacher_{tid}")]
    ]
    await query.edit_message_text(
        f"✏️ {html_bold('Edit Teacher Details')}\nSelect a field to update:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def edit_t_start_phone(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text("📱 Enter new **Phone Number**:", reply_markup=get_back_button())
    return EDIT_PHONE

async def edit_t_receive_phone(update: Update, context: ContextTypes.DEFAULT_TYPE):
    val = update.message.text
    tid = context.user_data.get('edit_teacher_id')
    api = APIClient(update.effective_user.id)
    
    # Fetch existing
    t = api.get(f"/api/teacher/{tid}")
    if not t: return ConversationHandler.END
    
    payload = {
        "firstName": t.get('firstName'),
        "lastName": t.get('lastName'),
        "department": t.get('department'),
        "specialization": t.get('specialization'),
        "qualification": t.get('qualification'),
        "experience": t.get('experience'),
        "phoneNumber": val 
    }
    
    resp = api.put(f"/api/teacher/{tid}", payload)
    if resp and "error" not in resp:
        await update.message.reply_text("✅ Phone Updated!")
    else:
        await update.message.reply_text(f"❌ Failed: {resp.get('error')}")
    return ConversationHandler.END

async def edit_t_start_qual(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text("📜 Enter new **Qualification**:", reply_markup=get_back_button())
    return EDIT_QUAL

async def edit_t_receive_qual(update: Update, context: ContextTypes.DEFAULT_TYPE):
    val = update.message.text
    tid = context.user_data.get('edit_teacher_id')
    api = APIClient(update.effective_user.id)
    
    t = api.get(f"/api/teacher/{tid}")
    if not t: return ConversationHandler.END
    
    payload = {
        "firstName": t.get('firstName'),
        "lastName": t.get('lastName'),
        "department": t.get('department'),
        "specialization": t.get('specialization'),
        "qualification": val,
        "experience": t.get('experience'),
        "phoneNumber": t.get('phoneNumber')
    }
    
    resp = api.put(f"/api/teacher/{tid}", payload)
    if resp and "error" not in resp:
        await update.message.reply_text("✅ Qualification Updated!")
    else:
        await update.message.reply_text(f"❌ Failed: {resp.get('error')}")
    return ConversationHandler.END

edit_teacher_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(edit_teacher_menu, pattern='^edit_teacher_')],
    states={
        EDIT_PHONE: [MessageHandler(filters.TEXT, edit_t_receive_phone)],
        EDIT_QUAL: [MessageHandler(filters.TEXT, edit_t_receive_qual)]
    },
    fallbacks=[
        CallbackQueryHandler(edit_t_start_phone, pattern='^edit_t_phone$'),
        CallbackQueryHandler(edit_t_start_qual, pattern='^edit_t_qual$'),
        CallbackQueryHandler(list_teachers, pattern='^admin_teachers')
    ]
)

# -------------------------------------------------------------------------
#  Direct Message Teacher
# -------------------------------------------------------------------------

async def dm_teacher_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    tid = query.data.split("_")[2] # TeacherID
    
    # Look up Telegram ID via Session Manager
    # Currently session manager maps userId -> telegramId
    # Teacher object has 'userId'. We need to fetch teacher first to get userId.
    
    api = APIClient(update.effective_user.id)
    teacher = api.get(f"/api/teacher/{tid}")
    
    uid = teacher.get('userId')
    if not uid:
        await query.edit_message_text("❌ User ID not found linked to this teacher.", reply_markup=get_back_button())
        return ConversationHandler.END
        
    teleg_id = session_manager.get_telegram_id(uid)
    
    if not teleg_id:
        await query.edit_message_text(
            "⚠️ **Cannot Message Teacher**\n\nTeacher has not logged into the bot yet.",
            reply_markup=get_back_button()
        )
        return ConversationHandler.END

    context.user_data['dm_teacher_tid'] = teleg_id # Store target telegram ID
    await query.edit_message_text("📩 **Direct Message**\nEnter message for teacher:", reply_markup=get_back_button())
    return DM_MESSAGE

async def dm_teacher_send(update: Update, context: ContextTypes.DEFAULT_TYPE):
    msg = update.message.text
    target_id = context.user_data.get('dm_teacher_tid')
    
    try:
        await context.bot.send_message(chat_id=target_id, text=f"🔔 {html_bold('Admin Notification')}\n\n{esc(msg)}", parse_mode="HTML")
        await update.message.reply_text("✅ Message sent!")
    except Exception as e:
        await update.message.reply_text(f"❌ Failed: {e}")
        
    return ConversationHandler.END

dm_teacher_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(dm_teacher_start, pattern='^dm_teacher_')],
    states={DM_MESSAGE: [MessageHandler(filters.TEXT, dm_teacher_send)]},
    fallbacks=[CallbackQueryHandler(list_teachers, pattern='^admin_teachers')]
)

# -------------------------------------------------------------------------
#  Add Teacher
# -------------------------------------------------------------------------

async def add_teacher_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text("➕ **Add Teacher**\n\nEnter **First Name**:", reply_markup=get_back_button())
    return T_ADD_NAME

async def t_receive_name(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_teacher_name'] = update.message.text
    await update.message.reply_text("Enter **Email Address**:")
    return T_ADD_EMAIL

async def t_receive_email(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_teacher_email'] = update.message.text
    await update.message.reply_text("Enter **Department**:")
    return T_ADD_DEPT

async def t_receive_dept(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_teacher_dept'] = update.message.text
    await update.message.reply_text("Enter **Qualification** (e.g. PhD, M.Tech):")
    return T_ADD_QUAL

async def t_receive_qual(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_teacher_qual'] = update.message.text
    await update.message.reply_text("Enter **Initial Password**:")
    return T_ADD_PASS

async def t_receive_pass(update: Update, context: ContextTypes.DEFAULT_TYPE):
    password = update.message.text
    
    payload = {
        "firstName": context.user_data['new_teacher_name'],
        "lastName": ".", 
        "email": context.user_data['new_teacher_email'],
        "department": context.user_data['new_teacher_dept'],
        "qualification": context.user_data['new_teacher_qual'],
        "password": password,
        "role": "Teacher",
        "experience": 0
    }
    
    api = APIClient(update.effective_user.id)
    resp = api.post("/api/teacher", payload)
    
    if resp and "error" not in resp:
        await update.message.reply_text("✅ Teacher Created Successfully!")
    else:
        await update.message.reply_text(f"❌ Failed: {resp.get('error')}")
        
    return ConversationHandler.END

add_teacher_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(add_teacher_start, pattern='^add_teacher_start$')],
    states={
        T_ADD_NAME: [MessageHandler(filters.TEXT, t_receive_name)],
        T_ADD_EMAIL: [MessageHandler(filters.TEXT, t_receive_email)],
        T_ADD_DEPT: [MessageHandler(filters.TEXT, t_receive_dept)],
        T_ADD_QUAL: [MessageHandler(filters.TEXT, t_receive_qual)],
        T_ADD_PASS: [MessageHandler(filters.TEXT, t_receive_pass)]
    },
    fallbacks=[CallbackQueryHandler(list_teachers, pattern='^admin_teachers$')]
)

# -------------------------------------------------------------------------
#  View Teacher Schedule
# -------------------------------------------------------------------------

async def view_teacher_schedule(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    tid = query.data.split("_")[2]
    api = APIClient(update.effective_user.id)
    
    # Fetch Teacher to get Name
    teacher = api.get(f"/api/teacher/{tid}")
    t_name = f"{teacher.get('firstName', '')} {teacher.get('lastName', '')}".strip() if teacher else "Teacher"

    # Fetch TimeSlots
    slots = api.get(f"/api/timeslot/teacher/{tid}")
    
    if not slots or (isinstance(slots, dict) and "error" in slots) or len(slots) == 0:
        await query.edit_message_text(
            f"📅 <b>Schedule for {esc(t_name)}</b>\n━━━━━━━━━━━━━━━━━━━━\n\n<i>No classes assigned yet.</i>",
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("🔙 Back to Profile", callback_data=f"view_teacher_{tid}")]])
        )
        return

    # Pre-fetch ALL courses to ensure we get names (Optimization: Fetch only needed if possible, but GET /api/course is easiest)
    course_map = {}
    try:
        all_courses = api.get("/api/course?PageSize=100")
        c_list = []
        if isinstance(all_courses, list): c_list = all_courses
        elif isinstance(all_courses, dict): c_list = all_courses.get("data") or all_courses.get("items") or all_courses.get("value") or []
        
        for c in c_list:
            cid = c.get('courseId') or c.get('id')
            cname = c.get('name') or c.get('courseName')
            if cid and cname:
                 course_map[cid] = cname
    except Exception as e:
        logging.error(f"Failed to fetch course map: {e}")

    # Sort and Group by Day
    days_order = {
        "Monday": 1, "Tuesday": 2, "Wednesday": 3, "Thursday": 4, "Friday": 5, "Saturday": 6, "Sunday": 7
    }
    
    grouped = {d: [] for d in days_order.keys()}

    for s in slots:
        day = s.get('dayOfWeek', 'Monday')
        day = day.capitalize()
        if day in grouped:
            grouped[day].append(s)
    
    # Modern Modern UI Construction
    text = f"🗓 <b>Weekly Timetable</b>\n"
    text += f"👤 <i>{esc(t_name)}</i>\n"
    text += f"━━━━━━━━━━━━━━━━━━━━\n"
    
    has_classes = False
    
    for day, day_slots in grouped.items():
        if not day_slots: continue
        has_classes = True
        
        # Sort by StartTime
        day_slots.sort(key=lambda x: x.get('startTime', '00:00:00'))
        
        text += f"\n📅 <b>{day}</b>\n"
        
        for s in day_slots:
            start = s.get('startTime', '')[:5] # HH:MM
            end = s.get('endTime', '')[:5]
            room = s.get('room', 'N/A')
            cid = s.get('courseId')
            
            # Resolve Course Name
            c_name = course_map.get(cid, f"Course #{cid}")
            
            # Format:  09:00 - 10:00  |  Room 101
            #          📘 CS101 - Intro to CS
            text += f"⏰ {html_code(start)} - {html_code(end)}  📍 {html_code(room)}\n"
            text += f"📚 <b>{esc(c_name)}</b>\n"
            text += f"〰️〰️〰️〰️〰️〰️\n"

    if not has_classes:
         text += "\n<i>No classes scheduled.</i>"

    await query.edit_message_text(
        text, 
        parse_mode="HTML", 
        reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("🔙 Back to Profile", callback_data=f"view_teacher_{tid}")]])
    )

# -------------------------------------------------------------------------
#  Reset Teacher Password
# -------------------------------------------------------------------------
RESET_PASS_NEW = range(1)

async def reset_tpass_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    tid = query.data.split("_")[2]
    context.user_data['reset_t_id'] = tid
    
    await query.edit_message_text(
        "🔑 <b>Reset Password</b>\n\nEnter the <b>New Password</b> for this teacher:",
        parse_mode="HTML",
        reply_markup=get_back_button()
    )
    return RESET_PASS_NEW

async def reset_tpass_complete(update: Update, context: ContextTypes.DEFAULT_TYPE):
    new_pass = update.message.text
    tid = context.user_data.get('reset_t_id')
    api = APIClient(update.effective_user.id)
    
    # Need Teacher Email
    teacher = api.get(f"/api/teacher/{tid}")
    if not teacher:
        await update.message.reply_text("❌ Teacher not found.")
        return ConversationHandler.END
        
    email = teacher.get('email')
    
    # Call Auth ChangePassword
    payload = {
        "email": email,
        "newPassword": new_pass
    }
    
    resp = api.put("/api/auth/change-password", payload)
    
    if resp and "message" in resp and "success" in resp.get("message", "").lower():
         await update.message.reply_text("✅ Password Reset Successfully!")
    elif resp and "error" not in resp:
         # Some endpoints return simple 200 OK with message
         await update.message.reply_text("✅ Password Reset Successfully!")
    else:
         await update.message.reply_text(f"❌ Failed: {resp.get('error') or resp.get('message')}")
         
    return ConversationHandler.END

reset_teacher_pass_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(reset_tpass_start, pattern='^reset_tpass_')],
    states={RESET_PASS_NEW: [MessageHandler(filters.TEXT, reset_tpass_complete)]},
    fallbacks=[CallbackQueryHandler(view_teacher, pattern='^view_teacher_')]
)

# -------------------------------------------------------------------------
#  Teacher Stats
# -------------------------------------------------------------------------

async def view_teacher_stats(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    tid = query.data.split("_")[2]
    api = APIClient(update.effective_user.id)
    
    # Fetch Basic Info
    teacher = api.get(f"/api/teacher/{tid}")
    t_name = f"{teacher.get('firstName', '')} {teacher.get('lastName', '')}".strip() if teacher else "Teacher"
    
    # Fetch TimeSlots (Workload)
    slots = api.get(f"/api/timeslot/teacher/{tid}")
    
    total_classes = 0
    unique_courses = set()
    total_minutes = 0
    
    if slots and isinstance(slots, list):
        total_classes = len(slots)
        for s in slots:
            unique_courses.add(s.get('courseId'))
            
            # Calculate Duration
            try:
                start = s.get('startTime', '00:00:00')
                end = s.get('endTime', '00:00:00')
                h1, m1, s1 = map(int, start.split(':'))
                h2, m2, s2 = map(int, end.split(':'))
                minutes = (h2 * 60 + m2) - (h1 * 60 + m1)
                total_minutes += minutes
            except:
                pass

    total_hours = total_minutes / 60
    
    text = (
        f"📊 <b>Teacher Statistics</b>\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"👤 <b>{esc(t_name)}</b>\n\n"
        f"📚 <b><u>Workload Analysis</u></b>\n"
        f"├ 🏫 <b>Courses:</b> {len(unique_courses)}\n"
        f"├ 🗓 <b>Weekly Classes:</b> {total_classes}\n"
        f"└ ⏱ <b>Total Hours:</b> {total_hours:.1f} hrs/week\n\n"
        f"<i>(Note: Attendance stats not available yet)</i>"
    )
    
    await query.edit_message_text(
        text,
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("🔙 Back to Profile", callback_data=f"view_teacher_{tid}")]])
    )
