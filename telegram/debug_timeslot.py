import asyncio
from services.api import APIClient
from services.session import session_manager

async def test():
    # Load sessions
    if not session_manager.sessions:
        print("No sessions found in SessionManager. Please login to the bot first.")
        return

    # Use first available session
    telegram_id = list(session_manager.sessions.keys())[0]
    print(f"Using Session ID: {telegram_id}")
    
    api = APIClient(telegram_id)
    
    print("--- Searching for Kavita ---")
    teachers = api.get("/api/teacher?SearchQuery=Kavita")
    
    t_data = []
    if isinstance(teachers, list):
        t_data = teachers
    elif isinstance(teachers, dict):
        t_data = teachers.get("data") or teachers.get("items") or teachers.get("value") or []
        
    if not t_data:
        print("No teacher named Kavita found.")
        # Debug: list all teachers
        print("Fetching all teachers to check...")
        all_t = api.get("/api/teacher?PageSize=100")
        print(f"All Teachers Response Type: {type(all_t)}")
        return

    teacher = t_data[0]
    tid = teacher.get('teacherId')
    name = f"{teacher.get('firstName')} {teacher.get('lastName')}"
    print(f"Found Teacher: {name} (ID: {tid})")
    
    print(f"--- Fetching TimeSlots for ID {tid} (Raw Request) ---")
    import requests
    
    token = session_manager.get_user_token(telegram_id)
    headers = {"Authorization": f"Bearer {token}"}
    url = f"{api.base_url}/api/timeslot/teacher/{tid}"
    
    print(f"GET {url}")
    try:
        r = requests.get(url, headers=headers, verify=False)
        print(f"Status Code: {r.status_code}")
        print(f"Content: {r.text}")
    except Exception as e:
        print(f"Request Failed: {e}")

if __name__ == "__main__":
    asyncio.run(test())
