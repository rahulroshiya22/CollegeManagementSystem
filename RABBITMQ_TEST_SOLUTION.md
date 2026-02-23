# 🔍 RabbitMQ Error Test & Solution Summary

## 📊 Current Configuration Status

### ✅ What's Working:
1. **RabbitMQ.Client Version**: 7.2.0 (correct)
2. **Connection String**: Properly configured in both services
3. **Code Structure**: Uses modern async API (`IChannel` instead of `IModel`)
4. **Project References**: Both services reference `CMS.Common.Messaging`

### 📝 Configuration Files Checked:

**EnrollmentService/appsettings.json:**
```json
{
  "CloudAMQP": {
    "ConnectionString": "amqps://jxlnlvrr:aS15sCekjI13tw8SC4jEEuYi_cIq4ENS@duck.lmq.cloudamqp.com/jxlnlvrr"
  }
}
```

**FeeService/appsettings.json:**
```json
{
  "CloudAMQP": {
    "ConnectionString": "amqps://jxlnlvrr:aS15sCekjI13tw8SC4jEEuYi_cIq4ENS@duck.lmq.cloudamqp.com/jxlnlvrr"
  }
}
```

✅ Both have **identical** connection strings ✅

---

## 🎯 Most Common RabbitMQ Errors & Solutions

### Error 1: "BrokerUnreachableException"
**Cause:** Cannot connect to CloudAMQP
**Solutions:**
1. Check if CloudAMQP instance is active: https://customer.cloudamqp.com/
2. Verify internet connection
3. Check firewall (port 5671)

### Error 2: Messages Not Being Consumed
**Cause:** Queue doesn't exist or subscriber not running
**Solution:**
```
1. Stop all services
2. Start FeeService FIRST (creates queue)
3. Start EnrollmentService
4. Test enrollment creation
```

### Error 3: "Queue doesn't exist"
**Solution:** Restart FeeService - it auto-creates the queue on startup

---

## 🧪 Step-by-Step Testing Guide

### Test 1: Check Service Startup Order
```powershell
# IMPORTANT: Start in this order!
1. Start FeeService first (subscriber/consumer)
   - Check console for: "Application started"
   - This creates the RabbitMQ queue

2. Start EnrollmentService (publisher)
   - Check console for: "Application started"

3. Start API Gateway
```

### Test 2: Create Test Enrollment
1. Open Swagger: `https://localhost:7000/swagger`
2. Find `POST /api/Enrollment`
3. Use this test data:
```json
{
  "studentId": 1,
  "courseId": 1,
  "semester": 1,
  "year": 2024,
  "enrollmentDate": "2024-01-15"
}
```

### Test 3: Verify Message Flow
**Check EnrollmentService Console:**
Should see:
```
---> Published to student-enrolled: {"EnrollmentId":...,"StudentId":1,"CourseId":1,...}
```

**Check FeeService Console:**
Should see:
```
---> Received from student-enrolled: {"StudentId":1,"CourseId":1,"Semester":1,"Year":2024}
---> Fee auto-generated for Student 1: $5000
```

### Test 4: Verify in Database
```sql
-- Check if fee was created
SELECT * FROM Fees WHERE StudentId = 1
```

---

## 🔧 Troubleshooting Commands

### Check RabbitMQ Package Version
```powershell
cd "d:\ADV DOT NET ums  project\CollegeManagementSystem\Backend"
Get-ChildItem -Recurse -Filter "*.csproj" | Select-String "RabbitMQ.Client"
```

### Rebuild All Projects
```powershell
cd "d:\ADV DOT NET ums  project\CollegeManagementSystem"
dotnet clean
dotnet build
```

### Check CloudAMQP Dashboard
1. Go to: https://customer.cloudamqp.com/
2. Login and select instance
3. Click **"RabbitMQ Manager"**
4. Check **Queues** tab for `student-enrolled` exchange

---

## 📋 Pre-flight Checklist

Before running the application:
- [ ] CloudAMQP instance is active
- [ ] Both `appsettings.json` have same connection string
- [ ] Student and Course exist in database (IDs 1 and 1)
- [ ] FeeService database `clgmansys4` exists
- [ ] EnrollmentService database `clgmansys3` exists

---

## 🎬 Quick Demo Script

1. **Setup** (30 seconds)
   - Start FeeService
   - Start EnrollmentService
   - Open CloudAMQP Manager → Queues tab

2. **Demo** (2 minutes)
   - Show empty queue (0 messages)
   - Create enrollment via Swagger
   - Show message flow in CloudAMQP (0→1→0)
   - Query fees to show auto-created record

3. **Explain** (1 minute)
   - Event-driven architecture
   - Loose coupling
   - Async processing
   - Message persistence

---

## 🚨 If You See Errors

### Connection Error in Console
```
Error: BrokerUnreachableException: None of the specified endpoints were reachable
```
**Solution:** 
1. Check CloudAMQP status
2. Verify connection string
3. Test with `RabbitMQ_Connection_Test.cs` file

### No Message Received
```
EnrollmentService: ---> Published to student-enrolled: {...}
FeeService: (nothing)
```
**Solution:**
1. Restart FeeService
2. Check if queue exists in CloudAMQP dashboard
3. Verify FeeService console shows "Application started"

### Database Error in FeeService
```
Error processing enrollment event: Cannot open database "clgmansys4"
```
**Solution:**
```powershell
cd Backend/CMS.FeeService
dotnet ef database update
```

---

## 💡 Key Architecture Points

### Message Flow:
```
1. User creates enrollment via API Gateway
2. EnrollmentService saves to DB
3. EnrollmentService publishes event to RabbitMQ exchange "student-enrolled"
4. FeeService listens to "student-enrolled" exchange
5. FeeService receives event automatically
6. FeeService creates fee record in its own database
```

### Exchange Details:
- **Name:** `student-enrolled`
- **Type:** Fanout (broadcasts to all listeners)
- **Durable:** Yes (survives broker restart)
- **Auto-Delete:** No (persists even if no consumers)

### Event Data:
```json
{
  "EnrollmentId": 123,
  "StudentId": 1,
  "CourseId": 1,
  "Semester": 1,
  "Year": 2024,
  "EnrollmentDate": "2024-01-15"
}
```

---

## 📂 Files Created for Testing

1. **RABBITMQ_ERROR_DIAGNOSIS.md** - Complete error reference guide
2. **RabbitMQ_Connection_Test.cs** - Standalone connection test
3. **RABBITMQ_TEST_SOLUTION.md** - This file (testing & solutions guide)

---

## 🔗 Useful Resources

- **CloudAMQP Dashboard:** https://customer.cloudamqp.com/
- **API Gateway Swagger:** https://localhost:7000/swagger
- **EnrollmentService Swagger:** https://localhost:7003/swagger
- **FeeService Swagger:** https://localhost:7004/swagger
- **Quick Reference:** See `RABBITMQ_DEMO_QUICK_REF.md`

---

**Status:** ✅ Configuration is correct
**Next Step:** Start services and test
**Last Checked:** 2026-01-06 10:05 IST
