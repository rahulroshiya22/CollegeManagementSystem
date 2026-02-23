import requests
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

API_URL = "https://localhost:7000/api"
# API_URL = "http://localhost:5000/api"

CREDENTIALS = [
    {"email": "admin@cms.com", "password": "Admin@123", "role": "Admin"},
    {"email": "priya.sharma@cms.com", "password": "Teacher@123", "role": "Teacher"},
    {"email": "student@cms.com", "password": "Student@123", "role": "Student"}
]

def test_login_and_fetch():
    print(f"Testing API Connection to: {API_URL}")
    
    for user in CREDENTIALS:
        print(f"\n--- Testing {user['role']} Login ({user['email']}) ---")
        try:
            # 1. Login
            login_payload = {"email": user['email'], "password": user['password']}
            resp = requests.post(f"{API_URL}/auth/login", json=login_payload, verify=False, timeout=5)
            
            if resp.status_code != 200:
                print(f"Login Failed: {resp.status_code} {resp.text}")
                continue
                
            data = resp.json()
            print(f"Response JSON: {data}")
            
            # Smart unwrapping simulation
            if "data" in data: data = data["data"]
            if "value" in data: data = data["value"]
            
            token = data.get("token")
            print(f"Login Successful! Token len: {len(token)}")
            
            headers = {"Authorization": f"Bearer {token}"}
            
            # 2. Test Role-Specific Endpoints
            if user['role'] == 'Admin':
                # Test Users List
                print("   > Fetching Users List...")
                users_resp = requests.get(f"{API_URL}/admin/users", headers=headers, verify=False)
                if users_resp.status_code == 200:
                    count = len(users_resp.json())
                    print(f"   Success! Found {count} users.")
                else:
                    print(f"   Failed: {users_resp.status_code} {users_resp.text}")
                    
            elif user['role'] == 'Teacher':
                # Test Teacher Profile or Classes
                # Assuming /teacher/me or similar based on whatever we find in Ocelot
                pass 

            elif user['role'] == 'Student':
                # Test Student Profile
                pass
                
        except Exception as e:
            print(f"Exception: {e}")

if __name__ == "__main__":
    test_login_and_fetch()
