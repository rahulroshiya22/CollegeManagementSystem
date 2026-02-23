from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, CallbackQueryHandler
from services.api import APIClient
from handlers.menu import get_back_button
from utils.formatting import html_bold, html_code, html_italic, html_expandable_quote, esc
from datetime import datetime

# -------------------------------------------------------------------------
#  Main Exam Dashboard
# -------------------------------------------------------------------------

async def list_exams(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Shows Exam Dashboard with Stats and Options.
    """
    query = update.callback_query
    await query.answer()

    api = APIClient(update.effective_user.id)
    # Fetch All Exams for Stats
    response = api.get("/api/exam")
    exams = response if isinstance(response, list) else []
    
    # Calculate Stats
    total = len(exams)
    now = datetime.now()
    upcoming = sum(1 for e in exams if datetime.fromisoformat(e.get('scheduledDate', str(now))).date() >= now.date())
    past = total - upcoming
    published = sum(1 for e in exams if e.get('isPublished'))

    text = (
        f"📝 {html_bold('Exam Management')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Manage schedules, results, and view exam details.\n\n"
        f"📊 {html_bold('Overview')}\n"
        f"🔹 Total Exams: {html_bold(total)}\n"
        f"🗓️ Upcoming: {html_bold(upcoming)}\n"
        f"🏁 Completed: {html_bold(past)}\n"
        f"📢 Published: {html_bold(published)}\n\n"
        f"👇 {html_bold('Select an option:')}"
    )
    
    keyboard = [
        [InlineKeyboardButton("🏢 Filter by Department", callback_data="admin_exam_depts")],
        [InlineKeyboardButton("🗓️ Upcoming Exams", callback_data="admin_exams_upcoming")],
        [InlineKeyboardButton("🏁 Past Exams", callback_data="admin_exams_past")],
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
#  Filter Flow
# -------------------------------------------------------------------------

async def list_exam_departments(update: Update, context: ContextTypes.DEFAULT_TYPE):
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
        if not did: continue
        row.append(InlineKeyboardButton(f"{name}", callback_data=f"admin_exam_dept_{did}"))
        if len(row) == 2:
            keyboard.append(row)
            row = []
    if row: keyboard.append(row)
    
    keyboard.append([InlineKeyboardButton("🔙 Back", callback_data="admin_exams")])
    await query.edit_message_text(f"🏢 {html_bold('Select Department:')}", parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def list_exam_courses(update: Update, context: ContextTypes.DEFAULT_TYPE):
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
        keyboard.append([InlineKeyboardButton(f"{code}", callback_data=f"admin_exam_list_{cid}")])
        
    keyboard.append([InlineKeyboardButton("🔙 Back", callback_data="admin_exam_depts")])
    await query.edit_message_text(f"📚 {html_bold('Select Course to View Exams:')}", parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# -------------------------------------------------------------------------
#  Exam List
# -------------------------------------------------------------------------

async def list_course_exams(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    cid = query.data.split("_")[3]
    api = APIClient(update.effective_user.id)
    
    # Fetch Exams for Course
    response = api.get(f"/api/exam?courseId={cid}")
    exams = response if isinstance(response, list) else []

    # Fetch Course Name (Optional)
    c_resp = api.get(f"/api/course/{cid}")
    c_name = c_resp.get('courseName') if c_resp else f"Course #{cid}"
    
    if not exams:
        keyboard = [[InlineKeyboardButton("🔙 Back", callback_data="admin_exam_depts")]]
        await query.edit_message_text(f"🚫 No exams found for {html_bold(esc(c_name))}.", parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
        return

    text = f"📝 {html_bold(f'Exams for {esc(c_name)}')}\nSelect an exam to view details:\n"
    keyboard = []
    
    for exam in exams:
        eid = exam.get('examId')
        title = exam.get('title')
        date = exam.get('scheduledDate', 'TBD').split('T')[0]
        
        keyboard.append([InlineKeyboardButton(f"📅 {date} - {title}", callback_data=f"view_exam_{eid}")])
        
    keyboard.append([InlineKeyboardButton("🔙 Back", callback_data="admin_exam_depts")])
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# -------------------------------------------------------------------------
#  Exam Detail
# -------------------------------------------------------------------------

async def view_exam_detail(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    eid = query.data.split("_")[2]
    api = APIClient(update.effective_user.id)
    
    exam = api.get(f"/api/exam/{eid}")
    if not exam or "error" in exam:
        await query.edit_message_text("❌ Exam not found.", reply_markup=get_back_button())
        return

    # Fetch Course Name
    cid = exam.get('courseId')
    c_resp = api.get(f"/api/course/{cid}")
    c_name = c_resp.get('courseName') if c_resp else f"Course #{cid}"
    
    title = exam.get('title')
    desc = exam.get('description', 'No description')
    date = exam.get('scheduledDate', 'TBD').replace('T', ' ')[:16] # YYYY-MM-DD HH:MM
    duration = exam.get('duration', '00:00:00')
    marks = exam.get('totalMarks')
    pass_marks = exam.get('passingMarks')
    is_pub = exam.get('isPublished')
    status_icon = "🟢 Published" if is_pub else "🔴 Draft/Hidden"
    
    text = (
        f"📝 {html_bold('Exam Details')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"📘 {html_bold('Course:')} {esc(c_name)}\n"
        f"📌 {html_bold('Title:')} {esc(title)}\n"
        f"📅 {html_bold('Date:')} {esc(date)}\n"
        f"⏳ {html_bold('Duration:')} {esc(duration)}\n\n"
        f"🔢 {html_bold('Marks:')} {pass_marks}/{marks}\n"
        f"📢 {html_bold('Status:')} {status_icon}\n"
        f"📝 {html_bold('Description:')}\n{html_expandable_quote(esc(desc))}\n"
    )
    
    keyboard = []
    if not is_pub:
        keyboard.append([InlineKeyboardButton("🚀 Publish Results", callback_data=f"pub_exam_{eid}")])
    else:
        keyboard.append([InlineKeyboardButton("📊 View Results", callback_data=f"view_exam_results_{eid}")])
        
    keyboard.append([InlineKeyboardButton("🔙 Back to List", callback_data=f"admin_exam_list_{cid}")])
    
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def publish_exam_results(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    eid = query.data.split("_")[2]
    api = APIClient(update.effective_user.id)
    
    # Determine the status and method first
    # Checking endpoints... ExamController has [HttpPost("{id}/publish")]
    
    resp = api.post(f"/api/exam/{eid}/publish", {})
    
    if resp and "error" not in resp:
        await query.answer("✅ Exam Results Published!", show_alert=True)
        # Refresh View
        query.data = f"view_exam_{eid}"
        await view_exam_detail(update, context)
    else:
        err = resp.get("error") if isinstance(resp, dict) else "Unknown"
        await query.answer(f"Failed: {err}", show_alert=True)
