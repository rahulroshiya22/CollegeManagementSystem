import requests
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

API_URL = "http://localhost:5000/api/auth"
SYSTEM_ADMIN_EMAIL = "admin@cms.com"
SYSTEM_ADMIN_PASS = "Admin@123"

NEW_USER_EMAIL = "admin@test.com"
NEW_USER_PASS = "admin123"

def fix_issue():
    print(f"Fixing Issue: Creating {NEW_USER_EMAIL}...")

    # 1. Login as System Admin
    print("1. Logging in as System Admin...")
    resp = requests.post(f"{API_URL}/login", json={"email": SYSTEM_ADMIN_EMAIL, "password": SYSTEM_ADMIN_PASS}, verify=False)
    if resp.status_code != 200:
        print(f"Failed to login as System Admin: {resp.text}")
        return
    
    admin_token = resp.json().get("token")
    print("System Admin Logged In.")

    # 2. Register New User
    print(f"2. Registering {NEW_USER_EMAIL}...")
    reg_payload = {
        "email": NEW_USER_EMAIL,
        "password": NEW_USER_PASS,
        "firstName": "Test",
        "lastName": "Admin",
        "role": "Student" # Default register role, will upgrade later
    }
    resp = requests.post(f"{API_URL}/register", json=reg_payload, verify=False)
    
    new_user_id = 0
    if resp.status_code == 200:
        new_user_id = resp.json().get("userId")
        print(f"User Registered (ID: {new_user_id})")
    elif "Email already exists" in resp.text:
        print("User already exists. Finding ID...")
        # Need to find ID to promote. 
        # Since we are admin, we can list all users.
        users_resp = requests.get(f"http://localhost:5000/api/admin/users", headers={"Authorization": f"Bearer {admin_token}"}, verify=False)
        if users_resp.status_code == 200:
            users = users_resp.json()
            for u in users:
                if u["email"] == NEW_USER_EMAIL:
                    new_user_id = u["userId"]
                    print(f"Found existing user ID: {new_user_id}")
                    break
    else:
        print(f"Registration Failed: {resp.text}")
        return

    if not new_user_id:
        print("Could not determine User ID.")
        return

    # 3. Promote to Admin
    print(f"3. Promoting User {new_user_id} to Admin...")
    # AdminController Route: [HttpPut("users/{id}/role")]
    promote_resp = requests.put(
        f"http://localhost:5000/api/admin/users/{new_user_id}/role", 
        json={"role": "Admin"}, 
        headers={"Authorization": f"Bearer {admin_token}"}, 
        verify=False
    )

    if promote_resp.status_code == 200:
        print("SUCCESS! User promoted to Admin.")
        print(f"You can now login with: {NEW_USER_EMAIL} / {NEW_USER_PASS}")
    else:
        print(f"Promotion Failed: {promote_resp.status_code} {promote_resp.text}")

if __name__ == "__main__":
    fix_issue()
