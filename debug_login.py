import requests
import urllib3
import json
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

API_URL = "https://localhost:7000/api/auth/login"
CREDENTIALS = {"email": "admin@cms.com", "password": "Admin@123"}

def debug_login():
    print(f"Debugging Login to: {API_URL}")
    try:
        resp = requests.post(API_URL, json=CREDENTIALS, verify=False, timeout=10)
        print(f"Status Code: {resp.status_code}")
        print(f"Raw Response Text: {resp.text}")
        
        try:
            data = resp.json()
            print(f"Parsed JSON: {json.dumps(data, indent=2)}")
        except:
            print("Response is not JSON")
            
    except Exception as e:
        print(f"Exception: {e}")

if __name__ == "__main__":
    debug_login()
