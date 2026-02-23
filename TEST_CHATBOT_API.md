## Test AI Chatbot API

Open PowerShell and run this command to test if the API is working:

```powershell
$body = @{
    studentId = 1
    message = "Hello!"
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://localhost:7000/api/chat/message" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body `
    -SkipCertificateCheck
```

If you get an error about `SkipCertificateCheck`, try:

```powershell
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/chat/message" -Method POST -ContentType "application/json" -Body $body
$response
```

This will test if the chatbot API is responding correctly!
