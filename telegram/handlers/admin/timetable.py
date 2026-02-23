from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from handlers.menu import get_back_button
from utils.formatting import get_breadcrumbs, html_bold, html_code, html_italic, esc

# States
TT_SELECT_COURSE, TT_SELECT_DAY, TT_START_TIME, TT_END_TIME, TT_ROOM, TT_CONFIRM = range(6)
TT_SEARCH_COURSE = range(1)

# -------------------------------------------------------------------------
#  Main Menu & View
# -------------------------------------------------------------------------

# -------------------------------------------------------------------------
#  Main Menu & View
# -------------------------------------------------------------------------

async def timetable_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    keyboard = [
        [InlineKeyboardButton("➕ Add Time Slot", callback_data="tt_add_start")],
        [InlineKeyboardButton("🗓️ Full Weekly View", callback_data="tt_view_day_0_all_0")],
        [InlineKeyboardButton("👨‍🏫 By Teacher", callback_data="tt_filter_tea"),
         InlineKeyboardButton("🏢 By Dept", callback_data="tt_filter_dep")],
        [InlineKeyboardButton("📚 By Course", callback_data="tt_filter_cou")],
        [InlineKeyboardButton("🔙 Main Menu", callback_data="main_menu")]
    ]
    
    text_content = (
        f"🏠 Home > 📅 {html_bold('Timetable Management')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Manage class schedules, assign rooms, and resolving conflicts.\n\n"
        f"🔍 {html_bold('Filtering:')} Select a specific Teacher, Department, or Course to view their schedule."
    )

    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(
            chat_id=query.message.chat_id,
            text=text_content,
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup(keyboard)
        )
    else:
        await query.edit_message_text(
            text=text_content,
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup(keyboard)
        )

async def tt_show_filter_list(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Shows list of Teachers, Departments, or Courses to select.
    """
    query = update.callback_query
    await query.answer()
    
    filter_type = query.data.split("_")[2] # tea, dep, cou
    api = APIClient(update.effective_user.id)
    
    items = []
    title = ""
    
    if filter_type == "tea":
        title = "Select Teacher"
        resp = api.get("/api/teacher")
        data = resp if isinstance(resp, list) else resp.get("data", [])
        items = [(t.get('teacherId'), t.get('firstName') + " " + t.get('lastName')) for t in data]
        
    elif filter_type == "dep":
        title = "Select Department"
        resp = api.get("/api/department")
        data = resp if isinstance(resp, list) else resp.get("data", [])
        items = [(d.get('departmentId'), d.get('name')) for d in data]
        
    elif filter_type == "cou":
        title = "Select Course"
        resp = api.get("/api/course")
        data = resp if isinstance(resp, list) else resp.get("data", [])
        items = [(c.get('courseId'), c.get('courseCode') or c.get('name')) for c in data]

    keyboard = []
    row = []
    # Simple pagination limit for now (top 20)
    for i, (iid, name) in enumerate(items[:20]):
        row.append(InlineKeyboardButton(name[:30], callback_data=f"tt_view_day_0_{filter_type}_{iid}"))
        if len(row) == 2:
            keyboard.append(row)
            row = []
    if row: keyboard.append(row)
    
    keyboard.append([InlineKeyboardButton("🔙 Back", callback_data="admin_timetable")])
    
    await query.edit_message_text(
        f"🔍 {html_bold(esc(title))}\nSelect one to view timetable:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def view_timetable_day(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Callback Format: tt_view_day_{day_idx}_{type}_{id}
    # legacy fallback: tt_view_day_{day_idx} -> treat as all_0
    
    parts = query.data.split("_")
    try:
        day_idx = int(parts[3])
    except:
        day_idx = 0
        
    filter_type = parts[4] if len(parts) > 4 else "all"
    filter_id = parts[5] if len(parts) > 5 else "0"
    
    days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
    current_day = days[day_idx]
    
    api = APIClient(update.effective_user.id)
    # Fetch all slots (Optimization: cache this if possible, but API call is okay for now)
    resp = api.get("/api/timeslot")
    slots = resp if isinstance(resp, list) else resp.get("data", [])
    
    # Fetch Metadata for Names
    c_map = {}
    t_map = {}
    
    # Helper for Case-Insensitive Get
    def get_val(obj, keys, default=None):
        for k in keys:
            if k in obj: return obj[k]
        return default

    # Always fetch courses for naming
    c_resp = api.get("/api/course?PageSize=100")
    c_data = c_resp if isinstance(c_resp, list) else c_resp.get("data", [])
    
    # Map CourseId (or courseId) -> Course Obj
    for c in c_data:
        cid = get_val(c, ['courseId', 'CourseId', 'id', 'Id'])
        if cid: c_map[int(cid)] = c

    # Fetch Teachers if needed for naming or filtering
    t_resp = api.get("/api/teacher?PageSize=100")
    t_data = t_resp if isinstance(t_resp, list) else t_resp.get("data", [])
    
    # Map TeacherId -> Teacher Obj
    for t in t_data:
        tid = get_val(t, ['teacherId', 'TeacherId', 'id', 'Id'])
        if tid: t_map[int(tid)] = t

    # FILTER LOGIC
    # print(f"[DEBUG] Loaded {len(c_data)} courses and {len(t_data)} teachers.")
    
    filtered_slots = []
    filter_title = ""
    
    for s in slots:
        # Day Check
        day_val = get_val(s, ['dayOfWeek', 'DayOfWeek'], '')
        if day_val.lower() != current_day.lower():
            continue
            
        sid = get_val(s, ['timeSlotId', 'TimeSlotId', 'id', 'Id'])
        cid = get_val(s, ['courseId', 'CourseId'])
        
        # KEY CHANGE: TeacherId is on the SLOT, not the Course
        tid = get_val(s, ['teacherId', 'TeacherId'])
        
        # Type Check
        if filter_type == "all":
            filtered_slots.append(s)
            filter_title = "Full View"
            
        elif filter_type == "tea":
            # Direct match on Slot's TeacherId
            if str(tid) == str(filter_id):
                 filtered_slots.append(s)
            
            t_obj = t_map.get(int(filter_id), {})
            t_name = get_val(t_obj, ['firstName', 'FirstName'], 'Teacher')
            filter_title = f"Teacher: {t_name}"

        elif filter_type == "dep":
            # Department is on Course
            if cid:
                course = c_map.get(int(cid))
                if course:
                    c_did = get_val(course, ['departmentId', 'DepartmentId'])
                    if str(c_did) == str(filter_id):
                        filtered_slots.append(s)
            filter_title = "Department View"

        elif filter_type == "cou":
            if str(cid) == str(filter_id):
                filtered_slots.append(s)
            
            c_obj = c_map.get(int(filter_id), {})
            c_name = get_val(c_obj, ['courseCode', 'CourseCode', 'name', 'Name'], 'Course')
            filter_title = f"Course: {c_name}"

    filtered_slots.sort(key=lambda x: get_val(x, ['startTime', 'StartTime'], ''))
    
    # Build Message
    text = (
        f"🏠 Home > 📅 {html_bold('Timetable')} > 🗓 {html_bold(current_day)}\n"
        f"🔍 Filter: {html_italic(esc(filter_title))}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
    )
    
    keyboard = []
    
    if not filtered_slots:
        text += f"\n🚫 {html_italic('No classes found for this filter.')}\n"
    else:
        for s in filtered_slots:
            sid = get_val(s, ['timeSlotId', 'TimeSlotId', 'id', 'Id'])
            start = get_val(s, ['startTime', 'StartTime'], '')[:5]
            end = get_val(s, ['endTime', 'EndTime'], '')[:5]
            room = get_val(s, ['room', 'Room'])
            cid = get_val(s, ['courseId', 'CourseId'])
            tid = get_val(s, ['teacherId', 'TeacherId'])
            
            # Safe Lookup
            course = c_map.get(int(cid) if cid else 0, {})
            cname = get_val(course, ['courseCode', 'CourseCode', 'name', 'Name']) or f"Unknown Course ({cid})"
            
            # Teacher Lookup using SLOT's TeacherId
            try:
                tid = int(tid) if tid else 0
            except (ValueError, TypeError):
                tid = 0
                
            t_obj = t_map.get(tid, {})
            tname = get_val(t_obj, ['firstName', 'FirstName'], 'N/A')
            
            text += (
                f"⏰ {html_bold(f'{start} - {end}')}\n"
                f"📍 Room: {html_code(esc(room))}\n"
                f"📘 {html_bold(esc(cname))} ({esc(tname)})\n"
                f"〰️〰️〰️〰️〰️〰️\n"
            )
            
            # Action Buttons: tt_del_confirm_{sid}_{day_idx}_{type}_{id}
            # We pass full context to return correctly
            # But callback limit! 64 chars.
            # tt_del_confirm_105_0_tea_5 -> 26 chars. OK.
            
            ctx_str = f"{day_idx}_{filter_type}_{filter_id}"
            
            keyboard.append([
                InlineKeyboardButton(f"✏️ Edit", callback_data=f"tt_edit_{sid}"),
                InlineKeyboardButton(f"🗑️ Del", callback_data=f"tt_del_confirm_{sid}_{ctx_str}") 
            ])
            
    # Pagination & Navigation
    # Ensure prev/next buttons keep the filter
    nav_row = []
    prev_idx = (day_idx - 1) % 7
    next_idx = (day_idx + 1) % 7
    
    # tt_view_day_{idx}_{type}_{id}
    nav_row.append(InlineKeyboardButton(f"⬅️ {days[prev_idx][:3]}", callback_data=f"tt_view_day_{prev_idx}_{filter_type}_{filter_id}"))
    nav_row.append(InlineKeyboardButton(f"🗓 {current_day}", callback_data="noop"))
    nav_row.append(InlineKeyboardButton(f"{days[next_idx][:3]} ➡️", callback_data=f"tt_view_day_{next_idx}_{filter_type}_{filter_id}"))
    
    keyboard.append(nav_row)
    keyboard.append([InlineKeyboardButton("🔙 Back to Menu", callback_data="admin_timetable")])
    
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# -------------------------------------------------------------------------
#  Add Time Slot Wizard
# -------------------------------------------------------------------------

async def tt_add_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Fetch Courses for Selection
    api = APIClient(update.effective_user.id)
    resp = api.get("/api/course")
    courses = resp if isinstance(resp, list) else resp.get("data", [])
    
    keyboard = []
    # Limit to 10 for now, or add search later
    for c in courses[:10]:
        cname = c.get('courseCode')
        cid = c.get('courseId')
        keyboard.append([InlineKeyboardButton(f"📘 {cname}", callback_data=f"tt_sel_course_{cid}")])
        
    keyboard.append([InlineKeyboardButton("🔙 Cancel", callback_data="admin_timetable")])
    
    await query.edit_message_text(
        f"🏠 Home > 📅 Timetable > ➕ {html_bold('Add Slot')}\n"
        "━━━━━━━━━━━━━━━━━━━━\n"
        f"{html_bold('Step 1/5:')} Select Course",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )
    return TT_SELECT_COURSE

async def tt_sel_course(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    cid = query.data.split("_")[3]
    context.user_data['tt_cid'] = cid
    
    days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]
    keyboard = []
    row = []
    for d in days:
        row.append(InlineKeyboardButton(d, callback_data=f"tt_sel_day_{d}"))
        if len(row) == 2:
            keyboard.append(row)
            row = []
            
    if row: keyboard.append(row)
    keyboard.append([InlineKeyboardButton("🔙 Cancel", callback_data="admin_timetable")])
    
    await query.edit_message_text(
        f"🏠 Home > 📅 Timetable > ➕ {html_bold('Add Slot')}\n"
        "━━━━━━━━━━━━━━━━━━━━\n"
        f"{html_bold('Step 2/5:')} Select Day of Week",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )
    return TT_SELECT_DAY

async def tt_sel_day(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    day = query.data.split("_")[3]
    context.user_data['tt_day'] = day
    
    await query.edit_message_text(
        f"🏠 Home > 📅 Timetable > ➕ {html_bold('Add Slot')}\n"
        "━━━━━━━━━━━━━━━━━━━━\n"
        f"{html_bold('Step 3/5:')} Enter Start Time (24-hour format, e.g. {html_code('09:00')})",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("🔙 Cancel", callback_data="admin_timetable")]])
    )
    return TT_START_TIME

async def tt_receive_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    time = update.message.text
    # Basic Validation regex would be good, for now trust user
    context.user_data['tt_start'] = time
    
    await update.message.reply_text(f"{html_bold('Step 4/5:')} Enter {html_bold('End Time')} (e.g. {html_code('10:00')}):", parse_mode="HTML")
    return TT_END_TIME

async def tt_receive_end(update: Update, context: ContextTypes.DEFAULT_TYPE):
    time = update.message.text
    context.user_data['tt_end'] = time
    
    await update.message.reply_text(f"{html_bold('Step 5/5:')} Enter {html_bold('Room Number')} (e.g. {html_code('101')} or {html_code('Lab A')}):", parse_mode="HTML")
    return TT_ROOM

async def tt_receive_room(update: Update, context: ContextTypes.DEFAULT_TYPE):
    room = update.message.text
    context.user_data['tt_room'] = room
    
    # Review
    data = context.user_data
    text = (
        f"📝 {html_bold('Confirm Details')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"📘 {html_bold('Course ID:')} {data['tt_cid']}\n"
        f"🗓 {html_bold('Day:')} {data['tt_day']}\n"
        f"⏰ {html_bold('Time:')} {data['tt_start']} - {data['tt_end']}\n"
        f"📍 {html_bold('Room:')} {esc(room)}\n"
    )
    
    keyboard = [
        [InlineKeyboardButton("✅ Confirm & Add", callback_data="tt_confirm_submit")],
        [InlineKeyboardButton("❌ Cancel", callback_data="admin_timetable")]
    ]
    
    await update.message.reply_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    return TT_CONFIRM

async def tt_submit(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    api = APIClient(update.effective_user.id)
    data = context.user_data
    
    payload = {
        "courseId": int(data['tt_cid']),
        "dayOfWeek": data['tt_day'],
        "startTime": data['tt_start'],
        "endTime": data['tt_end'],
        "room": data['tt_room']
    }
    
    resp = api.post("/api/timeslot", payload)
    
    if resp and "error" not in resp:
        await query.edit_message_text(
            f"✅ {html_bold('Time Slot Added Successfully!')}",
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("🔙 Timetable Menu", callback_data="admin_timetable")]])
        )
    else:
        err = resp.get('error', 'Unknown Error')
        await query.edit_message_text(f"❌ Failed: {err}", reply_markup=get_back_button())
        
    return ConversationHandler.END

# -------------------------------------------------------------------------
#  Edit Time Slot
# -------------------------------------------------------------------------

TT_EDIT_ROOM = range(1)

async def tt_edit_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    tsid = query.data.split("_")[3]
    context.user_data['edit_tt_id'] = tsid
    
    keyboard = [
        [InlineKeyboardButton("📍 Change Room", callback_data="tt_edit_room_start")],
        [InlineKeyboardButton("🗑️ Delete Slot", callback_data=f"tt_del_confirm_{tsid}")],
        [InlineKeyboardButton("🔙 Back to Timetable", callback_data="tt_view_day_0_all_0")]
    ]
    
    await query.edit_message_text(
        f"✏️ {html_bold('Edit Time Slot')}\n\nSelect a field to update:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def tt_edit_room_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    await query.edit_message_text(
        f"📍 Enter new {html_bold('Room Number')}:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("🔙 Cancel", callback_data="tt_view_day_0_all_0")]])
    )
    return TT_EDIT_ROOM

async def tt_receive_new_room(update: Update, context: ContextTypes.DEFAULT_TYPE):
    room = update.message.text
    tsid = context.user_data['edit_tt_id']
    api = APIClient(update.effective_user.id)
    
    # Fetch existing to keep other fields
    # NOTE: API might need full PUT. Let's assume PUT /api/timeslot/{id} needs full object
    # If partial update not supported, we need to fetch first.
    # We can GET /api/timeslot/{id} but we might not have that endpoint readily available? 
    # Usually /api/timeslot returns list. Let's try GET /api/timeslot/{id}
    
    # If GET by ID not available, we have to iterate list (inefficient but works for small sets)
    all_slots = api.get("/api/timeslot")
    if isinstance(all_slots, dict): all_slots = all_slots.get("data", [])
    
    target = next((s for s in all_slots if str(s.get('timeSlotId') or s.get('id')) == str(tsid)), None)
    
    if not target:
        await update.message.reply_text("❌ Error: Slot not found.", reply_markup=get_back_button())
        return ConversationHandler.END
        
    payload = {
        "courseId": target.get('courseId'),
        "dayOfWeek": target.get('dayOfWeek'),
        "startTime": target.get('startTime'),
        "endTime": target.get('endTime'),
        "room": room
    }
    
    resp = api.put(f"/api/timeslot/{tsid}", payload)
    
    if resp and "error" not in resp:
        await update.message.reply_text("✅ Room Updated!")
    else:
        await update.message.reply_text(f"❌ Failed: {resp.get('error')}")
        
    return ConversationHandler.END

tt_edit_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(tt_edit_menu, pattern='^tt_edit_')],
    states={
        TT_EDIT_ROOM: [MessageHandler(filters.TEXT, tt_receive_new_room)]
    },
    fallbacks=[CallbackQueryHandler(view_timetable_day, pattern='^tt_view_day_')]
)

# -------------------------------------------------------------------------
#  Delete
# -------------------------------------------------------------------------

async def tt_delete_confirm(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Format: tt_del_confirm_{sid}_{day_idx}_{type}_{id}
    parts = query.data.split("_")
    tsid = parts[3]
    # Reconstruct context
    ctx = "_".join(parts[4:]) if len(parts) > 4 else "0_all_0"
    
    keyboard = [
        [InlineKeyboardButton("🗑️ Yes, Delete", callback_data=f"tt_del_final_{tsid}_{ctx}")],
        [InlineKeyboardButton("❌ Cancel", callback_data=f"tt_view_day_{ctx}")]
    ]
    
    await query.edit_message_text(
        f"⚠️ {html_bold('Delete Time Slot?')}\nAre you sure you want to remove this slot?",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def tt_delete_final(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    
    # Format: tt_del_final_{sid}_{day_idx}_{type}_{id}
    parts = query.data.split("_")
    tsid = parts[3]
    ctx = "_".join(parts[4:]) if len(parts) > 4 else "0_all_0"
    
    api = APIClient(update.effective_user.id)
    resp = api.delete(f"/api/timeslot/{tsid}")
    
    # Redirect back
    query.data = f"tt_view_day_{ctx}"
    
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await query.answer("✅ Deleted!", show_alert=True)
    else:
        await query.answer("❌ Failed to delete.", show_alert=True)
        
    await view_timetable_day(update, context)

# -------------------------------------------------------------------------
#  Handler Registry
# -------------------------------------------------------------------------

tt_add_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(tt_add_start, pattern='^tt_add_start$')],
    states={
        TT_SELECT_COURSE: [CallbackQueryHandler(tt_sel_course, pattern='^tt_sel_course_')],
        TT_SELECT_DAY: [CallbackQueryHandler(tt_sel_day, pattern='^tt_sel_day_')],
        TT_START_TIME: [MessageHandler(filters.TEXT, tt_receive_start)],
        TT_END_TIME: [MessageHandler(filters.TEXT, tt_receive_end)],
        TT_ROOM: [MessageHandler(filters.TEXT, tt_receive_room)],
        TT_CONFIRM: [CallbackQueryHandler(tt_submit, pattern='^tt_confirm_submit$')]
    },
    fallbacks=[CallbackQueryHandler(timetable_menu, pattern='^admin_timetable$')]
)
