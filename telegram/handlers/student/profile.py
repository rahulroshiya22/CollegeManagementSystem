from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from handlers.menu import get_back_button
from utils.formatting import get_progress_bar, get_role_badge, html_bold, html_code, html_italic, esc

async def view_profile(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    user_id = update.effective_user.id
    api = APIClient(user_id)
    
    # Get Current User Profile
    # Assuming /api/auth/me or fetching by stored ID
    # For now, let's use the stored session ID to fetch from student endpoint
    from services.session import session_manager
    user_data = session_manager.get_user_data(user_id)
    stored_student_id = user_data.get('userId') # This might be UserID, need StudentID. 
    # Usually Backend links User -> Student. Let's try fetching /api/student/{id} if we know it, 
    # or /api/student/profile (if it exists). 
    # Fallback: Search student by email
    
    student = None
    if stored_student_id:
        # Try fetching as if userId == studentId (common in simple apps) or search
        # Better: Filter list by email
        all_students = api.get("/api/student?PageSize=1000")
        if isinstance(all_students, dict): all_students = all_students.get("data", [])
        
        student = next((s for s in all_students if s.get('email') == user_data.get('email')), None)
    
    if not student:
        msg = "❌ Profile not found."
        if query.message.photo:
             await query.message.delete()
             await context.bot.send_message(chat_id=query.message.chat_id, text=msg, parse_mode="HTML", reply_markup=get_back_button())
        else:
             await query.edit_message_text(msg, parse_mode="HTML", reply_markup=get_back_button())
        return

    # Attendance Stats (Mock or Calc)
    # We can fetch attendance records for this student and calc %
    
    # Safe extraction of nested objects
    dept_val = student.get('department')
    dept_name = dept_val.get('name', 'N/A') if isinstance(dept_val, dict) else str(dept_val or 'N/A')

    # Truncate address if suspiciously long (though unlikely to cause 4k chars alone, it helps)
    raw_addr = str(student.get('address', 'N/A'))
    addr = (raw_addr[:100] + '...') if len(raw_addr) > 100 else raw_addr

    # Format DOB
    dob = student.get('dateOfBirth')
    dob_str = "N/A"
    if dob:
        try:
            dob_str = dob.split('T')[0] # Simple YYYY-MM-DD
        except: pass

    status = student.get('status', 'Active')
    status_icon = "🟢" if status == "Active" else "🔴"

    # --- FETCH EXTRA DATA (Like Admin View) ---
    sid = str(student.get('studentId'))
    
    # 1. Attendance Summary
    att_resp = api.get(f"/api/attendance/student/{sid}")
    att_text = "N/A"
    if isinstance(att_resp, list) and att_resp:
        total = len(att_resp)
        present = sum(1 for a in att_resp if a.get('isPresent'))
        att_text = f"{present}/{total} ({(present/total*100):.1f}%)"
    elif isinstance(att_resp, list):
         att_text = "0% (No Records)"

    # 2. Enrollments
    enroll_resp = api.get(f"/api/enrollment/student/{sid}")
    courses_text = f"{html_italic('No active enrollments found.')}"
    
    if isinstance(enroll_resp, list) and enroll_resp:
        c_list = []
        for e in enroll_resp[:10]: # Up to 10
            c = e.get('course', {})
            # Try to populate if missing (sometimes enrollment allocs doesn't include full course obj)
            if not c or not c.get('courseCode'):
                try:
                    c_fetch = api.get(f"/api/course/{e.get('courseId')}")
                    if c_fetch and "error" not in c_fetch:
                        c = c_fetch
                except: pass
            
            code = c.get('courseCode', 'code?')
            name = c.get('title') or c.get('name') or c.get('courseName') or f"Course {e.get('courseId')}"
            credits = c.get('credits', '?')
            
            # Format: 📘 CODE: Name (Credits)
            c_list.append(f"📘 {html_code(esc(code))}: {esc(name)} ({credits} Cr)")
            
        courses_text = "\n".join(c_list)
        if len(enroll_resp) > 10:
            courses_text += f"\n{html_italic(f'...and {len(enroll_resp)-10} more')}"

    # --- FINAL FORMATTING ---
    info = (
        f"🎓 {html_bold('STUDENT PROFILE')}\n"
        f"━━━━━━━━━━━━━━━━━━━━━━\n\n"
        
        f"👤 {html_bold(f'{student.get('firstName')} {student.get('lastName', '')}')}\n"
        f"🆔 {html_code(sid)}  |  📜 {html_code(student.get('rollNumber'))}\n"
        f"{status_icon} {html_bold('Status:')} {status}  |  🎂 {dob_str}\n\n"
        
        f"🏫 {html_bold('ACADEMIC DETAILS')}\n"
        f"├ 🏛 {html_bold('Dept:')} {esc(dept_name)}\n"
        f"├ 📅 {html_bold('Batch:')} {html_code(str(student.get('admissionYear', 'N/A')))}\n"
        f"└ ⚧ {html_bold('Gender:')} {esc(student.get('gender', 'N/A'))}\n\n"
        
        f"📊 {html_bold('PERFORMANCE')}\n"
        f"├ 🙋‍♂️ {html_bold('Attendance:')} {html_code(att_text)}\n"
        f"└ 📚 {html_bold('Courses:')} {len(enroll_resp) if isinstance(enroll_resp, list) else 0} Active\n\n"
        
        f"📝 {html_bold('ENROLLED COURSES')}\n"
        f"{courses_text}\n\n"
        
        f"📞 {html_bold('CONTACT INFO')}\n"
        f"📧 {html_code(student.get('email'))}\n"
        f"📱 {html_code(student.get('phone', 'N/A'))}\n"
        f"📍 {esc(addr)}\n"
        f"━━━━━━━━━━━━━━━━━━━━━━"
    )
    
    
    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(
            chat_id=query.message.chat_id,
            text=info,
            parse_mode="HTML", 
            reply_markup=get_back_button()
        )
    else:
        await query.edit_message_text(info, parse_mode="HTML", reply_markup=get_back_button())

async def view_fees(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Placeholder for Fee API
    # resp = api.get(f"/api/fee/student/{id}")
    msg_text = "💰 <b>Fee Status</b>\n\nFeature coming soon!"
    
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

async def view_attendance_stats(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # Calculate Attendance %
    user_id = update.effective_user.id
    api = APIClient(user_id)
    
    # 1. Get My Student ID
    from services.session import session_manager
    user_data = session_manager.get_user_data(user_id)
    email = user_data.get('email')
    
    # 2. Fetch my attendance
    # This might differ based on backend. Assuming GET /api/attendance returns all, we filter.
    # Or GET /api/attendance/student/{id}
    # Let's try generic fetch & filter for now (Safe bet)
    resp = api.get("/api/attendance") 
    all_recs = resp if isinstance(resp, list) else resp.get("data", [])
    
    # We need to find the student ID from email first (as done in profile)
    all_students = api.get("/api/student?PageSize=1000")
    if isinstance(all_students, dict): all_students = all_students.get("data", [])
    student = next((s for s in all_students if s.get('email') == email), None)
    
    if not student:
        msg = "❌ Could not resolve student profile."
        if query.message.photo:
             await query.message.delete()
             await context.bot.send_message(chat_id=query.message.chat_id, text=msg, parse_mode="HTML", reply_markup=get_back_button())
        else:
             await query.edit_message_text(msg, parse_mode="HTML", reply_markup=get_back_button())
        return
        
    sid = student.get('studentId') or student.get('id')
    my_recs = [r for r in all_recs if str(r.get('studentId')) == str(sid)]
    
    if not my_recs:
        msg = "📅 <b>My Attendance</b>\n\nNo records found."
        if query.message.photo:
             await query.message.delete()
             await context.bot.send_message(chat_id=query.message.chat_id, text=msg, parse_mode="HTML", reply_markup=get_back_button())
        else:
             await query.edit_message_text(msg, parse_mode="HTML", reply_markup=get_back_button())
        return
        
    total = len(my_recs)
    present = sum(1 for r in my_recs if r.get('isPresent'))
    pct = (present / total * 100) if total > 0 else 0
    
    bar = get_progress_bar(pct, total=100)
    
    status_msg = ""
    if pct < 75:
        status_msg = f"⚠️ {html_bold('Warning:')} Low Attendance (<75%)"
    else:
        status_msg = f"✅ {html_bold('Good Attendance')}"
    
    text = (
        f"🏠 Home > 📅 {html_bold('Attendance')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"📊 {html_bold('Overall Performance')}\n\n"
        f"{html_code(bar)}\n"
        f"{html_bold(f'{pct:.1f}% Attendance')}\n\n"
        f"✅ Present: {html_bold(str(present))}\n"
        f"❌ Absent: {html_bold(str(total - present))}\n"
        f"📅 Total Classes: {html_bold(str(total))}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"{status_msg}"
    )
        
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
