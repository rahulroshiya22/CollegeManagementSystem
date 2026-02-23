###############################################
#  College Management System - Start All Services
#  Run: powershell -ExecutionPolicy Bypass -File StartAllServices.ps1
###############################################

$root = "D:\ADV DOT NET ums  project\CollegeManagementSystem"

Write-Host ""
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host "    NeoVerse CMS - Starting All Services" -ForegroundColor Cyan
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host ""

# Kill any existing dotnet processes to avoid port conflicts
$existing = Get-Process dotnet -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "  (~) Stopping existing dotnet processes..." -ForegroundColor Yellow
    $existing | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Kill any python http.server on port 3000
try {
    $pyProcs = Get-NetTCPConnection -LocalPort 3000 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($p in $pyProcs) {
        Stop-Process -Id $p -Force -ErrorAction SilentlyContinue
    }
}
catch {}

# Define all services
$services = @(
    @{ Name = "StudentService"; Port = 7001; Path = "$root\Backend\CMS.StudentService" },
    @{ Name = "CourseService"; Port = 7002; Path = "$root\Backend\CMS.CourseService" },
    @{ Name = "EnrollmentService"; Port = 7003; Path = "$root\Backend\CMS.EnrollmentService" },
    @{ Name = "FeeService"; Port = 7004; Path = "$root\Backend\CMS.FeeService" },
    @{ Name = "AttendanceService"; Port = 7005; Path = "$root\Backend\CMS.AttendanceService" },
    @{ Name = "AIService"; Port = 7006; Path = "$root\Backend\CMS.AIService" },
    @{ Name = "AuthService"; Port = 7007; Path = "$root\Backend\CMS.AuthService" },
    @{ Name = "AcademicService"; Port = 7008; Path = "$root\Backend\CMS.AcademicService" }
)

# Start each microservice
foreach ($svc in $services) {
    Write-Host "  >> Starting $($svc.Name) on port $($svc.Port)..." -ForegroundColor Green
    $cmd = "Set-Location '$($svc.Path)'; dotnet run --launch-profile https"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", $cmd -WindowStyle Minimized
    Start-Sleep -Milliseconds 800
}

# Wait for services to build and start
Write-Host ""
Write-Host "  (~) Waiting 20 seconds for services to build..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

# Start API Gateway last (so downstream services are ready)
Write-Host "  >> Starting API Gateway on port 7000..." -ForegroundColor Cyan
$gwCmd = "Set-Location '$root\Backend\CMS.ApiGateway'; dotnet run --launch-profile https"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $gwCmd -WindowStyle Minimized

# Start Frontend server
Write-Host "  >> Starting Frontend on port 3000..." -ForegroundColor Magenta
$feCmd = "Set-Location '$root\Frontend2'; python -m http.server 3000"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $feCmd -WindowStyle Minimized

# Wait for gateway to start
Write-Host "  (~) Waiting 12 seconds for gateway..." -ForegroundColor Yellow
Start-Sleep -Seconds 12

# Health check all services
Write-Host ""
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host "           Service Health Check" -ForegroundColor Cyan
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host ""

try { [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true } } catch {}

$checks = @(
    @{ Name = "Student Service    (7001)"; Url = "https://localhost:7001/api/Student" },
    @{ Name = "Course Service     (7002)"; Url = "https://localhost:7002/api/Course" },
    @{ Name = "Enrollment Service (7003)"; Url = "https://localhost:7003/api/Enrollment" },
    @{ Name = "Fee Service        (7004)"; Url = "https://localhost:7004/api/Fee" },
    @{ Name = "Attendance Service (7005)"; Url = "https://localhost:7005/api/Attendance" },
    @{ Name = "Auth Service       (7007)"; Url = "https://localhost:7007/api/Auth/users" },
    @{ Name = "Academic Service   (7008)"; Url = "https://localhost:7008/api/TimeSlot" },
    @{ Name = "API Gateway        (7000)"; Url = "https://localhost:7000/api/course" },
    @{ Name = "Frontend           (3000)"; Url = "http://localhost:3000" }
)

$passed = 0
$failed = 0
foreach ($c in $checks) {
    try {
        $wc = New-Object System.Net.WebClient
        $null = $wc.DownloadString($c.Url)
        Write-Host "  OK  $($c.Name)" -ForegroundColor Green
        $passed++
    }
    catch {
        Write-Host "  ERR $($c.Name)" -ForegroundColor Red
        $failed++
    }
}

Write-Host ""
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host "  Results: $passed passed, $failed failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Frontend:  http://localhost:3000" -ForegroundColor Cyan
Write-Host "  Gateway:   https://localhost:7000" -ForegroundColor Cyan
Write-Host "  Swagger:   https://localhost:7000/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Login Credentials:" -ForegroundColor White
Write-Host "  Admin:   admin@cms.com / Admin@123" -ForegroundColor Gray
Write-Host "  Teacher: priya.sharma@cms.com / Teacher@123" -ForegroundColor Gray
Write-Host "  Student: student@cms.com / Student@123" -ForegroundColor Gray
Write-Host ""
