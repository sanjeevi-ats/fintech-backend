# Logging System - Quick Start Guide

## What Was Implemented

✅ **Comprehensive logging across all 20 API controllers**  
✅ **Centralized error handling with try-catch blocks**  
✅ **Daily log files**: `Logs/log-yyyy-MM-dd.txt`  
✅ **Execution time tracking** for every endpoint  
✅ **Stored procedure audit logging** capability  
✅ **Zero breaking changes** to existing APIs  

---

## For Developers

### 1. **Using the Logger in Your Code**

```csharp
// Inject in constructor
private readonly IFileLoggerService _fileLogger;
private readonly ILogger<YourController> _logger;

public YourController(IFileLoggerService fileLogger, ILogger<YourController> logger)
{
    _fileLogger = fileLogger;
    _logger = logger;
}

// Use in action method
[HttpPost]
public async Task<IActionResult> YourAction([FromBody] YourRequest request)
{
    var stopwatch = LoggingHelper.StartTimer();
    try
    {
        // Your business logic here
        var result = await _service.DoSomethingAsync(request);
        var executionTime = LoggingHelper.StopTimer(stopwatch);
        
        await _fileLogger.LogInfoAsync(
            "Your API Name",
            nameof(YourController),
            nameof(YourAction),
            requestBody: request,
            responseData: result,
            executionTimeMs: executionTime,
            successMessage: "Operation successful"
        );
        
        return Ok(result);
    }
    catch (Exception ex)
    {
        var executionTime = LoggingHelper.StopTimer(stopwatch);
        _logger.LogError(ex, "Error in YourAction");
        
        await _fileLogger.LogErrorAsync(
            "Your API Name",
            nameof(YourController),
            nameof(YourAction),
            ex,
            requestBody: request,
            executionTimeMs: executionTime
        );
        
        return StatusCode(500, new { Success = false, Message = "An error occurred", Error = ex.Message });
    }
}
```

### 2. **Logging Stored Procedure Calls**

```csharp
var stopwatch = LoggingHelper.StartTimer();
var inputParams = new Dictionary<string, object?> { { "loanId", loanId } };

try
{
    await _context.Database.ExecuteSqlInterpolatedAsync($"CALL proc_approve_loan({loanId})");
    var executionTime = LoggingHelper.StopTimer(stopwatch);
    
    await _fileLogger.LogStoredProcedureAsync(
        "proc_approve_loan",
        inputParams,
        executionTime,
        result: new { message = "Loan approved" }
    );
}
catch (Exception ex)
{
    var executionTime = LoggingHelper.StopTimer(stopwatch);
    await _fileLogger.LogStoredProcedureAsync(
        "proc_approve_loan",
        inputParams,
        executionTime,
        exception: ex
    );
    throw;
}
```

---

## For DevOps / Operations

### 1. **Log Files Location**

```
D:\Finance\Backend\Fintech\Fintech\Fintech\Logs\
├── log-2026-06-08.txt
├── log-2026-06-09.txt
├── log-2026-06-10.txt
└── ... (daily files)
```

### 2. **Monitoring Log Files**

```powershell
# View latest logs (Windows PowerShell)
Get-Content "Logs\log-$(Get-Date -Format 'yyyy-MM-dd').txt" -Tail 50

# View logs with grep (PowerShell)
Select-String "ERROR" "Logs\log-*.txt"

# Search for specific endpoint
Select-String "AuthController" "Logs\log-*.txt"

# Count errors per day
(Select-String "LogLevel.*ERROR" "Logs\log-*.txt").Count
```

### 3. **Log Rotation Strategy**

**Current**: New file created daily  
**Recommended**: Archive logs monthly

```powershell
# Archive logs older than 90 days
$archivePath = "D:\Finance\Logs\Archive\"
Get-ChildItem "Logs\log-*.txt" | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-90) } | 
  Move-Item -Destination $archivePath
```

### 4. **Disk Space Monitoring**

```powershell
# Check log directory size
(Get-ChildItem "Logs\" -Recurse | Measure-Object -Property Length -Sum).Sum / 1GB

# Alert if logs exceed 10GB
if ((Get-ChildItem "Logs\" -Recurse | Measure-Object -Property Length -Sum).Sum -gt 10GB) {
    Write-Error "Log directory exceeded 10GB"
}
```

---

## For QA / Testing

### 1. **Verify Logging is Working**

**Test Case**: Create a customer via API

```bash
curl -X POST http://localhost:5000/api/v1/customers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"name": "Test", "email": "test@example.com"}'
```

**Verification**: Check `Logs/log-yyyy-MM-dd.txt` for entry with:
- ✓ ApiName: "Customer API"
- ✓ ActionName: "Create"
- ✓ ExecutionTimeMs: (milliseconds)
- ✓ SuccessMessage: "Customer created successfully"

### 2. **Verify Error Logging**

**Test Case**: Call API with invalid data

```bash
curl -X POST http://localhost:5000/api/v1/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"customerId": "invalid-uuid"}' # Wrong format
```

**Verification**: Check logs for entry with:
- ✓ LogLevel: "ERROR"
- ✓ ExceptionMessage: (the actual error)
- ✓ StackTrace: (call stack for debugging)

### 3. **Check Execution Times**

```powershell
# Extract all execution times for CollectionController
Select-String '"ControllerName": "CollectionController"' Logs\log-*.txt | 
  ForEach-Object { $_ -match '"ExecutionTimeMs": (\d+)' | Out-Null; [int]$matches[1] } | 
  Measure-Object -Average -Maximum -Minimum
```

---

## Troubleshooting

### Problem: Logs directory not created

**Cause**: Directory creation permission issue  
**Solution**:
```powershell
# Verify folder exists
Test-Path "D:\Finance\Backend\Fintech\Fintech\Fintech\Logs\"

# Create manually if needed
New-Item -ItemType Directory -Path "Logs" -Force

# Check permissions
Get-Acl "Logs\" | Select-Object Owner
```

### Problem: Logs not being written

**Cause**: IFileLoggerService not registered  
**Solution**: Verify in `Program.cs`:
```csharp
builder.Services.AddScoped<IFileLoggerService, FileLoggerService>();
```

### Problem: Slow log writing performance

**Cause**: Disk I/O bottleneck  
**Solution**: Consider implementing log buffering or moving logs to faster disk

### Problem: Large log files

**Cause**: High-traffic APIs generating many entries  
**Solution**: Implement log rotation or archival strategy

---

## API Endpoints Covered

**Authentication (5 endpoints)**
- POST /api/v1/auth/login
- POST /api/v1/auth/register
- POST /api/v1/auth/refresh-token
- POST /api/v1/auth/enable-totp
- POST /api/v1/auth/change-password

**Branch Management (7 endpoints)**
- GET /api/v1/branch
- GET /api/v1/branch/{id}
- GET /api/v1/branch/search
- POST /api/v1/branch
- PUT /api/v1/branch/{id}
- DELETE /api/v1/branch/{id}
- PUT /api/v1/branch/{id}/settings

**Collections (2 endpoints)**
- POST /api/collection/collect
- POST /api/collection/sync

**Loans (5 endpoints)**
- GET /api/v1/loancases
- POST /api/v1/loancases
- GET /api/v1/loancases/{id}
- POST /api/v1/loancases/{id}/approve
- POST /api/v1/loancases/{id}/disburse

**And 30+ more endpoints** across remaining controllers...

---

## Log File Example

```json
{
  "Timestamp": "2026-06-08T10:30:45.1234567Z",
  "LogLevel": "INFO",
  "ApiName": "Collection API",
  "ControllerName": "CollectionController",
  "ActionName": "Collect",
  "RequestParameters": {
    "installmentId": "550e8400-e29b-41d4-a716-446655440000",
    "amountPaid": 50000,
    "mode": "CASH"
  },
  "RequestBody": {
    "InstallmentId": "550e8400-e29b-41d4-a716-446655440000",
    "AmountPaid": 50000,
    "Mode": "CASH",
    "UtrRef": "TXN123456"
  },
  "ResponseData": {
    "Id": "660e8400-e29b-41d4-a716-446655440001",
    "PublicId": "RCP-A1B2C3D4",
    "AmountPaid": 50000,
    "Mode": "CASH",
    "CapturedAt": "2026-06-08T10:30:45Z"
  },
  "ExecutionTimeMs": 245,
  "SuccessMessage": "Payment collected successfully"
}
────────────────────────────────────────────────────────────────────────────
```

---

## Support

For issues or questions:
1. Check LOGGING_IMPLEMENTATION_SUMMARY.md for detailed documentation
2. Review sample logs for patterns
3. Check Program.cs for service registration
4. Verify directory permissions

**Implementation Complete ✅**
