#!/bin/bash
set -e

echo "========================================="
echo " CMS Backend - Starting All Services"
echo "========================================="

# Start all microservices in the background
echo "[1/9] Starting StudentService on port 5001..."
dotnet /app/services/CMS.StudentService/CMS.StudentService.dll --urls "http://0.0.0.0:5001" &

echo "[2/9] Starting CourseService on port 5002..."
dotnet /app/services/CMS.CourseService/CMS.CourseService.dll --urls "http://0.0.0.0:5002" &

echo "[3/9] Starting EnrollmentService on port 5003..."
dotnet /app/services/CMS.EnrollmentService/CMS.EnrollmentService.dll --urls "http://0.0.0.0:5003" &

echo "[4/9] Starting FeeService on port 5004..."
dotnet /app/services/CMS.FeeService/CMS.FeeService.dll --urls "http://0.0.0.0:5004" &

echo "[5/9] Starting AttendanceService on port 5005..."
dotnet /app/services/CMS.AttendanceService/CMS.AttendanceService.dll --urls "http://0.0.0.0:5005" &

echo "[6/9] Starting AIAssistantService on port 5006..."
dotnet /app/services/CMS.AIAssistantService/CMS.AIAssistantService.dll --urls "http://0.0.0.0:5006" &

echo "[7/9] Starting AuthService on port 5007..."
dotnet /app/services/CMS.AuthService/CMS.AuthService.dll --urls "http://0.0.0.0:5007" &

echo "[8/9] Starting AcademicService on port 5008..."
dotnet /app/services/CMS.AcademicService/CMS.AcademicService.dll --urls "http://0.0.0.0:5008" &

# Wait a moment for services to start
echo "Waiting for services to initialize..."
sleep 5

# Start the API Gateway (foreground - Render monitors this process)
PORT=${PORT:-10000}
echo "[9/9] Starting API Gateway on port $PORT..."
echo "========================================="
echo " All services started! Gateway ready."
echo "========================================="
dotnet /app/gateway/CMS.ApiGateway.dll --urls "http://0.0.0.0:$PORT"
