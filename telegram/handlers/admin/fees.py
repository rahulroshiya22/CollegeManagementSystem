from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, ConversationHandler, CommandHandler, MessageHandler, filters, CallbackQueryHandler
from services.api import APIClient
from handlers.menu import get_pagination_keyboard, get_back_button
from utils.formatting import format_currency, html_bold, html_code, html_italic, html_expandable_quote, esc
import logging
from datetime import datetime

# States
F_ADD_STUDENT, F_ADD_AMOUNT, F_ADD_DESC, F_ADD_DATE = range(4)
SEARCH_FEE = range(1)

# Helper: Fetch Student Map
def get_student_map(api_client):
    try:
        resp = api_client.get("/api/student?Page=1&PageSize=1000") # Fetch all (limit 1000)
        students = []
        if isinstance(resp, list): students = resp
        elif isinstance(resp, dict): students = resp.get("data") or resp.get("items") or resp.get("value") or []
        
        # Map ID -> "First Last"
        return {s.get('studentId'): f"{s.get('firstName', '')} {s.get('lastName', '')}".strip() for s in students}
    except Exception as e:
        logging.error(f"Failed to fetch students for fee mapping: {e}")
        return {}

# -------------------------------------------------------------------------
#  Fee Dashboard & Lists
# -------------------------------------------------------------------------

async def list_fees(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Main Fee Dashboard.
    Shows Stats and options to Filter or Search.
    """
    query = update.callback_query
    await query.answer()
    
    api = APIClient(update.effective_user.id)
    response = api.get("/api/fee") 
    
    if not response or (isinstance(response, dict) and "error" in response):
         await query.edit_message_text(f"❌ Error: {response.get('error', 'Unknown')}", reply_markup=get_back_button())
         return

    fees = response if isinstance(response, list) else response.get("data", []) or response.get("value", [])
    
    # Calculate Stats
    total_collected = sum(f.get('amount', 0) for f in fees if f.get('status') == 'Paid' or f.get('isPaid'))
    total_pending = sum(f.get('amount', 0) for f in fees if f.get('status') != 'Paid' and not f.get('isPaid'))
    
    text = (
        f"💰 {html_bold('Fee Management Dashboard')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"✅ {html_bold('Collected:')} {format_currency(total_collected)}\n"
        f"⏳ {html_bold('Pending:')} {format_currency(total_pending)}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"Select an action:"
    )
    
    keyboard = [
        [InlineKeyboardButton("📜 View Pending Fees", callback_data="admin_fees_filter_pending_page_1")],
        [InlineKeyboardButton("✅ View Paid History", callback_data="admin_fees_filter_paid_page_1")],
        [InlineKeyboardButton("🔍 Search Student Fee", callback_data="search_fee_start")],
        [InlineKeyboardButton("➕ Create New Fee", callback_data="add_fee_start")],
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

async def view_filtered_fees(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """
    Lists fees based on filter (pending/paid).
    Pattern: admin_fees_filter_{status}_page_{page}
    """
    query = update.callback_query
    await query.answer()
    
    parts = query.data.split("_")
    # parts: ['admin', 'fees', 'filter', 'pending', 'page', '1']
    status_filter = parts[3] 
    page = int(parts[5])
    
    api = APIClient(update.effective_user.id)
    response = api.get("/api/fee")
    fees = response if isinstance(response, list) else response.get("data", [])
    
    # Filter
    filtered = []
    for f in fees:
        is_paid = f.get('status') == 'Paid' or f.get('isPaid')
        if status_filter == "pending" and not is_paid:
            filtered.append(f)
        elif status_filter == "paid" and is_paid:
            filtered.append(f)

    # Fetch Student Names for the current page
    student_map = get_student_map(api)
    
    # Pagination
    items_per_page = 5
    total_pages = (len(filtered) + items_per_page - 1) // items_per_page
    start = (page - 1) * items_per_page
    end = start + items_per_page
    current_items = filtered[start:end]

    title = f"⏳ {html_bold('Pending Fees')}" if status_filter == "pending" else f"✅ {html_bold('Paid History')}"
    text = f"{title} (Page {page}/{total_pages})\n━━━━━━━━━━━━━━━━━━━━\n\n"
    
    if not current_items:
        text += "_No records found._"
    
    keyboard = []
    for f in current_items:
        fid = f.get('feeId') or f.get('id')
        sid = f.get('studentId')
        s_name = student_map.get(sid, f"Student #{sid}")
        
        amt = format_currency(f.get('amount', 0))
        desc = f.get('description', 'Fee')
        
        text += f"👤 {html_bold(esc(s_name))}\n   💵 {amt} - {esc(desc)}\n\n"
        keyboard.append([InlineKeyboardButton(f"👁️ View {s_name.split()[0]}", callback_data=f"view_fee_{fid}")])

    # Nav Buttons
    nav = []
    if page > 1:
        nav.append(InlineKeyboardButton("⬅️ Prev", callback_data=f"admin_fees_filter_{status_filter}_page_{page-1}"))
    if page < total_pages:
        nav.append(InlineKeyboardButton("Next ➡️", callback_data=f"admin_fees_filter_{status_filter}_page_{page+1}"))
    if nav: keyboard.append(nav)
    
    keyboard.append([InlineKeyboardButton("🔙 Dashboard", callback_data="admin_fees")])
    
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def view_fee_detail(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    fid = query.data.split("_")[2]
    api = APIClient(update.effective_user.id)
    fee = api.get(f"/api/fee/{fid}")
    
    if not fee or "error" in fee:
        await query.edit_message_text("❌ Fee Record not found.", reply_markup=get_back_button())
        return

    # Fetch Student Name
    sid = fee.get('studentId')
    s_resp = api.get(f"/api/student/{sid}")
    s_name = f"Student #{sid}"
    if s_resp and "firstName" in s_resp:
        s_name = f"{s_resp.get('firstName')} {s_resp.get('lastName')}".strip()

    # Details
    amt = format_currency(fee.get('amount', 0))
    desc = fee.get('description', 'N/A')
    due = fee.get('dueDate', 'N/A').split('T')[0]
    status = fee.get('status', 'Pending')
    is_paid = status == 'Paid' or fee.get('isPaid')
    
    icon = "✅" if is_paid else "⏳"
    
    text = (
        f"🏠 Home > 💰 Fees > 🧾 {html_bold('Details')}\n\n"
        f"🧾 {html_bold('FEE RECEIPT')}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"🎓 {html_bold('Student:')} {esc(s_name)}\n"
        f"💵 {html_bold('Amount:')} {amt}\n"
        f"📅 {html_bold('Due Date:')} {due}\n"
        f"📝 {html_bold('Description:')} {esc(desc)}\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"{html_bold('Status:')} {icon} {status}\n"
    )
    
    keyboard = []
    if not is_paid:
        keyboard.append([InlineKeyboardButton("💳 Mark as Paid", callback_data=f"pay_fee_{fid}")])
        keyboard.append([InlineKeyboardButton("🔔 Send Reminder", callback_data=f"remind_fee_{fid}")])
    else:
        keyboard.append([InlineKeyboardButton("📥 Download Receipt", callback_data=f"receipt_fee_{fid}")])
        
    keyboard.append([InlineKeyboardButton("🗑️ Delete Fee", callback_data=f"delete_fee_{fid}")])
    keyboard.append([InlineKeyboardButton("🔙 Back to Dashboard", callback_data="admin_fees")])
    
    await query.edit_message_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))

async def delete_fee_confirm(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    fid = query.data.split("_")[2]
    
    keyboard = [
        [InlineKeyboardButton("🗑️ Yes, Delete Forever", callback_data=f"confirm_del_fee_{fid}")],
        [InlineKeyboardButton("❌ Cancel", callback_data=f"view_fee_{fid}")]
    ]
    
    await query.edit_message_text(
        f"⚠️ {html_bold('Delete Fee Record?')}\n\n"
        f"Are you sure you want to delete this fee record?\n"
        f"This action {html_bold('cannot')} be undone.",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def delete_fee(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    fid = query.data.split("_")[3] # confirm_del_fee_{id}
    
    api = APIClient(update.effective_user.id)
    resp = api.delete(f"/api/fee/{fid}")
    
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await query.answer("Fee Deleted!", show_alert=True)
        await list_fees(update, context)
    else:
        err = resp.get("error") if isinstance(resp, dict) else "Unknown"
        await query.answer(f"Failed: {err}", show_alert=True)
        # Return to view
        await view_fee_detail(update, context)

async def mark_fee_paid(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    fid = query.data.split("_")[2]
    
    api = APIClient(update.effective_user.id)
    # Using the specific PayFee endpoint or Update
    # API: [HttpPost("{id}/pay")]
    resp = api.post(f"/api/fee/{fid}/pay", {})
    
    if resp and "error" not in resp:
        await query.answer("✅ Fee Marked as Paid!", show_alert=True)
        # Refresh View
        query.data = f"view_fee_{fid}"
        await view_fee_detail(update, context) # Reload view
    else:
        await query.answer(f"Failed: {resp.get('error')}", show_alert=True)

async def download_receipt(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    fid = query.data.split("_")[2]
    
    await query.answer("Generating PDF Receipt...")

    api = APIClient(update.effective_user.id)
    fee = api.get(f"/api/fee/{fid}")
    
    if not fee or "error" in fee:
        await query.answer("❌ Error fetching fee data.", show_alert=True)
        return

    # Fetch Student Name
    sid = fee.get('studentId')
    s_resp = api.get(f"/api/student/{sid}")
    s_name = f"Student #{sid}"
    if s_resp and "firstName" in s_resp:
        s_name = f"{s_resp.get('firstName')} {s_resp.get('lastName')}".strip()

    # Data
    receipt_no = f"REC-{int(fid):06d}"
    date_paid = fee.get('paidDate', 'N/A').split('T')[0] if fee.get('paidDate') else datetime.now().strftime("%Y-%m-%d")
    amount = format_currency(fee.get('amount', 0))
    desc = fee.get('description', 'Tuition Fee')
    status = "PAID"
    
    # PDF Generation
    filename = f"Receipt_{fid}.pdf"
    
    try:
        from reportlab.lib.pagesizes import letter
        from reportlab.pdfgen import canvas
        from reportlab.lib import colors
        from reportlab.lib.units import inch
        
        c = canvas.Canvas(filename, pagesize=letter)
        width, height = letter
        
        # 1. Header Background
        c.setFillColor(colors.HexColor("#1E3A8A")) # Dark Blue
        c.rect(0, height - 1.5*inch, width, 1.5*inch, fill=1, stroke=0)
        
        # 2. Header Text
        c.setFillColor(colors.white)
        c.setFont("Helvetica-Bold", 24)
        c.drawString(0.5*inch, height - 0.9*inch, "COLLEGE MANAGEMENT SYSTEM")
        c.setFont("Helvetica", 14)
        c.drawString(0.5*inch, height - 1.2*inch, "Official Payment Receipt")
        
        # 3. Receipt Info (Right aligned in header)
        c.setFont("Helvetica-Bold", 12)
        c.drawRightString(width - 0.5*inch, height - 0.9*inch, f"{receipt_no}")
        c.setFont("Helvetica", 12)
        c.drawRightString(width - 0.5*inch, height - 1.2*inch, f"Date: {date_paid}")

        # 4. Student Details
        y = height - 2.5*inch
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 14)
        c.drawString(0.5*inch, y, "Received From:")
        
        c.setFont("Helvetica", 12)
        y -= 0.3*inch
        c.drawString(0.5*inch, y, f"Student Name: {s_name}")
        y -= 0.25*inch
        c.drawString(0.5*inch, y, f"Student ID: {sid}")
        
        # 5. Payment Table
        y -= 0.8*inch
        
        # Table Header
        c.setFillColor(colors.HexColor("#E5E7EB")) # Light Gray
        c.rect(0.5*inch, y, width - 1*inch, 0.4*inch, fill=1, stroke=0)
        
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 12)
        c.drawString(0.6*inch, y + 0.12*inch, "Description")
        c.drawRightString(width - 0.6*inch, y + 0.12*inch, "Amount")
        
        # Table Row
        y -= 0.4*inch
        c.setFont("Helvetica", 12)
        c.drawString(0.6*inch, y + 0.12*inch, desc)
        c.drawRightString(width - 0.6*inch, y + 0.12*inch, amount)
        
        # Line
        c.setStrokeColor(colors.lightgrey)
        c.line(0.5*inch, y, width - 0.5*inch, y)
        
        # Total
        y -= 0.4*inch
        c.setFont("Helvetica-Bold", 14)
        c.drawString(width - 3*inch, y + 0.12*inch, "Total Paid:")
        c.setFillColor(colors.HexColor("#059669")) # Green
        c.drawRightString(width - 0.6*inch, y + 0.12*inch, amount)
        
        # 6. Paid Stamp
        c.saveState()
        c.translate(width/2, height/2)
        c.rotate(30)
        c.setFillColor(colors.HexColor("#DEF7EC")) # Light Green
        c.setStrokeColor(colors.HexColor("#059669"))
        c.setLineWidth(3)
        c.roundRect(-2*inch, -0.75*inch, 4*inch, 1.5*inch, 10, fill=1, stroke=1)
        
        c.setFillColor(colors.HexColor("#059669"))
        c.setFont("Helvetica-Bold", 40)
        c.drawCentredString(0, -0.2*inch, status)
        c.restoreState()
        
        # 7. Footer
        c.setFillColor(colors.darkgrey)
        c.setFont("Helvetica", 9)
        c.drawCentredString(width/2, 0.5*inch, "This is a computer-generated receipt and requires no signature.")
        c.drawCentredString(width/2, 0.35*inch, "Thank you for your business.")

        c.save()
        
        # Send File
        with open(filename, "rb") as f:
            await context.bot.send_document(
                chat_id=update.effective_chat.id,
                document=f,
                caption=f"🧾 {html_bold(f'Official Receipt - {receipt_no}')}",
                parse_mode="HTML"
            )
            
    except Exception as e:
        logging.error(f"PDF Generation Error: {e}")
        await query.answer("❌ Failed to generate PDF.", show_alert=True)
        # Fallback to text if PDF fails?
        
    finally:
        import os
        if os.path.exists(filename):
            os.remove(filename)

# -------------------------------------------------------------------------
#  Search Fee
# -------------------------------------------------------------------------

async def search_fee_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"🔍 {html_bold('Search Fee')}\n\nEnter Student Name:", reply_markup=get_back_button())
    return SEARCH_FEE

async def perform_fee_search(update: Update, context: ContextTypes.DEFAULT_TYPE):
    q = update.message.text.lower()
    api = APIClient(update.effective_user.id)
    fees = api.get("/api/fee") or []
    if isinstance(fees, dict): fees = fees.get("data", [])
    
    # Needs Student Map to search by Name
    student_map = get_student_map(api)
    
    matches = []
    for f in fees:
        sid = f.get('studentId')
        s_name = student_map.get(sid, "").lower()
        if q in s_name:
             f['studentName'] = student_map.get(sid) # Populate name for display
             matches.append(f)
    
    if not matches:
        await update.message.reply_text("❌ No records found.", reply_markup=get_back_button())
        return ConversationHandler.END
        
    text = f"🔍 {html_bold(f'Results for \"{q}\"')}\n━━━━━━━━━━━━━━━━━━━━\n"
    keyboard = []
    
    for f in matches[:5]:
        fid = f.get('feeId') or f.get('id')
        s_name = f.get('studentName')
        amt = format_currency(f.get('amount', 0))
        text += f"• {s_name}: {amt}\n"
        keyboard.append([InlineKeyboardButton(f"View {s_name}", callback_data=f"view_fee_{fid}")])
        
    keyboard.append([InlineKeyboardButton("🔙 Dashboard", callback_data="admin_fees")])
    await update.message.reply_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    return ConversationHandler.END

search_fee_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(search_fee_start, pattern='^search_fee_start$')],
    states={SEARCH_FEE: [MessageHandler(filters.TEXT, perform_fee_search)]},
    fallbacks=[CallbackQueryHandler(list_fees, pattern='^admin_fees$')]
)

# -------------------------------------------------------------------------
#  Add Fee Wizard
# -------------------------------------------------------------------------

async def add_fee_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text(f"➕ {html_bold('Create Fee')}\n\nEnter {html_bold('Student ID')}:", parse_mode="HTML", reply_markup=get_back_button(callback_data="admin_fees"))
    return F_ADD_STUDENT

async def f_receive_student(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_fee_sid'] = update.message.text
    await update.message.reply_text(f"Enter {html_bold('Amount')} (e.g., 5000):", parse_mode="HTML")
    return F_ADD_AMOUNT

async def f_receive_amount(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_fee_amt'] = update.message.text
    await update.message.reply_text(f"Enter {html_bold('Description')} (e.g., Semester 1 Fee):", parse_mode="HTML")
    return F_ADD_DESC

async def f_receive_desc(update: Update, context: ContextTypes.DEFAULT_TYPE):
    context.user_data['new_fee_desc'] = update.message.text
    await update.message.reply_text(f"Enter {html_bold('Due Date')} (YYYY-MM-DD):", parse_mode="HTML")
    return F_ADD_DATE

async def f_receive_date(update: Update, context: ContextTypes.DEFAULT_TYPE):
    date_str = update.message.text
    
    payload = {
        "studentId": int(context.user_data['new_fee_sid']),
        "amount": float(context.user_data['new_fee_amt']),
        "description": context.user_data['new_fee_desc'],
        "dueDate": f"{date_str}T00:00:00Z"
    }
    
    api = APIClient(update.effective_user.id)
    resp = api.post("/api/fee", payload)
    
    if resp and "error" not in resp:
        await update.message.reply_text("✅ Fee Record Created!")
    else:
        await update.message.reply_text(f"❌ Failed: {resp.get('error')}")
        
    return ConversationHandler.END

add_fee_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(add_fee_start, pattern='^add_fee_start$')],
    states={
        F_ADD_STUDENT: [MessageHandler(filters.TEXT, f_receive_student)],
        F_ADD_AMOUNT: [MessageHandler(filters.TEXT, f_receive_amount)],
        F_ADD_DESC: [MessageHandler(filters.TEXT, f_receive_desc)],
        F_ADD_DATE: [MessageHandler(filters.TEXT, f_receive_date)]
    },
    fallbacks=[CallbackQueryHandler(list_fees, pattern='^admin_fees$')]
)
