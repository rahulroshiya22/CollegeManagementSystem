import json
import logging
from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import (
    ContextTypes, CallbackQueryHandler, MessageHandler, filters,
    ConversationHandler, CommandHandler
)
from utils.formatting import html_bold, html_italic, esc, html_code
from utils.group_db import get_groups_by_filter, get_all_groups
from handlers.menu import get_back_button

# States for the conversation
(
    SELECT_DEPT,
    SELECT_SEM,
    SELECT_CAT,
    AWAIT_MESSAGE,
    AWAIT_BUTTON,
    CONFIRM_BROADCAST
) = range(6)

async def start_broadcast(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Entry point for /broadcast command or from Admin Menu (CallbackQuery)."""
    # Check if there are any registered groups
    groups = get_all_groups()
    if not groups:
        text = "⚠️ <b>No Groups Registered!</b>\n\nPlease add the bot to a group and type <code>/register_group</code> before broadcasting."
        if update.callback_query:
            await update.callback_query.answer()
            try:
                await update.callback_query.message.delete()
            except: pass
            await context.bot.send_message(chat_id=update.effective_chat.id, text=text, parse_mode="HTML")
        else:
            await update.message.reply_text(text, parse_mode="HTML")
        return ConversationHandler.END

    text = (
        f"📢 <b>Post Management Engine</b>\n\n"
        f"Let's create a targeted broadcast.\n"
        f"Step 1: Which <b>Department</b> do you want to target?"
    )
    
    keyboard = [
        [InlineKeyboardButton("All Departments", callback_data="bc_dept_All")],
        [InlineKeyboardButton("BCA", callback_data="bc_dept_BCA"), InlineKeyboardButton("BBA", callback_data="bc_dept_BBA")],
        [InlineKeyboardButton("B.Tech", callback_data="bc_dept_B.Tech"), InlineKeyboardButton("B.Com", callback_data="bc_dept_B.Com")],
        [InlineKeyboardButton("Cancel", callback_data="bc_cancel")]
    ]
    reply_markup = InlineKeyboardMarkup(keyboard)

    if update.callback_query:
        await update.callback_query.answer()
        try:
            await update.callback_query.message.delete()
        except: pass
        await context.bot.send_message(
            chat_id=update.effective_chat.id, 
            text=text, 
            reply_markup=reply_markup, 
            parse_mode="HTML"
        )
    else:
        await update.message.reply_text(text, reply_markup=reply_markup, parse_mode="HTML")

    return SELECT_DEPT

async def handle_dept(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    if query.data == "bc_cancel": return await cancel_broadcast(update, context)

    dept = query.data.split("bc_dept_")[1]
    context.user_data['bc_dept'] = dept

    text = (
        f"✅ Targeted Department: <b>{dept}</b>\n\n"
        f"Step 2: Which <b>Semester</b> do you want to target?"
    )
    keyboard = [
        [InlineKeyboardButton("All Semesters", callback_data="bc_sem_All")],
        [InlineKeyboardButton("Sem 1", callback_data="bc_sem_Sem 1"), InlineKeyboardButton("Sem 2", callback_data="bc_sem_Sem 2")],
        [InlineKeyboardButton("Sem 3", callback_data="bc_sem_Sem 3"), InlineKeyboardButton("Sem 4", callback_data="bc_sem_Sem 4")],
        [InlineKeyboardButton("Sem 5", callback_data="bc_sem_Sem 5"), InlineKeyboardButton("Sem 6", callback_data="bc_sem_Sem 6")],
        [InlineKeyboardButton("Cancel", callback_data="bc_cancel")]
    ]
    await query.edit_message_text(text, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="HTML")
    return SELECT_SEM

async def handle_sem(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    if query.data == "bc_cancel": return await cancel_broadcast(update, context)

    sem = query.data.split("bc_sem_")[1]
    context.user_data['bc_sem'] = sem

    text = (
        f"✅ Targeted Department: <b>{context.user_data['bc_dept']}</b>\n"
        f"✅ Targeted Semester: <b>{sem}</b>\n\n"
        f"Step 3: Which <b>Category</b> do you want to target?"
    )
    keyboard = [
        [InlineKeyboardButton("All (Mixed)", callback_data="bc_cat_All")],
        [InlineKeyboardButton("Boys Only", callback_data="bc_cat_Boys"), InlineKeyboardButton("Girls Only", callback_data="bc_cat_Girls")],
        [InlineKeyboardButton("Cancel", callback_data="bc_cancel")]
    ]
    await query.edit_message_text(text, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="HTML")
    return SELECT_CAT

async def handle_cat(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    if query.data == "bc_cancel": return await cancel_broadcast(update, context)

    cat = query.data.split("bc_cat_")[1]
    context.user_data['bc_cat'] = cat
    
    # Check how many groups match this filter immediately
    matched_groups = get_groups_by_filter(
        context.user_data['bc_dept'],
        context.user_data['bc_sem'],
        cat
    )
    
    context.user_data['bc_targets'] = matched_groups

    if not matched_groups:
         text = (
            f"⚠️ <b>NO GROUPS MATCH YOUR FILTER.</b>\n"
            f"Dept: {context.user_data['bc_dept']} | Sem: {context.user_data['bc_sem']} | Cat: {cat}\n\n"
            f"Broadcast canceled. Please register groups matching these criteria first."
        )
         await query.edit_message_text(text, parse_mode="HTML", reply_markup=get_back_button())
         return ConversationHandler.END

    text = (
        f"🎯 <b>Targets Acquired:</b> {len(matched_groups)} Groups found.\n\n"
        f"Step 4: <b>Send your message now.</b>\n\n"
        f"<i>Tip: You can send Text, a Photo (with caption), a Video, or a Document (like a Syllabus PDF).</i>"
    )
    await query.edit_message_text(text, parse_mode="HTML")
    return AWAIT_MESSAGE

async def receive_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    # Store the exact message so we can copy it perfectly
    context.user_data['bc_message_id'] = update.message.message_id
    context.user_data['bc_chat_id'] = update.message.chat_id

    text = (
        f"✅ Message saved.\n\n"
        f"Step 5 (Optional): Do you want to add an <b>Inline Button</b> to the bottom of the message?\n\n"
        f"Reply with the format: <code>Button Text - https://link.com</code>\n"
        f"Or just type <code>skip</code> to continue without a button."
    )
    await update.message.reply_text(text, parse_mode="HTML")
    return AWAIT_BUTTON

async def receive_button(update: Update, context: ContextTypes.DEFAULT_TYPE):
    btn_text = update.message.text.strip()
    
    if btn_text.lower() == 'skip':
        context.user_data['bc_button'] = None
    else:
        try:
            label, url = map(str.strip, btn_text.split("-", 1))
            if not url.startswith("http"):
                url = "https://" + url
            context.user_data['bc_button'] = InlineKeyboardMarkup(
                [[InlineKeyboardButton(label, url=url)]]
            )
        except Exception:
            await update.message.reply_text("❌ Invalid format. Please use `Button Name - Link`, or type `skip`.")
            return AWAIT_BUTTON

    # PREVIEW
    await update.message.reply_text("👀 <b>Here is a preview of your broadcast:</b>", parse_mode="HTML")
    
    btn_markup = context.user_data.get('bc_button')
    try:
        await context.bot.copy_message(
            chat_id=update.message.chat_id,
            from_chat_id=context.user_data['bc_chat_id'],
            message_id=context.user_data['bc_message_id'],
            reply_markup=btn_markup
        )
    except Exception as e:
        await update.message.reply_text(f"⚠️ Error rendering preview: {e}")

    # Confirmation Controls
    targets = len(context.user_data['bc_targets'])
    confirm_text = (
        f"🎯 <b>Target Match:</b> {targets} Groups\n"
        f"📊 <b>Filters:</b> Dept: {context.user_data['bc_dept']} | Sem: {context.user_data['bc_sem']} | Cat: {context.user_data['bc_cat']}\n\n"
        f"Ready to blast?"
    )
    
    keyboard = [
        [InlineKeyboardButton("🚀 SEND NOW", callback_data="bc_send_normal")],
        [InlineKeyboardButton("🔕 Send Silently", callback_data="bc_send_silent")],
        [InlineKeyboardButton("📌 Send & Pin", callback_data="bc_send_pin")],
        [InlineKeyboardButton("❌ Cancel", callback_data="bc_cancel")]
    ]
    await update.message.reply_text(confirm_text, reply_markup=InlineKeyboardMarkup(keyboard), parse_mode="HTML")
    return CONFIRM_BROADCAST

async def execute_broadcast(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    
    if query.data == "bc_cancel": return await cancel_broadcast(update, context)

    await query.edit_message_text("🔄 <b>Broadcasting... Please wait.</b>", parse_mode="HTML")
    
    target_chats = context.user_data.get('bc_targets', [])
    from_chat = context.user_data['bc_chat_id']
    msg_id = context.user_data['bc_message_id']
    markup = context.user_data.get('bc_button')
    
    mode = query.data # normal, silent, pin
    
    success = 0
    failed = 0
    failed_reasons = []

    for target_chat_id in target_chats:
        try:
             # copy_message avoids "Forwarded from" tags and natively handles all media
             sent_msg = await context.bot.copy_message(
                 chat_id=target_chat_id,
                 from_chat_id=from_chat,
                 message_id=msg_id,
                 reply_markup=markup,
                 disable_notification=(mode == "bc_send_silent")
             )
             success += 1
             
             # Attempt to pin if requested
             if mode == "bc_send_pin":
                 try:
                     await context.bot.pin_chat_message(
                         chat_id=target_chat_id, 
                         message_id=sent_msg.message_id,
                         disable_notification=False
                     )
                 except Exception as e:
                     logging.error(f"Failed to pin in {target_chat_id}: {e}")
                     
        except Exception as e:
            failed += 1
            logging.error(f"Broadcast failed for {target_chat_id}: {e}")
            failed_reasons.append(str(e))

    # Send final report
    report = (
        f"✅ <b>Broadcast Complete!</b>\n\n"
        f"🟢 Delivered: <b>{success}</b>\n"
        f"🔴 Failed: <b>{failed}</b>\n"
    )
    if failed > 0:
        report += f"\n<i>Common fail reason: Bot kicked/banned from group.</i>"
        
    await query.message.reply_text(report, parse_mode="HTML", reply_markup=get_back_button())
    
    # Clean up user data
    context.user_data.clear()
    return ConversationHandler.END

async def cancel_broadcast(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if update.callback_query:
        await update.callback_query.answer()
        await update.callback_query.edit_message_text("❌ Broadcast canceled.", reply_markup=get_back_button())
    else:
        await update.message.reply_text("❌ Broadcast canceled.")
    context.user_data.clear()
    return ConversationHandler.END

# Define the Handler
broadcaster_conv_handler = ConversationHandler(
    entry_points=[
        CommandHandler('broadcast', start_broadcast),
        CallbackQueryHandler(start_broadcast, pattern='^admin_post_management$')
    ],
    states={
        SELECT_DEPT: [CallbackQueryHandler(handle_dept, pattern='^bc_dept_|^bc_cancel$')],
        SELECT_SEM: [CallbackQueryHandler(handle_sem, pattern='^bc_sem_|^bc_cancel$')],
        SELECT_CAT: [CallbackQueryHandler(handle_cat, pattern='^bc_cat_|^bc_cancel$')],
        AWAIT_MESSAGE: [MessageHandler(filters.ALL & ~filters.COMMAND, receive_message)],
        AWAIT_BUTTON: [MessageHandler(filters.TEXT & ~filters.COMMAND, receive_button)],
        CONFIRM_BROADCAST: [CallbackQueryHandler(execute_broadcast, pattern='^bc_send_|^bc_cancel$')]
    },
    fallbacks=[CommandHandler('cancel', cancel_broadcast)]
)
