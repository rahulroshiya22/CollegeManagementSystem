from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from datetime import datetime
from utils.formatting import html_bold, html_code, html_italic, esc

# Stages
ATT_SELECT_COURSE, ATT_SELECT_DATE, ATT_MARKING = range(3)

# Helper to store temporary attendance state
# context.user_data['att_data'] = { 'course_id': 123, 'date': '2024-01-01', 'students': { id: True/False } }

async def start_attendance(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # 1. Fetch Courses (Ideally "My Courses", for now all courses)
    api = APIClient(update.effective_user.id)
    resp = api.get("/api/course")
    courses = resp if isinstance(resp, list) else resp.get("data", [])
    
    if not courses:
        await query.edit_message_text("🚫 No courses found to take attendance for.")
        return ConversationHandler.END

    keyboard = []
    for c in courses:
        cname = c.get('courseCode') or c.get('name')
        cid = c.get('courseId') or c.get('id')
        keyboard.append([InlineKeyboardButton(f"📘 {cname}", callback_data=f"att_course_{cid}")])
    
    keyboard.append([InlineKeyboardButton("🔙 Cancel", callback_data="cancel_att")])
    
    msg_text = (
        f"🏠 Home > 📅 {html_bold('Attendance')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Select a course to mark attendance:"
    )

    if query.message.photo:
        await query.message.delete()
        await context.bot.send_message(
             chat_id=query.message.chat_id,
             text=msg_text,
             reply_markup=InlineKeyboardMarkup(keyboard), 
             parse_mode="HTML"
        )
    else:
        await query.edit_message_text(
            msg_text, 
            reply_markup=InlineKeyboardMarkup(keyboard), 
            parse_mode="HTML"
        )
    return ATT_SELECT_COURSE

async def select_date(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    course_id = query.data.split("_")[2]
    context.user_data['att_course_id'] = course_id
    
    # Simple Date Selection: Today or Manual
    today = datetime.now().strftime("%Y-%m-%d")
    keyboard = [
        [InlineKeyboardButton(f"Today ({today})", callback_data=f"att_date_{today}")],
        [InlineKeyboardButton("🔙 Cancel", callback_data="cancel_att")]
    ]
    
    await query.edit_message_text(f"📅 {html_bold('Select Date')}", reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="HTML")
    return ATT_SELECT_DATE

async def fetch_students_for_marking(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    date = query.data.split("_")[2]
    context.user_data['att_date'] = date
    course_id = context.user_data['att_course_id']
    
    api = APIClient(update.effective_user.id)
    
    # Get Enrollments for this course
    # Assuming /api/enrollment returns all, need to filtering client side or api
    # For efficiency in this demo, let's assume we can filter or fetch all students and check
    # Optimized: GET /api/enrollment (and filter)
    enrollments_resp = api.get("/api/enrollment")
    all_enrollments = enrollments_resp if isinstance(enrollments_resp, list) else enrollments_resp.get("data", [])
    
    # Filter for this course
    course_enrolls = [e for e in all_enrollments if str(e.get('courseId')) == str(course_id)]
    
    if not course_enrolls:
         await query.edit_message_text("🚫 No students enrolled in this course.")
         return ConversationHandler.END

    # We need Student Names. The enrollment might have student object or just ID
    # If just ID, we need to fetch students.
    # PRO TIP: The Frontend fetches "All Students" and maps them. We will do the same or fetch individually.
    # Efficient: Fetch All Students once.
    students_resp = api.get("/api/student?PageSize=100") # fetch many
    all_students = students_resp if isinstance(students_resp, list) else students_resp.get("data", [])
    
    student_map = {str(s.get('studentId') or s.get('id')): f"{s.get('firstName')} {s.get('lastName')}" for s in all_students}
    
    # Init Marking State: Default Present (True)
    marking_state = {}
    for e in course_enrolls:
        sid = str(e.get('studentId'))
        if sid in student_map:
            marking_state[sid] = True # Default Present
            
    context.user_data['att_marking'] = marking_state
    context.user_data['att_student_names'] = student_map
    
    await render_marking_list(query, context)
    return ATT_MARKING

async def render_marking_list(query, context):
    marking = context.user_data['att_marking']
    names = context.user_data['att_student_names']
    
    # Build Keyboard with Status Toggles
    keyboard = []
    # Limit to reasonable number per page (Telegram limit)
    # For now simply list first 10-15 or chunk
    
    present_count = sum(1 for v in marking.values() if v)
    absent_count = len(marking) - present_count
    
    text = (f"🏠 Home > 📅 Attd > 📝 {html_bold('Marking')}\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
            f"📘 {html_bold('Course:')} {context.user_data['att_course_id']}\n"
            f"📅 {html_bold('Date:')} {context.user_data['att_date']}\n\n"
            f"✅ {html_bold('Present:')} {present_count}  |  ❌ {html_bold('Absent:')} {absent_count}\n"
            f"━━━━━━━━━━━━━━━━━━━━\n"
            f"👇 Tap to toggle status:")
            
    for sid, is_present in marking.items():
        status_icon = "✅" if is_present else "❌"
        name = names.get(sid, f"Student {sid}")
        keyboard.append([InlineKeyboardButton(f"{status_icon} {name}", callback_data=f"toggle_att_{sid}")])
        
    keyboard.append([InlineKeyboardButton("💾 SUBMIT ATTENDANCE", callback_data="submit_att")])
    keyboard.append([InlineKeyboardButton("🔙 Cancel", callback_data="cancel_att")])
    
    await query.edit_message_text(text, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="HTML")

async def toggle_student_status(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    # Don't answer yet to avoid loading spinner freeze if rapid clicking, or answer immediately
    await query.answer() 
    
    sid = query.data.split("_")[2]
    current_status = context.user_data['att_marking'].get(sid, True)
    context.user_data['att_marking'][sid] = not current_status
    
    await render_marking_list(query, context)
    return ATT_MARKING

async def submit_attendance(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer("Submitting...")
    
    marking = context.user_data['att_marking']
    course_id = int(context.user_data['att_course_id'])
    date = context.user_data['att_date']
    api = APIClient(update.effective_user.id)
    
    success_count = 0
    errors = 0
    
    # Submit one by one (API limitation usually requires per record)
    for sid, is_present in marking.items():
        payload = {
            "studentId": int(sid),
            "courseId": course_id,
            "date": date,
            "isPresent": is_present,
            "remarks": ""
        }
        resp = api.post("/api/attendance", payload)
        if resp and "error" not in resp:
            success_count += 1
        else:
            errors += 1
            
    await query.edit_message_text(f"✅ {html_bold('Attendance Saved!')}\nSuccess: {success_count}\nFailed: {errors}", parse_mode="HTML")
    return ConversationHandler.END

async def cancel_att(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text("🚫 Attendance Cancelled.")
    return ConversationHandler.END

# Handler Registry
attendance_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(start_attendance, pattern='^teacher_attendance$')],
    states={
        ATT_SELECT_COURSE: [CallbackQueryHandler(select_date, pattern='^att_course_')],
        ATT_SELECT_DATE: [CallbackQueryHandler(fetch_students_for_marking, pattern='^att_date_')],
        ATT_MARKING: [
            CallbackQueryHandler(toggle_student_status, pattern='^toggle_att_'),
            CallbackQueryHandler(submit_attendance, pattern='^submit_att$')
        ]
    },

    fallbacks=[CallbackQueryHandler(cancel_att, pattern='^cancel_att$|^main_menu$')]
)

async def view_my_classes(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    user_id = update.effective_user.id
    api = APIClient(user_id)
    
    # 1. Resolve Teacher ID
    # This is inefficient (fetching all), but robust without specific endpoint
    teachers_resp = api.get("/api/teacher?PageSize=1000")
    all_teachers = teachers_resp if isinstance(teachers_resp, list) else teachers_resp.get("data", [])
    
    me = next((t for t in all_teachers if str(t.get('userId')) == str(user_id)), None)
    
    if not me:
        msg = "❌ Teacher profile not found."
        if query.message.photo:
             await query.message.delete()
             await context.bot.send_message(chat_id=query.message.chat_id, text=msg, parse_mode="HTML")
        else:
             await query.edit_message_text(msg)
        return

    tid = me.get('teacherId') or me.get('id')
    
    # 2. Fetch TimeSlots (Classes)
    slots = api.get(f"/api/timeslot/teacher/{tid}")
    my_slots = slots if isinstance(slots, list) else slots.get("data", [])
    
    if not my_slots:
        msg = f"👨‍🏫 {html_bold('My Classes')}\n\n{html_italic('No classes scheduled yet.')}"
        from handlers.menu import get_back_button
        if query.message.photo:
             await query.message.delete()
             await context.bot.send_message(chat_id=query.message.chat_id, text=msg, parse_mode="HTML", reply_markup=get_back_button())
        else:
             await query.edit_message_text(msg, parse_mode="HTML", reply_markup=get_back_button())
        return

    # 3. Aggregate Courses
    # We need course names. Fetch all courses once (cache optimization)
    # or fetch unique ones.
    unique_cids = {s.get('courseId') for s in my_slots if s.get('courseId')}
    
    # Fetch details for these courses
    # Optimization: If API supports /api/course/ids=[...] that would be great.
    # for now, fetch all and filter client side is usually faster than N calls for < 50 courses
    all_courses_resp = api.get("/api/course?PageSize=1000")
    all_courses = all_courses_resp if isinstance(all_courses_resp, list) else all_courses_resp.get("data", [])
    
    my_courses = [c for c in all_courses if c.get('courseId') in unique_cids]
    
    text = f"👨‍🏫 {html_bold('My Classes')}\n━━━━━━━━━━━━━━━━━━━━\n\n"
    
    for c in my_courses:
        code = c.get('courseCode') or "N/A"
        name = c.get('courseName') or c.get('name')
        credits = c.get('credits', 0)
        
        text += f"📘 {html_bold(esc(name))} ({html_code(esc(code))})\n   ⭐️ {credits} Credits\n\n"
        
    from handlers.menu import get_back_button
    if query.message.photo:
         await query.message.delete()
         await context.bot.send_message(chat_id=query.message.chat_id, text=text, parse_mode="HTML", reply_markup=get_back_button())
    else:
         await query.edit_message_text(text, parse_mode="HTML", reply_markup=get_back_button())

