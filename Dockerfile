# =============================================
# CMS Backend - Single Dockerfile for Render
# Place at REPO ROOT - builds from repo root context
# =============================================

# --- BUILD STAGE ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and all project files first (for better layer caching)
COPY Backend/CollegeManagementSystem.sln ./
COPY Backend/CMS.ApiGateway/CMS.ApiGateway.csproj CMS.ApiGateway/
COPY Backend/CMS.AuthService/CMS.AuthService.csproj CMS.AuthService/
COPY Backend/CMS.StudentService/CMS.StudentService.csproj CMS.StudentService/
COPY Backend/CMS.CourseService/CMS.CourseService.csproj CMS.CourseService/
COPY Backend/CMS.EnrollmentService/CMS.EnrollmentService.csproj CMS.EnrollmentService/
COPY Backend/CMS.FeeService/CMS.FeeService.csproj CMS.FeeService/
COPY Backend/CMS.AttendanceService/CMS.AttendanceService.csproj CMS.AttendanceService/
COPY Backend/CMS.AcademicService/CMS.AcademicService.csproj CMS.AcademicService/
COPY Backend/CMS.AIAssistantService/CMS.AIAssistantService.csproj CMS.AIAssistantService/
COPY Backend/CMS.AIService/CMS.AIService.csproj CMS.AIService/
COPY Backend/CMS.NotificationService/CMS.NotificationService.csproj CMS.NotificationService/
COPY Backend/CMS.TelegramService/CMS.TelegramService.csproj CMS.TelegramService/
COPY Backend/CMS.Common.Messaging/CMS.Common.Messaging.csproj CMS.Common.Messaging/

# Restore all packages
RUN dotnet restore CollegeManagementSystem.sln

# Copy all backend source code
COPY Backend/ .

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
COPY Backend/CMS.ApiGateway/ocelot.docker.json /app/gateway/ocelot.json

# Copy startup script
COPY Backend/start-all.sh /app/start-all.sh

# --- RUNTIME STAGE ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

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

ENTRYPOINT ["./start-all.sh"]
