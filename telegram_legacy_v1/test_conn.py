import requests
import urllib3
import socket

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

email = "admin@example.com"
password = "password"
payload = {"email": email, "password": password}
headers = {"Content-Type": "application/json"}

def test_url(name, url):
    print(f"\n--- Testing {name} ({url}) ---")
    try:
        resp = requests.post(url, json=payload, headers=headers, verify=False, timeout=5)
        print(f"Status: {resp.status_code}")
        if resp.status_code != 200:
            print(f"Error: {resp.text[:100]}")
    except Exception as e:
        print(f"Failed: {e}")

# Direct Auth Service Tests
test_url("Auth HTTP Localhost", "http://localhost:5007/api/auth/login")
test_url("Auth HTTPS Localhost", "https://localhost:7007/api/auth/login")
test_url("Auth HTTP 127.0.0.1", "http://127.0.0.1:5007/api/auth/login")

# Gateway Tests
test_url("Gateway HTTPS Localhost", "https://localhost:7000/api/auth/login")
test_url("Gateway HTTP Localhost", "http://localhost:5000/api/auth/login") # Just in case
