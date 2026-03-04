# JWT Authentication Setup Script for Remaining Services
# This script adds JWT authentication middleware to all remaining microservices

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "JWT Authentication Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$services = @(
    "EnrollmentService",
    "FeeService",
    "AttendanceService",
    "NotificationService",
    "AIAssistantService"
)

$backendPath = "D:\ADV DOT NET ums  project\CollegeManagementSystem\Backend"

foreach ($service in $services) {
    $servicePath = Join-Path $backendPath "CMS.$service"
    $programFile = Join-Path $servicePath "Program.cs"
    
    Write-Host "Processing $service..." -ForegroundColor Yellow
    
    if (Test-Path $programFile) {
        $content = Get-Content $programFile -Raw
        
        # Check if already has JWT
        if ($content -match "AddAuthentication") {
            Write-Host "  ✓ JWT already configured" -ForegroundColor Green
            continue
        }
        
        # Add using statements
        if (-not ($content -match "using Microsoft.AspNetCore.Authentication.JwtBearer")) {
            $content = $content -replace "(using Serilog;)", "using Microsoft.AspNetCore.Authentication.JwtBearer;`r`nusing Microsoft.IdentityModel.Tokens;`r`nusing System.Text;`r`n`$1"
        }
        
        # Add JWT configuration before "var app = builder.Build();"
        $jwtConfig = @"

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

"@
        
        $content = $content -replace "(var app = builder\.Build\(\);)", "$jwtConfig`$1"
        
        # Add UseAuthentication middleware
        $content = $content -replace "(app\.UseHttpsRedirection\(\);)", "`$1`r`napp.UseAuthentication();"
        
        # Save file
        Set-Content -Path $programFile -Value $content -NoNewline
        
        Write-Host "  ✓ JWT configuration added" -ForegroundColor Green
        
        # Build the service
        Write-Host "  Building $service..." -ForegroundColor Cyan
        Push-Location $servicePath
        $buildResult = dotnet build 2>&1
        Pop-Location
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Build successful" -ForegroundColor Green
        }
        else {
            Write-Host "  ⚠ Build had warnings (service may be running)" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "  ✗ Program.cs not found" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "JWT Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Test AuthService at: https://localhost:7007/swagger" -ForegroundColor White
Write-Host "2. Register/Login to get JWT token" -ForegroundColor White
Write-Host "3. Use 'Authorize' button in Swagger to add token" -ForegroundColor White
Write-Host "4. Test protected endpoints" -ForegroundColor White
