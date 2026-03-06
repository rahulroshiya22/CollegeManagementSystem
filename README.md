<div align="center">

# 🎓 NeoVerse CMS

### *A Next-Generation College Management System*

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![JavaScript](https://img.shields.io/badge/JavaScript-ES6+-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Render](https://img.shields.io/badge/Render-46E3B7?style=for-the-badge&logo=render&logoColor=black)
![Telegram Bot](https://img.shields.io/badge/Telegram_Bot-2CA5E0?style=for-the-badge&logo=telegram&logoColor=white)
![AI Powered](https://img.shields.io/badge/AI_Powered-Llama_3.3-FF6F00?style=for-the-badge&logo=meta&logoColor=white)

---

🚀 **A beautifully crafted microservices platform for modern educational institutions**
**Real-time analytics • Smart scheduling • AI-powered chatbot • Telegram integration**

[🌐 Live Demo](#-deployment) · [📖 Documentation](#-architecture) · [⚡ Quick Start](#-quick-start) · [🤖 AI Chatbot](#-ai-chatbot-service) · [📱 Telegram Bot](#-telegram-bot)

</div>

---

## 📋 Table of Contents

- [✨ Features](#-features)
- [🏗️ Architecture](#️-architecture)
- [🧩 Microservices](#-microservices)
- [🖥️ Frontend](#️-frontend)
- [🤖 AI Chatbot Service](#-ai-chatbot-service)
- [📱 Telegram Bot](#-telegram-bot)
- [🔐 Authentication & Security](#-authentication--security)
- [🗄️ Database Design](#️-database-design)
- [📡 API Gateway & Routing](#-api-gateway--routing)
- [🐳 Docker & Deployment](#-docker--deployment)
- [⚡ Quick Start](#-quick-start)
- [🛠️ Tech Stack](#️-tech-stack)
- [📂 Project Structure](#-project-structure)
- [🤝 Contributing](#-contributing)
- [📜 License](#-license)

---

## ✨ Features

<table>
<tr>
<td width="50%">

### 👨‍🎓 Student Management
- 📝 Complete student lifecycle (admission → graduation)
- 👤 Detailed student profiles with contact info
- 🎓 Roll number & department tracking
- 📊 Status management (Active / Inactive / Graduated / Suspended)

### 📚 Course & Enrollment
- 📖 Full course catalog with credit system
- 🗂️ Department-wise course organization
- 📝 Semester-based enrollment engine
- ✅ Active/inactive course management

### 💰 Fee Management
- 💳 Track payments (Pending / Paid / Overdue)
- 📅 Due date tracking with overdue alerts
- 📊 Financial reporting & analytics
- 🔔 Auto-generated fee records on enrollment

</td>
<td width="50%">

### 📊 Attendance System
- ✅ Digital attendance marking
- 📈 Percentage calculations & analytics
- 📅 Date-wise attendance records
- 👨‍🏫 Teacher-side marking interface

### 📝 Exam & Grades
- 🏆 Exam creation & scheduling
- 📊 Grade recording with percentage calculation
- 🎖️ Multi-exam result tracking
- 📈 Academic performance analytics

### 🤖 AI-Powered Features
- 💬 Natural language AI chatbot (Llama 3.3 70B)
- 📱 Telegram bot with full CMS access
- 🔄 Multi-provider AI failover (Groq + Cerebras)
- 🛡️ Security-hardened AI with sanitization

</td>
</tr>
</table>

---

## 🏗️ Architecture

This project follows a **Microservices Architecture** pattern where each domain (Students, Courses, Fees, etc.) is an independent .NET 8 Web API service. All services communicate through an **API Gateway** (Ocelot) that handles routing, load balancing, and cross-cutting concerns.

```mermaid
graph TB
    classDef frontend fill:#6366F1,stroke:#4F46E5,color:#fff,stroke-width:2px
    classDef gateway fill:#14B8A6,stroke:#0D9488,color:#fff,stroke-width:2px
    classDef service fill:#8B5CF6,stroke:#7C3AED,color:#fff,stroke-width:2px
    classDef db fill:#EF4444,stroke:#DC2626,color:#fff,stroke-width:2px
    classDef ai fill:#F59E0B,stroke:#D97706,color:#fff,stroke-width:2px
    classDef telegram fill:#2AABEE,stroke:#1986C9,color:#fff,stroke-width:2px

    subgraph Clients ["🌐 Client Layer"]
        WEB["🖥️ NeoVerse Web UI"]:::frontend
        TBOT["📱 Telegram Bot"]:::telegram
        AICHAT["🤖 AI Chat Widget"]:::ai
    end

    GW["🚪 API Gateway\nOcelot • Port 7000"]:::gateway

    WEB --> GW
    TBOT --> GW
    AICHAT --> GW

    subgraph Services ["⚙️ Microservices Layer"]
        AUTH["🔐 AuthService\nJWT • Port 7001"]:::service
        STU["👨‍🎓 StudentService\nPort 7002"]:::service
        CRS["📚 CourseService\nPort 7003"]:::service
        ENR["📝 EnrollmentService\nPort 7004"]:::service
        FEE["💰 FeeService\nPort 7005"]:::service
        ATT["📊 AttendanceService\nPort 7006"]:::service
        ACA["🎓 AcademicService\nPort 7007"]:::service
        AI["🤖 AIAssistantService\nPort 5006"]:::ai
    end

    GW --> AUTH
    GW --> STU
    GW --> CRS
    GW --> ENR
    GW --> FEE
    GW --> ATT
    GW --> ACA
    GW --> AI

    subgraph Database ["🗄️ Database Layer"]
        SQL[("💾 SQL Server / PostgreSQL")]:::db
    end

    AUTH --> SQL
    STU --> SQL
    CRS --> SQL
    ENR --> SQL
    FEE --> SQL
    ATT --> SQL
    ACA --> SQL
    AI --> SQL

    subgraph External ["☁️ External Services"]
        GROQ["⚡ Groq API\nLlama 3.3 70B"]:::ai
        TGAPI["📡 Telegram API"]:::telegram
    end

    AI --> GROQ
    TBOT --> TGAPI
```

---

## 🧩 Microservices

Each service is a self-contained .NET 8 Web API with its own models, controllers, and database context.

| # | Service | Port | Description | Key Entities |
|:-:|---------|:----:|-------------|--------------|
| 🔐 | **AuthService** | 7001 | JWT authentication, role management | Users, Roles, Tokens |
| 👨‍🎓 | **StudentService** | 7002 | Student CRUD, profiles, departments | Student, Department |
| 📚 | **CourseService** | 7003 | Course catalog, semesters, credits | Course, Department |
| 📝 | **EnrollmentService** | 7004 | Student-course enrollment | Enrollment |
| 💰 | **FeeService** | 7005 | Fee tracking, payments, due dates | Fee |
| 📊 | **AttendanceService** | 7006 | Daily attendance records | Attendance |
| 🎓 | **AcademicService** | 7007 | Exams, grades, timetables, notices | Exam, Grade, TimeSlot, Notice |
| 🤖 | **AIAssistantService** | 5006 | AI chatbot with tool calling | ChatMessage, Conversation |
| 🔔 | **NotificationService** | — | Event-driven notifications | — |
| 📡 | **Common.Messaging** | — | RabbitMQ/CloudAMQP shared library | Message Bus |
| 🚪 | **ApiGateway** | 7000 | Ocelot reverse proxy & router | Routes |

### Service Communication Flow

```mermaid
sequenceDiagram
    actor User as 👤 User
    participant FE as 🖥️ Frontend
    participant GW as 🚪 API Gateway
    participant Auth as 🔐 AuthService
    participant Svc as ⚙️ Any Service
    participant DB as 💾 Database

    User->>FE: Login Request
    FE->>GW: POST /api/auth/login
    GW->>Auth: Forward Request
    Auth->>DB: Validate Credentials
    DB-->>Auth: User Found ✅
    Auth-->>GW: JWT Token 🔑
    GW-->>FE: Token Response

    User->>FE: Fetch Data
    FE->>GW: GET /api/student (+ Bearer Token)
    GW->>GW: Validate Route
    GW->>Svc: Forward to StudentService
    Svc->>DB: EF Core Query
    DB-->>Svc: Data
    Svc-->>GW: JSON Response
    GW-->>FE: Data to Render
```

---

## 🖥️ Frontend

The project includes multiple frontend implementations showcasing different design approaches:

| Frontend | Design Style | Description |
|----------|-------------|-------------|
| `Frontend2/` | 🌌 **NeoVerse Dark** | Primary production UI with glassmorphism, gradient orbs, dark theme |
| `Frontend2_Claymorphism/` | 🎨 **Clay Design** | Soft 3D claymorphism variant |
| `Frontend2_Copy/` | 📋 **Extended Copy** | Enhanced version with AI chat widget |
| `Frontend_v3/` | 🆕 **V3 Experimental** | Next-gen experimental UI |

### Frontend Features

- 🌙 **Dark mode** with vibrant gradients and micro-animations
- 📱 **Responsive** design for all screen sizes
- ⚡ **Progressive loading** — dashboard stats load sequentially
- 🔄 **Smart caching** — `sessionStorage` with 60s TTL
- 🛡️ **Auto retry** — exponential backoff on 500/502/504 errors
- 📊 **Role-based dashboards** — Admin, Teacher, Student views
- 🤖 **AI Chat Widget** — embedded chatbot for instant queries

### Role-Based Access

```mermaid
graph LR
    classDef admin fill:#EF4444,stroke:#DC2626,color:#fff
    classDef teacher fill:#3B82F6,stroke:#2563EB,color:#fff
    classDef student fill:#10B981,stroke:#059669,color:#fff

    LOGIN["🔐 Login Page"]

    LOGIN --> ADMIN["🛡️ Admin Dashboard"]:::admin
    LOGIN --> TEACHER["👨‍🏫 Teacher Dashboard"]:::teacher
    LOGIN --> STUDENT["🎓 Student Dashboard"]:::student

    ADMIN --> A1["👨‍🎓 Manage Students"]
    ADMIN --> A2["📚 Manage Courses"]
    ADMIN --> A3["👨‍🏫 Manage Teachers"]
    ADMIN --> A4["💰 Fee Reports"]
    ADMIN --> A5["📊 Analytics"]
    ADMIN --> A6["📢 Notices & Broadcasts"]

    TEACHER --> T1["📅 My Classes"]
    TEACHER --> T2["✅ Mark Attendance"]
    TEACHER --> T3["📝 Create Exams"]
    TEACHER --> T4["📊 Enter Grades"]

    STUDENT --> S1["👤 My Profile"]
    STUDENT --> S2["📊 Attendance"]
    STUDENT --> S3["📚 My Courses"]
    STUDENT --> S4["💰 Fee Status"]
    STUDENT --> S5["📝 Exam Results"]
```

---

## 🤖 AI Chatbot Service

The AI Assistant is powered by **Llama 3.3 70B** via Groq's ultra-fast LPU inference. It uses **agentic tool calling** to query real CMS data and format responses beautifully.

### How It Works

```mermaid
graph LR
    classDef user fill:#6366F1,color:#fff
    classDef ai fill:#F59E0B,color:#fff
    classDef tool fill:#10B981,color:#fff
    classDef api fill:#8B5CF6,color:#fff

    USER["💬 User Query"]:::user
    LLM["🧠 Llama 3.3 70B\nGroq LPU"]:::ai
    TOOLS["🔧 Tool Calling\nget_records\nget_record_by_id"]:::tool
    API["📡 CMS APIs\nReal Data"]:::api
    FORMAT["✨ Formatted\nResponse"]:::ai

    USER --> LLM
    LLM --> TOOLS
    TOOLS --> API
    API --> LLM
    LLM --> FORMAT
    FORMAT --> USER
```

### AI Features

| Feature | Description |
|---------|-------------|
| 🧠 **Smart Model** | Llama 3.3 70B Versatile (primary) with 8B fallback |
| 🔧 **Tool Calling** | 5 tools: get_records, get_record_by_id, create, update, delete |
| 🛡️ **Security** | Credential leak prevention, response sanitization |
| ⚡ **Response Cache** | 5-minute in-memory cache for repeated queries |
| 📊 **Pre-Formatting** | C# pre-processes JSON → readable key-value pairs before AI |
| 🎨 **Emoji Formatting** | Clean Telegram-style output with emojis, no raw JSON |
| 📝 **Bad Response Logging** | Auto-detects leaked function tags or credentials |
| 🔄 **Multi-Key Failover** | Rotates API keys on rate limit (429) errors |

### Example AI Response

```
👤 Full Name: Rahul Sharma
🎓 Roll Number: 2021/CS/005
🏫 Department: Computer Science
📅 Date of Birth: Jan 15, 2001 (Age: 25)
🟢 Status: Active
📚 Batch of 2021

📧 Email: rahul@example.com
📞 Phone: 9876543210
```

---

## 📱 Telegram Bot

A complete Telegram interface for the CMS, allowing students, teachers, and admins to interact with all system data directly from mobile.

### Bot Architecture

```mermaid
stateDiagram-v2
    [*] --> Start: /start command

    Start --> CheckSession
    CheckSession --> LoginFlow: Not Logged In
    CheckSession --> Dashboard: Logged In

    state LoginFlow {
        [*] --> EnterEmail
        EnterEmail --> EnterPassword
        EnterPassword --> ValidateJWT
        ValidateJWT --> [*]: Success ✅
        ValidateJWT --> EnterEmail: Failed ❌
    }

    LoginFlow --> Dashboard

    state Dashboard {
        [*] --> RoleCheck
        RoleCheck --> AdminMenu: Role = Admin
        RoleCheck --> TeacherMenu: Role = Teacher
        RoleCheck --> StudentMenu: Role = Student
    }

    state AdminMenu {
        ManageStudents
        ManageCourses
        ManageFees
        Broadcasts
        Timetable
    }

    state TeacherMenu {
        MyClasses
        MarkAttendance
        CreateExams
    }

    state StudentMenu {
        MyProfile
        ViewAttendance
        ViewGrades
        FeeStatus
    }
```

### Bot Handler Modules

| Module | Handler | Capabilities |
|--------|---------|-------------|
| 🔐 | `AuthHandler` | Multi-step email/password login via state machine |
| 📋 | `MenuHandler` | Dynamic role-based dashboard generation |
| 👨‍🎓 | `StudentsHandler` | Paginated student list, profile drill-down |
| 👨‍🏫 | `TeachersHandler` | Faculty management, profile views |
| 📚 | `CoursesHandler` | Course catalog browsing |
| 💰 | `FeesHandler` | Financial aggregation, paid vs pending |
| 📊 | `AttendanceHandler` | Attendance overview & statistics |
| 📝 | `ExamsHandler` | Exam scheduling, grade entry |
| 📅 | `TimetableHandler` | Weekly schedule management |
| 📢 | `BroadcasterHandler` | Push notifications to all users |
| 🔄 | `GroupRegistrationHandler` | Link Telegram groups to CMS classes |
| 🕵️ | `ImpersonateHandler` | Admin debug: view bot as another role |

---

## 🔐 Authentication & Security

### JWT Token Flow

```mermaid
graph LR
    classDef secure fill:#10B981,color:#fff,stroke-width:2px

    A["📧 Email + 🔒 Password"] --> B["🔐 AuthService"]
    B --> C{"✅ Valid?"}
    C -->|Yes| D["🔑 Generate JWT"]:::secure
    C -->|No| E["❌ 401 Unauthorized"]
    D --> F["📦 Token Contains:\n• UserId\n• Role\n• Email\n• Expiry"]
    F --> G["🌐 Every API Call\nAuthorization: Bearer eyJ..."]:::secure
```

### Security Features

- 🔑 **JWT Bearer Authentication** with configurable expiry
- 👥 **Role-Based Access Control** (Admin, Teacher, Student)
- 🛡️ **API Gateway** validates all incoming requests
- 🔒 **CORS** configured for frontend origins
- 🚫 **AI Security** — credential leak prevention in chatbot
- 🧹 **Response Sanitization** — strips function tags, JSON, passwords

---

## 🗄️ Database Design

The system supports both **SQL Server** (development) and **PostgreSQL** (production/Supabase).

### Entity Relationship Diagram

```mermaid
erDiagram
    STUDENT ||--o{ ENROLLMENT : enrolls
    STUDENT ||--o{ FEE : "has fees"
    STUDENT ||--o{ ATTENDANCE : "has attendance"
    STUDENT }o--|| DEPARTMENT : "belongs to"
    COURSE }o--|| DEPARTMENT : "belongs to"
    COURSE ||--o{ ENROLLMENT : "enrolled in"
    COURSE ||--o{ EXAM : "has exams"
    COURSE ||--o{ TIMESLOT : "scheduled in"
    EXAM ||--o{ GRADE : "has grades"
    STUDENT ||--o{ GRADE : "receives"
    TEACHER ||--o{ TIMESLOT : teaches
    TEACHER }o--|| DEPARTMENT : "belongs to"

    STUDENT {
        int StudentId PK
        string FirstName
        string LastName
        string Email
        string Phone
        string RollNumber
        date DateOfBirth
        string Gender
        string Address
        int DepartmentId FK
        int AdmissionYear
        string Status
    }

    COURSE {
        int CourseId PK
        string CourseCode
        string CourseName
        int Credits
        int Semester
        int DepartmentId FK
        bool IsActive
    }

    FEE {
        int FeeId PK
        int StudentId FK
        decimal Amount
        string Status
        date DueDate
        date PaidDate
    }

    ENROLLMENT {
        int EnrollmentId PK
        int StudentId FK
        int CourseId FK
        string Status
    }

    EXAM {
        int ExamId PK
        int CourseId FK
        string Title
        date Date
        int TotalMarks
    }

    GRADE {
        int GradeId PK
        int StudentId FK
        int ExamId FK
        int MarksObtained
        string Grade
    }
```

---

## 📡 API Gateway & Routing

The **Ocelot API Gateway** acts as a single entry point, routing requests to the correct microservice.

| Route Pattern | Target Service | Method |
|--------------|----------------|--------|
| `/api/auth/**` | AuthService:7001 | ALL |
| `/api/student/**` | StudentService:7002 | ALL |
| `/api/course/**` | CourseService:7003 | ALL |
| `/api/enrollment/**` | EnrollmentService:7004 | ALL |
| `/api/fee/**` | FeeService:7005 | ALL |
| `/api/attendance/**` | AttendanceService:7006 | ALL |
| `/api/exam/**` | AcademicService:7007 | ALL |
| `/api/grade/**` | AcademicService:7007 | ALL |
| `/api/timeslot/**` | AcademicService:7007 | ALL |
| `/api/notice/**` | AcademicService:7007 | ALL |
| `/api/department/**` | CourseService:7003 | ALL |

---

## 🐳 Docker & Deployment

### Multi-Stage Docker Build

The project uses a single `Dockerfile` that builds all microservices and runs them together using a shell script orchestrator.

```
📦 Docker Build Pipeline
┌─────────────────────────┐
│  🔨 BUILD STAGE         │
│  SDK 8.0 Image          │
│  ├── dotnet restore     │
│  ├── publish Student    │
│  ├── publish Course     │
│  ├── publish Fee        │
│  ├── publish Enrollment │
│  ├── publish Attendance │
│  ├── publish Auth       │
│  ├── publish Academic   │
│  ├── publish AI         │
│  └── publish Gateway    │
├─────────────────────────┤
│  🏃 RUNTIME STAGE       │
│  ASP.NET 8.0 Image      │
│  ├── Copy all services  │
│  ├── start-all.sh       │
│  └── EXPOSE 10000       │
└─────────────────────────┘
```

### Deployment on Render

| Setting | Value |
|---------|-------|
| 🌐 Platform | Render (Free Tier) |
| 🐳 Build | Docker |
| 🔌 Port | 10000 |
| 💾 Database | Supabase PostgreSQL (Free) |
| 🧠 AI Provider | Groq Cloud (Free) |

---

## ⚡ Quick Start

### Prerequisites

| Tool | Version | Required |
|------|---------|:--------:|
| .NET SDK | 8.0+ | ✅ |
| SQL Server | 2019+ | ✅ |
| Node.js | 18+ | ❌ Optional |
| Docker | 24+ | ❌ Optional |
| Git | 2.0+ | ✅ |

### 1️⃣ Clone the Repository

```bash
git clone https://github.com/rahulroshiya22/CollegeManagementSystem.git
cd CollegeManagementSystem
```

### 2️⃣ Configure Database

Update connection strings in each service's `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CMS_Students;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### 3️⃣ Run All Services

```bash
# Terminal 1 - API Gateway
cd Backend/CMS.ApiGateway && dotnet run

# Terminal 2 - Auth Service
cd Backend/CMS.AuthService && dotnet run

# Terminal 3 - Student Service
cd Backend/CMS.StudentService && dotnet run

# Continue for each service...
```

Or use the solution launch profile:
```bash
cd Backend && dotnet run --launch-profile "MultipleStartupProjects"
```

### 4️⃣ Open Frontend

Simply open `Frontend2/index.html` in your browser. The landing page will auto-redirect logged-in users to their role-specific dashboard.

---

## 🛠️ Tech Stack

<table>
<tr>
<td align="center" width="120">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/dotnetcore/dotnetcore-original.svg" width="48" height="48" alt=".NET"/>
<br><b>.NET 8</b>
<br><sub>Backend Framework</sub>
</td>
<td align="center" width="120">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/csharp/csharp-original.svg" width="48" height="48" alt="C#"/>
<br><b>C# 12</b>
<br><sub>Primary Language</sub>
</td>
<td align="center" width="120">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/javascript/javascript-original.svg" width="48" height="48" alt="JavaScript"/>
<br><b>JavaScript</b>
<br><sub>Frontend Logic</sub>
</td>
<td align="center" width="120">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/html5/html5-original.svg" width="48" height="48" alt="HTML5"/>
<br><b>HTML5</b>
<br><sub>Markup</sub>
</td>
<td align="center" width="120">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/css3/css3-original.svg" width="48" height="48" alt="CSS3"/>
<br><b>CSS3</b>
<br><sub>Styling</sub>
</td>
</tr>
<tr>
<td align="center" width="120">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/microsoftsqlserver/microsoftsqlserver-plain.svg" width="48" height="48" alt="SQL Server"/>
<br><b>SQL Server</b>
<br><sub>Dev Database</sub>
</td>
<td align="center" width="120">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/postgresql/postgresql-original.svg" width="48" height="48" alt="PostgreSQL"/>
<br><b>PostgreSQL</b>
<br><sub>Prod Database</sub>
</td>
<td align="center" width="120">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/docker/docker-original.svg" width="48" height="48" alt="Docker"/>
<br><b>Docker</b>
<br><sub>Containerization</sub>
</td>
<td align="center" width="120">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/git/git-original.svg" width="48" height="48" alt="Git"/>
<br><b>Git</b>
<br><sub>Version Control</sub>
</td>
<td align="center" width="120">
<img src="https://cdn.simpleicons.org/telegram/26A5E4" width="48" height="48" alt="Telegram"/>
<br><b>Telegram API</b>
<br><sub>Bot Interface</sub>
</td>
</tr>
</table>

### Frameworks & Libraries

| Category | Technology |
|----------|-----------|
| 🌐 Web Framework | ASP.NET Core 8.0 |
| 🗄️ ORM | Entity Framework Core 8.0 |
| 🚪 API Gateway | Ocelot |
| 🔐 Authentication | JWT Bearer Tokens |
| 🤖 AI Model | Llama 3.3 70B via Groq |
| 📱 Telegram SDK | Telegram.Bot (.NET) |
| 📨 Message Bus | RabbitMQ / CloudAMQP |
| 📊 Logging | Serilog |
| 📦 Serialization | System.Text.Json |

---

## 📂 Project Structure

```
CollegeManagementSystem/
│
├── 📁 Backend/                    # PostgreSQL version (Production)
│   ├── CMS.ApiGateway/           # 🚪 Ocelot reverse proxy
│   ├── CMS.AuthService/          # 🔐 JWT authentication
│   ├── CMS.StudentService/       # 👨‍🎓 Student CRUD
│   ├── CMS.CourseService/        # 📚 Course catalog
│   ├── CMS.EnrollmentService/    # 📝 Enrollment engine
│   ├── CMS.FeeService/           # 💰 Fee management
│   ├── CMS.AttendanceService/    # 📊 Attendance tracking
│   ├── CMS.AcademicService/      # 🎓 Exams, grades, timetables
│   ├── CMS.AIAssistantService/   # 🤖 AI chatbot (Groq/Llama)
│   ├── CMS.AIService/            # 🧠 Legacy AI service
│   ├── CMS.NotificationService/  # 🔔 Push notifications
│   ├── CMS.TelegramService/      # 📱 Telegram bot
│   ├── CMS.Common.Messaging/     # 📨 RabbitMQ shared library
│   └── start-all.sh              # 🚀 Service orchestrator
│
├── 📁 Backend_SqlServer_Backup/   # SQL Server version (Development)
│   └── (same structure as above)
│
├── 📁 Frontend2/                  # 🌌 Primary NeoVerse UI
│   ├── index.html                # Landing page
│   ├── pages/                    # Dashboard, students, courses, etc.
│   └── assets/                   # CSS, JS, images
│       ├── css/core.css          # Design system
│       └── js/
│           ├── api.js            # API client + DataStore + caching
│           ├── app.js            # Dashboard logic + progressive loading
│           └── chat-widget.js    # AI chatbot widget
│
├── 📁 Frontend2_Claymorphism/     # 🎨 Clay design variant
├── 📁 Frontend2_Copy/             # 📋 Extended version
├── 📁 Frontend_v3/                # 🆕 Experimental UI
│
├── 🐳 Dockerfile                  # Multi-stage Docker build
├── 📄 CollegeManagementSystem.sln # Solution file
├── 📄 JWT_README.md               # JWT implementation guide
├── 📄 TELEGRAM_BOT_PLAN.md        # Bot architecture plan
├── 📄 CLOUDAMQP_IMPLEMENTATION.md # RabbitMQ setup guide
└── 📄 POSTGRESQL_MIGRATION.md     # DB migration guide
```

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. 🍴 **Fork** this repository
2. 🌿 Create a **feature branch** (`git checkout -b feature/amazing-feature`)
3. 💾 **Commit** your changes (`git commit -m 'feat: add amazing feature'`)
4. 🚀 **Push** to the branch (`git push origin feature/amazing-feature`)
5. 📬 Open a **Pull Request**

---

## 📜 License

This project is built for educational purposes as part of an Advanced .NET course project.

---

<div align="center">

### 🌟 Star this repo if you found it useful!

**Built with ❤️ using .NET 8 • Deployed on Render • AI by Groq**

![Made with Love](https://img.shields.io/badge/Made_with-❤️-red?style=for-the-badge)
![.NET 8](https://img.shields.io/badge/Powered_by-.NET_8-512BD4?style=for-the-badge&logo=dotnet)

</div>
