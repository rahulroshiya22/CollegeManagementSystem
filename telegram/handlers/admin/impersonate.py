from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, CallbackQueryHandler, ConversationHandler
from services.api import APIClient
from services.session import session_manager
from handlers.menu import show_dashboard
from utils.formatting import html_bold, esc

async def impersonate_menu(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    keyboard = [
        [InlineKeyboardButton("👨‍🏫 Login as Teacher", callback_data="imp_sel_role_Teacher")],
        [InlineKeyboardButton("🎓 Login as Student", callback_data="imp_sel_role_Student")],
        [InlineKeyboardButton("🔙 Back to Menu", callback_data="main_menu")]
    ]
    
    text_content = (
        f"🎭 {html_bold('Login As (Impersonation)')}\n\n"
        "Select a role to impersonate. You will browse and select a specific user."
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

async def imp_sel_role(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    role = query.data.split("_")[3]
    context.user_data['imp_role'] = role
    
    # Next: Select Department
    api = APIClient(update.effective_user.id)
    resp = api.get("/api/department")
    depts = resp if isinstance(resp, list) else resp.get("data", [])
    
    keyboard = []
    row = []
    for d in depts:
        row.append(InlineKeyboardButton(d['name'], callback_data=f"imp_sel_dept_{d['departmentId']}"))
        if len(row) == 2:
            keyboard.append(row)
            row = []
    if row: keyboard.append(row)
    
    keyboard.append([InlineKeyboardButton("🔙 Back", callback_data="admin_impersonate")])
    
    await query.edit_message_text(
        f"🏢 {html_bold('Select Department')} for {role}:",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def imp_list_users(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    dept_id = query.data.split("_")[3]
    role = context.user_data.get('imp_role', 'Student')
    
    api = APIClient(update.effective_user.id)
    
    
    # helper
    def get_val(obj, keys, default=None):
        for k in keys:
            if k in obj: return obj[k]
        return default

    # Pre-fetch Department Map for Name lookup
    d_resp = api.get("/api/department")
    d_list = d_resp if isinstance(d_resp, list) else d_resp.get("data", [])
    dept_map = {str(d.get('departmentId')): d.get('name') for d in d_list}
    
    selected_dept_name = dept_map.get(str(dept_id), "")
    
    print(f"[DEBUG] Dept Search: ID={dept_id}, Name='{selected_dept_name}'")

    users = []
    if role == "Student":
        # USE SERVER SIDE FILTERING!
        resp = api.get(f"/api/student?DepartmentId={dept_id}&PageSize=20")
        if resp is None: users = []
        elif isinstance(resp, list): users = resp
        else: users = resp.get("data", []) or resp.get("items", []) or []
            
        id_key = 'studentId'
        
    else: # Teacher
        # USE SERVER SIDE FILTERING!
        resp = api.get(f"/api/teacher?DepartmentId={dept_id}&PageSize=20")
        if resp is None: users = []
        elif isinstance(resp, list): users = resp
        else: users = resp.get("data", []) or resp.get("items", []) or []
        
        id_key = 'teacherId'
        
    keyboard = []
    row = []
    for u in users[:20]: # Limit 20
        fname = get_val(u, ['firstName', 'FirstName'], '')
        lname = get_val(u, ['lastName', 'LastName'], '')
        name = f"{fname} {lname}".strip()
        
        # ID Lookup
        if role == "Student":
             uid = get_val(u, ['studentId', 'StudentId', 'id', 'Id'])
        else:
             uid = get_val(u, ['teacherId', 'TeacherId', 'id', 'Id'])
             
        keyboard.append([InlineKeyboardButton(f"👤 {name}", callback_data=f"imp_do_{role}_{uid}")])
        
    keyboard.append([InlineKeyboardButton("🔙 Back", callback_data=f"imp_sel_role_{role}")])
    
    if not users:
        msg = f"🚫 No {role}s found in {html_bold(esc(selected_dept_name))}."
    else:
        msg = f"👤 {html_bold(f'Select {role}')} from {esc(selected_dept_name)}:"
        
    await query.edit_message_text(
        msg,
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def imp_perform_login(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    # imp_do_{role}_{uid}
    parts = query.data.split("_")
    role = parts[2]
    uid = parts[3]
    
    print(f"[DEBUG] Attempting Login As: {role} ID={uid}")
    
    api = APIClient(update.effective_user.id)
    target_data = {}
    
    def get_val_local(obj, keys, default=None):
        for k in keys:
            if k in obj: return obj[k]
        return default
    
    if role == "Student":
        resp = api.get(f"/api/student/{uid}")
        target_data = resp if isinstance(resp, dict) else {}
        # Ensure standard keys for session
        target_data['userId'] = get_val_local(target_data, ['studentId', 'StudentId', 'id'])
        target_data['firstName'] = get_val_local(target_data, ['firstName', 'FirstName'])
        target_data['email'] = get_val_local(target_data, ['email', 'Email'])
        target_data['role'] = "Student"
        
    else:
        resp = api.get(f"/api/teacher/{uid}")
        target_data = resp if isinstance(resp, dict) else {}
        target_data['userId'] = target_data.get('teacherId')
        target_data['role'] = "Teacher"
    
    print(f"[DEBUG] Target Data: {target_data}")
        
    if not target_data or not target_data.get('userId'):
        await query.answer("❌ Error: Could not fetch valid user data.", show_alert=True)
        return

    # Perform Impersonation
    success = session_manager.impersonate_user(update.effective_user.id, target_data)
    
    if success:
        await query.answer(f"✅ Logged in as {target_data.get('firstName')}", show_alert=True)
        # Redirect to Dashboard (which will now see the new Role)
        await show_dashboard(update, context)
    else:
        await query.answer("❌ Session Error", show_alert=True)
