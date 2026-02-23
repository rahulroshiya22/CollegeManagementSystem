import os
from dotenv import load_dotenv

load_dotenv()

BOT_TOKEN = os.getenv("BOT_TOKEN")
API_BASE_URL = "https://localhost:7000" # Gateway URL (HTTPS)
BOT_IMAGE_URL = "https://img.freepik.com/free-vector/cute-robot-holding-phone-tablet-cartoon-vector-icon-illustration-science-technology-icon-concept_138676-2130.jpg"

# Request timeout in seconds
REQUEST_TIMEOUT = 10

# Helper to construct full API URL
def get_api_url(endpoint):
    return f"{API_BASE_URL}{endpoint}"
