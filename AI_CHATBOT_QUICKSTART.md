# 🚀 Quick Start Guide - AI Chatbot

**IMPORTANT: You must add your Google Gemini API key before using the chatbot!**

## Step 1: Get Your FREE API Key

1. Go to: **https://ai.google.dev/**
2. Click **"Get API Key in Google AI Studio"**
3. Sign in with your Google account
4. Click **"Create API Key"**
5. Copy the key (looks like: `AIzaSy...`)

## Step 2: Add API Key to Configuration

Open this file:
```
d:\ADV DOT NET ums  project\CollegeManagementSystem\Backend\CMS.AIAssistantService\appsettings.json
```

Replace `YOUR_GEMINI_API_KEY_HERE` with your actual API key:

```json
{
  "GeminiAI": {
    "ApiKey": "AIzaSy...YOUR_ACTUAL_KEY"
  }
}
```

**Save the file!**

## Step 3: Run the Project

Run the PowerShell script (it now includes the AI service):

```powershell
cd "d:\ADV DOT NET ums  project\CollegeManagementSystem"
.\StartAllServices.ps1
```

This will start **7 services** (including the new AI chatbot on port 7006).

## Step 4: Use the Chatbot

1. The **Dashboard.html** will auto-open
2. Look for the **🤖 floating button** in the bottom-right corner
3. Click it to open the chat window
4. Start chatting!

## Try These Questions:

- "Hello!"
- "What can you help me with?"
- "What's my attendance?"
- "How much do I owe in fees?"
- "Show me available courses"

## Troubleshooting

**If chatbot doesn't respond:**
1. Check that API key is correctly added (Step 2)
2. Verify all 7 services are running (check PowerShell windows)
3. Check browser console for errors (F12)
4. Ensure you have internet connection (Gemini API requires it)

**If build fails:**
```powershell
cd Backend\CMS.AIAssistantService
dotnet restore
dotnet build
```

## What's Next?

- Add real student authentication
- Enhance service integration to show actual data
- Add more intents (assignments, grades, etc.)
- Implement voice input
- Add multi-language support

---

**Enjoy your AI assistant! 🎉**

Built by **Rahul Roshiya** with ❤️
