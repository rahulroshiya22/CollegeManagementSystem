import os
from dotenv import load_dotenv

load_dotenv()

API_URL = os.getenv("API_URL", "http://localhost:5000") # Default to Gateway
BOT_TOKEN = os.getenv("BOT_TOKEN")
if not BOT_TOKEN:
    print("❌ Error: BOT_TOKEN not found in .env")

# Gateway Ports mapping (if needed for direct access, but we should use Gateway)
# Student: 5001, Teacher: 5002, Course: 5003, Auth: 5000
