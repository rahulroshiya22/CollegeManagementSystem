from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api_client import api_client
from utils.keyboards import back_button_keyboard, student_action_keyboard, pagination_keyboard, main_menu_keyboard, student_academics_keyboard, student_finance_keyboard
from utils.helpers import get_course_name, edit_response

# States for Add/Edit Student
ST_FIRSTNAME, ST_LASTNAME, ST_EMAIL, ST_PHONE, ST_DOB, ST_GENDER, ST_ADDRESS = range(7)

async def list_students(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Fetches and displays a list of students with pagination."""
    print("DEBUG: list_students CALLED")
    query = update.callback_query
    await query.answer()
    
    try:
        # Check for page number in callback data (e.g., student_list_page_2)
        page = 1
        if "page_" in query.data:
            try:
                page = int(query.data.split("_")[-1])
            except (ValueError, IndexError):
                page = 1
                
        print(f"DEBUG: Fetching students page {page}")
        response = api_client.get(f"/api/student?Page={page}&PageSize=5")
        print(f"DEBUG: API Response Type: {type(response)}") # DEBUG

        # Handle List Response (Direct list of students)
        if isinstance(response, list):
            data = response
            total_count = len(data)
            total_pages = 1
        # Handle Paginated Response (Dict with "data")
        elif isinstance(response, dict) and "data" in response:
            data = response["data"]
            total_count = response.get("totalCount", 0)
            total_pages = response.get("totalPages", 1)
        # Handle Error Response
        elif isinstance(response, dict) and ("error" in response or "status" in response):
            err_msg = response.get("error") or response.get("text") or "Unknown API Error"
            await edit_response(query, f"❌ **API Error:** {err_msg}", back_button_keyboard())
            return
        else:
             data = []

        if not data:
            await edit_response(query, "🚫 **No students found.**", back_button_keyboard())
            return

        message_text = f"🎓 **Student List** (Page {page}/{total_pages})\n━━━━━━━━━━━━━━━━━━\n"
        keyboard = []
        
        # List students as buttons to view details
        for student in data:
            s_id = student.get('studentId')
            name = f"{student.get('firstName')} {student.get('lastName')}"
            # Using bullet point emoji and bold name
            message_text += f"🔹 **{name}** (ID: `{s_id}`)\n"
            keyboard.append([InlineKeyboardButton(f"👤 View {name}", callback_data=f"st_profile_{s_id}")])
            
        # Pagination Buttons
        if total_pages > 1:
            keyboard.extend(pagination_keyboard(page, total_pages, "student_list"))

        # Add Student & Back Buttons
        keyboard.append([InlineKeyboardButton("➕ Add New Student", callback_data="add_student")])
        keyboard.append([InlineKeyboardButton("🔙 Back to Main Menu", callback_data="menu_main")])

        await edit_response(query, message_text, InlineKeyboardMarkup(keyboard))

    except Exception as e:
        import traceback
        traceback.print_exc()
        try:
            await query.edit_message_text(f"❌ **System Error:** {str(e)}", reply_markup=back_button_keyboard(), parse_mode="Markdown")
        except:
            pass

async def view_student_profile(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Displays full details of a student."""
    query = update.callback_query
    await query.answer()
    
    student_id = query.data.split("_")[-1]
    
    try:
        response = api_client.get(f"/api/student/{student_id}")
        print(f"DEBUG: Student Profile Response: {response}")

        if isinstance(response, dict) and ("error" in response or "status" in response):
             err_msg = response.get("error") or response.get("text")
             await query.edit_message_text(f"❌ Error: {err_msg}", reply_markup=back_button_keyboard())
             return
             
        student = response # Should be dict
        if not student:
            await query.edit_message_text("❌ Student not found.", reply_markup=back_button_keyboard())
            return
            
        student = response
        full_name = f"{student.get('firstName')} {student.get('lastName')}"
        
        # Safe Date Parsing
        dob = student.get('dateOfBirth')
        dob = dob.split('T')[0] if dob else "N/A"
        
        enroll_date = student.get('enrollmentDate')
        enroll_date = enroll_date.split('T')[0] if enroll_date else "N/A"
        
        # Helper Data
        from utils.helpers import get_department_name
        dept_name = get_department_name(student.get('departmentId', 0))
        
        msg = f"🎓 **STUDENT PROFILE**\n━━━━━━━━━━━━━━━━━━━━━━\n\n"
        msg += f"👤 **Name:** `{full_name}`\n"
        msg += f"🆔 **ID:** `{student.get('studentId')}`\n"
        msg += f"📜 **Roll No:** `{student.get('rollNumber', 'N/A')}`\n\n"
        
        msg += f"📞 **Phone:** `{student.get('phone', 'N/A')}`\n"
        msg += f"📧 **Email:** `{student.get('email')}`\n"
        msg += f"🏠 **Address:** {student.get('address', 'N/A')}\n\n"
        
        msg += f"🏫 **Department:** *{dept_name}*\n"
        msg += f"📅 **Enrolled:** {enroll_date}\n"
        msg += f"🎂 **DOB:** {dob}\n"
        msg += f"🚻 **Gender:** {student.get('gender', 'N/A')}\n"
        
        await edit_response(query, msg, student_action_keyboard(student_id))
        
    except Exception as e:
        import traceback
        traceback.print_exc()
        try:
            await query.edit_message_text(f"❌ **System Error:** {str(e)}", reply_markup=back_button_keyboard(), parse_mode="Markdown")
        except:
            pass

async def student_action_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Handles various student actions."""
    query = update.callback_query
    data = query.data
    # st_acad_123, st_fin_123, etc.
    parts = data.split("_")
    action_type = parts[1] # acad, fin, att, etc.
    student_id = parts[2]
    
    if action_type == "acad":
        await query.edit_message_text("📚 **Academic Menu**", reply_markup=student_academics_keyboard(student_id), parse_mode="Markdown")
    elif action_type == "fin":
        await query.edit_message_text("💰 **Finance Menu**", reply_markup=student_finance_keyboard(student_id), parse_mode="Markdown")
    elif action_type == "att":
        await view_attendance(update, context)
        
    elif action_type == "profile":
        # Back to profile
        await view_student_profile(update, context)

# --- Feature Handlers ---

async def view_enrollments(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    student_id = query.data.split("_")[-1]
    
    response = api_client.get(f"/api/enrollment/student/{student_id}")
    enrollments = response if isinstance(response, list) else []
    
    msg = "📋 **Current Enrollments**\n━━━━━━━━━━━━━━━━━━\n\n"
    if not enrollments:
        msg += "🚫 No active enrollments found."
    else:
        for en in enrollments:
            c_id = en.get('courseId')
            status = "Unknown"
            # Getting Course Name instead of ID
            course_name = get_course_name(c_id)
            date = en.get('enrollmentDate', '').split('T')[0]
            
            msg += f"📘 **{course_name}**\n"
            msg += f"   📅 Enrolled: {date} | 🆔 `{c_id}`\n\n"
            
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data=f"st_acad_{student_id}")]]
    await query.edit_message_text(msg, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="Markdown")

async def view_fees(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    student_id = query.data.split("_")[-1]
    
    response = api_client.get(f"/api/fee/student/{student_id}")
    fees = response if isinstance(response, list) else []
    
    msg = "💰 **Fee Status**\n━━━━━━━━━━━━━━━━━━\n\n"
    total_due = 0
    if not fees:
        msg += "✅ No fee records found."
    else:
        for f in fees:
            is_paid = f.get("isPaid")
            status = "✅ Paid" if is_paid else "❌ **Unpaid**"
            amount = f.get('amount')
            due_date = f.get('dueDate', '').split('T')[0]
            
            msg += f"💵 **${amount}** - {status}\n"
            msg += f"   📅 Due: {due_date}\n\n"
            
            if not is_paid: total_due += amount
            
    msg += f"━━━━━━━━━━━━━━━━━━\n**Total Outstanding:** **${total_due}**"
            
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data=f"st_fin_{student_id}")]]
    await query.edit_message_text(msg, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="Markdown")

async def view_timetable(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Fetches and displays the timetable for a student based on their enrolled courses."""
    query = update.callback_query
    student_id = query.data.split("_")[-1]

    # 1. Get Enrollments
    en_res = api_client.get(f"/api/enrollment/student/{student_id}")
    enrollments = en_res if isinstance(en_res, list) else []
    
    if not enrollments:
        await query.edit_message_text("🚫 No enrollments found to generate timetable.", 
                                      reply_markup=InlineKeyboardMarkup([[InlineKeyboardButton("🔙 Back", callback_data=f"st_acad_{student_id}")]]))
        return

    msg = "📅 **Student Timetable**\n━━━━━━━━━━━━━━━━━━\n\n"
    
    # 2. For each course, fetch timeslots
    has_slots = False
    for en in enrollments:
        c_id = en.get('courseId')
        c_name = get_course_name(c_id)
        
        # Get slots for this course
        slots_res = api_client.get(f"/api/timeslot/course/{c_id}")
        slots = slots_res if isinstance(slots_res, list) else []
        
        if slots:
            has_slots = True
            msg += f"📘 **{c_name}**\n"
            for slot in slots:
                # Assuming slot has dayOfWeek, startTime, endTime, roomNumber
                day = slot.get('dayOfWeek', 'N/A')
                start = slot.get('startTime', '00:00')
                end = slot.get('endTime', '00:00')
                room = slot.get('room', 'N/A')
                msg += f"   🕒 {day}: {start} - {end} (Room {room})\n"
            msg += "\n"
            
    if not has_slots:
        msg += "⚠️ No scheduled classes found for enrolled courses."

    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data=f"st_acad_{student_id}")]]
    await edit_response(query, msg, InlineKeyboardMarkup(keyboard))

async def view_attendance(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Fetches and displays student attendance statistics."""
    query = update.callback_query
    student_id = query.data.split("_")[-1]
    
    response = api_client.get(f"/api/attendance/student/{student_id}")
    records = response if isinstance(response, list) else []
    
    if not records:
         await edit_response(query, "🚫 **No attendance records found.**", 
                                      InlineKeyboardMarkup([[InlineKeyboardButton("🔙 Back", callback_data=f"st_acad_{student_id}")]]))
         return

    # Group by Course
    stats = {}
    for r in records:
        c_id = r.get('courseId')
        if c_id not in stats:
            stats[c_id] = {'total': 0, 'present': 0}
        stats[c_id]['total'] += 1
        if r.get('isPresent'):
            stats[c_id]['present'] += 1
            
    msg = "📅 **Attendance Record**\n━━━━━━━━━━━━━━━━━━\n\n"
    
    for c_id, stat in stats.items():
        c_name = get_course_name(c_id)
        total = stat['total']
        present = stat['present']
        percentage = (present / total * 100) if total > 0 else 0
        
        status_emoji = "🟢" if percentage >= 75 else "🟠" if percentage >= 60 else "🔴"
        
        msg += f"📘 **{c_name}**\n"
        msg += f"   {status_emoji} **{percentage:.1f}%** ({present}/{total})\n\n"
        
    keyboard = [[InlineKeyboardButton("🔙 Back", callback_data=f"st_acad_{student_id}")]]
    await edit_response(query, msg, InlineKeyboardMarkup(keyboard))

# Reuse existing Add/Delete logic...
async def confirm_delete_student(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    student_id = query.data.split("_")[3]
    response = api_client.delete(f"/api/student/{student_id}")
    if "error" in response:
        await query.edit_message_text(f"❌ Failed: {response['error']}", reply_markup=back_button_keyboard())
    else:
        await query.edit_message_text("✅ Student Deleted Successfully!", reply_markup=back_button_keyboard())

# --- Add Student Conversation (Same as before) ---
async def add_student_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text("📝 **Add New Student**\n\nEnter **First Name**:")
    return ST_FIRSTNAME

async def st_firstname(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["st_firstname"] = update.message.text
    await update.message.reply_text("Enter **Last Name**:")
    return ST_LASTNAME

async def st_lastname(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["st_lastname"] = update.message.text
    await update.message.reply_text("Enter **Email**:")
    return ST_EMAIL

async def st_email(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["st_email"] = update.message.text
    await update.message.reply_text("Enter **Phone Number**:")
    return ST_PHONE

async def st_phone(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["st_phone"] = update.message.text
    await update.message.reply_text("Enter **Date of Birth** (YYYY-MM-DD):")
    return ST_DOB

async def st_dob(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["st_dob"] = update.message.text
    await update.message.reply_text("Enter **Gender** (Male/Female/Other):")
    return ST_GENDER

async def st_gender(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data["st_gender"] = update.message.text
    await update.message.reply_text("Enter **Address**:")
    return ST_ADDRESS

async def st_address(update: Update, context: ContextTypes.DEFAULT_TYPE):
    address = update.message.text
    data = {
        "firstName": context.user_data["st_firstname"],
        "lastName": context.user_data["st_lastname"],
        "email": context.user_data["st_email"],
        "phoneNumber": context.user_data["st_phone"],
        "dateOfBirth": context.user_data["st_dob"],
        "gender": context.user_data["st_gender"],
        "address": address,
        "enrollmentDate": "2024-01-01"
    }
    response = api_client.post("/api/student", data)
    if "error" in response:
        await update.message.reply_text(f"❌ Failed: {response['error']}\nType /start to restart.")
    else:
        await update.message.reply_text("✅ Student Created Successfully!", reply_markup=back_button_keyboard())
    return ConversationHandler.END

async def cancel_op(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await update.message.reply_text("Operation Cancelled.", reply_markup=back_button_keyboard())
    return ConversationHandler.END
