import json
import os
import logging

DATA_DIR = os.path.join(os.path.dirname(os.path.dirname(__file__)), 'data')
DB_FILE = os.path.join(DATA_DIR, 'registered_groups.json')
# We need a new file to track ALL chats the bot is currently inside
TRACK_FILE = os.path.join(DATA_DIR, 'tracked_chats.json')

def _ensure_db_exists():
    if not os.path.exists(DATA_DIR):
        os.makedirs(DATA_DIR)
    if not os.path.exists(DB_FILE):
        with open(DB_FILE, 'w') as f: json.dump({}, f)
    if not os.path.exists(TRACK_FILE):
        with open(TRACK_FILE, 'w') as f: json.dump({}, f)

# --- Registered Groups Logic ---
def get_all_groups() -> dict:
    _ensure_db_exists()
    try:
        with open(DB_FILE, 'r') as f: return json.load(f)
    except: return {}

def save_group(chat_id: str, title: str, department: str, semester: str, category: str, added_by: int):
    groups = get_all_groups()
    groups[str(chat_id)] = {
        "title": title,
        "department": department,
        "semester": semester,
        "category": category,
        "added_by": added_by
    }
    with open(DB_FILE, 'w') as f: json.dump(groups, f, indent=4)
    return True

def get_groups_by_filter(department: str = "All", semester: str = "All", category: str = "All") -> list:
    groups = get_all_groups()
    matched = []
    for chat_id, data in groups.items():
        if (department == "All" or data.get("department") in ["All", department]) and \
           (semester == "All" or data.get("semester") in ["All", semester]) and \
           (category == "All" or data.get("category") in ["All", category]):
            matched.append(chat_id)
    return matched

def delete_group(chat_id: str):
    groups = get_all_groups()
    if str(chat_id) in groups:
        del groups[str(chat_id)]
        with open(DB_FILE, 'w') as f: json.dump(groups, f, indent=4)

# --- Chat Tracking Logic (For Inline Selection) ---
def get_tracked_chats() -> dict:
    _ensure_db_exists()
    try:
        with open(TRACK_FILE, 'r') as f: return json.load(f)
    except: return {}

def track_chat(chat_id: str, title: str, chat_type: str):
    """Saves a chat when the bot is added to it."""
    chats = get_tracked_chats()
    chats[str(chat_id)] = {"title": title, "type": chat_type}
    with open(TRACK_FILE, 'w') as f: json.dump(chats, f, indent=4)

def untrack_chat(chat_id: str):
    """Removes a chat if the bot is kicked."""
    chats = get_tracked_chats()
    if str(chat_id) in chats:
        del chats[str(chat_id)]
        with open(TRACK_FILE, 'w') as f: json.dump(chats, f, indent=4)
