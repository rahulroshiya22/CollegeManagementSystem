# PowerShell script to update appsettings.json for all remaining services
$services = @(
    @{Name = "CMS.EnrollmentService"; DB = "clgmansys3"; LogFile = "enrollmentservice-" },
    @{Name = "CMS.FeeService"; DB = "clgmansys4"; LogFile = "feeservice-" },
    @{Name = "CMS.AttendanceService"; DB = "clgmansys5"; LogFile = "attendanceservice-" },
    @{Name = "CMS.NotificationService"; DB = "clgmansys6"; LogFile = "notificationservice-" },
    @{Name = "CMS.ApiGateway"; DB = ""; LogFile = "apigateway-" }
)

foreach ($service in $services) {
    $serviceName = $service.Name
    $logFile = $service.LogFile
    $appName = $serviceName
    
    Write-Host "Updating appsettings.json for $serviceName..." -ForegroundColor Cyan
    
    $path = "d:\ADV DOT NET ums  project\CollegeManagementSystem\Backend\$serviceName\appsettings.json"
    
    # Read existing appsettings
    $settings = Get-Content $path -Raw | ConvertFrom-Json
    
    # Create Serilog configuration
    $serilogConfig = @{
        Using        = @("Serilog.Sinks.Console", "Serilog.Sinks.Seq", "Serilog.Sinks.File")
        MinimumLevel = @{
            Default  = "Information"
            Override = @{
                Microsoft                    = "Warning"
                System                       = "Warning"
                "Microsoft.Hosting.Lifetime" = "Information"
            }
        }
        Enrich       = @("FromLogContext", "WithMachineName", "WithThreadId")
        WriteTo      = @(
            @{
                Name = "Console"
                Args = @{
                    outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {Message:lj}{NewLine}{Exception}"
                }
            },
            @{
                Name = "File"
                Args = @{
                    path                   = "logs/$logFile.txt"
                    rollingInterval        = "Day"
                    retainedFileCountLimit = 7
                    outputTemplate         = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {TraceId} {Message:lj}{NewLine}{Exception}"
                }
            },
            @{
                Name = "Seq"
                Args = @{
                    serverUrl = "http://localhost:5341"
                }
            }
        )
        Properties   = @{
            Application = $appName
        }
    }
    
    # Remove old Logging section and add Serilog
    $settings.PSObject.Properties.Remove('Logging')
    $settings | Add-Member -MemberType NoteProperty -Name "Serilog" -Value $serilogConfig -Force
    
    # Save updated settings
    $settings | ConvertTo-Json -Depth 10 | Set-Content $path
    
    Write-Host "Updated $serviceName" -ForegroundColor Green
}

Write-Host "`nAll appsettings.json files updated!" -ForegroundColor Green
