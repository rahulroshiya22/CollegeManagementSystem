from telegram import Update
from telegram.ext import ContextTypes, CallbackQueryHandler
from services.api_client import api_client
from utils.keyboards import user_action_keyboard, back_button_keyboard

async def list_users(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Fetches and displays a list of users."""
    query = update.callback_query
    
    # Fetch users from API
    response = api_client.get("/api/admin/users")
    
    if "error" in response:
        await query.edit_message_text(f"❌ Error: {response['error']}")
        return

    users = response
    if not users:
        await query.edit_message_text("No users found.", reply_markup=back_button_keyboard())
        return

    # Show first 5 users (Pagination can be added later)
    message_text = "👥 **User List**\n\n"
    for user in users[:5]:
        status = "Active" if user.get("isActive") else "Inactive"
        message_text += f"👤 {user.get('firstName')} {user.get('lastName')} ({user.get('role')})\n"
        message_text += f"📧 {user.get('email')} - {status}\n"
        message_text += f"🆔 {user.get('userId')}\n\n"
        
    # For simplicity in this demo, we add actions for the first user if available
    if users:
        first_user = users[0]
        message_text += f"👇 **Actions for {first_user.get('firstName')}**:"
        keyboard = user_action_keyboard(first_user.get("userId"))
    else:
        keyboard = back_button_keyboard()

    await query.edit_message_text(text=message_text, reply_markup=keyboard, parse_mode="Markdown")

async def user_action_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Handles Approve/Block/Delete actions."""
    query = update.callback_query
    data = query.data
    
    # Parse action and user_id (e.g., user_approve_123)
    parts = data.split("_")
    action = parts[1]
    user_id = parts[2]
    
    if action == "approve":
        # Call API to approve (update role/status)
        # Note: API might differ, adjusting to common pattern
        response = api_client.put(f"/api/admin/users/{user_id}/role", {"role": "Student"}) # Defaulting to Student for now
        if "error" in response:
             await query.answer(f"❌ Failed: {response['error']}")
        else:
             await query.answer("✅ User Approved")
             
    elif action == "block":
        response = api_client.put(f"/api/admin/users/{user_id}/status", {"isActive": False})
        if "error" in response:
             await query.answer(f"❌ Failed: {response['error']}")
        else:
             await query.answer("🚫 User Blocked")
             
    elif action == "delete":
        response = api_client.delete(f"/api/admin/users/{user_id}")
        if "error" in response:
             await query.answer(f"❌ Failed: {response['error']}")
        else:
             await query.answer("🗑 User Deleted")

    # Refresh list or return to menu
    # await list_users(update, context) 
