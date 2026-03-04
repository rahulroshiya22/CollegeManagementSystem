# PowerShell script to add Serilog packages to all microservices
$services = @(
    "CMS.CourseService",
    "CMS.EnrollmentService",
    "CMS.FeeService",
    "CMS.AttendanceService",
    "CMS.NotificationService",
    "CMS.ApiGateway"
)

$packages = @(
    "Serilog.AspNetCore",
    "Serilog.Sinks.Seq",
    "Serilog.Sinks.Console",
    "Serilog.Sinks.File",
    "Serilog.Enrichers.Environment"
)

foreach ($service in $services) {
    Write-Host "Installing Serilog packages in $service..." -ForegroundColor Cyan
    $path = "d:\ADV DOT NET ums  project\CollegeManagementSystem\Backend\$service"
    
    foreach ($package in $packages) {
        dotnet add "$path\$service.csproj" package $package
    }
    
    Write-Host "Completed $service" -ForegroundColor Green
}

Write-Host "`nAll packages installed!" -ForegroundColor Green
