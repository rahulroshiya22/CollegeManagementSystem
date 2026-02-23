# 🎯 RabbitMQ Demo - Quick Reference Card

## 🔗 Quick Links

### CloudAMQP Dashboard
1. Go to: https://customer.cloudamqp.com/
2. Login and select your instance
3. Click **"RabbitMQ Manager"** button

### API Endpoints (via Swagger)
- API Gateway: https://localhost:7000/swagger
- Enrollment Service: https://localhost:7003/swagger
- Fee Service: https://localhost:7004/swagger

---

## 📝 Demo Script (5 Minutes)

### 1️⃣ Setup (30 seconds)
```
✓ Start all services in VS 2022
✓ Open CloudAMQP RabbitMQ Manager → Queues tab
✓ Open Swagger: https://localhost:7000/swagger
```

### 2️⃣ Explain Architecture (1 minute)
> "This system uses event-driven microservices architecture. When a student enrolls, 
> the EnrollmentService publishes an event to RabbitMQ, and FeeService automatically 
> creates a fee record—no direct service-to-service calls."

### 3️⃣ Live Demo (2 minutes)

**Step 1 - Show Empty Queue:**
- Point to CloudAMQP: `student-enrolled-queue` has 0 messages

**Step 2 - Create Enrollment:**
```http
POST /api/Enrollment
{
  "studentId": 1,
  "courseId": 1,
  "enrollmentDate": "2024-01-15"
}
```

**Step 3 - Show Message Flow:**
- Switch to CloudAMQP → message appears → instantly consumed → back to 0
- Explain: "Message was published, queued, and consumed in milliseconds"

**Step 4 - Verify Fee Created:**
```http
GET /api/Fee
```
- Show fee record was automatically created

### 4️⃣ Q&A and benefits (1.5 minutes)
- ✅ Loose coupling between services
- ✅ Asynchronous processing
- ✅ Message persistence
- ✅ Scalable (can add more consumers)

---

## 🎬 Sample Data for Demo

### Create Student
```json
POST /api/Student
{
  "firstName": "Rahul",
  "lastName": "Kumar",
  "email": "rahul@college.edu",
  "dateOfBirth": "2002-05-15",
  "enrollmentDate": "2024-01-15"
}
```

### Create Course
```json
POST /api/Course
{
  "courseCode": "CS101",
  "courseName": "Computer Science",
  "credits": 3,
  "departmentId": 1
}
```

### Create Enrollment (Main Demo)
```json
POST /api/Enrollment
{
  "studentId": 1,
  "courseId": 1,
  "enrollmentDate": "2024-01-15"
}
```

---

## 🔧 RabbitMQ Configuration

**Location:** `Backend/CMS.EnrollmentService/appsettings.json`

```json
{
  "CloudAMQP": {
    "ConnectionString": "amqps://jxlnlvrr:***@duck.lmq.cloudamqp.com/jxlnlvrr"
  }
}
```

**Queue Name:** `student-enrolled-queue`  
**Event Type:** `StudentEnrolled`

---

## 📊 What to Show in CloudAMQP

### Queues Tab
- Queue name: `student-enrolled-queue`
- Message count (0 → 1 → 0)
- Ready messages
- Unacknowledged messages

### Overview Tab
- Message rates graph
- Publish/Deliver rates spike

### Connections Tab
- Shows active connections from services
- EnrollmentService (publisher)
- FeeService (consumer)

---

## ⚡ Pro Tips

1. **Prepare test data beforehand** - Have student and course already created
2. **Keep windows arranged** - CloudAMQP left, Swagger right
3. **Refresh CloudAMQP** - Refresh the queues page to see real-time updates
4. **Multiple enrollments** - Create 2-3 enrollments quickly to show scalability
5. **Show console logs** - Point to service console showing message processing

---

## 🛠️ Troubleshooting

**Queue doesn't exist?**
→ Restart FeeService (auto-creates queue on startup)

**Messages not flowing?**
→ Check service console logs for RabbitMQ connection errors

**Can't see messages in CloudAMQP?**
→ Messages are consumed too fast. Create multiple enrollments rapidly.

---

## 💬 Key Phrases for Explanation

> "Unlike traditional REST API calls, these services communicate through events."

> "If FeeService is down, messages wait in the queue—nothing is lost."

> "We can scale by adding more FeeService instances without touching EnrollmentService."

> "CloudAMQP is a managed RabbitMQ service, so we don't manage infrastructure."

---

**CloudAMQP URL:** https://customer.cloudamqp.com/  
**Full Guide:** See `RABBITMQ_DEMO_GUIDE.md`

**Good luck! 🚀**
