@echo off
echo ========================================
echo  College Management System - RabbitMQ Test
echo ========================================
echo.

cd /d "d:\ADV DOT NET ums  project\CollegeManagementSystem"

echo [1/6] Starting FeeService (Subscriber - MUST START FIRST)...
start "FeeService" dotnet run --project Backend/CMS.FeeService/CMS.FeeService.csproj
timeout /t 8 /nobreak > nul

echo [2/6] Starting EnrollmentService (Publisher)...
start "EnrollmentService" dotnet run --project Backend/CMS.EnrollmentService/CMS.EnrollmentService.csproj
timeout /t 8 /nobreak > nul

echo [3/6] Starting API Gateway...
start "API Gateway" dotnet run --project Backend/CMS.ApiGateway/CMS.ApiGateway.csproj
timeout /t 8 /nobreak > nul

echo [4/6] Starting StudentService...
start "StudentService" dotnet run --project Backend/CMS.StudentService/CMS.StudentService.csproj
timeout /t 5 /nobreak > nul

echo [5/6] Starting CourseService...
start "CourseService" dotnet run --project Backend/CMS.CourseService/CMS.CourseService.csproj
timeout /t 5 /nobreak > nul

echo [6/6] Opening Swagger UI...
timeout /t 5 /nobreak > nul
start https://localhost:7000/swagger

echo.
echo ========================================
echo  All Services Started!
echo ========================================
echo.
echo Next Steps:
echo 1. Wait for all service windows to show "Application started"
echo 2. Use Swagger to test: POST /api/Enrollment
echo 3. Watch FeeService console for auto-generated fees
echo.
echo Test Data Example:
echo {"studentId": 7, "courseId": 2, "semester": 1, "year": 2024, "enrollmentDate": "2024-08-15"}
echo.
echo Check RABBITMQ_COMPLETE_TEST_GUIDE.md for detailed instructions
echo.
pause
