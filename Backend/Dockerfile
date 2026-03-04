# =============================================
# CMS Backend - Single Dockerfile for Render
# Builds ALL microservices + API Gateway
# =============================================

# --- BUILD STAGE ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and all project files first (for better layer caching)
COPY CollegeManagementSystem.sln ./
COPY CMS.ApiGateway/CMS.ApiGateway.csproj CMS.ApiGateway/
COPY CMS.AuthService/CMS.AuthService.csproj CMS.AuthService/
COPY CMS.StudentService/CMS.StudentService.csproj CMS.StudentService/
COPY CMS.CourseService/CMS.CourseService.csproj CMS.CourseService/
COPY CMS.EnrollmentService/CMS.EnrollmentService.csproj CMS.EnrollmentService/
COPY CMS.FeeService/CMS.FeeService.csproj CMS.FeeService/
COPY CMS.AttendanceService/CMS.AttendanceService.csproj CMS.AttendanceService/
COPY CMS.AcademicService/CMS.AcademicService.csproj CMS.AcademicService/
COPY CMS.AIAssistantService/CMS.AIAssistantService.csproj CMS.AIAssistantService/
COPY CMS.AIService/CMS.AIService.csproj CMS.AIService/
COPY CMS.NotificationService/CMS.NotificationService.csproj CMS.NotificationService/
COPY CMS.TelegramService/CMS.TelegramService.csproj CMS.TelegramService/
COPY CMS.Common.Messaging/CMS.Common.Messaging.csproj CMS.Common.Messaging/

# Restore all packages
RUN dotnet restore CollegeManagementSystem.sln

# Copy all source code
COPY . .

# Publish each service
RUN dotnet publish CMS.StudentService/CMS.StudentService.csproj -c Release -o /app/services/CMS.StudentService --no-restore
RUN dotnet publish CMS.CourseService/CMS.CourseService.csproj -c Release -o /app/services/CMS.CourseService --no-restore
RUN dotnet publish CMS.EnrollmentService/CMS.EnrollmentService.csproj -c Release -o /app/services/CMS.EnrollmentService --no-restore
RUN dotnet publish CMS.FeeService/CMS.FeeService.csproj -c Release -o /app/services/CMS.FeeService --no-restore
RUN dotnet publish CMS.AttendanceService/CMS.AttendanceService.csproj -c Release -o /app/services/CMS.AttendanceService --no-restore
RUN dotnet publish CMS.AuthService/CMS.AuthService.csproj -c Release -o /app/services/CMS.AuthService --no-restore
RUN dotnet publish CMS.AcademicService/CMS.AcademicService.csproj -c Release -o /app/services/CMS.AcademicService --no-restore
RUN dotnet publish CMS.AIAssistantService/CMS.AIAssistantService.csproj -c Release -o /app/services/CMS.AIAssistantService --no-restore
RUN dotnet publish CMS.ApiGateway/CMS.ApiGateway.csproj -c Release -o /app/gateway --no-restore

# Copy docker-specific ocelot config to gateway
COPY CMS.ApiGateway/ocelot.docker.json /app/gateway/ocelot.json

# Copy startup script
COPY start-all.sh /app/start-all.sh

# --- RUNTIME STAGE ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install .NET 10 runtime for AIAssistantService (targets net10.0)
# For now, we'll skip this service or it needs to be downgraded
# The aspnet:8.0 image handles all net8.0 services

# Copy published services
COPY --from=build /app/services ./services
COPY --from=build /app/gateway ./gateway

# Copy startup script
COPY --from=build /app/start-all.sh ./start-all.sh
RUN chmod +x ./start-all.sh

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose Render's default port
EXPOSE 10000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD curl -f http://localhost:${PORT:-10000}/api/student || exit 1

ENTRYPOINT ["./start-all.sh"]
