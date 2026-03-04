# 🐘 SQL Server → PostgreSQL Migration Guide

## Overview

This guide helps you migrate your **College Management System** from **SQL Server** to **PostgreSQL** so you can host on free services like **Neon**, **Supabase**, or **Railway**.

---

## 📁 Files Created

| File | Purpose |
|------|---------|
| `PostgreSQL/00_create_databases.sql` | Creates all 8 PostgreSQL databases |
| `PostgreSQL/01_cms_auth.sql` | Auth DB schema (Users, Teachers) |
| `PostgreSQL/02_cms_students.sql` | Students DB schema + departments seed |
| `PostgreSQL/03_cms_courses.sql` | Courses DB schema + departments seed |
| `PostgreSQL/04_cms_enrollments.sql` | Enrollments DB schema |
| `PostgreSQL/05_cms_fees.sql` | Fees DB schema |
| `PostgreSQL/06_cms_attendance.sql` | Attendance DB schema |
| `PostgreSQL/07_cms_academic.sql` | Academic DB schema + timetable & notices seed |
| `PostgreSQL/08_cms_chat.sql` | Chat/AI Assistant DB schema |
| `export_sqlserver_to_postgresql.ps1` | Exports ALL your SQL Server data to PostgreSQL SQL |
| `POSTGRESQL_MIGRATION.md` | This guide |

---

## 🗄️ Database Mapping

| Service | SQL Server DB | PostgreSQL DB |
|---------|--------------|---------------|
| AuthService | `clgmansys_auth` | `cms_auth` |
| StudentService | `clgmansys1` | `cms_students` |
| CourseService | `clgmansys2` | `cms_courses` |
| EnrollmentService | `clgmansys3` | `cms_enrollments` |
| FeeService | `clgmansys4` | `cms_fees` |
| AttendanceService | `clgmansys5` | `cms_attendance` |
| AcademicService | `CMS_AcademicDb` | `cms_academic` |
| AIAssistantService | `ChatDB` | `cms_chat` |

---

## 🚀 Step-by-Step Migration

### Step 1: Export Your SQL Server Data

Run this PowerShell script (your SQL Server must be running):

```powershell
cd "d:\ADV DOT NET ums  project\CollegeManagementSystem"
.\export_sqlserver_to_postgresql.ps1
```

This creates `PostgreSQL\data_export\` folder with INSERT statements for all your data.

### Step 2: Set Up PostgreSQL

**Option A: Local PostgreSQL**
1. Install [PostgreSQL](https://www.postgresql.org/download/windows/)
2. Open pgAdmin or psql terminal

**Option B: Free Cloud PostgreSQL (Recommended for hosting)**
- [Neon](https://neon.tech/) — Free tier, serverless
- [Supabase](https://supabase.com/) — Free tier
- [Railway](https://railway.app/) — Free tier

### Step 3: Create Databases

```bash
# Local PostgreSQL
psql -U postgres -f "PostgreSQL/00_create_databases.sql"
```

For cloud services: Create 8 databases manually via their dashboard, or use one database with different schemas.

### Step 4: Create Tables (run schema files)

```bash
psql -U postgres -d cms_auth        -f "PostgreSQL/01_cms_auth.sql"
psql -U postgres -d cms_students    -f "PostgreSQL/02_cms_students.sql"
psql -U postgres -d cms_courses     -f "PostgreSQL/03_cms_courses.sql"
psql -U postgres -d cms_enrollments -f "PostgreSQL/04_cms_enrollments.sql"
psql -U postgres -d cms_fees        -f "PostgreSQL/05_cms_fees.sql"
psql -U postgres -d cms_attendance  -f "PostgreSQL/06_cms_attendance.sql"
psql -U postgres -d cms_academic    -f "PostgreSQL/07_cms_academic.sql"
psql -U postgres -d cms_chat        -f "PostgreSQL/08_cms_chat.sql"
```

### Step 5: Import Your Data

```bash
psql -U postgres -d cms_auth        -f "PostgreSQL/data_export/01_auth_data.sql"
psql -U postgres -d cms_students    -f "PostgreSQL/data_export/02_students_data.sql"
psql -U postgres -d cms_courses     -f "PostgreSQL/data_export/03_courses_data.sql"
psql -U postgres -d cms_enrollments -f "PostgreSQL/data_export/04_enrollments_data.sql"
psql -U postgres -d cms_fees        -f "PostgreSQL/data_export/05_fees_data.sql"
psql -U postgres -d cms_attendance  -f "PostgreSQL/data_export/06_attendance_data.sql"
psql -U postgres -d cms_academic    -f "PostgreSQL/data_export/07_academic_data.sql"
psql -U postgres -d cms_chat        -f "PostgreSQL/data_export/08_chat_data.sql"
```

---

## 🔧 Backend Code Changes (When Ready to Switch)

When you're ready to make your backend connect to PostgreSQL instead of SQL Server, you'll need these changes:

### 1. NuGet Packages (each .csproj)

```diff
- <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
+ <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
```

For AttendanceService (Dapper):
```diff
- <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />
+ <PackageReference Include="Npgsql" Version="8.0.6" />
```

### 2. Program.cs (each service)

```diff
- options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
+ options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
```

### 3. Connection Strings (each appsettings.json)

```diff
- "DefaultConnection": "Server=RAHUL;Database=clgmansys_auth;Trusted_Connection=True;..."
+ "DefaultConnection": "Host=localhost;Port=5432;Database=cms_auth;Username=postgres;Password=YOUR_PASSWORD"
```

For cloud services like Neon:
```json
"DefaultConnection": "Host=ep-xxx.us-east-2.aws.neon.tech;Port=5432;Database=cms_auth;Username=your_user;Password=your_pass;SSL Mode=Require"
```

### 4. FeeDbContext.cs — Column Type

```diff
- entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
+ entity.Property(e => e.Amount).HasColumnType("numeric(18,2)");
```

### 5. AcademicDbContext.cs — Column Type

```diff
- entity.Property(e => e.Marks).HasColumnType("decimal(5,2)");
+ entity.Property(e => e.Marks).HasColumnType("numeric(5,2)");
```

### 6. AttendanceDapperRepository.cs

```diff
- using Microsoft.Data.SqlClient;
+ using Npgsql;

- private SqlConnection GetConnection() => new SqlConnection(_connectionString);
+ private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

  // In CreateAsync method:
- SELECT CAST(SCOPE_IDENTITY() as int)
+ RETURNING "AttendanceId"
```

---

## 📝 SQL Server vs PostgreSQL Type Mapping

| SQL Server | PostgreSQL |
|-----------|-----------|
| `int IDENTITY` | `SERIAL` |
| `nvarchar(N)` | `VARCHAR(N)` |
| `nvarchar(max)` | `TEXT` |
| `bit` | `BOOLEAN` |
| `datetime2` | `TIMESTAMP` |
| `decimal(N,M)` | `NUMERIC(N,M)` |
| `time` | `TIME` |
| `uniqueidentifier` | `UUID` |

---

## ✅ Verification

After importing data, verify in psql or pgAdmin:

```sql
-- Check Auth DB
SELECT COUNT(*) AS users FROM "Users";         -- Expected: 16
SELECT COUNT(*) AS teachers FROM "Teachers";    -- Expected: 10

-- Check Students DB
SELECT COUNT(*) AS departments FROM "Departments"; -- Expected: 5

-- Check Academic DB
SELECT COUNT(*) AS timeslots FROM "TimeSlots";  -- Expected: 30
SELECT COUNT(*) AS notices FROM "Notices";       -- Expected: 10
```
