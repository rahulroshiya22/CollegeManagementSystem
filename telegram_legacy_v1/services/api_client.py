import requests
import urllib3
from config import API_BASE_URL, REQUEST_TIMEOUT

# Disable SSL warnings for localhost dev environment
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

class APIClient:
    def __init__(self):
        self.token = None
        self.headers = {"Content-Type": "application/json"}

    def login(self, email, password):
        """Authenticates user and stores JWT."""
        url = f"{API_BASE_URL}/api/auth/login"
        payload = {"email": email, "password": password}
        
        print(f"DEBUG: Attempting login to {url} for {email}") # DEBUG
        try:
            # verify=False to allow self-signed certs on localhost
            print("DEBUG: Sending request...") # DEBUG
            response = requests.post(url, json=payload, headers=self.headers, timeout=REQUEST_TIMEOUT, verify=False)
            print(f"DEBUG: Response received: {response.status_code}") # DEBUG
            
            if response.status_code == 200:
                data = response.json()
                self.token = data.get("token")
                self.headers["Authorization"] = f"Bearer {self.token}"
                print("DEBUG: Login successful breakdown") # DEBUG
                return True, "Login Successful"
            else:
                print(f"DEBUG: Login failed. output: {response.text}") # DEBUG
                return False, f"Error {response.status_code}: {response.text}"
        except Exception as e:
            print(f"DEBUG: Exception during login: {e}") # DEBUG
            return False, str(e)

    def get(self, endpoint):
        """Generic GET request."""
        if not self.token: return {"error": "Not authenticated"}
        try:
            response = requests.get(f"{API_BASE_URL}{endpoint}", headers=self.headers, timeout=REQUEST_TIMEOUT, verify=False)
            if response.status_code == 200:
                return response.json()
            return {"status": response.status_code, "text": response.text}
        except Exception as e:
            return {"error": str(e)}

    # Same pattern for POST/PUT/DELETE...
    def post(self, endpoint, data):
        if not self.token: return {"error": "Not authenticated"}
        try:
            response = requests.post(f"{API_BASE_URL}{endpoint}", json=data, headers=self.headers, timeout=REQUEST_TIMEOUT, verify=False)
            if response.status_code in [200, 201]:
                return response.json()
            return {"error": f"{response.status_code}: {response.text}"}
        except Exception as e:
            return {"error": str(e)}

    def put(self, endpoint, data):
        if not self.token: return {"error": "Not authenticated"}
        try:
            response = requests.put(f"{API_BASE_URL}{endpoint}", json=data, headers=self.headers, timeout=REQUEST_TIMEOUT, verify=False)
            if response.status_code == 200:
                return response.json()
            return {"error": f"{response.status_code}: {response.text}"}
        except Exception as e:
            return {"error": str(e)}

    def delete(self, endpoint):
        if not self.token: return {"error": "Not authenticated"}
        try:
            response = requests.delete(f"{API_BASE_URL}{endpoint}", headers=self.headers, timeout=REQUEST_TIMEOUT, verify=False)
            if response.status_code in [200, 204]:
                return {"success": True}
            return {"error": f"{response.status_code}: {response.text}"}
        except Exception as e:
            return {"error": str(e)}

# Global instance
api_client = APIClient()
