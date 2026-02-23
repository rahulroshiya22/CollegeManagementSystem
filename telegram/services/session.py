import json
import os
from typing import Dict, Optional

SESSION_FILE = "bot_sessions.json"

class SessionManager:
    def __init__(self):
        self.sessions: Dict[int, Dict] = {}
        self.load_sessions()

    def load_sessions(self):
        if os.path.exists(SESSION_FILE):
            try:
                with open(SESSION_FILE, "r") as f:
                    self.sessions = {int(k): v for k, v in json.load(f).items()}
            except Exception as e:
                print(f"Error loading sessions: {e}")
                self.sessions = {}

    def save_sessions(self):
        try:
            with open(SESSION_FILE, "w") as f:
                json.dump(self.sessions, f)
        except Exception as e:
            print(f"Error saving sessions: {e}")

    def get_user_token(self, telegram_id: int) -> Optional[str]:
        return self.sessions.get(telegram_id, {}).get("token")

    def get_user_role(self, telegram_id: int) -> Optional[str]:
        return self.sessions.get(telegram_id, {}).get("role")

    def get_user_data(self, telegram_id: int) -> Optional[Dict]:
        return self.sessions.get(telegram_id)

    def save_user_session(self, telegram_id: int, token: str, user_data: Dict):
        self.sessions[telegram_id] = {
            "token": token,
            "role": user_data.get("role", "Student"), # Default to student if missing
            "userId": user_data.get("userId"),
            "email": user_data.get("email"),
            "name": user_data.get("firstName", "User")
        }
        self.save_sessions()

    def impersonate_user(self, admin_tg_id: int, target_user_data: Dict):
        """
        Overwrite the admin's session with the target user's details.
        """
        if admin_tg_id not in self.sessions:
            return False
            
        # We perform a "Soft Login" - we don't have their token but we can trick the bot logic?
        # Actually, `APIClient` uses the token from session. 
        # IF we don't have their Token, we can't make API calls as them (if API requires User Token).
        # HOWEVER, if the API is "Role Based" and we are Admin, maybe we can fetch their data?
        # The user said "Login As". Usually this means obtaining a token for them.
        # If we can't get their token, maybe we can just SET the role and ID, and hope the API doesn't validate the token against the user ID strictly?
        # OR, we might have an endpoint /api/auth/impersonate?
        # Assuming for now we just overwrite the local metadata and keep the ADMIN token (if backend allows admin to act as others) 
        # OR we just hope the backend doesn't check "Token User == Requested Data User".
        
        # Strategy: Overwrite Session Data. 
        # WARNING: If token is bound to Identity, this might fail API calls.
        # But let's try.
        
        current_token = self.sessions[admin_tg_id]["token"] # Keep Admin Token? Or usage requires their token?
        # If we need THEIR token, we can't do this without a backend endpoint.
        # Let's assume we update the Role and UserID to match the target.
        
        self.sessions[admin_tg_id].update({
            "role": target_user_data.get("role"),
            "userId": target_user_data.get("userId"),
            "name": target_user_data.get("firstName"),
            "email": target_user_data.get("email"),
            "is_impersonating": True # Flag to maybe show "Stop Impersonating" later
        })
        self.save_sessions()
        return True

    def clear_session(self, telegram_id: int):
        if telegram_id in self.sessions:
            del self.sessions[telegram_id]
            self.save_sessions()

    def get_telegram_id(self, user_id: int) -> Optional[int]:
        """Reverse lookup: Find Telegram ID by Backend User ID"""
        for tid, data in self.sessions.items():
            if str(data.get("userId")) == str(user_id):
                return tid
        return None

# Global Instance
session_manager = SessionManager()
