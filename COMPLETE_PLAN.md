# 🏫 COLLEGE MANAGEMENT SYSTEM — COMPLETE PRACTICAL PLAN
> **3 Roles: Student | Teacher | Admin**
> **Strategy: 1 New Microservice + Edit Existing + Frontend First**

---

## 📊 WHAT EXISTS vs WHAT'S MISSING

### ✅ EXISTING MICROSERVICES (Keep & Enhance)
| # | Service | Port | Has | Missing |
|---|---------|------|-----|---------|
| 1 | **ApiGateway** | 7000 | Ocelot routing, Swagger | JWT forwarding, new routes |
| 2 | **StudentService** | 7001 | Full CRUD, Redis cache, DTOs | Authorization, link to UserId |
| 3 | **CourseService** | 7002 | CRUD, Repository | TeacherId, Service Layer, Dept CRUD |
| 4 | **EnrollmentService** | 7003 | Basic CRUD | DTOs, proper service, validation |
| 5 | **FeeService** | 7004 | Basic CRUD, RabbitMQ subscriber | DTOs, FeeType, PaymentMethod, service layer |
| 6 | **AttendanceService** | 7005 | Basic CRUD, Bulk | ❌ NO Timetable! No teacher marking, no % |
| 7 | **AIAssistantService** | 7006 | AI Chat with Gemini | OK for now |
| 8 | **AuthService** | 7007 | Login/Register/JWT/Google/Teacher model | TeacherController✅, AdminController, role auth |
| 9 | **AIService** | - | Gemini integration | OK for now |
| 10 | **NotificationService** | - | RabbitMQ subscriber | OK for now |
| 11 | **Common.Messaging** | - | RabbitMQ publisher/subscriber | OK for now |

### ❌ DOES NOT EXIST (Need to Build)
| Feature | Where to Build |
|---------|---------------|
| **Timetable/TimeSlots** | NEW: CMS.AcademicService (Port 7008) |
| **Grade/Results** | NEW: CMS.AcademicService |
| **Notice/Announcements** | NEW: CMS.AcademicService |
| **Admin User Management** | AuthService (add AdminController) |
| **Department CRUD** | CourseService (add DepartmentController) |
| **Teacher-Course Assignment** | CourseService (update Course model) |
| **Timetable-Based Attendance** | AttendanceService (enhance) + AcademicService |
| **JWT on ALL endpoints** | All services |
| **Role-Based Frontend** | Frontend (new pages + auth.js) |

---

## 🏗️ ARCHITECTURE: 1 NEW MICROSERVICE FOR ALL NEW FEATURES

### **CMS.AcademicService** (Port 7008) — ONE service for:
```
📁 CMS.AcademicService/
├── Models/
│   ├── TimeSlot.cs          ← Timetable slots
│   ├── Grade.cs             ← Student grades
│   └── Notice.cs            ← Announcements
├── DTOs/
│   ├── TimeSlotDtos.cs
│   ├── GradeDtos.cs
│   └── NoticeDtos.cs
├── Services/
│   ├── TimeSlotService.cs
│   ├── GradeService.cs
│   └── NoticeService.cs
├── Controllers/
│   ├── TimeSlotController.cs
│   ├── GradeController.cs
│   └── NoticeController.cs
├── Data/
│   └── AcademicDbContext.cs
└── Program.cs
```

---

## 🎯 FULL FEATURE LIST BY ROLE

### 👨‍🎓 STUDENT ROLE — What Student Can Do:
| # | Feature | Page | API Endpoint | Status |
|---|---------|------|-------------|--------|
| 1 | Login/Register | login.html | POST /api/auth/login | ✅ Exists |
| 2 | View My Profile | student-dashboard.html | GET /api/auth/me | ✅ Exists |
| 3 | View My Courses (enrolled) | my-courses.html | GET /api/enrollment/student/{id} | ✅ Exists |
| 4 | View Course Details | course-detail.html | GET /api/course/{id} | ✅ Exists |
| 5 | View My Attendance % | my-attendance.html | GET /api/attendance/student/{id}/summary | ❌ NEW |
| 6 | View Timetable/Schedule | timetable.html | GET /api/timeslot/student/{studentId} | ❌ NEW |
| 7 | View My Fees & Payment Status | my-fees.html | GET /api/fee/student/{id} | ✅ Exists |
| 8 | View My Grades/Results | my-grades.html | GET /api/grade/student/{id} | ❌ NEW |
| 9 | View Notices | notices.html | GET /api/notice | ❌ NEW |
| 10 | AI Assistant Chat | ai-assistant.html | POST /api/chat | ✅ Exists |

### 🧑‍🏫 TEACHER ROLE — What Teacher Can Do:
| # | Feature | Page | API Endpoint | Status |
|---|---------|------|-------------|--------|
| 1 | Login | login.html | POST /api/auth/login | ✅ Exists |
| 2 | View My Profile | teacher-dashboard.html | GET /api/teacher/user/{userId} | ✅ Just Built |
| 3 | View My Courses (assigned) | teacher-courses.html | GET /api/course/teacher/{teacherId} | ❌ NEW endpoint |
| 4 | View My Timetable | teacher-timetable.html | GET /api/timeslot/teacher/{teacherId} | ❌ NEW |
| 5 | **Mark Attendance** (by timeslot) | mark-attendance.html | POST /api/attendance/bulk | ⚠️ Enhance |
| 6 | View Attendance Reports | attendance-report.html | GET /api/attendance/course/{id}/summary | ❌ NEW |
| 7 | **Add/Edit Grades** | manage-grades.html | POST /api/grade | ❌ NEW |
| 8 | View Students in Course | course-students.html | GET /api/enrollment/course/{id} | ✅ Exists |
| 9 | Post Notice/Announcement | manage-notices.html | POST /api/notice | ❌ NEW |
| 10 | AI Assistant Chat | ai-assistant.html | POST /api/chat | ✅ Exists |

### 👨‍💼 ADMIN ROLE — What Admin Can Do (EVERYTHING):
| # | Feature | Page | API Endpoint | Status |
|---|---------|------|-------------|--------|
| **Dashboard** | | | | |
| 1 | Dashboard with Stats | admin-dashboard.html | GET /api/admin/stats | ❌ NEW |
| **User Management** | | | | |
| 2 | View All Users | users.html | GET /api/admin/users | ❌ NEW |
| 3 | Create User (any role) | users.html | POST /api/admin/users | ❌ NEW |
| 4 | Edit User / Change Role | users.html | PUT /api/admin/users/{id} | ❌ NEW |
| 5 | Deactivate/Delete User | users.html | DELETE /api/admin/users/{id} | ❌ NEW |
| **Teacher Management** | | | | |
| 6 | View All Teachers | teachers.html | GET /api/teacher | ✅ Just Built |
| 7 | Add Teacher | teachers.html | POST /api/teacher | ✅ Just Built |
| 8 | Edit Teacher | teachers.html | PUT /api/teacher/{id} | ✅ Just Built |
| 9 | Assign Teacher to Course | courses.html | PUT /api/course/{id}/assign-teacher | ❌ NEW |
| **Student Management** | | | | |
| 10 | View All Students | students.html | GET /api/student | ✅ Exists |
| 11 | Add Student | students.html | POST /api/student | ✅ Exists |
| 12 | Edit/Delete Student | students.html | PUT/DELETE /api/student/{id} | ✅ Exists |
| **Course & Department** | | | | |
| 13 | Manage Departments | departments.html | CRUD /api/department | ❌ NEW Controller |
| 14 | Manage Courses | courses.html | CRUD /api/course | ✅ Exists |
| 15 | Assign Teacher to Course | courses.html | PUT /api/course/{id} | ⚠️ Enhance |
| **Timetable** | | | | |
| 16 | Create Time Slots | timetable-manage.html | POST /api/timeslot | ❌ NEW |
| 17 | Edit/Delete Time Slots | timetable-manage.html | PUT/DELETE /api/timeslot/{id} | ❌ NEW |
| 18 | View Full Timetable | timetable-manage.html | GET /api/timeslot | ❌ NEW |
| **Enrollment** | | | | |
| 19 | Manage Enrollments | enrollment.html | CRUD /api/enrollment | ✅ Exists |
| **Fee Management** | | | | |
| 20 | View All Fees | fees.html | GET /api/fee | ✅ Exists |
| 21 | Create Fee | fees.html | POST /api/fee | ✅ Exists |
| 22 | Bulk Generate Fees | fees.html | POST /api/fee/bulk-generate | ❌ NEW |
| 23 | View Defaulters | fees.html | GET /api/fee/defaulters | ❌ NEW |
| **Attendance** | | | | |
| 24 | View All Attendance | attendance.html | GET /api/attendance | ✅ Exists |
| 25 | Mark Attendance | attendance.html | POST /api/attendance/bulk | ✅ Exists |
| 26 | Low Attendance Report | attendance.html | GET /api/attendance/low/{threshold} | ❌ NEW |
| **Grades** | | | | |
| 27 | View All Grades | grades.html | GET /api/grade | ❌ NEW |
| 28 | Add/Edit Grades | grades.html | POST/PUT /api/grade | ❌ NEW |
| **Notices** | | | | |
| 29 | Manage Notices | notices-manage.html | CRUD /api/notice | ❌ NEW |

---

## 🔄 TIMETABLE-BASED ATTENDANCE SYSTEM (Your Special Request)

### How It Works:
```
ADMIN creates TimeSlots → Course+Teacher+Day+Time+Room
  ↓
TEACHER sees their timetable → selects a slot → marks attendance for all students
  ↓
STUDENT sees their timetable → views attendance % per course
```

### TimeSlot Model:
```csharp
public class TimeSlot
{
    int TimeSlotId
    int CourseId          // Which course
    string CourseName     // Denormalized for display
    int TeacherId         // Which teacher
    string TeacherName    // Denormalized for display
    int DepartmentId
    int Semester
    string DayOfWeek      // "Monday", "Tuesday"...
    TimeSpan StartTime    // 09:00
    TimeSpan EndTime      // 10:00
    string RoomNumber     // "A-101"
    string AcademicYear   // "2025-2026"
    bool IsActive
}
```

### Attendance Enhancement:
```csharp
public class Attendance
{
    int AttendanceId
    int StudentId
    int CourseId
    int TimeSlotId        // ← LINKED TO TIMETABLE!
    int MarkedByUserId    // Teacher who marked
    DateTime Date
    bool IsPresent
    string? Remarks
}
```

---

## 📋 STEP-BY-STEP EXECUTION ORDER (Frontend First!)

### PHASE A: BACKEND ENHANCEMENTS (Edit Existing Services)
| Step | What | Where | Files |
|------|------|-------|-------|
| **A1** | Add TeacherId to Course model + DepartmentController | CourseService | 4 files |
| **A2** | Enhance Attendance model (TimeSlotId, MarkedByUserId) + DTOs | AttendanceService | 4 files |
| **A3** | Add Enrollment DTOs + enhance service | EnrollmentService | 3 files |
| **A4** | Add Fee DTOs + enhance service | FeeService | 3 files |
| **A5** | Add AdminController to AuthService | AuthService | 3 files |

### PHASE B: NEW MICROSERVICE (CMS.AcademicService)
| Step | What | Files |
|------|------|-------|
| **B1** | Create project + TimeSlot model/DTOs/Service/Controller | 8 files |
| **B2** | Add Grade model/DTOs/Service/Controller | 4 files |
| **B3** | Add Notice model/DTOs/Service/Controller | 4 files |
| **B4** | Add to Ocelot gateway + Solution | 2 files |

### PHASE C: FRONTEND (Role-Based UI — MAIN FOCUS!)
| Step | What | Pages |
|------|------|-------|
| **C1** | Create auth.js (JWT token management, role guards) | 1 file |
| **C2** | Enhance login.html (store token, redirect by role) | 1 file |
| **C3** | Create role-based sidebar/navigation component | 1 file |
| **C4** | Admin Dashboard (stats, charts, quick actions) | admin-dashboard.html |
| **C5** | Admin: User Management page | users.html |
| **C6** | Admin: Teacher Management page | teachers.html |
| **C7** | Admin: Department Management page | departments.html |
| **C8** | Admin: Timetable Management page | timetable-manage.html |
| **C9** | Admin: Fee Management (enhance existing) | fees.html |
| **C10** | Admin: Grade Management page | grades-manage.html |
| **C11** | Admin: Notices Management page | notices-manage.html |
| **C12** | Teacher Dashboard page | teacher-dashboard.html |
| **C13** | Teacher: Mark Attendance (by timeslot) | mark-attendance.html |
| **C14** | Teacher: My Courses + Students page | teacher-courses.html |
| **C15** | Teacher: My Timetable page | teacher-timetable.html |
| **C16** | Teacher: Manage Grades page | teacher-grades.html |
| **C17** | Student Dashboard page | student-dashboard.html |
| **C18** | Student: My Courses page | my-courses.html |
| **C19** | Student: My Attendance page | my-attendance.html |
| **C20** | Student: My Timetable page | my-timetable.html |
| **C21** | Student: My Fees page | my-fees.html |
| **C22** | Student: My Grades page | my-grades.html |
| **C23** | Notices page (all roles) | notices.html |
| **C24** | Enhance existing pages (students, courses, enrollment, attendance) | update existing |

### PHASE D: JWT AUTHORIZATION (Lock Everything Down)
| Step | What |
|------|------|
| **D1** | Add [Authorize] with roles to ALL controllers |
| **D2** | Configure Ocelot Gateway JWT forwarding |
| **D3** | Frontend route guards (redirect unauthorized) |

---

## 🔐 SEED DATA / TEST ACCOUNTS
| Email | Password | Role |
|-------|----------|------|
| admin@cms.com | Admin@123 | Admin |
| teacher1@cms.com | Teacher@123 | Teacher |
| teacher2@cms.com | Teacher@123 | Teacher |
| student1@cms.com | Student@123 | Student |

---

## ⚡ READY TO START?
Say **"Start A1"** for backend enhancements, or  
Say **"Start C1"** to begin frontend first!

Since you want **frontend first**, I recommend: **Start C1** → auth.js + login enhancement
