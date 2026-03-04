#!/bin/bash
set -e

echo "========================================="
echo " CMS Backend - Starting All Services"
echo "========================================="

# Start all microservices in the background slowly to prevent OOM
echo "[1/9] Starting StudentService on port 5001..."
dotnet /app/services/CMS.StudentService/CMS.StudentService.dll --urls "http://0.0.0.0:5001" --contentRoot /app/services/CMS.StudentService &
sleep 2

echo "[2/9] Starting CourseService on port 5002..."
dotnet /app/services/CMS.CourseService/CMS.CourseService.dll --urls "http://0.0.0.0:5002" --contentRoot /app/services/CMS.CourseService &
sleep 2

echo "[3/9] Starting EnrollmentService on port 5003..."
dotnet /app/services/CMS.EnrollmentService/CMS.EnrollmentService.dll --urls "http://0.0.0.0:5003" --contentRoot /app/services/CMS.EnrollmentService &
sleep 2

echo "[4/9] Starting FeeService on port 5004..."
dotnet /app/services/CMS.FeeService/CMS.FeeService.dll --urls "http://0.0.0.0:5004" --contentRoot /app/services/CMS.FeeService &
sleep 2

echo "[5/9] Starting AttendanceService on port 5005..."
dotnet /app/services/CMS.AttendanceService/CMS.AttendanceService.dll --urls "http://0.0.0.0:5005" --contentRoot /app/services/CMS.AttendanceService &
sleep 2

echo "[6/9] Starting AIAssistantService on port 5006..."
dotnet /app/services/CMS.AIAssistantService/CMS.AIAssistantService.dll --urls "http://0.0.0.0:5006" --contentRoot /app/services/CMS.AIAssistantService &
sleep 2

echo "[7/9] Starting AuthService on port 5007..."
dotnet /app/services/CMS.AuthService/CMS.AuthService.dll --urls "http://0.0.0.0:5007" --contentRoot /app/services/CMS.AuthService &
sleep 2

echo "[8/9] Starting AcademicService on port 5008..."
dotnet /app/services/CMS.AcademicService/CMS.AcademicService.dll --urls "http://0.0.0.0:5008" --contentRoot /app/services/CMS.AcademicService &
sleep 2

echo "[BOT] Starting TelegramBot on port 5009..."
dotnet /app/services/CMS.TelegramService/CMS.TelegramService.dll --urls "http://0.0.0.0:5009" --contentRoot /app/services/CMS.TelegramService &

# Wait for services to finish their initial startup
echo "Waiting for services to initialize..."
sleep 5

# ─── Auto-Ping Keepalive (prevents Render free tier from sleeping) ───
RENDER_URL="${RENDER_EXTERNAL_URL:-https://collegemanagementsystem-2gp3.onrender.com}"
echo "[KEEPALIVE] Starting auto-ping every 5 minutes to $RENDER_URL"
(
  sleep 30  # Wait for gateway to fully start
  while true; do
    curl -s -o /dev/null -w "PING %{http_code} in %{time_total}s" "$RENDER_URL/swagger/index.html" 2>/dev/null || true
    echo " [$(date '+%H:%M:%S')]"
    sleep 300  # 5 minutes
  done
) &

# Start the API Gateway (foreground - Render monitors this process)
PORT=${PORT:-10000}
echo "[9/9] Starting API Gateway on port $PORT..."
echo "========================================="
echo " All services started! Gateway ready."
echo "========================================="
dotnet /app/gateway/CMS.ApiGateway.dll --urls "http://0.0.0.0:$PORT" --contentRoot /app/gateway
