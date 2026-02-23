import requests
import json
from config import API_URL
from services.session import session_manager

class APIClient:
    def __init__(self, telegram_id: int):
        self.telegram_id = telegram_id
        self.base_url = API_URL
        self.token = session_manager.get_user_token(telegram_id)

    def _get_headers(self):
        headers = {"Content-Type": "application/json"}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        return headers

    def _handle_response(self, response, raw=False):
        """
        Smart response handler mirroring Frontend logic:
        - If 401: return None (Auth failed)
        - If 204: return None (No Content)
        - If JSON:
            - Check if it wraps data in 'data' key (Pagination) or 'value' (some .NET APIs)
            - Or return raw list/dict
        """
        if response.status_code == 401:
            # Token expired or invalid
            session_manager.clear_session(self.telegram_id)
            return {"error": "Unauthorized. Please login again."}
        
        if response.status_code == 204:
            return None

        try:
            data = response.json()
        except ValueError:
            return {"error": f"Invalid API Response: {response.text}"}

        if not response.ok:
            # Try to extract error message
            msg = data.get("message") or data.get("error") or f"Error {response.status_code}"
            return {"error": msg}

        # If raw is requested, return the full dict (metadata + data)
        if raw:
            return data

        # --- SMART UNWRAPPING (The Fix) ---
        if isinstance(data, dict):
            # Case 1: Standard Paged Response { "data": [...], "total": 10 }
            if "data" in data:
                return data["data"]
            # Case 2: .NET 8 Web API default wrapper { "value": [...], "statusCode": 200 }
            if "value" in data and isinstance(data["value"], (list, dict)):
                return data["value"]
            
        # Case 3: Direct List or Raw Dict
        return data

    import urllib3
    urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

    def get(self, endpoint: str, params=None, raw=False):
        url = f"{self.base_url}{endpoint}"
        try:
            resp = requests.get(url, headers=self._get_headers(), params=params, timeout=10, verify=False)
            return self._handle_response(resp, raw=raw)
        except requests.exceptions.RequestException as e:
            return {"error": f"Connection Failed: {str(e)}"}

    def post(self, endpoint: str, payload: dict):
        url = f"{self.base_url}{endpoint}"
        print(f"POST {url} | Payload: {payload}")
        try:
            resp = requests.post(url, headers=self._get_headers(), json=payload, timeout=10, verify=False)
            print(f"Status: {resp.status_code} | Response: {resp.text[:200]}")
            return self._handle_response(resp)
        except requests.exceptions.RequestException as e:
            print(f"Connection Error: {e}")
            return {"error": f"Connection Failed: {str(e)}"}

    def put(self, endpoint: str, payload: dict):
        url = f"{self.base_url}{endpoint}"
        try:
            resp = requests.put(url, headers=self._get_headers(), json=payload, timeout=10, verify=False)
            return self._handle_response(resp)
        except requests.exceptions.RequestException as e:
            return {"error": f"Connection Failed: {str(e)}"}

    def delete(self, endpoint: str):
        url = f"{self.base_url}{endpoint}"
        try:
            resp = requests.delete(url, headers=self._get_headers(), timeout=10, verify=False)
            return self._handle_response(resp)
        except requests.exceptions.RequestException as e:
            return {"error": f"Connection Failed: {str(e)}"}
