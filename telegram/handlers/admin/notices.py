from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes, CallbackQueryHandler, ConversationHandler, MessageHandler, filters, CommandHandler
from services.api import APIClient
from services.session import session_manager
from handlers.menu import get_back_button
from utils.formatting import html_bold, html_code, html_italic, html_expandable_quote, esc

# States for Add Notice Conversation
SELECT_ROLE, SELECT_CATEGORY, ENTER_TITLE, ENTER_CONTENT, CONFIRM_NOTICE = range(5)

# -------------------------------------------------------------------------
#  Notice Dashboard
# -------------------------------------------------------------------------

async def list_notices(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()

    api = APIClient(update.effective_user.id)
    response = api.get("/api/notice")
    notices = response if isinstance(response, list) else []
    
    # Sort by recent first
    notices.sort(key=lambda x: x.get('createdAt', ''), reverse=True)
    
    msg = "🏠 Home > 📢 <b>Notice Board</b>\n━━━━━━━━━━━━━━━━━━━━\n"
    keyboard = []
    
    if not notices:
        msg += "🚫 <i>No active notices.</i>"
    else:
        for n in notices[:5]: # Show top 5
            nid = n.get('noticeId')
            title = esc(n.get('title', 'No Title'))
            content = esc(n.get('content', ''))
            role = n.get('targetRole') or "Everyone"
            date = n.get('createdAt', '').split('T')[0]
            category = n.get('category', 'General')
            
            icon = "🎓" if role == "Student" else "👨‍🏫" if role == "Teacher" else "📢"
            
            # Use Expandable Quote for Content!
            msg += (
                f"{icon} <b>{title}</b> ({category})\n"
                f"   └ 📅 {date} | 👥 {role}\n"
                f"{html_expandable_quote(content)}\n\n"
            )
            
            keyboard.append([InlineKeyboardButton(f"🗑️ Delete: {title[:15]}...", callback_data=f"del_notice_{nid}")])

    keyboard.insert(0, [InlineKeyboardButton("➕ Post New Notice", callback_data="add_notice")])
    keyboard.append([InlineKeyboardButton("🔙 Main Menu", callback_data="main_menu")])
    
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

async def delete_notice_confirm(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    nid = query.data.split("_")[2]
    
    keyboard = [
        [InlineKeyboardButton("🗑️ Yes, Delete Forever", callback_data=f"confirm_del_notice_{nid}")],
        [InlineKeyboardButton("❌ Cancel", callback_data="admin_notices")]
    ]
    
    await query.edit_message_text(
        f"⚠️ <b>Delete Notice?</b>\n\n"
        f"Are you sure you want to delete this notice?\n"
        f"This action <b>cannot</b> be undone.",
        parse_mode="HTML",
        reply_markup=InlineKeyboardMarkup(keyboard)
    )

async def delete_notice(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    nid = query.data.split("_")[3] # confirm_del_notice_{id}
    api = APIClient(update.effective_user.id)
    
    resp = api.delete(f"/api/notice/{nid}")
    if resp is None or (isinstance(resp, dict) and "error" not in resp):
        await query.answer("✅ Notice deleted!", show_alert=True)
        await list_notices(update, context)
    else:
        await query.answer("❌ Failed to delete notice.", show_alert=True)
        await list_notices(update, context)

# -------------------------------------------------------------------------
#  Add Notice Wizard
# -------------------------------------------------------------------------

async def add_notice_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    keyboard = [
        [InlineKeyboardButton("👥 Everyone", callback_data="role_All")],
        [InlineKeyboardButton("🎓 Students Only", callback_data="role_Student")],
        [InlineKeyboardButton("👨‍🏫 Teachers Only", callback_data="role_Teacher")],
        [InlineKeyboardButton("🔙 Cancel", callback_data="cancel_notice")]
    ]
    await query.edit_message_text("📢 <b>Post New Notice</b>\n\nWho is this notice for?", parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    return SELECT_ROLE

async def select_role(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    role = query.data.split("_")[1]
    context.user_data['notice_role'] = None if role == "All" else role
    
    keyboard = [
        [InlineKeyboardButton("📌 General", callback_data="cat_General")],
        [InlineKeyboardButton("🎓 Academic", callback_data="cat_Academic")],
        [InlineKeyboardButton("📝 Exam", callback_data="cat_Exam")],
        [InlineKeyboardButton("🎉 Event", callback_data="cat_Event")]
    ]
    await query.edit_message_text(f"Selected Audience: <b>{role}</b>\n\nNow select a <b>Category</b>:", parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    return SELECT_CATEGORY

async def select_category(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    cat = query.data.split("_")[1]
    context.user_data['notice_cat'] = cat
    
    await query.edit_message_text(f"Selected Category: <b>{cat}</b>\n\nPlease type the <b>Title</b> of the notice:", parse_mode="HTML")
    return ENTER_TITLE

async def enter_title(update: Update, context: ContextTypes.DEFAULT_TYPE):
    title = update.message.text
    context.user_data['notice_title'] = title
    await update.message.reply_text(f"📝 Title: <b>{esc(title)}</b>\n\nNow type the <b>Content/Body</b> of the notice:", parse_mode="HTML")
    return ENTER_CONTENT

async def enter_content(update: Update, context: ContextTypes.DEFAULT_TYPE):
    content = update.message.text
    context.user_data['notice_content'] = content
    
    data = context.user_data
    role = data.get('notice_role') or "Everyone"
    
    text = (
        f"📢 <b>Preview Notice</b>\n"
        f"━━━━━━━━━━━━━━━━━━━━\n"
        f"👥 <b>To:</b> {role}\n"
        f"🏷️ <b>Category:</b> {data['notice_cat']}\n"
        f"📌 <b>Title:</b> {esc(data['notice_title'])}\n"
        f"📝 <b>Content:</b>\n{html_expandable_quote(esc(content))}\n\n"
        f"Send this notice?"
    )
    
    keyboard = [
        [InlineKeyboardButton("✅ Send Notice", callback_data="confirm_send_notice")],
        [InlineKeyboardButton("❌ Cancel", callback_data="cancel_notice")]
    ]
    await update.message.reply_text(text, parse_mode="HTML", reply_markup=InlineKeyboardMarkup(keyboard))
    return CONFIRM_NOTICE

async def confirm_notice(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    if query.data == "cancel_notice":
        await query.edit_message_text("❌ Notice creation cancelled.")
        return ConversationHandler.END
        
    data = context.user_data
    api = APIClient(update.effective_user.id)
    
    payload = {
        "title": data['notice_title'],
        "content": data['notice_content'],
        "category": data['notice_cat'],
        "targetRole": data['notice_role'],
        "isActive": True
    }
    
    user = session_manager.get_user_data(update.effective_user.id)
    payload["createdByName"] = f"{user.get('firstName')} {user.get('lastName')}"
    payload["createdByUserId"] = update.effective_user.id # Assuming ID matches, or handle separately
    
    resp = api.post("/api/notice", payload)
    
    if resp and "error" not in resp:
        await query.edit_message_text("✅ <b>Notice Posted Successfully!</b>", parse_mode="HTML")
    else:
        err = resp.get("error") if isinstance(resp, dict) else "Unknown"
        await query.edit_message_text(f"❌ Failed to post notice: {err}")
        
    return ConversationHandler.END

async def cancel_conv(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if update.callback_query:
        await update.callback_query.edit_message_text("❌ Action Cancelled.", reply_markup=get_back_button())
    else:
        await update.message.reply_text("❌ Action Cancelled.", reply_markup=get_back_button())
    return ConversationHandler.END

# Conversation Handler
add_notice_conv = ConversationHandler(
    entry_points=[CallbackQueryHandler(add_notice_start, pattern="^add_notice$")],
    states={
        SELECT_ROLE: [CallbackQueryHandler(select_role, pattern="^role_")],
        SELECT_CATEGORY: [CallbackQueryHandler(select_category, pattern="^cat_")],
        ENTER_TITLE: [MessageHandler(filters.TEXT & ~filters.COMMAND, enter_title)],
        ENTER_CONTENT: [MessageHandler(filters.TEXT & ~filters.COMMAND, enter_content)],
        CONFIRM_NOTICE: [CallbackQueryHandler(confirm_notice, pattern="^confirm_send_notice$"), CallbackQueryHandler(cancel_conv, pattern="^cancel_notice$")]
    },
    fallbacks=[CommandHandler("cancel", cancel_conv), CallbackQueryHandler(cancel_conv, pattern="^cancel_notice$")]
)
