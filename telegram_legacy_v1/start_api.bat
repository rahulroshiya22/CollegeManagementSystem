@echo off
echo Starting College Management System Microservices...

start "API Gateway" dotnet run --project "..\Backend\CMS.ApiGateway\CMS.ApiGateway.csproj"
timeout /t 5

start "Auth Service" dotnet run --project "..\Backend\CMS.AuthService\CMS.AuthService.csproj"
start "Student Service" dotnet run --project "..\Backend\CMS.StudentService\CMS.StudentService.csproj"
start "Academic Service" dotnet run --project "..\Backend\CMS.AcademicService\CMS.AcademicService.csproj"
start "Attendance Service" dotnet run --project "..\Backend\CMS.AttendanceService\CMS.AttendanceService.csproj"
start "Course Service" dotnet run --project "..\Backend\CMS.CourseService\CMS.CourseService.csproj"
start "Fee Service" dotnet run --project "..\Backend\CMS.FeeService\CMS.FeeService.csproj"

echo All services started!
