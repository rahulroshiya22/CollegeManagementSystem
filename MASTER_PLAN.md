# 🏫 COLLEGE MANAGEMENT SYSTEM — COMPLETE MASTER PLAN
> **3 Roles: Student | Teacher | Admin**  
> **Strategy: First complete ALL features → Then apply JWT Role-Based Auth**

---
a
## 📊 CURRENT STATE ANALYSIS

### What Already EXISTS and is WORKING:
| Service | Model | CRUD | Repository | Service Layer | DTOs | Notes |
|---------|-------|------|------------|---------------|------|-------|
| **AuthService** | ✅ User (with Role enum), Teacher | ✅ Login/Register/Refresh/Revoke | N/A | ✅ AuthService, JwtService, GoogleAuth, Cache, FileUpload | ✅ AuthDTOs | Teacher model exists but NO controller/service |
| **StudentService** | ✅ Student, Department | ✅ Full CRUD + pagination + search | ✅ StudentRepository | ✅ StudentService (with Redis cache) | ✅ Create/Update/Query DTOs | Well-built. Missing: no link to User |
| **CourseService** | ✅ Course, Department | ✅ Full CRUD | ✅ CourseRepository | ❌ No service layer (controller → repo directly) | ✅ Create/Update DTOs | Missing: No teacher assignment |
| **EnrollmentService** | ✅ Enrollment | ✅ Basic CRUD | ❌ No | ⚠️ Minimal (only EnrollStudent) | ❌ No DTOs | Publishes RabbitMQ event. Missing: Update, proper service |
| **FeeService** | ✅ Fee | ✅ Basic CRUD + Pay | ❌ No | ❌ No service layer | ❌ No DTOs | Subscribes to enrollment event. Missing: Update fee, receipts |
| **AttendanceService** | ✅ Attendance | ✅ Basic CRUD + Bulk | ❌ No | ❌ No service layer | ❌ No DTOs | Missing: Who marked, percentage calc, date range |
| **NotificationService** | ⚠️ Unknown | ⚠️ Minimal | ❌ | ❌ | ❌ | Mostly subscriber/publisher |
| **AI Services** | ✅ | ✅ Chat | N/A | ✅ GeminiService | ✅ | Working chatbot |
| **ApiGateway** | N/A | N/A | N/A | N/A | N/A | Ocelot routing. No auth. |

### What DOES NOT EXIST at all:
| Missing Feature | Priority | Needed For |
|----------------|----------|------------|
| **Teacher CRUD & Management** | 🔴 Critical | Teacher role |
| **Teacher-Course Assignment** | 🔴 Critical | Teacher role |
| **Department CRUD Controller** | 🔴 Critical | Admin role |
| **Grade/Result Management** | 🔴 Critical | All 3 roles |
| **User Management by Admin** | 🔴 Critical | Admin role |
| **Timetable/Schedule** | 🟡 Important | All 3 roles |
| **Exam Management** | 🟡 Important | All 3 roles |
| **Notice/Announcement** | 🟡 Important | All 3 roles |
| **JWT Auth on all services** | 🔴 Critical | Security |
| **Role-based `[Authorize]`** | 🔴 Critical | Security |
| **Frontend dashboards per role** | 🔴 Critical | Usability |

---

## 🎯 EXECUTION PLAN — 10 STEPS (One at a Time)

---

### 📌 STEP 1: Complete Teacher Management (AuthService)
> **What:** Build full Teacher CRUD so the Teacher role is functional  
> **Where:** `CMS.AuthService`

#### 1.1 — Enhance Teacher Model
```
File: CMS.AuthService/Models/User.cs
Add to Teacher model:
- Qualification (string)
- Experience (int, years)  
- IsActive (bool)
- CreatedAt (DateTime)
- UpdatedAt (DateTime?)
```

#### 1.2 — Create Teacher DTOs
```
File: CMS.AuthService/DTOs/TeacherDTOs.cs (NEW)
- CreateTeacherDto (FirstName, LastName, Email, Password, Department, Specialization, 
                     Qualification, Experience, PhoneNumber)
- UpdateTeacherDto (Department, Specialization, Qualification, Experience, PhoneNumber)
- TeacherResponseDto (TeacherId, UserId, FirstName, LastName, Email, Department, 
                       Specialization, Qualification, Experience, PhoneNumber, IsActive)
```

#### 1.3 — Create TeacherService
```
File: CMS.AuthService/Services/TeacherService.cs (NEW)
Interface ITeacherService:
- GetAllTeachersAsync() → List<TeacherResponseDto>
- GetTeacherByIdAsync(int id) → TeacherResponseDto
- GetTeacherByUserIdAsync(int userId) → TeacherResponseDto
- CreateTeacherAsync(CreateTeacherDto) → TeacherResponseDto  (creates User + Teacher)
- UpdateTeacherAsync(int id, UpdateTeacherDto) → TeacherResponseDto
- DeleteTeacherAsync(int id) → bool
- GetTeachersByDepartmentAsync(string dept) → List<TeacherResponseDto>
```

#### 1.4 — Create TeacherController
```
File: CMS.AuthService/Controllers/TeacherController.cs (NEW)
Endpoints:
- GET    /api/teacher           → Get all teachers
- GET    /api/teacher/{id}      → Get teacher by ID
- POST   /api/teacher           → Create teacher (creates User with Role=Teacher + Teacher record)
- PUT    /api/teacher/{id}      → Update teacher profile
- DELETE /api/teacher/{id}      → Delete teacher
- GET    /api/teacher/department/{dept} → Get teachers by department
```

#### 1.5 — Update AuthDbContext
- Add Teacher seeding (2-3 sample teachers)
- Add migration

---

### 📌 STEP 2: Add Teacher-Course Assignment (CourseService)
> **What:** Link teachers to courses so we know who teaches what  
> **Where:** `CMS.CourseService`

#### 2.1 — Update Course Model
```
File: CMS.CourseService/Models/Course.cs
Add:
- TeacherId (int?, nullable — course may not have teacher yet)
- MaxStudents (int, default 60)
- AcademicYear (string, e.g. "2025-2026")
```

#### 2.2 — Update Course DTOs
```
Add TeacherId to CreateCourseDto and UpdateCourseDto
Add new: AssignTeacherDto { CourseId, TeacherId }
```

#### 2.3 — Create CourseService (Service Layer — currently missing!)
```
File: CMS.CourseService/Services/CourseService.cs (NEW)
Interface ICourseService:
- GetAllCoursesAsync() → List<Course>
- GetCourseByIdAsync(int id) → Course
- CreateCourseAsync(CreateCourseDto) → Course
- UpdateCourseAsync(int id, UpdateCourseDto) → Course
- DeleteCourseAsync(int id) → bool
- AssignTeacherAsync(int courseId, int teacherId) → Course
- GetCoursesByTeacherAsync(int teacherId) → List<Course>
- GetCoursesByDepartmentAsync(int deptId) → List<Course>
```

#### 2.4 — Update CourseController
- Add `PUT /api/course/{id}/assign-teacher` endpoint
- Add `GET /api/course/teacher/{teacherId}` endpoint
- Refactor to use service layer

#### 2.5 — Add Department CRUD Controller
```
File: CMS.CourseService/Controllers/DepartmentController.cs (NEW)
- GET    /api/department           → List all departments
- GET    /api/department/{id}      → Get department by ID
- POST   /api/department           → Create department
- PUT    /api/department/{id}      → Update department
- DELETE /api/department/{id}      → Delete department
```

#### 2.6 — Update Ocelot Gateway for Department routes
- Add `/api/department/{everything}` route → CourseService

#### 2.7 — Migration for Course changes

---

### 📌 STEP 3: Enhance Enrollment Service
> **What:** Make enrollment a proper full-featured service  
> **Where:** `CMS.EnrollmentService`

#### 3.1 — Create Enrollment DTOs
```
File: CMS.EnrollmentService/DTOs/EnrollmentDtos.cs (NEW)
- CreateEnrollmentDto (StudentId, CourseId, Semester, Year)
- UpdateEnrollmentDto (Status, Grade)
- EnrollmentQueryDto (Page, PageSize, StudentId?, CourseId?, Semester?, Status?)
```

#### 3.2 — Create Full EnrollmentService
```
Enhance existing service:
- GetAllEnrollmentsAsync(query) → paginated
- GetEnrollmentByIdAsync(id)
- GetEnrollmentsByStudentAsync(studentId) → List (with course names)
- GetEnrollmentsByCourseAsync(courseId) → List (with student names)
- EnrollStudentAsync(CreateEnrollmentDto) → with validation (check duplicate)
- UpdateEnrollmentAsync(id, UpdateEnrollmentDto) → approve/reject/grade
- DropEnrollmentAsync(id)
- GetEnrollmentCountByCourseAsync(courseId) → int (for max student check)
```

#### 3.3 — Update EnrollmentController with proper endpoints
- Add `PUT /api/enrollment/{id}` → Update enrollment
- Add `GET /api/enrollment/course/{courseId}/students` → Students in a course
- Improve error handling and validation

---

### 📌 STEP 4: Enhance Fee Service
> **What:** Complete fee management with proper service layer  
> **Where:** `CMS.FeeService`

#### 4.1 — Enhance Fee Model
```
Add:
- FeeType (string: "Tuition", "Library", "Lab", "Exam", "Other")
- Semester (int)
- AcademicYear (string)
- PaymentMethod (string?: "Cash", "Online", "Bank Transfer")
- TransactionId (string?) — for payment reference
- UpdatedAt (DateTime?)
```

#### 4.2 — Create Fee DTOs
```
File: CMS.FeeService/DTOs/FeeDtos.cs (NEW)
- CreateFeeDto (StudentId, Amount, FeeType, Description, Semester, AcademicYear, DueDate)
- UpdateFeeDto (Amount, Description, DueDate, Status)
- PayFeeDto (PaymentMethod, TransactionId?)
- FeeQueryDto (Page, PageSize, StudentId?, Status?, FeeType?, Semester?)
- FeeSummaryDto (TotalFees, TotalPaid, TotalPending, TotalOverdue)
```

#### 4.3 — Create FeeService
```
File: CMS.FeeService/Services/FeeService.cs (NEW)
- GetAllFeesAsync(query) → paginated
- GetFeeByIdAsync(id)
- GetFeesByStudentAsync(studentId) → with summary
- CreateFeeAsync(CreateFeeDto) → Fee
- UpdateFeeAsync(id, UpdateFeeDto) → Fee
- RecordPaymentAsync(id, PayFeeDto) → Fee
- GetFeeDefaultersAsync() → List (overdue + unpaid)
- GetFeeSummaryAsync() → FeeSummaryDto (for admin dashboard)
- GenerateBulkFeesAsync(semester, year, amount, feeType) → for admin to generate fees for all students
```

#### 4.4 — Update FeeController with full endpoints
- Add `PUT /api/fee/{id}` → Update fee
- Add `GET /api/fee/summary` → Fee summary stats
- Add `POST /api/fee/bulk-generate` → Generate fees for all students
- Add `GET /api/fee/student/{id}/summary` → Student fee summary

#### 4.5 — Migration

---

### 📌 STEP 5: Enhance Attendance Service
> **What:** Complete attendance with teacher marking, percentages, reports  
> **Where:** `CMS.AttendanceService`

#### 5.1 — Enhance Attendance Model
```
Add:
- MarkedByTeacherId (int) — who marked this attendance
- UpdatedAt (DateTime?)
- Reason (string?) — for absent students
```

#### 5.2 — Create Attendance DTOs
```
File: CMS.AttendanceService/DTOs/AttendanceDtos.cs (NEW)
- CreateAttendanceDto (StudentId, CourseId, Date, IsPresent, Remarks?)
- BulkAttendanceDto { CourseId, Date, MarkedByTeacherId, 
                       Records: List<{ StudentId, IsPresent, Remarks? }> }
- AttendanceQueryDto (Page, PageSize, StudentId?, CourseId?, DateFrom?, DateTo?, IsPresent?)
- AttendanceSummaryDto (TotalClasses, PresentCount, AbsentCount, Percentage)
```

#### 5.3 — Create AttendanceService
```
File: CMS.AttendanceService/Services/AttendanceService.cs (NEW)
- GetAllAttendanceAsync(query) → paginated
- GetAttendanceByStudentAsync(studentId, courseId?) → with summary
- GetAttendanceByCourseAsync(courseId, date?) → class-wise
- MarkAttendanceAsync(CreateAttendanceDto) → Attendance
- MarkBulkAttendanceAsync(BulkAttendanceDto) → for teacher (whole class at once)
- GetStudentAttendanceSummaryAsync(studentId) → AttendanceSummaryDto
- GetCourseAttendanceSummaryAsync(courseId) → per-student summary
- GetLowAttendanceStudentsAsync(threshold%) → students below threshold
```

#### 5.4 — Update AttendanceController
- Add `GET /api/attendance/student/{id}/summary` → attendance %
- Add `GET /api/attendance/course/{id}/summary` → course attendance stats
- Add `GET /api/attendance/low-attendance/{threshold}` → low attendance alerts
- Add `PUT /api/attendance/{id}` → Update attendance record

#### 5.5 — Migration

---

### 📌 STEP 6: Create Grade/Result Service (NEW Microservice)
> **What:** Brand new service for grades and results  
> **Where:** `CMS.GradeService` (NEW PROJECT)

#### 6.1 — Create Project
```
dotnet new webapi -n CMS.GradeService
Port: 7008
```

#### 6.2 — Models
```
public class Grade
{
    int GradeId
    int StudentId
    int CourseId
    int TeacherId (who gave the grade)
    string ExamType ("Internal1", "Internal2", "Midterm", "Final", "Assignment", "Lab")
    decimal MarksObtained
    decimal MaxMarks
    int Semester
    string AcademicYear
    DateTime GradedDate
    string? Remarks
}

public class Result
{
    int ResultId
    int StudentId
    int Semester
    string AcademicYear
    decimal SGPA
    decimal CGPA (calculated)
    string Status ("Pass", "Fail", "ATKT")
    DateTime PublishedDate
}
```

#### 6.3 — DTOs
```
- CreateGradeDto, UpdateGradeDto, GradeQueryDto
- BulkGradeDto { CourseId, ExamType, Semester, Grades: List<{StudentId, Marks}> }
- StudentResultDto (per-semester breakdown)
```

#### 6.4 — GradeService + GradeController
```
- POST   /api/grade                  → Add grade (Teacher)
- POST   /api/grade/bulk             → Add grades for whole class (Teacher)
- PUT    /api/grade/{id}             → Update grade (Teacher)
- GET    /api/grade/student/{id}     → Student's all grades
- GET    /api/grade/course/{id}      → Course grades (Teacher/Admin)
- GET    /api/grade/result/{studentId}/{semester} → Semester result with SGPA
- GET    /api/grade/result/{studentId}           → All results with CGPA
```

#### 6.5 — Database, Migration, add to Ocelot, add to solution

---

### 📌 STEP 7: Create Timetable/Schedule Service (NEW Microservice)
> **What:** Class scheduling system  
> **Where:** `CMS.TimetableService` (NEW PROJECT)

#### 7.1 — Create Project
```
dotnet new webapi -n CMS.TimetableService
Port: 7009
```

#### 7.2 — Models
```
public class ClassSchedule
{
    int ScheduleId
    int CourseId
    int TeacherId
    int DepartmentId
    int Semester
    string DayOfWeek ("Monday", "Tuesday", ...)
    TimeSpan StartTime
    TimeSpan EndTime
    string RoomNumber
    string AcademicYear
    bool IsActive
}
```

#### 7.3 — DTOs + Service + Controller
```
- POST   /api/timetable                       → Create schedule entry (Admin)
- PUT    /api/timetable/{id}                   → Update schedule (Admin)
- DELETE /api/timetable/{id}                   → Remove schedule (Admin)
- GET    /api/timetable                        → All schedules
- GET    /api/timetable/teacher/{teacherId}    → Teacher's schedule
- GET    /api/timetable/department/{id}/semester/{sem} → Class timetable
- GET    /api/timetable/room/{roomNumber}      → Room schedule (for conflict checking)
- GET    /api/timetable/today                  → Today's classes
```

#### 7.4 — Database, Migration, add to Ocelot, add to solution

---

### 📌 STEP 8: Create Notice/Announcement Service (NEW Microservice)
> **What:** College notice board system  
> **Where:** `CMS.NoticeService` (NEW PROJECT)

#### 8.1 — Create Project
```
dotnet new webapi -n CMS.NoticeService
Port: 7010
```

#### 8.2 — Models
```
public class Notice
{
    int NoticeId
    string Title
    string Content
    string Category ("Academic", "Administrative", "Event", "Exam", "General")
    string TargetAudience ("All", "Student", "Teacher", "Admin", "Department:CS")
    int PostedByUserId
    string PostedByName
    string PostedByRole
    bool IsImportant
    bool IsActive
    DateTime CreatedAt
    DateTime? ExpiryDate
    string? AttachmentUrl
}
```

#### 8.3 — DTOs + Service + Controller
```
- POST   /api/notice              → Create notice (Admin/Teacher)
- PUT    /api/notice/{id}         → Update notice (Admin/Author)
- DELETE /api/notice/{id}         → Delete notice (Admin/Author)
- GET    /api/notice              → All active notices (with filters)
- GET    /api/notice/{id}         → Get notice detail
- GET    /api/notice/role/{role}  → Notices for specific role
- GET    /api/notice/important    → Important notices
- GET    /api/notice/my-notices   → Notices posted by current user
```

#### 8.4 — Database, Migration, add to Ocelot, add to solution

---

### 📌 STEP 9: Add Admin User Management + Dashboard Stats
> **What:** Admin can manage all users, view system stats  
> **Where:** `CMS.AuthService`

#### 9.1 — Create AdminController
```
File: CMS.AuthService/Controllers/AdminController.cs (NEW)
- GET    /api/admin/users                → List all users (paginated, searchable)
- GET    /api/admin/users/{id}           → Get user detail
- PUT    /api/admin/users/{id}/role      → Change user role
- PUT    /api/admin/users/{id}/status    → Activate/Deactivate user
- DELETE /api/admin/users/{id}           → Delete user
- GET    /api/admin/stats                → Dashboard statistics
```

#### 9.2 — Create AdminService
```
File: CMS.AuthService/Services/AdminService.cs (NEW)
- GetAllUsersAsync(page, pageSize, search, role?) → paginated user list
- GetUserDetailAsync(id)
- ChangeUserRoleAsync(id, newRole)
- ToggleUserStatusAsync(id)
- DeleteUserAsync(id)
- GetDashboardStatsAsync() → calls all microservices for counts
```

#### 9.3 — Create Dashboard Stats DTO
```
DashboardStatsDto:
- TotalStudents, ActiveStudents
- TotalTeachers
- TotalCourses, ActiveCourses
- TotalEnrollments
- TotalFeesCollected, PendingFees
- TotalAttendanceRecords
- RecentRegistrations (last 7 days)
- UsersByRole breakdown
```

#### 9.4 — Add admin routes to Ocelot

---

### 📌 STEP 10: Apply JWT Role-Based Authorization to ALL Services
> **What:** Secure EVERYTHING with JWT + Role-based access  
> **Where:** ALL microservices

#### 10.1 — Add JWT Config to ALL Services Missing It
Services that need JWT middleware added to `Program.cs`:
- ✅ AuthService (already has)
- ✅ StudentService (already has)
- ✅ CourseService (already has)
- ❌ **EnrollmentService** → ADD
- ❌ **FeeService** → ADD
- ❌ **AttendanceService** → ADD
- ❌ **AIAssistantService** → ADD
- ❌ **GradeService** → ADD (new, will set up during creation)
- ❌ **TimetableService** → ADD (new, will set up during creation)
- ❌ **NoticeService** → ADD (new, will set up during creation)

#### 10.2 — Add Authorization Policies to All Services
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("TeacherOnly", p => p.RequireRole("Teacher"));
    options.AddPolicy("TeacherOrAdmin", p => p.RequireRole("Teacher", "Admin"));
    options.AddPolicy("StudentOnly", p => p.RequireRole("Student"));
    options.AddPolicy("AllAuthenticated", p => p.RequireAuthenticatedUser());
});
```

#### 10.3 — Apply `[Authorize]` to ALL Controllers
Full Role Access Matrix:

| Endpoint | Student | Teacher | Admin |
|----------|---------|---------|-------|
| **Students** | | | |
| GET /api/student (list all) | ❌ | ✅ | ✅ |
| GET /api/student/{id} | ✅ Own only | ✅ | ✅ |
| POST /api/student | ❌ | ❌ | ✅ |
| PUT /api/student/{id} | ❌ | ❌ | ✅ |
| DELETE /api/student/{id} | ❌ | ❌ | ✅ |
| **Courses** | | | |
| GET /api/course (list) | ✅ | ✅ | ✅ |
| GET /api/course/{id} | ✅ | ✅ | ✅ |
| POST /api/course | ❌ | ❌ | ✅ |
| PUT /api/course/{id} | ❌ | ❌ | ✅ |
| DELETE /api/course/{id} | ❌ | ❌ | ✅ |
| PUT /api/course/{id}/assign-teacher | ❌ | ❌ | ✅ |
| GET /api/course/teacher/{id} | ❌ | ✅ Own | ✅ |
| **Departments** | | | |
| GET /api/department (list) | ✅ | ✅ | ✅ |
| POST/PUT/DELETE department | ❌ | ❌ | ✅ |
| **Enrollment** | | | |
| GET /api/enrollment (list all) | ❌ | ✅ | ✅ |
| GET /api/enrollment/student/{id} | ✅ Own | ✅ | ✅ |
| POST /api/enrollment | ❌ | ❌ | ✅ |
| PUT /api/enrollment/{id} | ❌ | ❌ | ✅ |
| DELETE /api/enrollment/{id} | ❌ | ❌ | ✅ |
| **Fees** | | | |
| GET /api/fee (list all) | ❌ | ❌ | ✅ |
| GET /api/fee/student/{id} | ✅ Own | ❌ | ✅ |
| POST /api/fee | ❌ | ❌ | ✅ |
| POST /api/fee/{id}/pay | ✅ Own | ❌ | ✅ |
| GET /api/fee/defaulters | ❌ | ❌ | ✅ |
| **Attendance** | | | |
| GET /api/attendance (list all) | ❌ | ✅ | ✅ |
| GET /api/attendance/student/{id} | ✅ Own | ✅ | ✅ |
| POST /api/attendance | ❌ | ✅ | ✅ |
| POST /api/attendance/bulk | ❌ | ✅ | ✅ |
| **Grades** | | | |
| GET /api/grade/student/{id} | ✅ Own | ✅ | ✅ |
| POST /api/grade | ❌ | ✅ | ✅ |
| POST /api/grade/bulk | ❌ | ✅ | ✅ |
| GET /api/grade/course/{id} | ❌ | ✅ Own Course | ✅ |
| **Timetable** | | | |
| GET /api/timetable | ✅ | ✅ | ✅ |
| POST/PUT/DELETE timetable | ❌ | ❌ | ✅ |
| **Notices** | | | |
| GET /api/notice | ✅ | ✅ | ✅ |
| POST /api/notice | ❌ | ✅ | ✅ |
| PUT/DELETE /api/notice/{id} | ❌ | ✅ Own | ✅ |
| **Teachers** | | | |
| GET /api/teacher | ❌ | ✅ | ✅ |
| POST /api/teacher | ❌ | ❌ | ✅ |
| PUT /api/teacher/{id} | ❌ | ✅ Own | ✅ |
| DELETE /api/teacher/{id} | ❌ | ❌ | ✅ |
| **Admin** | | | |
| ALL /api/admin/* | ❌ | ❌ | ✅ |

#### 10.4 — Configure Ocelot Gateway for JWT Auth
- Add `AuthenticationOptions` in `ocelot.json` routes
- Add JWT config in ApiGateway `Program.cs`
- Forward JWT token headers to downstream services

#### 10.5 — Update Frontend for Role-Based Auth
- Create `auth.js` → Token management, role extraction, route guards
- Update `login.html` → Store JWT, redirect by role
- Create `student-dashboard.html` → Student-specific view
- Create `teacher-dashboard.html` → Teacher-specific view
- Update `dashboard.html` → Admin-specific (enhance existing)
- Add role-based sidebar/navigation on all pages

---

## 📋 STEP EXECUTION SUMMARY

| Step | What | New Files | Modified Files | Priority |
|------|------|-----------|----------------|----------|
| **Step 1** | Teacher Management | 3 new files | 2 modified | 🔴 Do First |
| **Step 2** | Course + Dept Enhancement | 4 new files | 3 modified | 🔴 Do Second |
| **Step 3** | Enrollment Enhancement | 2 new files | 2 modified | 🔴 |
| **Step 4** | Fee Enhancement | 2 new files | 3 modified | 🔴 |
| **Step 5** | Attendance Enhancement | 2 new files | 2 modified | 🔴 |
| **Step 6** | Grade Service (NEW) | ~10 new files | ocelot + sln | 🟡 |
| **Step 7** | Timetable Service (NEW) | ~10 new files | ocelot + sln | 🟡 |
| **Step 8** | Notice Service (NEW) | ~10 new files | ocelot + sln | 🟡 |
| **Step 9** | Admin Management | 3 new files | 1 modified | 🔴 |
| **Step 10** | JWT + Role Auth on ALL | 1 new frontend file | 10+ modified | 🔴 Do Last |

---

## ⚡ READY? Tell me: **"Start Step 1"** and I'll begin implementing!
