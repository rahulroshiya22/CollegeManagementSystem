from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from handlers.menu import get_back_button, get_pagination_keyboard
from handlers.menu import get_back_button, get_pagination_keyboard
from utils.formatting import get_progress_bar, html_bold, html_code, html_italic, esc
import logging

# States
SEARCH_ATT_STUDENT = range(1)

# -------------------------------------------------------------------------
#  Attendance Dashboard
# -------------------------------------------------------------------------

async def view_attendance_dashboard(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    text = (
        f"🏠 Home > 📅 {html_bold('Attendance')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Monitor student attendance and class records.\n\n"
        f"👇 {html_bold('Select an option:')}"
    )
    
    keyboard = [
        [InlineKeyboardButton("🏢 View by Department", callback_data="admin_att_depts")],
        [InlineKeyboardButton("🔍 Search Student Stats", callback_data="search_att_start")],
        [InlineKeyboardButton("🔙 Main Menu", callback_data="main_menu")]
    ]
    
    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(
            chat_id=query.message.chat_id,
            text=text,
            parse_mode="HTML",
            reply_markup=InlineKeyboardMarkup(keyboard)
        )
    else:
        await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# -------------------------------------------------------------------------
#  Department Selection & Actions
# -------------------------------------------------------------------------

async def list_att_departments(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    api = APIClient(update.effective_user.id)
    departments = api.get("/api/department") or []
    if isinstance(departments, dict): departments = departments.get("data", [])

    keyboard = []
    row = []
    for dept in departments:
        name = dept.get('name', 'Unknown')
        did = dept.get('departmentId') or dept.get('id')
        
        # Validation
        if not did:
            continue
            
        row.append(InlineKeyboardButton(f"{name}", callback_data=f"admin_att_action_{did}")) # Goto Action Menu
        if len(row) == 2:
            keyboard.append(row)
            row = []
    if row: keyboard.append(row)
    
    keyboard.append([InlineKeyboardButton("🔙 Back", callback_data="admin_attendance")])
    
    await query.edit_message_text(f"🏢 {html_bold('Select Department:')}", parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def select_dept_action(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    dept_id = query.data.split("_")[3]
    
    # Optional: Fetch Dept Name for better UI
    api = APIClient(update.effective_user.id)
    dept = api.get(f"/api/department/{dept_id}")
    d_name = dept.get('name', 'Department') if dept else f"Department #{dept_id}"

    text = f"🏢 {html_bold(d_name)}\n━━━━━━━━━━━━━━━━━━━━\nSelect View Mode:"
    
    keyboard = [
        [InlineKeyboardButton("📊 Course Statistics", callback_data=f"admin_att_courses_{dept_id}")],
        [InlineKeyboardButton("👥 Student Attendance List", callback_data=f"admin_att_students_{dept_id}_page_1")],
        [InlineKeyboardButton("🔙 Change Department", callback_data="admin_att_depts")]
    ]
    
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# -------------------------------------------------------------------------
#  Flow A: Course Statistics
# -------------------------------------------------------------------------

async def list_att_courses(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    dept_id = query.data.split("_")[3]
    
    api = APIClient(update.effective_user.id)
    courses = api.get(f"/api/course/department/{dept_id}") or []
    if isinstance(courses, dict): courses = courses.get("data", [])
    
    if not courses:
        await query.edit_message_text(f"❌ No courses found for Dept #{dept_id}.", reply_markup=get_back_button())
        return

    keyboard = []
    for c in courses:
        cid = c.get('courseId') or c.get('id')
        code = c.get('courseCode') or c.get('code')
        name = c.get('courseName') or c.get('name')
        keyboard.append([InlineKeyboardButton(f"{code} - {name}", callback_data=f"view_att_course_{cid}")])
        
    keyboard.append([InlineKeyboardButton("🔙 Back", callback_data=f"admin_att_action_{dept_id}")])
    
    await query.edit_message_text(f"📚 {html_bold('Select Course to View Stats:')}", parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def view_course_att_stats(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    cid = query.data.split("_")[3]
    api = APIClient(update.effective_user.id)
    
    # Fetch Course Info
    course = api.get(f"/api/course/{cid}")
    c_name = course.get('courseName') if course else "Unknown Course"
    dept_id = course.get("departmentId", 0)
    
    # Fetch Attendance Records for this Course
    records = api.get(f"/api/attendance/course/{cid}") or []
    if isinstance(records, dict): records = records.get("data", [])
    
    if not records:
        await query.edit_message_text(f"📉 {html_bold(esc(c_name))}\n\nNo attendance records found.", reply_markup=get_back_button(), parse_mode="HTML")
        return

    # Calculate Stats
    total_records = len(records)
    present_count = sum(1 for r in records if r.get('isPresent'))
    avg_measure = (present_count / total_records * 100) if total_records > 0 else 0
    dates = set(r.get('date', '').split('T')[0] for r in records)
    total_classes = len(dates)
    
    bar = get_progress_bar(avg_measure, total=100)
    
    text = (
        f"🏠 Home > 📅 Attd > 📊 {html_bold('Stats')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"📘 {html_bold(esc(c_name))}\n\n"
        f"📅 {html_bold('Total Classes Held:')} {total_classes}\n"
        f"👥 {html_bold('Total Records:')} {total_records}\n"
        f"✅ {html_bold('Overall Presence:')} {avg_measure:.1f}%\n"
        f"{html_code(bar)}\n\n"
    )
    
    keyboard = [[InlineKeyboardButton("🔙 Back to Courses", callback_data=f"admin_att_courses_{dept_id}")]]
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# -------------------------------------------------------------------------
#  Flow B: Student List & Detailed View
# -------------------------------------------------------------------------

async def list_att_dept_students(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Pattern: admin_att_students_{dept_id}_page_{page}
    parts = query.data.split("_")
    dept_id = parts[3]
    page = int(parts[5])
    
    api = APIClient(update.effective_user.id)
    
    # Fetch Students Filtered by Dept
    endpoint = f"/api/student?DepartmentId={dept_id}&Page={page}&PageSize=10"
    print(f"DEBUG: Fetching students from {endpoint}")
    response = api.get(endpoint)
    print(f"DEBUG: API Response: {response}")
    
    students = []
    total_pages = 1
    
    if isinstance(response, dict):
        students = response.get("data") or response.get("items") or response.get("value") or []
        total_pages = response.get("totalPages", 1)
    elif isinstance(response, list):
        students = response
        
    if not students:
        keyboard = [[InlineKeyboardButton("🔙 Back", callback_data=f"admin_att_action_{dept_id}")]]
        await query.edit_message_text(f"❌ No students found for Dept #{dept_id}.", reply_markup=InlineKeyboardMarkup(keyboard))
        return

    text = f"👥 {html_bold('Student List')} (Page {page}/{total_pages})\nSelect a student to view detailed attendance:\n"
    
    keyboard = []
    for s in students:
        sid = s.get('studentId')
        name = f"{s.get('firstName')} {s.get('lastName')}"
        keyboard.append([InlineKeyboardButton(f"👤 {name}", callback_data=f"view_att_student_{sid}_{dept_id}")])

    # Pagination
    nav = []
    if page > 1:
        nav.append(InlineKeyboardButton("⬅️ Prev", callback_data=f"admin_att_students_{dept_id}_page_{page-1}"))
    if page < total_pages:
        nav.append(InlineKeyboardButton("Next ➡️", callback_data=f"admin_att_students_{dept_id}_page_{page+1}"))
    if nav: keyboard.append(nav)
    
    keyboard.append([InlineKeyboardButton("🔙 Back to Options", callback_data=f"admin_att_action_{dept_id}")])
    
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def view_student_att_detail(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Pattern: view_att_student_{sid}_{dept_id} (dept_id for back button)
    parts = query.data.split("_")
    sid = parts[3]
    dept_id = parts[4] if len(parts) > 4 else 0
    
    api = APIClient(update.effective_user.id)
    
    # 1. Fetch Student Info
    student = api.get(f"/api/student/{sid}")
    name = f"{student.get('firstName', 'Student')} {student.get('lastName', '')}"
    
    # 2. Fetch Attendance
    records = api.get(f"/api/attendance/student/{sid}")
    if isinstance(records, dict): records = records.get("data", [])
    
    if not records:
        text = f"📊 {html_bold(f'Attendance: {esc(name)}')}\n\nNo attendance records found."
        keyboard = [[InlineKeyboardButton("🔙 Back to List", callback_data=f"admin_att_students_{dept_id}_page_1")]]
        await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
        return

    # 3. Aggregate Stats
    total_present = 0
    total_lectures = 0
    course_stats = {} # cid -> {present, total, name}
    
    for r in records:
        cid = r.get('courseId')
        if cid not in course_stats:
            # Try to get course name? Doing it per record is slow.
            # Optimization: Just show ID or fetch unique course IDs later.
            course_stats[cid] = {'present': 0, 'total': 0}
            
        course_stats[cid]['total'] += 1
        total_lectures += 1
        if r.get('isPresent'):
            course_stats[cid]['present'] += 1
            total_present += 1
            
    # Calculate Overall
    overall_pct = (total_present / total_lectures * 100) if total_lectures > 0 else 0
    overall_bar = get_progress_bar(overall_pct, total=100, length=10)
    
    # Modern Design
    text = (
        f"🏠 Home > 📅 Attd > 👤 {html_bold('Detail')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"👤 {html_bold(esc(name))}\n"
        f"🆔 Student ID: {html_code(sid)}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n\n"
        f"📊 {html_bold('Overall Attendance')}\n"
        f"{html_code(overall_bar)} {html_bold(f'{overall_pct:.1f}%')}\n"
        f"🔹 Present: {html_bold(total_present)} / {total_lectures} Lectures\n\n"
        f"📚 {html_bold('Subject Breakdown')}\n"
        f"────────────────────\n"
    )
    
    for cid, stats in course_stats.items():
        pct = (stats['present'] / stats['total']) * 100
        
        # Status Icon & State
        if pct >= 75:
            icon = "🟢"
            state = "Excellent"
        elif pct >= 60:
            icon = "🟡"
            state = "Fair"
        else:
            icon = "🔴"
            state = "Low"
        
        # Fetch Course Name
        c_resp = api.get(f"/api/course/{cid}")
        c_name = c_resp.get('courseName') if c_resp else f"Subject #{cid}"
        
        # Course Block
        text += (
            f"{icon} {html_bold(esc(c_name))}\n"
            f"   └ 📅 {stats['present']}/{stats['total']}  |  📊 {pct:.0f}% ({state})\n\n"
        )
        
    keyboard = [[InlineKeyboardButton("🔙 Back to List", callback_data=f"admin_att_students_{dept_id}_page_1")]]
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# -------------------------------------------------------------------------
#  Search Student Stats (Reuse Logic)
# -------------------------------------------------------------------------

async def search_att_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"🔍 {html_bold('Search Student Attendance')}\n\nEnter Student Name:", reply_markup=get_back_button())
    return SEARCH_ATT_STUDENT

async def perform_att_search(update: Update, context: ContextTypes.DEFAULT_TYPE):
    q = update.message.text.lower()
    api = APIClient(update.effective_user.id)
    
    all_students = api.get("/api/student?Page=1&PageSize=100")
    students = all_students if isinstance(all_students, list) else all_students.get("data", [])
    
    match = next((s for s in students if q in (s.get('firstName', '') + ' ' + s.get('lastName', '')).lower()), None)
    
    if not match:
        await update.message.reply_text("❌ Student not found.", reply_markup=get_back_button())
        return ConversationHandler.END
        
    sid = match.get('studentId')
    name = f"{match.get('firstName')} {match.get('lastName')}"
    
    # Fetch Attendance
    records = api.get(f"/api/attendance/student/{sid}")
    if isinstance(records, dict): records = records.get("data", [])
    
    if not records:
        await update.message.reply_text(f"📉 {html_bold(esc(name))}\n\nNo attendance records found.", reply_markup=get_back_button(), parse_mode="HTML")
        return ConversationHandler.END

    total_present = 0
    total_lectures = 0
    course_stats = {}
    
    for r in records:
        cid = r.get('courseId')
        if cid not in course_stats: course_stats[cid] = {'present': 0, 'total': 0}
        course_stats[cid]['total'] += 1
        total_lectures += 1
        if r.get('isPresent'):
            course_stats[cid]['present'] += 1
            total_present += 1
            
    overall_pct = (total_present / total_lectures * 100) if total_lectures > 0 else 0
    overall_bar = get_progress_bar(overall_pct, total=100, length=10)
    
    text = (
        f"👤 {html_bold(esc(name))}\n"
        f"🆔 Student ID: {html_code(sid)}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n\n"
        f"📊 {html_bold('Overall Attendance')}\n"
        f"{html_code(overall_bar)} {html_bold(f'{overall_pct:.1f}%')}\n"
        f"🔹 Present: {html_bold(total_present)} / {total_lectures} Lectures\n\n"
        f"📚 {html_bold('Subject Breakdown')}\n"
        f"────────────────────\n"
    )
    
    for cid, stats in course_stats.items():
        pct = (stats['present'] / stats['total']) * 100
        
        if pct >= 75:
            icon = "🟢"
            state = "Excellent"
        elif pct >= 60:
            icon = "🟡"
            state = "Fair"
        else:
            icon = "🔴"
            state = "Low"
        
        c_resp = api.get(f"/api/course/{cid}")
        c_name = c_resp.get('courseName') if c_resp else f"Subject #{cid}"
        
        text += (
            f"{icon} {html_bold(esc(c_name))}\n"
            f"   └ 📅 {stats['present']}/{stats['total']}  |  📊 {pct:.0f}% ({state})\n\n"
        )
        
    keyboard = [[InlineKeyboardButton("🔙 Dashboard", callback_data="admin_attendance")]]
    await update.message.reply_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    return ConversationHandler.END

search_att_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(search_att_start, pattern='^search_att_start$')],
    states={SEARCH_ATT_STUDENT: [MessageHandler(filters.TEXT, perform_att_search)]},
    fallbacks=[CallbackQueryHandler(view_attendance_dashboard, pattern='^admin_attendance$')]
)
