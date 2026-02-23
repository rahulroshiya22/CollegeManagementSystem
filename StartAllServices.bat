@echo off
echo ========================================
echo College Management System
echo Starting All Microservices...
echo ========================================
echo.

echo [1/6] Starting Student Service (Port 7001)...
start "Student Service - Port 7001" cmd /k "cd Backend\CMS.StudentService && dotnet run"
timeout /t 2 /nobreak >nul

echo [2/6] Starting Course Service (Port 7002)...
start "Course Service - Port 7002" cmd /k "cd Backend\CMS.CourseService && dotnet run"
timeout /t 2 /nobreak >nul

echo [3/6] Starting Enrollment Service (Port 7003)...
start "Enrollment Service - Port 7003" cmd /k "cd Backend\CMS.EnrollmentService && dotnet run"
timeout /t 2 /nobreak >nul

echo [4/6] Starting Fee Service (Port 7004)...
start "Fee Service - Port 7004" cmd /k "cd Backend\CMS.FeeService && dotnet run"
timeout /t 2 /nobreak >nul

echo [5/6] Starting Attendance Service (Port 7005)...
start "Attendance Service - Port 7005" cmd /k "cd Backend\CMS.AttendanceService && dotnet run"
timeout /t 2 /nobreak >nul

echo [6/6] Starting API Gateway (Port 7000)...
start "API Gateway - Port 7000" cmd /k "cd Backend\CMS.ApiGateway && dotnet run"

echo.
echo ========================================
echo All Services Started!
echo ========================================
echo.
echo Access Points:
echo   Student Service:    https://localhost:7001/swagger
echo   Course Service:     https://localhost:7002/swagger
echo   Enrollment Service: https://localhost:7003/swagger
echo   Fee Service:        https://localhost:7004/swagger
echo   Attendance Service: https://localhost:7005/swagger
echo   API Gateway:        https://localhost:7000
echo.
echo Press any key to exit...
pause >nul
