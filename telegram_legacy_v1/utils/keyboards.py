from telegram import InlineKeyboardButton, InlineKeyboardMarkup

def main_menu_keyboard():
    keyboard = [
        [
            InlineKeyboardButton("👥 Users", callback_data="menu_users"),
            InlineKeyboardButton("🎓 Students", callback_data="menu_students")
        ],
        [
            InlineKeyboardButton("👨‍🏫 Teachers", callback_data="menu_teachers"),
            InlineKeyboardButton("📚 Courses", callback_data="menu_courses")
        ],
        [
            InlineKeyboardButton("📅 Academics", callback_data="menu_academics"),
            InlineKeyboardButton("💰 Fees", callback_data="menu_fees")
        ],
        [
            InlineKeyboardButton("⚙️ Settings", callback_data="menu_settings")
        ]
    ]
    return InlineKeyboardMarkup(keyboard)

def back_button_keyboard():
    keyboard = [[InlineKeyboardButton("🔙 Back to Main Menu", callback_data="menu_main")]]
    return InlineKeyboardMarkup(keyboard)

def user_action_keyboard(user_id):
    keyboard = [
        [
            InlineKeyboardButton("✅ Approve", callback_data=f"user_approve_{user_id}"),
            InlineKeyboardButton("🚫 Block", callback_data=f"user_block_{user_id}"),
        ],
        [
            InlineKeyboardButton("🗑 Delete", callback_data=f"user_delete_{user_id}")
        ],
        [
            InlineKeyboardButton("🔙 Back", callback_data="menu_users")
        ]
    ]
    return InlineKeyboardMarkup(keyboard)

def student_action_keyboard(student_id):
    keyboard = [
        [
            InlineKeyboardButton("📜 Profile", callback_data=f"st_profile_{student_id}"),
            InlineKeyboardButton("📚 Academics", callback_data=f"st_acad_{student_id}"),
        ],
        [
            InlineKeyboardButton("📅 Attendance", callback_data=f"st_att_{student_id}"),
            InlineKeyboardButton("💰 Finance", callback_data=f"st_fin_{student_id}"),
        ],
        [
            InlineKeyboardButton("✏️ Edit", callback_data=f"student_edit_{student_id}"),
            InlineKeyboardButton("🗑 Delete", callback_data=f"student_delete_{student_id}")
        ],
        [
            InlineKeyboardButton("🔙 Back", callback_data="menu_students")
        ]
    ]
    return InlineKeyboardMarkup(keyboard)

def student_academics_keyboard(student_id):
    keyboard = [
        [
            InlineKeyboardButton("📋 Enrollments", callback_data=f"st_enrolls_{student_id}"),
            InlineKeyboardButton("🎓 Results", callback_data=f"st_results_{student_id}")
        ],
        [
            InlineKeyboardButton("🕒 Timetable", callback_data=f"st_timetable_{student_id}")
        ],
        [
            InlineKeyboardButton("🔙 Back to Student", callback_data=f"st_profile_{student_id}")
        ]
    ]
    return InlineKeyboardMarkup(keyboard)
    
def student_finance_keyboard(student_id):
    keyboard = [
        [
            InlineKeyboardButton("🧾 Fee Status", callback_data=f"st_fees_{student_id}"),
            InlineKeyboardButton("💳 Record Payment", callback_data=f"st_pay_{student_id}")
        ],
        [
            InlineKeyboardButton("🔙 Back to Student", callback_data=f"st_profile_{student_id}")
        ]
    ]
    return InlineKeyboardMarkup(keyboard)

def pagination_keyboard(current_page, total_pages, callback_prefix):
    buttons = []
    if current_page > 1:
        buttons.append(InlineKeyboardButton("⬅️ Prev", callback_data=f"{callback_prefix}_page_{current_page - 1}"))
    
    buttons.append(InlineKeyboardButton(f"{current_page}/{total_pages}", callback_data="noop"))
    
    if current_page < total_pages:
        buttons.append(InlineKeyboardButton("Next ➡️", callback_data=f"{callback_prefix}_page_{current_page + 1}"))
        
    return [buttons]

def teacher_action_keyboard(teacher_id):
    keyboard = [
        [
            InlineKeyboardButton("✏️ Edit", callback_data=f"teacher_edit_{teacher_id}"),
            InlineKeyboardButton("🗑 Delete", callback_data=f"teacher_delete_{teacher_id}")
        ],
        [
            InlineKeyboardButton("🔙 Back", callback_data="menu_teachers")
        ]
    ]
    return InlineKeyboardMarkup(keyboard)

def course_action_keyboard(course_id):
    keyboard = [
        [
            InlineKeyboardButton("✏️ Edit", callback_data=f"course_edit_{course_id}"),
            InlineKeyboardButton("🗑 Delete", callback_data=f"course_delete_{course_id}")
        ],
        [
            InlineKeyboardButton("🔙 Back", callback_data="menu_courses")
        ]
    ]
    return InlineKeyboardMarkup(keyboard)
