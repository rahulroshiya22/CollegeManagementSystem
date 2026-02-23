import logging
from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import (
    ContextTypes, CallbackQueryHandler, CommandHandler, ConversationHandler,
    MessageHandler, filters
)
from utils.group_db import save_group, delete_group, get_tracked_chats

(
    REG_CHAT_ID,
    REG_DEPT,
    REG_SEM,
    REG_CAT,
    REG_CONFIRM
) = range(5)

async def start_registration(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Triggered by /register_group. Can be used in a group or in DM."""
    chat = update.effective_chat
    user_id = update.effective_user.id
    
    if chat.type == 'private':
        # DM FLOW: Fetch tracked chats
        tracked_chats = get_tracked_chats()
        
        if not tracked_chats:
            text = "⚠️ I am not inside any Groups or Channels yet!\nPlease add me to a group as an Admin first."
            if update.callback_query:
                await update.callback_query.answer()
                await update.callback_query.message.reply_text(text)
            else:
                await update.message.reply_text(text)
            return ConversationHandler.END

        text = "⚙️ <b>Remote Group/Channel Registration</b>\n\nSelect a group/channel you want to configure:"
        keyboard = []
        for c_id, data in tracked_chats.items():
            # Build inline buttons for each group
            btn_text = f"📢 {data.get('title', 'Unknown')} ({data.get('type','chat')})"
            keyboard.append([InlineKeyboardButton(btn_text, callback_data=f"reg_sel_{c_id}")])
            
        keyboard.append([InlineKeyboardButton("❌ Cancel", callback_data="reg_cancel")])
        markup = InlineKeyboardMarkup(keyboard)

        context.user_data['reg_flow'] = 'dm'
        context.user_data['reg_added_by'] = user_id
        
        if update.callback_query:
            await update.callback_query.answer()
            try:
                await update.callback_query.message.delete()
            except: pass
            await context.bot.send_message(chat_id=update.effective_chat.id, text=text, parse_mode="HTML", reply_markup=markup)
        else:
            await update.message.reply_text(text, parse_mode="HTML", reply_markup=markup)
            
        return REG_CHAT_ID
    else:
        # GROUP FLOW: Register the current group
        try:
            member = await chat.get_member(user_id)
            if member.status not in ['administrator', 'creator']:
                await update.message.reply_text("❌ Only Group Admins can register this group.")
                return ConversationHandler.END
        except Exception as e:
            logging.error(f"Error checking admin status: {e}")
            return ConversationHandler.END

        context.user_data['reg_flow'] = 'group'
        context.user_data['reg_chat_id'] = str(chat.id)
        context.user_data['reg_title'] = chat.title
        context.user_data['reg_added_by'] = user_id
        return await ask_dept(update, context)


async def handle_remote_selection(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Handles the inline button selection for a chat."""
    query = update.callback_query
    await query.answer()
    
    if query.data == "reg_cancel":
        return await cancel_reg(update, context)

    # Extract chat ID from callback data (e.g., reg_sel_-100123...)
    chat_id = query.data.split("reg_sel_")[1]
    context.user_data['reg_chat_id'] = str(chat_id)
    
    # Get Title from DB
    tracked_chats = get_tracked_chats()
    if chat_id in tracked_chats:
        context.user_data['reg_title'] = tracked_chats[chat_id].get("title", "Unknown")
    else:
        context.user_data['reg_title'] = "Unknown Group"

    return await ask_dept(update, context)


async def ask_dept(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Helper to display the Department selection keyboard."""
    text = (
        f"Step 1: Which <b>Department</b> does this group/channel belong to?\n"
        f"Target: <i>{context.user_data.get('reg_title', 'Unknown')}</i>"
    )
    
    keyboard = [
        [InlineKeyboardButton("All Departments", callback_data="reg_dept_All")],
        [InlineKeyboardButton("BCA", callback_data="reg_dept_BCA"), InlineKeyboardButton("BBA", callback_data="reg_dept_BBA")],
        [InlineKeyboardButton("B.Tech", callback_data="reg_dept_B.Tech"), InlineKeyboardButton("B.Com", callback_data="reg_dept_B.Com")],
        [InlineKeyboardButton("Cancel", callback_data="reg_cancel")]
    ]
    markup = InlineKeyboardMarkup(keyboard)
    
    if update.callback_query:
        await update.callback_query.edit_message_text(text, reply_markup=markup, parse_mode="HTML")
    else:
        await update.message.reply_text(text, reply_markup=markup, parse_mode="HTML")
        
    return REG_DEPT


async def set_dept(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    if query.data == "reg_cancel": return await cancel_reg(update, context)

    dept = query.data.split("reg_dept_")[1]
    context.user_data['reg_dept'] = dept

    text = f"✅ Tagged Department: <b>{dept}</b>\n\nStep 2: Which <b>Semester</b>?"
    
    keyboard = [
        [InlineKeyboardButton("All Semesters", callback_data="reg_sem_All")],
        [InlineKeyboardButton("Sem 1", callback_data="reg_sem_Sem 1"), InlineKeyboardButton("Sem 2", callback_data="reg_sem_Sem 2")],
        [InlineKeyboardButton("Sem 3", callback_data="reg_sem_Sem 3"), InlineKeyboardButton("Sem 4", callback_data="reg_sem_Sem 4")],
        [InlineKeyboardButton("Sem 5", callback_data="reg_sem_Sem 5"), InlineKeyboardButton("Sem 6", callback_data="reg_sem_Sem 6")],
        [InlineKeyboardButton("Cancel", callback_data="reg_cancel")]
    ]
    await query.edit_message_text(text, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="HTML")
    return REG_SEM


async def set_sem(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    if query.data == "reg_cancel": return await cancel_reg(update, context)

    sem = query.data.split("reg_sem_")[1]
    context.user_data['reg_sem'] = sem

    text = f"✅ Tagged Semester: <b>{sem}</b>\n\nStep 3: Which <b>Category (Gender)</b>?"
    
    keyboard = [
        [InlineKeyboardButton("All (Mixed)", callback_data="reg_cat_All")],
        [InlineKeyboardButton("Boys Only", callback_data="reg_cat_Boys"), InlineKeyboardButton("Girls Only", callback_data="reg_cat_Girls")],
        [InlineKeyboardButton("Cancel", callback_data="reg_cancel")]
    ]
    await query.edit_message_text(text, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="HTML")
    return REG_CAT

async def set_cat(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    if query.data == "reg_cancel": return await cancel_reg(update, context)

    cat = query.data.split("reg_cat_")[1]
    context.user_data['reg_cat'] = cat
    
    # Save the group
    success = save_group(
        chat_id=context.user_data['reg_chat_id'],
        title=context.user_data['reg_title'],
        department=context.user_data['reg_dept'],
        semester=context.user_data['reg_sem'],
        category=context.user_data['reg_cat'],
        added_by=context.user_data['reg_added_by']
    )

    if success:
        text = (
            f"🎉 <b>GROUP REGISTRATION SUCCESSFUL!</b>\n\n"
            f"Title: <i>{context.user_data['reg_title']}</i>\n"
            f"Tags: [{context.user_data['reg_dept']}] - [{context.user_data['reg_sem']}] - [{context.user_data['reg_cat']}]\n\n"
            f"This group will now receive targeted broadcasts."
        )
    else:
        text = "❌ Error saving group. Please check backend logs."
        
    await query.edit_message_text(text, parse_mode="HTML")
    context.user_data.clear()
    return ConversationHandler.END


async def unregister_group(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Command /unregister_group to remove the group from DB"""
    chat = update.effective_chat
    if chat.type == 'private':
        return

    # Check admin privileges
    user_id = update.effective_user.id
    try:
        member = await chat.get_member(user_id)
        if member.status not in ['administrator', 'creator']:
            await update.message.reply_text("❌ Only Group Admins can unregister this group.")
            return
    except Exception:
        return

    deleted = delete_group(str(chat.id))
    if deleted:
        await update.message.reply_text("✅ Group unregistered. You will no longer receive broadcasts.")
    else:
        await update.message.reply_text("⚠️ This group was not registered.")


async def cancel_reg(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    await query.edit_message_text("❌ Registration canceled.")
    context.user_data.clear()
    return ConversationHandler.END


group_registration_handler = ConversationHandler(
    entry_points=[
        CommandHandler('register_group', start_registration),
        CallbackQueryHandler(start_registration, pattern="^admin_add_group$")
    ],
    states={
        REG_CHAT_ID: [CallbackQueryHandler(handle_remote_selection, pattern='^reg_sel_|^reg_cancel$')],
        REG_DEPT: [CallbackQueryHandler(set_dept, pattern='^reg_dept_|^reg_cancel$')],
        REG_SEM: [CallbackQueryHandler(set_sem, pattern='^reg_sem_|^reg_cancel$')],
        REG_CAT: [CallbackQueryHandler(set_cat, pattern='^reg_cat_|^reg_cancel$')],
    },
    fallbacks=[CommandHandler('cancel', cancel_reg)]
)

unregister_cmd_handler = CommandHandler('unregister_group', unregister_group)
