# 🤖 CMS Telegram Bot Service - Developer Deep Dive

Welcome to the **College Management System (CMS) Telegram Bot**! This service provides a fully operational Telegram interface for Students, Teachers, and Administrators to interact directly with the main CMS application from their phones.

This README is a **detailed, step-by-step technical guide** designed specifically for computer science and engineering students. It explains exactly how this application is structured, the design patterns used, and how everything connects under the hood.

---

## 🛠️ 1. Tools, Libraries, & Technologies

Before diving into the code, you need to know the core tools powering this system:

*   **Language & Framework**: C# on [.NET 8.0 SDK Web](https://dotnet.microsoft.com/). It's fast, modern, and runs everywhere.
*   **Telegram.Bot (v22.3.0)**: The official C# NuGet package for the Telegram Bot API. It handles all the networking complexity of communicating with Telegram servers.
*   **System.Text.Json**: Used heavily to parse JSON responses from our main backend API.
*   **Dependency Injection (DI)**: A design pattern used heavily in .NET to manage object creation. Instead of using `new Service()`, .NET automatically provides services to our classes.
*   **State Machine / Session Management**: Since HTTP API and Telegram messages are "stateless," we created a custom in-memory `SessionService` to track where a user is in a conversation.

---

## 🏗️ 2. Architectural Flow: The "Middleman" Pattern

This bot does **NOT** contain a database connection using SQL or Entity Framework. Why? 
Because our main Web API already does that! The Telegram Bot acts purely as a **Middleman** (or API Client).

**The exact data flow:**
1. **User** taps a button on Telegram (e.g., "View Attendance").
2. **Telegram Servers** send a JSON `Update` to our Bot.
3. **Our Bot** uses `HttpClient` to send a GET request to the main CMS Backend API (e.g., `https://localhost:7000/api/attendance`).
4. **Backend API** checks the database and returns JSON data.
5. **Our Bot** parses this JSON, formats it beautifully with HTML and emojis, and posts it back to Telegram.

---

## 📂 3. Step-by-Step Codebase Breakdown

Let's walk through the actual codebase, file by file.

### Step A: The Entry Point (`Program.cs`)
In .NET 8 Web Applications, `Program.cs` is where the app starts.

**What it does:**
1. **Loads Config**: Reads the `BotToken` from `appsettings.json`.
2. **Setup Dependency Injection**: 
   ```csharp
   builder.Services.AddSingleton<SessionService>();
   builder.Services.AddSingleton<ApiService>();
   builder.Services.AddSingleton<UpdateHandler>();
   // Registers dozens of specific Handlers like:
   builder.Services.AddSingleton<CMS.TelegramService.Handlers.Admin.StudentsHandler>();
   ```
   *Why `AddSingleton`?* Because we only need ONE instance of these handlers alive for the whole lifetime of the application.
3. **Start Long Polling**: 
   Standard web apps wait for HTTP requests (Webhooks). Here, we use **Long Polling**.
   ```csharp
   botClient.StartReceiving(
       updateHandler: async (bot, update, ct) => { await updateHandler.HandleUpdateAsync(update); },
       errorHandler: (bot, ex, src, ct) => { /* handle errors */ },
       cancellationToken: cts.Token
   );
   ```
   *Long Polling* means the bot constantly keeps a connection open to Telegram. If a user sends a message, Telegram pushes it instantly down this open pipe.

### Step B: The Traffic Cop (`UpdateHandler.cs`)
Every single message, button click, or image sent to the bot arrives here.

**What it does:**
1. **Checks the Type**: Is this a text message (`UpdateType.Message`) or an inline button click (`UpdateType.CallbackQuery`)?
2. **Routing Messages**: If a user types `/start`, it sends them to the `AuthHandler`. If the user has a "State" (meaning they are in the middle of a process like filling out a form), it routes them based on their state prefix.
   ```csharp
   // Example of routing based on state:
   if (state.StartsWith("student_adm_")) await _adminStudents.HandleState(msg, state);
   else if (state.StartsWith("fee_")) await _adminFees.HandleState(msg, state);
   ```
3. **Routing Callbacks (Button Clicks)**: Telegram buttons send hidden string data. We check the prefix of this data.
   ```csharp
   // Example of routing an inline keyboard tap:
   if (data.StartsWith("admin_students")) await _adminStudents.HandleCallback(query);
   ```

### Step C: Connecting to the API (`Services/ApiService.cs`)
This file is the bridge to the main database backend.

**What it does:**
1. **Wraps HttpClient**: It encapsulates `IHttpClientFactory`.
2. **JWT Injection**: Every time the bot asks the API for data, it grabs the user's JWT Token from `SessionService` and injects it into the HTTP Header.
   ```csharp
   var token = _sessions.GetToken(telegramId);
   if (!string.IsNullOrEmpty(token))
       client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
   ```
3. **Error Handling**: If the API returns `401 Unauthorized`, it automatically logs the user out and tells them to type `/start` again.

### Step D: State & Session Management (`Services/SessionService.cs`)
If a teacher wants to add marks, it takes multiple steps (Select Course -> Select Student -> Enter Marks).
Because Telegram is stateless, the bot stores this progress in memory using a simple `Dictionary<long, UserSession>`.

A `UserSession` contains:
*   `Email`, `Token`, `Role` (Student/Teacher/Admin).
*   `State`: A string like `"tch_exam_waiting_marks"`.
*   `TempData`: A dictionary to temporarily hold data between steps (e.g., storing the selected Student ID while waiting for the user to type the marks).

### Step E: The Visuals (`Handlers/MenuHandler.cs`)
This is where the magic of the UI happens. Telegram allows rich text formatting using `ParseMode.Html` and `InlineKeyboardMarkup`.

**What it does:**
1. **Dashboard UI**: If the user is an an Admin, it calls 4 different APIs (Students, Teachers, Courses, Fees) to gather live statistics, formats them beautifully using emojis and HTML (`<b>`, `<i>`, `<code>`), and attaches an image.
2. **Dynamic Keyboards**: It generates layout grids of buttons based on the user's role.
   ```csharp
   // Example of creating an inline button:
   InlineKeyboardButton.WithCallbackData("👥 Students", "admin_students")
   // If a user clicks this, Telegram fires a callback with data = "admin_students"
   ```

### Step F: Specific Feature Handlers (`Handlers/Admin`, `Handlers/Student`...)
To prevent the codebase from becoming messy (spaghetti code), each feature has its own file.
For example, `AttendanceHandler.cs` knows *only* how to show attendance menus, fetch attendance data via `ApiService`, and display it.

---

## 🚀 4. Putting It All Together: A Real-World Scenario

Let's trace exactly what happens when a **Student clicks the "My Profile" button**.

1. **User Action**: The student taps the "👤 My Profile" inline button.
2. **Telegram**: Sends an `Update` object to the bot. Inside, `Update.CallbackQuery.Data` is equal to the string `"student_profile"`.
3. **UpdateHandler.cs**: Receives the update, looks at the string.
   ```csharp
   else if (data == "student_profile") await _studentProfile.HandleCallback(query);
   ```
4. **StudentProfileHandler.cs**: The `HandleCallback` method triggers. It grabs the user's Telegram ID.
5. **SessionService**: Retrieves the `UserSession` to find out who this actually is.
6. **ApiService**: (Optional) Makes a GET request to `https://localhost:7000/api/student/profile` using the student's JWT token.
7. **Telegram.Bot**: The bot formats the response text with HTML and an "🔙 Back" button.
   ```csharp
   await _bot.SendMessage(chatId, "👤 <b>My Profile</b>\nName: ...", parseMode: ParseMode.Html, replyMarkup: backBtn);
   ```
8. **User Action**: The student instantly sees their profile appear on screen!

---

## ⚙️ 5. Setting Up Core Infrastructure Locally

To run this yourself and experiment with the code:

1. **Get a Telegram Bot Token**: 
   * Open Telegram and search for the user `@BotFather`.
   * Type `/newbot`, give it a name, and copy the **HTTP API Token**.
2. **Configure your App**: 
   Create or edit `Backend/CMS.TelegramService/appsettings.json`:
    ```json
    {
      "BotConfiguration": {
        "BotToken": "123456789:YOUR_SECRET_TOKEN_HERE",
        "ApiBaseUrl": "https://localhost:7000" 
      }
    }
    ```
3. **Run the API Backend**: Ensure the main Database REST API is running (usually on port 7000).
4. **Start the Bot Application**:
    ```bash
    cd Backend/CMS.TelegramService
    dotnet run
    ```
5. **Test It Out**: Open Telegram on your phone or PC, find your newly created bot, and type `/start`.

---
*Happy coding! This architecture of cleanly separated Handlers and injected API Services is a standard industry practice you will use throughout your software engineering career.*
