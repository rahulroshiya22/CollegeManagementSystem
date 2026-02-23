# College Management System - Quick Start Guide

## 🚀 Easy Startup (Like LMS)

### Option 1: PowerShell Script (Recommended)
Double-click or run:
```powershell
.\StartAllServices.ps1
```

### Option 2: Batch File
Double-click:
```
StartAllServices.bat
```

Both scripts will:
- Start all 6 services automatically
- Open each in a separate window
- Display service URLs
- Allow easy shutdown

---

## 📋 What Gets Started

1. **Student Service** - Port 7001
2. **Course Service** - Port 7002  
3. **Enrollment Service** - Port 7003
4. **Fee Service** - Port 7004
5. **Attendance Service** - Port 7005
6. **API Gateway** - Port 7000

---

## 🌐 Access URLs

After starting, access:

### Direct Services (Swagger)
- Student: https://localhost:7001/swagger
- Course: https://localhost:7002/swagger
- Enrollment: https://localhost:7003/swagger
- Fee: https://localhost:7004/swagger
- Attendance: https://localhost:7005/swagger

### Via API Gateway
- https://localhost:7000/student
- https://localhost:7000/course
- https://localhost:7000/enrollment
- https://localhost:7000/fee
- https://localhost:7000/attendance

---

## ⏹️ Stopping Services

**PowerShell Script**: Press any key in the main window  
**Batch File**: Close all service windows  
**Manual**: Run `Stop-Process -Name "dotnet" -Force`

---

## 🧪 Quick Test

Once all services are running:

```powershell
# Create Student
Invoke-RestMethod -Uri "https://localhost:7001/api/Student" -Method Get -SkipCertificateCheck

# Via Gateway
Invoke-RestMethod -Uri "https://localhost:7000/student" -Method Get -SkipCertificateCheck
```

---

## ✅ All Set!

Your College Management System is ready to use, just like the LMS microservice project!
