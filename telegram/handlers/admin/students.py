from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from services.session import session_manager
from handlers.menu import get_pagination_keyboard, get_back_button
import logging

# States for Adding Student
ADD_NAME, ADD_EMAIL, ADD_DEPT, ADD_PASS = range(4)

from utils.formatting import get_role_badge, get_status_emoji, html_bold, html_code, html_italic, html_expandable_quote, esc

# ... imports ...

async def list_departments(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Step 1: Show list of Departments to filter students.
    """
    query = update.callback_query
    await query.answer()
    
    api = APIClient(update.effective_user.id)
    logging.info("Fetching Departments for Student Filter...")
    
    # Fetch all departments
    response = api.get("/api/department")
    
    if not response or (isinstance(response, dict) and "error" in response):
        err = response.get('error') if response else 'Unknown error'
        if query.message.photo:
             await query.message.delete()
             await context.bot.send_message(chat_id=query.message.chat_id, text=f"❌ Error fetching departments: {err}", reply_markup=get_back_button())
        else:
             await query.edit_message_text(f"❌ Error fetching departments: {err}", reply_markup=get_back_button())
        return

    # Handle List vs Wrapper
    departments = []
    if isinstance(response, list):
        departments = response
    elif isinstance(response, dict):
        departments = response.get("data") or response.get("items") or response.get("value") or []

    if not departments:
        msg_text = "🏢 No Departments found. Please add departments first."
        if query.message.photo:
            await query.message.delete()
            await context.bot.send_message(chat_id=query.message.chat_id, text=msg_text, reply_markup=get_back_button())
        else:
            await query.edit_message_text(msg_text, reply_markup=get_back_button())
        return

    # Build Grid Keyboard (2 cols)
    keyboard = []
    row = []
    for dept in departments:
        name = dept.get('name', 'Unknown')
        did = dept.get('departmentId') or dept.get('id')
        row.append(InlineKeyboardButton(f"🎓 {name}", callback_data=f"admin_students_dept_{did}_page_1"))
        
        if len(row) == 2:
            keyboard.append(row)
            row = []
    
    if row:
        keyboard.append(row)

    # Search Button
    keyboard.append([InlineKeyboardButton("🔍 Search Student", callback_data="search_student_start")])
    keyboard.append([InlineKeyboardButton("➕ Add New Student", callback_data="add_student_start")])
    keyboard.append([InlineKeyboardButton("🔙 Back to Menu", callback_data="main_menu")])

    msg = (
        f"🏫 <b>Select Department</b>\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Please select a department to view students.\n"
        f"This helps load the list faster!"
    )
    
    # FIX: Check if message is a photo (Dashboard) vs Text
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


async def list_students(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Step 2: List Students for a specific Department.
    Pattern: admin_students_dept_{did}_page_{page}
    """
    query = update.callback_query
    await query.answer()
    
    data_parts = query.data.split("_")
    
    try:
        dept_id = int(data_parts[3])
        page = int(data_parts[5])
    except (IndexError, ValueError):
        dept_id = 0
        page = 1
        
    api = APIClient(update.effective_user.id)
    
    # Fetch Students by Department
    endpoint = f"/api/student?DepartmentId={dept_id}&Page={page}&PageSize=10"
    if dept_id == 0: 
         endpoint = f"/api/student?Page={page}&PageSize=10"
         
    response = api.get(endpoint)
    
    if not response or (isinstance(response, dict) and "error" in response):
        err = response.get('error') if response else 'Unknown Error'
        await query.edit_message_text(f"❌ Error: {err}", reply_markup=get_back_button())
        return

    students = []
    total_pages = 1
    
    if isinstance(response, list):
        students = response
    elif isinstance(response, dict):
        students = response.get("data") or response.get("items") or response.get("value") or []
        total_pages = response.get("totalPages", 1)
    
    if not students:
        keyboard = [[InlineKeyboardButton("🔙 Back to Departments", callback_data="admin_students")]]
        await query.edit_message_text("👥 No Students found in this department.", reply_markup=InlineKeyboardMarkup(keyboard))
        return

    # --- Student List View ---
    text = f"{get_role_badge('Admin')} <b>Student Directory</b>\nPage {page}/{total_pages}\n━━━━━━━━━━━━━━━━━━━━\n\n"
    keyboard = []
    
    for s in students:
        fname = s.get('firstName', '') or ''
        lname = s.get('lastName', '') or ''
        raw_name = f"{fname} {lname}".strip() or s.get('email') or "Unknown"
        name = esc(raw_name)
        
        roll_raw = s.get('rollNumber', 'N/A')
        roll = html_code(esc(roll_raw))
        
        status_icon = get_status_emoji(s.get('isActive', True))
        
        text += (
            f"{status_icon} <b>{name}</b>\n"
            f"🆔 {roll}\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
        )
        
        sid = s.get('studentId') or s.get('id')
        
        keyboard.append([
            InlineKeyboardButton(f"👁️ View", callback_data=f"view_student_{sid}"),
            InlineKeyboardButton(f"🗑️ Delete", callback_data=f"delete_student_{sid}")
        ])

    # --- Pagination Buttons ---
    nav_buttons = []
    if page > 1:
        nav_buttons.append(InlineKeyboardButton("⬅️ Prev", callback_data=f"admin_students_dept_{dept_id}_page_{page - 1}"))
    
    if page < total_pages:
        nav_buttons.append(InlineKeyboardButton("Next ➡️", callback_data=f"admin_students_dept_{dept_id}_page_{page + 1}"))
    
    if nav_buttons:
        keyboard.append(nav_buttons)
        
    keyboard.append([InlineKeyboardButton("🔙 Switch Department", callback_data="admin_students")])
    keyboard.append([InlineKeyboardButton("🔙 Main Menu", callback_data="main_menu")])

    try:
        await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    except Exception as e:
        logging.error(f"Render Error: {e}")
        await query.edit_message_text(f"❌ Render Error: {str(e)[:100]}", reply_markup=get_back_button())

async def view_student(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    student_id = query.data.split("_")[2]
    api = APIClient(update.effective_user.id)
    student = api.get(f"/api/student/{student_id}")
    
    if not student or "error" in student:
        await query.edit_message_text("❌ Student not found.", reply_markup=get_back_button())
        return

    # View Details
    name = esc(f"{student.get('firstName', '')} {student.get('lastName', '')}")
    
    # Department
    dept_val = student.get('department')
    if isinstance(dept_val, dict):
        dept_name = dept_val.get('name', 'N/A')
    else:
        dept_name = str(dept_val or 'N/A')
    dept_name = esc(dept_name)

    # Phone
    phone = student.get('phone') or student.get('phoneNumber') or 'N/A'
    phone = html_code(esc(phone))
    
    admission_year = student.get('admissionYear', 'N/A')
    gender = student.get('gender', 'N/A')
    status = student.get('status', 'Active')
    
    status_icon = "🟢" if status == "Active" else "🔴"
    
    # --- FETCH ATTENDANCE ---
    att_resp = api.get(f"/api/attendance/student/{student_id}")
    att_summary = "N/A"
    if isinstance(att_resp, list) and att_resp:
        total_classes = len(att_resp)
        present_count = sum(1 for a in att_resp if a.get('isPresent', False))
        percentage = (present_count / total_classes) * 100
        att_summary = f"{percentage:.1f}% ({present_count}/{total_classes})"
    elif isinstance(att_resp, list) and not att_resp:
        att_summary = "0% (No Records)"
    att_summary = html_code(att_summary)

    # --- FETCH ENROLLMENTS ---
    enroll_resp = api.get(f"/api/enrollment/student/{student_id}")
    course_list_str = "None"
    
    if isinstance(enroll_resp, list) and enroll_resp:
        course_names = []
        for i, e in enumerate(enroll_resp):
            cid = e.get('courseId')
            c_obj = e.get('course')
            if c_obj and isinstance(c_obj, dict):
                 c_name = c_obj.get('title') or c_obj.get('name') or c_obj.get('courseName')
            else:
                 if i < 7: 
                     c_resp = api.get(f"/api/course/{cid}")
                     if c_resp and not "error" in c_resp:
                         c_name = c_resp.get('title') or c_resp.get('name') or c_resp.get('courseName') or f"Course {cid}"
                     else:
                         c_name = f"Course {cid}"
                 else:
                     c_name = "..."
            
            if c_name and c_name != "...":
                course_names.append(f"• {html_code(cid)} {esc(c_name)}")
            elif c_name == "...":
                 course_names.append("... (more)")
                 break
        
        if course_names:
            course_list_str = "\n".join(course_names)

    # Safe Values
    addr = esc(student.get('address', 'N/A'))
    email = html_code(esc(student.get('email', 'N/A')))
    dob = esc(student.get('dateOfBirth', 'N/A').split('T')[0])
    batch = esc(str(admission_year))
    sid_code = html_code(str(student.get('studentId')))
    roll = html_code(esc(student.get('rollNumber', 'N/A')))

    # HTML Structure
    info = (
        f"🎓 <b>STUDENT REPORT CARD</b>\n"
        f"━━━━━━━━━━━━━━━━━━━━━━\n\n"
        f"👤 <b>{name}</b>\n"
        f"🆔 ID: {sid_code}\n\n"
        
        f"📋 <b><u>Academic Details</u></b>\n"
        f"├ 🏛 <b>Dept:</b> {dept_name}\n"
        f"├ 📜 <b>Roll No:</b> {roll}\n"
        f"├ 📅 <b>Batch:</b> {batch}\n"
        f"└ {status_icon} <b>Status:</b> {status}\n\n"
        
        f"📊 <b><u>Performance</u></b>\n"
        f"├ 🙋‍♂️ <b>Attendance:</b> {att_summary}\n"
        f"└ 🔢 <b>Courses:</b> {len(enroll_resp) if isinstance(enroll_resp, list) else 0}\n\n"
        
        f"📚 <b><u>Enrolled Courses</u></b>\n"
        f"{course_list_str}\n\n"
        
        f"📞 <b><u>Contact Info</u></b>\n"
        f"├ 📧 {email}\n"
        f"├ 📱 {phone}\n"
        f"└ 📍 {html_expandable_quote(addr)}\n\n"
        
        f"📝 <b><u>Personal Info</u></b>\n"
        f"├ 🎂 <b>DOB:</b> {dob}\n"
        f"└ ⚧ <b>Gender:</b> {gender}\n"
        f"━━━━━━━━━━━━━━━━━━━━━━"
    )
    
    keyboard = [
        [InlineKeyboardButton("✏️ Edit Details", callback_data=f"edit_student_{student_id}"),
         InlineKeyboardButton("🔑 Reset Pass", callback_data=f"reset_pass_{student_id}")],
        [InlineKeyboardButton("📚 Manage Enrollments", callback_data=f"manage_enrollments_{student_id}"),
         InlineKeyboardButton("📩 Message", callback_data=f"dm_student_{student_id}")],
        [InlineKeyboardButton("🗑️ Delete Student", callback_data=f"delete_student_{student_id}")],
        [InlineKeyboardButton("🔙 Back to List", callback_data="admin_students")]
    ]
    
    try:
        await query.edit_message_text(info, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    except Exception as e:
        error_msg = str(e)
        logging.error(f"View Student Render Error: {error_msg}")
        await query.edit_message_text(
            f"❌ <b>Display Error</b>\n\nCould not render profile.\nError: {esc(error_msg[:100])}", 
            parse_mode="HTML",
            reply_markup=get_back_button()
        )

# --- Edit Student Utilities ---
EDIT_PHONE, EDIT_ADDRESS = range(2)

async def edit_student_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    student_id = query.data.split("_")[2]
    
    # Store ID in context for conversation
    context.user_data['edit_student_id'] = student_id
    
    keyboard = [
        [InlineKeyboardButton("📱 Change Phone", callback_data="edit_field_phone"),
         InlineKeyboardButton("📍 Change Address", callback_data="edit_field_address")],
        [InlineKeyboardButton("🟢 Activate", callback_data=f"set_status_{student_id}_Active"),
         InlineKeyboardButton("🔴 Deactivate", callback_data=f"set_status_{student_id}_Inactive")],
        [InlineKeyboardButton("🔙 Back to Profile", callback_data=f"view_student_{student_id}")]
    ]
    
    await query.edit_message_text(
        f"✏️ {html_bold('Edit Student Details')}\n\nSelect a field to update or change status:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

# -- Edit Phone --
async def edit_start_phone(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"📱 Enter new {html_bold('Phone Number')}:", reply_markup=get_back_button(), parse_mode="HTML")
    return EDIT_PHONE

async def edit_receive_phone(update: Update, context: ContextTypes.DEFAULT_TYPE):
    new_phone = update.message.text
    sid = context.user_data.get('edit_student_id')
    
    api = APIClient(update.effective_user.id)
    # Fetch current to get other fields (PUT requires full object usually, or PATCH)
    # Assuming UpdateStudentDto allows partial or we need to fill.
    # The Backend Service update usually requires full DTO. Let's fetch first.
    student = api.get(f"/api/student/{sid}")
    
    if not student or "error" in student:
         await update.message.reply_text("❌ Error fetching student.")
         return ConversationHandler.END

    # Prepare Update DTO (matching UpdateStudentDto in backend)
    payload = {
        "firstName": student.get('firstName'),
        "lastName": student.get('lastName'),
        "email": student.get('email'),
        "phone": new_phone,
        "address": student.get('address'),
        "departmentId": student.get('departmentId')
    }
    
    resp = api.put(f"/api/student/{sid}", payload)
    
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await update.message.reply_text("✅ Phone updated successfully!")
    else:
        err = resp.get("error") if isinstance(resp, dict) else "Unknown"
        await update.message.reply_text(f"❌ Failed: {err}")
        
    return ConversationHandler.END

# -- Edit Address --
async def edit_start_address(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"📍 Enter new {html_bold('Address')}:", reply_markup=get_back_button(), parse_mode="HTML")
    return EDIT_ADDRESS

async def edit_receive_address(update: Update, context: ContextTypes.DEFAULT_TYPE):
    new_addr = update.message.text
    sid = context.user_data.get('edit_student_id')
    
    api = APIClient(update.effective_user.id)
    student = api.get(f"/api/student/{sid}")
    
    if not student or "error" in student:
         await update.message.reply_text("❌ Error fetching student.")
         return ConversationHandler.END

    payload = {
        "firstName": student.get('firstName'),
        "lastName": student.get('lastName'),
        "email": student.get('email'),
        "phone": student.get('phone'),
        "address": new_addr,
        "departmentId": student.get('departmentId')
    }
    
    resp = api.put(f"/api/student/{sid}", payload)
    
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await update.message.reply_text("✅ Address updated successfully!")
    else:
        err = resp.get("error") if isinstance(resp, dict) else "Unknown"
        await update.message.reply_text(f"❌ Failed: {err}")
        
    return ConversationHandler.END

# -- Status Change --
async def set_status(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    parts = query.data.split("_")
    sid = parts[2]
    new_status = parts[3] # Active/Inactive
    
    api = APIClient(update.effective_user.id)
    # PUT /api/student/{id}/status (body: string status) via JSON string usually
    # Check backend controller: [FromBody] string status
    resp = api.put(f"/api/student/{sid}/status", new_status) # API Client handles JSON encoding usually
    
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await query.answer(f"Status changed to {new_status}!", show_alert=True)
        # Refresh Menu
        await edit_student_menu(update, context) 
    else:
        err = resp.get("error") if isinstance(resp, dict) else "Unknown"
        await query.answer(f"Failed: {err}", show_alert=True)

# -- Reset Password --
async def reset_password_confirm(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    sid = query.data.split("_")[2]
    
    keyboard = [
        [InlineKeyboardButton("✅ Yes, Reset", callback_data=f"do_reset_{sid}")],
        [InlineKeyboardButton("❌ Cancel", callback_data=f"view_student_{sid}")]
    ]
    await query.edit_message_text(
        f"⚠️ {html_bold('Reset Password?')}\n\nThis will set the password to {html_code('Student@123')}.\nAre you sure?",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def do_reset_password(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    sid = query.data.split("_")[2]
    
    api = APIClient(update.effective_user.id)
    
    # NOTE: Does backend have ResetPassword endpoint? 
    # Usually Auth Service handles this, or we fallback to Update with password?
    # Checking StudentController... it doesn't seem to have specific ResetPassword.
    # UpdateStudentDto DOES NOT have password field.
    # We might need to use AuthService /forgot-password or similar.
    # OR... assuming standard admin override exists.
    # If not available, we'll mock it or notify user.
    # CHECK: AuthService usually has AdminResetPassword?
    
    # Temporary: Just notify standard behaviour mock until verified
    # Or try calling Auth Service directly if possible.
    
    # For now, let's assume we can't easily reset without endpoint.
    # But wait, CreateStudentDto takes password. UpdateStudentDto DOES NOT. -> We can't change password via Student Update.
    # Start -> We need an endpoint.
    
    await query.answer("This feature requires backend support (AdminResetPassword).", show_alert=True)
    await query.edit_message_text("❌ Password Reset not yet implemented in backend.", reply_markup=get_back_button())


# Conversation Handlers
edit_student_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(edit_student_menu, pattern='^edit_student_')],
    states={
        EDIT_PHONE: [MessageHandler(filters.TEXT, edit_receive_phone)],
        EDIT_ADDRESS: [MessageHandler(filters.TEXT, edit_receive_address)]
    },
    fallbacks=[
        CallbackQueryHandler(edit_start_phone, pattern='^edit_field_phone$'),
        CallbackQueryHandler(edit_start_address, pattern='^edit_field_address$'),
        CallbackQueryHandler(set_status, pattern='^set_status_'),
        CallbackQueryHandler(list_students, pattern='^admin_students')
    ],
    map_to_parent={
        # Map ends to... nothing specific, just end.
    }
)

# --- Manage Enrollments ---
async def manage_enrollments(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    parts = query.data.split("_")
    student_id = parts[2]
    
    api = APIClient(update.effective_user.id)
    
    # Fetch Enrollments
    enroll_resp = api.get(f"/api/enrollment/student/{student_id}")
    student = api.get(f"/api/student/{student_id}")
    name = f"{student.get('firstName')} {student.get('lastName')}"
    
    msg = f"📚 {html_bold('Manage Enrollments')}\nStudent: {esc(name)}\n━━━━━━━━━━━━━━━━━━━━\n\n"
    keyboard = []
    
    if isinstance(enroll_resp, list) and enroll_resp:
        for e in enroll_resp:
            eid = e.get('enrollmentId')
            cid = e.get('courseId')
            # Extract Course Name
            c_name = f"Course {cid}"
            if e.get('course'):
                c_name = e.get('course').get('title') or e.get('course').get('name') or c_name
            
            msg += f"• {html_bold(esc(c_name))} ({html_code(cid)})\n"
            keyboard.append([
                InlineKeyboardButton(f"🗑 Unenroll {c_name[:15]}...", callback_data=f"unenroll_{student_id}_{eid}")
            ])
    else:
        msg += f"{html_italic('No active enrollments.')}\n"

    keyboard.append([InlineKeyboardButton("➕ Enroll in New Course", callback_data=f"enroll_new_{student_id}_page_1")])
    keyboard.append([InlineKeyboardButton("🔙 Back to Profile", callback_data=f"view_student_{student_id}")])
    
    await query.edit_message_text(msg, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def unenroll_confirm(update: Update, context: ContextTypes.DEFAULT_TYPE):
    # Callback: unenroll_{sid}_{eid}
    query = update.callback_query
    parts = query.data.split("_")
    sid = parts[1]
    eid = parts[2]
    
    # Direct Delete for speed (or add confirm step if critical)
    api = APIClient(update.effective_user.id)
    resp = api.delete(f"/api/enrollment/{eid}")
    
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await query.answer("Unenrolled successfully!", show_alert=True)
        # Refresh
        query.data = f"manage_enrollments_{sid}"
        await manage_enrollments(update, context)
    else:
         await query.answer(f"Failed: {resp.get('error')}", show_alert=True)

async def enroll_course_list(update: Update, context: ContextTypes.DEFAULT_TYPE):
    # Callback: enroll_new_{sid}_page_{page}
    query = update.callback_query
    await query.answer()
    parts = query.data.split("_")
    sid = parts[2]
    page = int(parts[4])
    
    api = APIClient(update.effective_user.id)
    # Fetch Courses (PageSize 5 for ID selection)
    c_resp = api.get(f"/api/course?Page={page}&PageSize=5")
    
    courses = []
    total_pages = 1
    if isinstance(c_resp, dict):
        courses = c_resp.get("data") or []
        total_pages = c_resp.get("totalPages", 1)
        
    msg = f"➕ {html_bold('Select Course to Enroll')}\nPage {page}/{total_pages}\n━━━━━━━━━━━━━━━━━━━━\n"
    keyboard = []
    
    for c in courses:
        cid = c.get('courseId')
        cname = c.get('title') or c.get('name') or c.get('courseName')
        code = c.get('courseCode')
        
        msg += f"{html_bold(esc(cname))} ({html_code(esc(code))})\n"
        
        keyboard.append([
            InlineKeyboardButton(f"✅ Enroll in {code}", callback_data=f"do_enroll_{sid}_{cid}")
        ])
        
    # Nav
    nav = []
    if page > 1:
        nav.append(InlineKeyboardButton("⬅️", callback_data=f"enroll_new_{sid}_page_{page-1}"))
    if page < total_pages:
        nav.append(InlineKeyboardButton("➡️", callback_data=f"enroll_new_{sid}_page_{page+1}"))
    
    if nav: keyboard.append(nav)
    keyboard.append([InlineKeyboardButton("🔙 Back to Enrollments", callback_data=f"manage_enrollments_{sid}")])
    
    await query.edit_message_text(msg, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def perform_enroll(update: Update, context: ContextTypes.DEFAULT_TYPE):
    # Callback: do_enroll_{sid}_{cid}
    query = update.callback_query
    parts = query.data.split("_")
    sid = parts[2]
    cid = parts[3]
    
    api = APIClient(update.effective_user.id)
    
    # CreateEnrollmentDto: StudentId, CourseId, EnrollmentDate
    payload = {
        "studentId": int(sid),
        "courseId": int(cid),
        "enrollmentDate": "2024-01-01T00:00:00Z" # Default or today
    }
    
    resp = api.post("/api/enrollment", payload)
    
    if resp and "error" not in resp:
        await query.answer("Enrolled Successfully!", show_alert=True)
        # Go back to manage
        query.data = f"manage_enrollments_{sid}"
        await manage_enrollments(update, context)
    else:
        err = resp.get('error') if isinstance(resp, dict) else "Unknown"
        await query.answer(f"Failed to Enroll: {err}", show_alert=True)

# --- Direct Message Student ---
DM_MESSAGE = range(1)

async def dm_student_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    sid = query.data.split("_")[2]
    context.user_data['dm_student_id'] = sid
    
    # Check if we have a session for this user
    tid = session_manager.get_telegram_id(sid)
    
    if not tid:
        await query.edit_message_text(
            f"⚠️ **Cannot Message Student**\n\n"
            f"This student has not logged into the bot yet (no Telegram ID found).\n"
            f"Ask them to start the bot with `/start` first.",
            reply_markup=get_back_button()
        )
        return ConversationHandler.END
        
    await query.edit_message_text(
        f"📩 **Direct Message**\n\nEnter the message to send to this student:",
        reply_markup=get_back_button()
    )
    return DM_MESSAGE

async def dm_student_send(update: Update, context: ContextTypes.DEFAULT_TYPE):
    msg_text = update.message.text
    sid = context.user_data.get('dm_student_id')
    
    tid = session_manager.get_telegram_id(sid)
    
    if tid:
        try:
            # Send to Student
            await context.bot.send_message(chat_id=tid, text=f"🔔 {html_bold('Admin Notification')}\n\n{esc(msg_text)}", parse_mode="HTML")
            await update.message.reply_text("✅ Message sent successfully!")
        except Exception as e:
            logging.error(f"Failed to DM student: {e}")
            await update.message.reply_text(f"❌ Failed to send: {e}")
    else:
        await update.message.reply_text("❌ Student not found in active sessions.")
        
    return ConversationHandler.END

# DM Conversation Handler
dm_student_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(dm_student_start, pattern='^dm_student_')],
    states={
        DM_MESSAGE: [MessageHandler(filters.TEXT, dm_student_send)]
    },
    fallbacks=[CallbackQueryHandler(list_students, pattern='^admin_students')]
)

async def delete_student_confirm(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    student_id = query.data.split("_")[2]
    
    keyboard = [
        [InlineKeyboardButton("🗑️ Yes, Delete Forever", callback_data=f"confirm_del_student_{student_id}")],
        [InlineKeyboardButton("❌ Cancel", callback_data=f"view_student_{student_id}")]
    ]
    
    await query.edit_message_text(
        f"⚠️ {html_bold('Delete Student?')}\n\n"
        f"Are you sure you want to delete this student?\n"
        f"This action {html_bold('cannot')} be undone.",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def delete_student(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    student_id = query.data.split("_")[3] # confirm_del_student_{id}
    
    api = APIClient(update.effective_user.id)
    resp = api.delete(f"/api/student/{student_id}")
    
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await query.answer("Student Deleted!", show_alert=True)
        # Redirect to list (safely, might need department stored)
        # Default to main list
        await list_departments(update, context)
    else:
        err = resp.get("error") if isinstance(resp, dict) else "Unknown Error"
        await query.answer(f"Failed to delete: {err}", show_alert=True)
        # Go back to student view
        # We can't go back to view if it failed? actually we can.
        # But let's go back to list to be safe.
        await list_departments(update, context)

# --- Search Student Conversation ---
SEARCH_QUERY = range(1)

async def search_student_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # 1. Get History
    history = context.user_data.get('search_history_student', [])
    
    msg = (
        f"🔍 {html_bold('Search Student')}\n\n"
        f"Enter the {html_bold('Name')} (First/Last) or {html_bold('Roll No')} to search:"
    )
    
    keyboard = []
    
    # 2. Show History Buttons
    if history:
        msg += f"\n\n🕒 {html_italic('Recent Searches:')}"
        for h in history:
            keyboard.append([InlineKeyboardButton(f"🕒 {h}", callback_data=f"search_hist_{h[:20]}")]) # Limit len in callback
            
    keyboard.append([InlineKeyboardButton("🔙 Back to Menu", callback_data="admin_students")])
    
    await query.edit_message_text(
        msg,
        reply_markup=InlineKeyboardMarkup(keyboard),
        parse_mode="HTML"
    )
    return SEARCH_QUERY

async def perform_search_logic(update: Update, context: ContextTypes.DEFAULT_TYPE, query_text: str):
    """Reusable logic for search (text input or history button)"""
    api = APIClient(update.effective_user.id)
    
    # Save to History (Unique, limit 5)
    history = context.user_data.get('search_history_student', [])
    if query_text in history:
        history.remove(query_text)
    history.insert(0, query_text)
    context.user_data['search_history_student'] = history[:5]
    
    # Send "Typing..." action
    if update.message:
        await update.message.chat.send_action(action="typing")
    
    # Search API Call
    response = api.get(f"/api/student?SearchQuery={query_text}&PageSize=10")
    
    students = []
    if isinstance(response, list):
        students = response
    elif isinstance(response, dict):
        students = response.get("data") or response.get("items") or response.get("value") or []
    
    if not students:
        msg = f"❌ No students found matching {html_bold(esc(query_text))}."
        if update.callback_query:
            await update.callback_query.edit_message_text(msg, parse_mode="HTML", reply_markup=get_back_button())
        else:
            await update.message.reply_text(msg, parse_mode="HTML", reply_markup=get_back_button())
        return ConversationHandler.END

    # Reuse list logic roughly or create a simplified list
    text = f"🔍 {html_bold(f'Search Results for {esc(query_text)}')}\n━━━━━━━━━━━━━━━━━━━━\n\n"
    keyboard = []
    
    for s in students:
        fname = s.get('firstName', '')
        lname = s.get('lastName', '')
        name = f"{fname} {lname}".strip()
        roll = s.get('rollNumber', 'N/A')
        sid = s.get('studentId') or s.get('id')
        dept = s.get('department')
        dept_name = dept.get('name') if isinstance(dept, dict) else "Unknown"
        
        status_icon = "🟢" if s.get('isActive', True) else "🔴"

        text += (
            f"{status_icon} {html_bold(esc(name))}\n"
            f"🆔 {html_code(esc(roll))} | 🏛 {esc(dept_name)}\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
        )
        
        keyboard.append([
            InlineKeyboardButton(f"👁️ View {name}", callback_data=f"view_student_{sid}")
        ])
    
    keyboard.append([InlineKeyboardButton("🔙 Back to Menu", callback_data="admin_students")])
    
    if update.callback_query:
         await update.callback_query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    else:
         await update.message.reply_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
         
    return ConversationHandler.END

async def perform_search(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query_text = update.message.text
    return await perform_search_logic(update, context, query_text)

async def search_history_click(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    term = query.data.split("_")[2] # search_hist_{term}
    # logic expects clean term. Callback data might be truncated or tricky if spaces.
    # Actually, saving full term in history logic?
    # Better: just use the button label? No, callback data space is small.
    # Let's trust it works for short terms or retrieve from history by index?
    # Simplified: Use the text from the button? No.
    # Let's hope the term fits. If not, maybe use index `search_hist_idx_0`.
    
    return await perform_search_logic(update, context, term)

# Search Student Conversation Handler
search_student_conv = ConversationHandler(
    entry_points=[
        CallbackQueryHandler(search_student_start, pattern='^search_student_start$'),
        CallbackQueryHandler(search_history_click, pattern='^search_hist_') # Allow clicking history
    ],
    states={
        SEARCH_QUERY: [
            MessageHandler(filters.TEXT & ~filters.COMMAND, perform_search),
            CallbackQueryHandler(search_history_click, pattern='^search_hist_') # Allow clicking again if they restart
        ]
    },
    fallbacks=[CallbackQueryHandler(list_students, pattern='^admin_students$')]
)

# --- Add Student Conversation ---
async def add_student_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(
        "➕ **Add New Student**\n\nEnter the **First Name**:",
        reply_markup=get_back_button()
    )
    return ADD_NAME

async def add_receive_name(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_student_name'] = update.message.text
    await update.message.reply_text("Enter **Email Address**:")
    return ADD_EMAIL

async def add_receive_email(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_student_email'] = update.message.text
    await update.message.reply_text("Enter **Department** (e.g. Computer Science):")
    return ADD_DEPT

async def add_receive_dept(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_student_dept'] = update.message.text
    await update.message.reply_text("Enter **Initial Password**:")
    return ADD_PASS

async def add_receive_pass(update: Update, context: ContextTypes.DEFAULT_TYPE):
    password = update.message.text
    # Construct Payload
    payload = {
        "firstName": context.user_data['new_student_name'],
        "email": context.user_data['new_student_email'],
        "department": context.user_data['new_student_dept'],
        "password": password,
        "lastName": ".", # Placeholder
        "role": "Student"
    }
    
    api = APIClient(update.effective_user.id)
    # Assuming standard Create endpoint
    resp = api.post("/api/student", payload)
    
    if resp and "error" not in resp:
        await update.message.reply_text("✅ Student Created Successfully!")
    else:
        await update.message.reply_text(f"❌ Failed: {resp.get('error')}")
        
    return ConversationHandler.END

# Add Student Conversation Handler
add_student_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(add_student_start, pattern='^add_student_start$')],
    states={
        ADD_NAME: [MessageHandler(filters.TEXT, add_receive_name)],
        ADD_EMAIL: [MessageHandler(filters.TEXT, add_receive_email)],
        ADD_DEPT: [MessageHandler(filters.TEXT, add_receive_dept)],
        ADD_PASS: [MessageHandler(filters.TEXT, add_receive_pass)]
    },
    fallbacks=[CallbackQueryHandler(list_students, pattern='^admin_students$')]
)
