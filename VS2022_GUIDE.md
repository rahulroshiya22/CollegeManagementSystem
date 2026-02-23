# Running College Management System in Visual Studio 2022

## Prerequisites
- Visual Studio 2022
- .NET SDK installed
- SQL Server LocalDB or SQL Server instance
- CloudAMQP account (for RabbitMQ messaging)

## Opening the Solution

1. Navigate to the project folder: `d:\ADV DOT NET ums  project\CollegeManagementSystem\`
2. Double-click `CollegeManagementSystem.sln` to open in Visual Studio 2022

## Running All Microservices

### Option 1: Using Visual Studio Multi-Startup

1. Open the solution in Visual Studio 2022
2. In the toolbar, you'll see a dropdown next to the Start button
3. Select **"All Microservices"** from the dropdown
4. Click the **Start** button (or press F5)

This will start all 7 microservices simultaneously:
- Student Service (Port 7001)
- Course Service (Port 7002)
- Enrollment Service (Port 7003)
- Fee Service (Port 7004)
- Attendance Service (Port 7005)
- AI Assistant Service (Port 7006)
- API Gateway (Port 7000)

### Option 2: Using PowerShell Script

Alternatively, you can run the PowerShell script from within VS:
1. Open Terminal in Visual Studio (View → Terminal)
2. Run: `.\StartAllServices.ps1`

## Accessing the Application

Once all services are running:

- **Dashboard**: Open `Dashboard.html` in Chrome
- **API Gateway Swagger**: https://localhost:7000/swagger
- **Individual Service Swagger UIs**:
  - Student: https://localhost:7001/swagger
  - Course: https://localhost:7002/swagger
  - Enrollment: https://localhost:7003/swagger
  - Fee: https://localhost:7004/swagger
  - Attendance: https://localhost:7005/swagger
  - AI Assistant: https://localhost:7006/swagger

## Debugging

To debug a specific service:
1. Right-click on the service project in Solution Explorer
2. Select **Debug → Start New Instance**
3. Set breakpoints in the code as needed

## Stopping Services

- Click the **Stop** button in Visual Studio toolbar
- Or close all debugging windows

## Project Structure

```
CollegeManagementSystem/
├── Backend/
│   ├── CMS.StudentService/          # Student management
│   ├── CMS.CourseService/           # Course management
│   ├── CMS.EnrollmentService/       # Enrollment handling
│   ├── CMS.FeeService/              # Fee management
│   ├── CMS.AttendanceService/       # Attendance tracking
│   ├── CMS.AIAssistantService/      # AI Chatbot
│   ├── CMS.ApiGateway/              # Ocelot Gateway
│   └── CMS.Common.Messaging/        # Shared RabbitMQ library
├── Frontend/                         # (If applicable)
├── Dashboard.html                    # Main dashboard
└── CollegeManagementSystem.sln      # VS 2022 Solution
```

## Configuration

Each microservice has its own `appsettings.json` for:
- Database connection strings
- RabbitMQ settings (CloudAMQP)
- Service-specific configurations

Make sure to update these if needed before running.

## Troubleshooting

### Services not starting
- Check that all required NuGet packages are restored
- Verify SQL Server is running
- Check port availability (7000-7006)

### Build errors
- Clean and rebuild solution (Build → Clean Solution, then Build → Rebuild Solution)
- Restore NuGet packages (Right-click solution → Restore NuGet Packages)

### RabbitMQ connection issues
- Verify CloudAMQP credentials in appsettings.json
- Check internet connectivity

## Architecture

This is a **microservices-based architecture** using:
- **ASP.NET Core Web API** for services
- **Entity Framework Core** for data access
- **Dapper** for high-performance queries
- **Ocelot** for API Gateway
- **RabbitMQ** (CloudAMQP) for event-driven messaging
- **Google Gemini AI** for chatbot functionality
