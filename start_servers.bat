@echo off
echo Starting Backend Services...

cd Backend

start "API Gateway" dotnet run --project CMS.ApiGateway/CMS.ApiGateway.csproj --launch-profile http
timeout /t 5

start "Auth Service" dotnet run --project CMS.AuthService/CMS.AuthService.csproj --launch-profile http
timeout /t 5

echo Services Started!
