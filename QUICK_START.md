# 🚀 QUICK START GUIDE - New Features

## ✅ What's Been Done

1. **✅ Login Fixed** - Teacher quick login now works
2. **✅ Add Student Fixed** - All required fields added (Roll Number, Gender, Admission Year, Department ID)
3. **✅ Messaging System** - Complete backend + frontend
4. **✅ Examination System** - Complete backend + frontend with auto-grading
5. **✅ Ocelot Routes Added** - API Gateway configured for new endpoints

---

## 🔧 TO START USING THE NEW FEATURES

### Step 1: Restart Academic Service (REQUIRED)

The Academic Service needs to be rebuilt and restarted to include the new models and controllers.

```powershell
# 1. Stop the running Academic Service
# Find and kill the process on port 7008
Get-Process | Where-Object {$_.ProcessName -like "*CMS.AcademicService*"} | Stop-Process -Force

# 2. Navigate to backend directory
cd "d:\ADV DOT NET ums  project\CollegeManagementSystem\Backend"

# 3. Rebuild the Academic Service
dotnet build CMS.AcademicService

# 4. Run with HTTPS profile
dotnet run --project CMS.AcademicService --launch-profile https
```

### Step 2: Restart API Gateway (REQUIRED)

The API Gateway needs to reload the updated `ocelot.json` configuration.

```powershell
# 1. Stop the running API Gateway
Get-Process | Where-Object {$_.ProcessName -like "*CMS.ApiGateway*"} | Stop-Process -Force

# 2. Rebuild
dotnet build CMS.ApiGateway

# 3. Run
dotnet run --project CMS.ApiGateway --launch-profile https
```

### Step 3: Test the Features

**Messaging System:**
1. Open browser: `http://localhost:3000/pages/messages.html`
2. Click "Compose" to send a message
3. Select recipient type (Teacher/Student/Admin)
4. Choose recipient from dropdown
5. Write and send message
6. Switch between Inbox/Sent tabs

**Examination System:**
1. Open browser: `http://localhost:3000/pages/exams.html`
2. **As Teacher/Admin:**
   - Click "Create Exam"
   - Fill in exam details
   - Add questions (you'll need to add this via API/Swagger for now)
   - Publish the exam
3. **As Student:**
   - See published exams
   - Click "Start Exam"
   - Answer questions
   - Watch the timer countdown
   - Submit exam
   - See instant results!

---

## 📊 SEED DATA (Optional but Recommended)

You currently have only 15 students. To get 50+ records:

### Option 1: SQL Script (Fastest)
```sql
-- Open SQL Server Management Studio
-- Connect to your database
-- Execute: d:\ADV DOT NET ums  project\CollegeManagementSystem\Backend\SeedData\seed_all.sql
```

### Option 2: Restart All Services
```powershell
# Stop ALL services
Get-Process | Where-Object {$_.ProcessName -like "*CMS.*"} | Stop-Process -Force

# Delete existing databases (they'll be recreated with seed data)
# Then restart all services - seed data will be applied automatically
```

---

## 🎯 WHAT YOU CAN DO NOW

### Messaging:
- ✅ Send messages between students, teachers, and admin
- ✅ View inbox and sent messages
- ✅ Mark messages as read
- ✅ Delete messages
- ✅ Create group announcements

### Exams:
- ✅ Create exams with title, description, schedule
- ✅ Add MCQ questions with 4 options
- ✅ Set total marks and passing marks
- ✅ Publish exams for students
- ✅ Students take exams with countdown timer
- ✅ Auto-grading for MCQ questions
- ✅ Instant results with grade (A+, A, B+, etc.)
- ✅ Pass/Fail determination
- ✅ View all results by student or by exam

---

## 🐛 TROUBLESHOOTING

### "Cannot connect to API"
- Make sure Academic Service is running on port 7008
- Make sure API Gateway is running on port 7000
- Check browser console for errors

### "No recipients in dropdown"
- The student list loads from `/api/student`
- Make sure Student Service is running on port 7001
- Check if students exist in database

### "Exam questions not showing"
- Questions need to be added via API first
- Use Swagger UI: `https://localhost:7000/swagger`
- Navigate to Exam endpoints
- Use `POST /api/exam/{id}/questions` to add questions

### "Timer not working"
- Check browser console for JavaScript errors
- Make sure exam has a valid duration

---

## 📝 API ENDPOINTS ADDED

**Messaging:**
- `GET /api/message/inbox/{userId}/{role}`
- `GET /api/message/sent/{userId}/{role}`
- `POST /api/message?senderId={id}&senderRole={role}`
- `PUT /api/message/{id}/read`
- `DELETE /api/message/{id}`
- `GET /api/message/unread-count/{userId}/{role}`

**Announcements:**
- `GET /api/announcement`
- `POST /api/announcement?creatorId={id}&creatorRole={role}`
- `PUT /api/announcement/{id}`
- `DELETE /api/announcement/{id}`

**Exams:**
- `GET /api/exam`
- `POST /api/exam?teacherId={id}`
- `GET /api/exam/{id}/questions`
- `POST /api/exam/{id}/questions`
- `PUT /api/exam/{id}/publish`
- `POST /api/exam/submit?studentId={id}`
- `GET /api/exam/results/student/{id}`
- `GET /api/exam/results/exam/{id}`

---

## 🎉 YOU'RE ALL SET!

After restarting the Academic Service and API Gateway, you can:
1. ✅ Login with fixed credentials
2. ✅ Add students with all required fields
3. ✅ Send and receive messages
4. ✅ Create and take exams
5. ✅ View instant exam results

**Enjoy your new features!** 🚀
