from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from handlers.menu import get_pagination_keyboard, get_back_button
from services.session import session_manager
from utils.formatting import get_role_badge, html_bold, html_code, html_italic, html_expandable_quote, esc
import logging

# States
C_ADD_NAME, C_ADD_CODE, C_ADD_CREDITS, C_ADD_SEM, C_ADD_DEPT = range(5)
SEARCH_QUERY = range(1)
EDIT_DESC, EDIT_CREDITS = range(2)

# -------------------------------------------------------------------------
#  Department Selection & Course List
# -------------------------------------------------------------------------

async def list_departments(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Step 1: Show list of Departments to filter courses.
    """
    query = update.callback_query
    await query.answer()
    
    api = APIClient(update.effective_user.id)
    
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
        row.append(InlineKeyboardButton(f"📚 {name}", callback_data=f"admin_courses_dept_{did}_page_1"))
        
        if len(row) == 2:
            keyboard.append(row)
            row = []
    
    if row:
        keyboard.append(row)

    # All Courses Option
    keyboard.append([InlineKeyboardButton("🌐 All Courses", callback_data="admin_courses_dept_0_page_1")])
    
    # Search & Add
    keyboard.append([InlineKeyboardButton("🔍 Search Course", callback_data="search_course_start")])
    keyboard.append([InlineKeyboardButton("➕ Add New Course", callback_data="add_course_start")])
    keyboard.append([InlineKeyboardButton("🔙 Back to Menu", callback_data="main_menu")])

    msg = (
        f"🏫 {html_bold('Course Management')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Select a department to view courses:"
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


async def list_courses(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Step 2: List Courses for a specific Department.
    Pattern: admin_courses_dept_{did}_page_{page}
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
    
    # Fetch Courses (Filtered or All)
    endpoint = f"/api/course?Page={page}&PageSize=10"
    if dept_id > 0:
        endpoint = f"/api/course/department/{dept_id}" # Note: This might not support pagination in backend, we handle manual paging if list returned
    
    response = api.get(endpoint)
    
    if not response or (isinstance(response, dict) and "error" in response):
        err = response.get('error') if response else 'Unknown'
        await query.edit_message_text(f"❌ Error: {err}", reply_markup=get_back_button())
        return

    courses = []
    total_pages = 1
    
    if isinstance(response, list):
        # Full list returned (e.g. from /department/{id}), manual pagination needed
        all_courses = response
        total_items = len(all_courses)
        items_per_page = 10
        total_pages = (total_items + items_per_page - 1) // items_per_page
        
        start = (page - 1) * items_per_page
        end = start + items_per_page
        courses = all_courses[start:end]
        
    elif isinstance(response, dict):
        # Paged response
        courses = response.get("data") or response.get("items") or response.get("value") or []
        total_pages = response.get("totalPages", 1)

    if not courses:
        keyboard = [[InlineKeyboardButton("🔙 Back to Departments", callback_data="admin_courses")]]
        await query.edit_message_text("📚 No Courses found in this department.", reply_markup=InlineKeyboardMarkup(keyboard))
        return

    text = f"📚 {html_bold('Course Catalog')} (Page {page}/{total_pages})\n━━━━━━━━━━━━━━━━━━━━\n\n"
    keyboard = []
    
    for c in courses:
        name = esc(c.get("courseName") or c.get("name"))
        code = esc(c.get("courseCode") or c.get("code") or "N/A")
        cid = c.get("courseId") or c.get("id")
        credits = c.get("credits", 0)
        sem = c.get("semester", "?")
        
        text += f"📖 {html_bold(name)}\n   └ {html_code(code)} | Sem {sem} | ⭐️ {credits} Cr\n\n"
        
        keyboard.append([
            InlineKeyboardButton(f"👁️ View {code}", callback_data=f"view_course_{cid}")
        ])

    # Pagination
    nav_buttons = []
    if page > 1:
        nav_buttons.append(InlineKeyboardButton("⬅️ Prev", callback_data=f"admin_courses_dept_{dept_id}_page_{page - 1}"))
    if page < total_pages:
        nav_buttons.append(InlineKeyboardButton("Next ➡️", callback_data=f"admin_courses_dept_{dept_id}_page_{page + 1}"))
    if nav_buttons: keyboard.append(nav_buttons)
        
    keyboard.append([InlineKeyboardButton("🔙 Switch Department", callback_data="admin_courses")])
    keyboard.append([InlineKeyboardButton("🔙 Main Menu", callback_data="main_menu")])

    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

# -------------------------------------------------------------------------
#  View Course Details
# -------------------------------------------------------------------------

async def view_course(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    cid = query.data.split("_")[2]
    api = APIClient(update.effective_user.id)
    course = api.get(f"/api/course/{cid}")
    
    if not course or "error" in course:
        await query.edit_message_text("❌ Course not found.", reply_markup=get_back_button())
        return

    # Details
    name = esc(course.get("courseName") or course.get("name"))
    code = esc(course.get("courseCode") or course.get("code"))
    desc = esc(course.get("description", "No description available."))
    credits = course.get("credits", 0)
    sem = course.get("semester", "N/A")
    dept_id = course.get("departmentId")
    
    # Fetch Dept Name if possible (optimization: cache departments)
    dept_name = "Unknown Dept"
    if dept_id:
        d = api.get(f"/api/department/{dept_id}")
        if d and "name" in d: dept_name = esc(d["name"])

    # Fetch Instructors (via TimeSlots)
    instructors_text = ""
    try:
        slots = api.get(f"/api/timeslot/course/{cid}")
        unique_tids = set()
        if slots and isinstance(slots, list):
            for s in slots:
                if s.get("teacherId"): unique_tids.add(s.get("teacherId"))
        
        if unique_tids:
            instructors_text = f"\n👨‍🏫 {html_bold('Instructors:')}\n"
            for tid in unique_tids:
                t = api.get(f"/api/teacher/{tid}")
                if t and "firstName" in t:
                    tname = esc(f"{t.get('firstName')} {t.get('lastName')}".strip())
                    instructors_text += f"   • {tname}\n"
        else:
            instructors_text = f"\n👨‍🏫 {html_bold('Instructors:')} {html_italic('None assigned')}\n"
    except:
        pass

    info = (
        f"📘 {html_bold('COURSE DETAILS')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n\n"
        f"📖 {html_bold(name)}\n"
        f"🔖 Code: {html_code(code)}\n\n"
        f"🏛 {html_bold('Department:')} {dept_name}\n"
        f"📅 {html_bold('Semester:')} {sem}\n"
        f"⭐️ {html_bold('Credits:')} {credits}\n"
        f"{instructors_text}"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"📝 {html_bold('Description:')}\n{html_expandable_quote(desc)}\n"
        f"━━━━━━━━━━━━━━━━━━━━"
    )
    
    keyboard = [
        [InlineKeyboardButton("✏️ Edit Description", callback_data=f"edit_course_desc_{cid}"),
         InlineKeyboardButton("⭐️ Edit Credits", callback_data=f"edit_course_cred_{cid}")],
        [InlineKeyboardButton("🗑️ Delete Course", callback_data=f"delete_course_{cid}")],
        [InlineKeyboardButton("🔙 Back to List", callback_data="admin_courses")]
    ]
    
    await query.edit_message_text(info, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def delete_course_confirm(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    cid = query.data.split("_")[2]
    
    keyboard = [
        [InlineKeyboardButton("🗑️ Yes, Delete Forever", callback_data=f"confirm_del_course_{cid}")],
        [InlineKeyboardButton("❌ Cancel", callback_data=f"view_course_{cid}")]
    ]
    
    await query.edit_message_text(
        f"⚠️ {html_bold('Delete Course?')}\n\n"
        f"Are you sure you want to delete this course?\n"
        f"This action {html_bold('cannot')} be undone.",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def delete_course(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    cid = query.data.split("_")[3] # confirm_del_course_{id}
    
    api = APIClient(update.effective_user.id)
    resp = api.delete(f"/api/course/{cid}")
    
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await query.answer("Course Deleted!", show_alert=True)
        # Reset to page 1 to allow refresh
        query.data = "admin_courses" 
        await list_departments(update, context)
    else:
        err = resp.get("error") if isinstance(resp, dict) else "Unknown"
        await query.answer(f"Failed: {err}", show_alert=True)
        await list_departments(update, context)

# -------------------------------------------------------------------------
#  Search Course
# -------------------------------------------------------------------------

async def search_course_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(
        f"🔍 {html_bold('Search Course')}\n\nEnter Course Name or Code:",
        reply_markup=get_back_button()
    )
    return SEARCH_QUERY

async def perform_search(update: Update, context: ContextTypes.DEFAULT_TYPE):
    q = update.message.text
    api = APIClient(update.effective_user.id)
    
    # Filter locally or usage search endpoint if available. 
    # CourseController doesn't seem to have SearchQuery in GetAll, but let's try GetAll and filter in python (good for small sets)
    # OR if GET /api/course supports ?search=... checking.. usually it does or we use Filter
    
    courses_resp = api.get("/api/course")
    all_courses = []
    if isinstance(courses_resp, list): all_courses = courses_resp
    elif isinstance(courses_resp, dict): all_courses = courses_resp.get("data") or courses_resp.get("value") or []
    
    matches = []
    q_lower = q.lower()
    for c in all_courses:
        name = (c.get("courseName") or c.get("name") or "").lower()
        code = (c.get("courseCode") or c.get("code") or "").lower()
        if q_lower in name or q_lower in code:
            matches.append(c)
            
    if not matches:
        await update.message.reply_text(f"❌ No courses found for '{q}'", reply_markup=get_back_button())
        return ConversationHandler.END
        
    text = f"🔍 {html_bold(f'Results for \"{q}\"')}\n━━━━━━━━━━━━━━━━━━━━\n\n"
    keyboard = []
    
    # Limit to top 5
    for c in matches[:5]:
        name = esc(c.get("courseName") or c.get("name"))
        code = esc(c.get("courseCode") or c.get("code"))
        cid = c.get("courseId") or c.get("id")
        
        text += f"📖 {html_code(code)} - {name}\n"
        keyboard.append([InlineKeyboardButton(f"👁️ View {code}", callback_data=f"view_course_{cid}")])
        
    keyboard.append([InlineKeyboardButton("🔙 Back to List", callback_data="admin_courses")])
    await update.message.reply_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    return ConversationHandler.END

search_course_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(search_course_start, pattern='^search_course_start$')],
    states={SEARCH_QUERY: [MessageHandler(filters.TEXT, perform_search)]},
    fallbacks=[CallbackQueryHandler(list_departments, pattern='^admin_courses')]
)

# -------------------------------------------------------------------------
#  Add Course
# -------------------------------------------------------------------------

async def add_course_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"➕ {html_bold('Add Course')}\n\nEnter {html_bold('Course Name')}:", reply_markup=get_back_button())
    return C_ADD_NAME

async def c_receive_name(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_course_name'] = update.message.text
    await update.message.reply_text(f"Enter {html_bold('Course Code')} (e.g. CS101):", parse_mode="HTML")
    return C_ADD_CODE

async def c_receive_code(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_course_code'] = update.message.text
    await update.message.reply_text(f"Enter {html_bold('Credits')} (1-5):", parse_mode="HTML")
    return C_ADD_CREDITS

async def c_receive_credits(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_course_credits'] = update.message.text
    await update.message.reply_text(f"Enter {html_bold('Semester')} (1-8):", parse_mode="HTML")
    return C_ADD_SEM

async def c_receive_sem(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_course_sem'] = update.message.text
    
    # List departments for selection? Too complex for text input. Ask ID.
    # Ideally showed buttons in previous step. Simplified for now.
    await update.message.reply_text(f"Enter {html_bold('Department ID')} (e.g. 1):", parse_mode="HTML")
    return C_ADD_DEPT

async def c_receive_dept(update: Update, context: ContextTypes.DEFAULT_TYPE):
    dept_id = update.message.text
    
    payload = {
        "courseName": context.user_data['new_course_name'],
        "courseCode": context.user_data['new_course_code'],
        "credits": int(context.user_data['new_course_credits']),
        "semester": int(context.user_data['new_course_sem']),
        "departmentId": int(dept_id),
        "description": "Added via Telegram Bot"
    }
    
    api = APIClient(update.effective_user.id)
    resp = api.post("/api/course", payload)
    
    if resp and "error" not in resp:
        await update.message.reply_text("✅ Course Created Successfully!")
    else:
        await update.message.reply_text(f"❌ Failed: {resp.get('error')}")
        
    return ConversationHandler.END

add_course_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(add_course_start, pattern='^add_course_start$')],
    states={
        C_ADD_NAME: [MessageHandler(filters.TEXT, c_receive_name)],
        C_ADD_CODE: [MessageHandler(filters.TEXT, c_receive_code)],
        C_ADD_CREDITS: [MessageHandler(filters.TEXT, c_receive_credits)],
        C_ADD_SEM: [MessageHandler(filters.TEXT, c_receive_sem)],
        C_ADD_DEPT: [MessageHandler(filters.TEXT, c_receive_dept)]
    },
    fallbacks=[CallbackQueryHandler(list_departments, pattern='^admin_courses$')]
)
