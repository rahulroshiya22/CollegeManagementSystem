# 🔧 RabbitMQ Error Diagnosis and Solutions

## 📋 Common RabbitMQ Errors and Solutions

### Issue 1: Connection Errors
**Symptoms:**
- Services fail to start
- "Connection refused" errors
- "Unable to connect to CloudAMQP" errors

**Solutions:**
1. **Check CloudAMQP Connection String**
   - Verify the connection string in `appsettings.json` is correct
   - Current connection: `amqps://jxlnlvrr:aS15sCekjI13tw8SC4jEEuYi_cIq4ENS@duck.lmq.cloudamqp.com/jxlnlvrr`
   
2. **Verify CloudAMQP Instance is Active**
   - Login to: https://customer.cloudamqp.com/
   - Check if instance is running
   - Check connection limits

3. **Network/Firewall Issues**
   - Ensure port 5671 (AMQPS) is not blocked
   - Check if your network allows outbound AMQPS connections

---

### Issue 2: Queue Doesn't Exist
**Symptoms:**
- Messages not being consumed
- Queue not visible in CloudAMQP dashboard

**Solution:**
1. Restart **FeeService** - it auto-creates the queue on startup
2. Check logs for queue creation messages

---

### Issue 3: Messages Not Flowing
**Symptoms:**
- Enrollment created but fee not generated
- No messages in CloudAMQP dashboard

**Diagnostic Steps:**
1. **Check EnrollmentService Logs**
   - Look for: `"Published StudentEnrolled event"`
   - If missing, publishing failed

2. **Check FeeService Logs**
   - Look for: `"Received from student-enrolled"`
   - Look for: `"Fee auto-generated for Student"`

3. **Check CloudAMQP Dashboard**
   - Go to Queues tab
   - Look for `student-enrolled` exchange
   - Check message rates

---

### Issue 4: RabbitMQ Client Version Mismatch
**Symptoms:**
- Compilation errors
- Runtime errors related to `IModel` vs `IChannel`

**Current Setup:**
- Using RabbitMQ.Client 7.2.0 (newer async API)
- Uses `IChannel` instead of `IModel`
- All methods are async (`CreateConnectionAsync`, `CreateChannelAsync`, etc.)

**Solution if error occurs:**
```powershell
# Ensure consistent version across all projects
dotnet remove Backend/CMS.Common.Messaging/CMS.Common.Messaging.csproj package RabbitMQ.Client
dotnet add Backend/CMS.Common.Messaging/CMS.Common.Messaging.csproj package RabbitMQ.Client --version 7.2.0

dotnet remove Backend/CMS.EnrollmentService/CMS.EnrollmentService.csproj package RabbitMQ.Client  
dotnet add Backend/CMS.EnrollmentService/CMS.EnrollmentService.csproj package RabbitMQ.Client --version 7.2.0

dotnet remove Backend/CMS.FeeService/CMS.FeeService.csproj package RabbitMQ.Client
dotnet add Backend/CMS.FeeService/CMS.FeeService.csproj package RabbitMQ.Client --version 7.2.0
```

---

## 🧪 Testing RabbitMQ Connection

### Step 1: Manual Connection Test
Create a test file to verify CloudAMQP connection:

```csharp
using RabbitMQ.Client;

var factory = new ConnectionFactory
{
    Uri = new Uri("amqps://jxlnlvrr:aS15sCekjI13tw8SC4jEEuYi_cIq4ENS@duck.lmq.cloudamqp.com/jxlnlvrr"),
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
};

try
{
    var connection = await factory.CreateConnectionAsync();
    var channel = await connection.CreateChannelAsync();
    Console.WriteLine("✅ Successfully connected to CloudAMQP!");
    
    await connection.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Connection failed: {ex.Message}");
}
```

### Step 2: Test Message Publishing
1. Start FeeService first (creates queue)
2. Start EnrollmentService
3. Create an enrollment via Swagger
4. Check console logs

**Expected Output:**
```
EnrollmentService console:
---> Published to student-enrolled: {"StudentId":1,"CourseId":1,"Semester":1,"Year":2024}

FeeService console:
---> Received from student-enrolled: {"StudentId":1,"CourseId":1,"Semester":1,"Year":2024}
---> Fee auto-generated for Student 1: $5000
```

---

## 🎯 Quick Fixes

### Fix 1: Restart Services in Correct Order
```powershell
# Stop all services
# Start in this order:
1. Start FeeService (subscriber) - creates the queue
2. Start EnrollmentService (publisher)
3. Test enrollment creation
```

### Fix 2: Clear Old Queues (if stuck)
1. Go to CloudAMQP Dashboard
2. Click on Queues tab
3. Delete `student-enrolled` queue if it exists
4. Restart FeeService to recreate it

### Fix 3: Check appsettings.json
Ensure both services have the same connection string:

**EnrollmentService/appsettings.json:**
```json
{
  "CloudAMQP": {
    "ConnectionString": "amqps://jxlnlvrr:aS15sCekjI13tw8SC4jEEuYi_cIq4ENS@duck.lmq.cloudamqp.com/jxlnlvrr"
  }
}
```

**FeeService/appsettings.json:**
```json
{
  "CloudAMQP": {
    "ConnectionString": "amqps://jxlnlvrr:aS15sCekjI13tw8SC4jEEuYi_cIq4ENS@duck.lmq.cloudamqp.com/jxlnlvrr"
  }
}
```

---

## 📊 Monitoring and Debugging

### Enable Detailed Logging
Update `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "RabbitMQ.Client": "Debug"
    }
  }
}
```

### Check CloudAMQP Dashboard
1. **Overview Tab**: Check connection count (should be 2)
2. **Connections Tab**: Verify both services are connected
3. **Queues Tab**: Monitor message flow
4. **Exchanges Tab**: Verify `student-enrolled` exchange exists

---

## 🚨 Error Messages Reference

| Error Message | Cause | Solution |
|--------------|-------|----------|
| `BrokerUnreachableException` | CloudAMQP not reachable | Check connection string and network |
| `OperationInterruptedException` | Queue/Exchange doesn't exist | Restart FeeService |
| `IOException: Connection reset` | Network issue | Check firewall, retry connection |
| `PossibleAuthenticationFailureException` | Wrong credentials | Verify connection string |
| `NotSupportedException: PublishAsync` | Old RabbitMQ.Client version | Update to 7.x |

---

## ✅ Health Check Checklist

Before testing RabbitMQ:
- [ ] CloudAMQP instance is active
- [ ] Both services have correct connection string
- [ ] FeeService started before EnrollmentService
- [ ] No firewall blocking port 5671
- [ ] RabbitMQ.Client version is 7.2.0 in all projects
- [ ] Console logs are visible for both services

---

## 🔗 Useful Links
- CloudAMQP Dashboard: https://customer.cloudamqp.com/
- RabbitMQ Documentation: https://www.rabbitmq.com/docs
- RabbitMQ.Client GitHub: https://github.com/rabbitmq/rabbitmq-dotnet-client

---

**Last Updated:** 2026-01-06
