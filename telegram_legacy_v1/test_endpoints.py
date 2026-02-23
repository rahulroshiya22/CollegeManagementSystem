import requests
import urllib3
import json

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# Config
BASE_URL = "https://localhost:7000"
headers = {"Content-Type": "application/json"}

# 1. Login to get Token
print("--- Logging in ---")
try:
    auth_resp = requests.post(
        f"{BASE_URL}/api/auth/login", 
        json={"email": "admin@example.com", "password": "password"}, 
        verify=False, timeout=10
    )
    if auth_resp.status_code == 200:
        token = auth_resp.json().get("token")
        headers["Authorization"] = f"Bearer {token}"
        print("✅ Login Successful")
    else:
        print(f"❌ Login Failed: {auth_resp.status_code} {auth_resp.text}")
        exit()
except Exception as e:
    print(f"❌ Login Exception: {e}")
    exit()

# Helper
def test_get(endpoint):
    print(f"\n--- Testing GET {endpoint} ---")
    try:
        resp = requests.get(f"{BASE_URL}{endpoint}", headers=headers, verify=False, timeout=10)
        print(f"Status: {resp.status_code}")
        if resp.status_code == 200:
            data = resp.json()
            # Print type and short preview
            print(f"Type: {type(data)}")
            if isinstance(data, dict):
                print(f"Keys: {list(data.keys())}")
            elif isinstance(data, list):
                print(f"Count: {len(data)}")
                if data: print(f"First Item: {data[0]}")
            else:
                print(f"Data: {data}")
        else:
            print(f"Error: {resp.text[:200]}")
    except Exception as e:
        print(f"Failed: {e}")

# 2. Test Endpoints
test_get("/api/student?Page=1&PageSize=5") # Check structure (dict vs list)
test_get("/api/course")
test_get("/api/teacher")
test_get("/api/department")
test_get("/api/announcement")
