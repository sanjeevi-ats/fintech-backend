# Controller-Wise Logging - Quick Start Guide

## 🚀 30-Second Setup

The controller-wise logging system is **already installed and ready to use**. Just add one attribute to your controllers!

---

## 📝 5-Minute Implementation

### Step 1: Add AutoLog Attribute to Your Controller
```csharp
using Fintech.Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;

[AutoLog]  // ← Add this
[ApiController]
[Route("api/[controller]")]
public class MyController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MyRequest request)
    {
        // All logs automatically go to:
        // Logs/Controllers/My/log-2024-06-13.txt
        var result = await _service.CreateAsync(request);
        return Ok(result);
    }
}
```

### Step 2: Run Your Application
```bash
dotnet run
```

### Step 3: Make an API Call
```bash
curl -X POST https://localhost:5001/api/my \
  -H "Content-Type: application/json" \
  -d '{"name": "Test"}'
```

### Step 4: View the Logs
Check: `Logs/Controllers/My/log-2024-06-13.txt`

```json
{
  "Timestamp": "2024-06-13T10:30:45.1234567Z",
  "LogLevel": "INFO",
  "ControllerName": "MyController",
  "ActionName": "Create",
  "RequestParameters": {...},
  "ResponseData": {...},
  "ExecutionTimeMs": 125,
  "SuccessMessage": "Create completed successfully"
}
```

---

## 🎯 Common Scenarios

### Scenario 1: Log All Controller Methods (EASIEST)
```csharp
[AutoLog]  // Apply to entire controller
public class CustomersController : BaseApiController
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(int id) { }
    
    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateRequest req) { }
    
    // All methods are logged automatically
}
```

### Scenario 2: Log Only Specific Methods
```csharp
public class OrdersController : BaseApiController
{
    [AutoLog]  // Only this method is logged
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req) { }
    
    [HttpGet]
    public async Task<IActionResult> ListOrders() { }  // Not logged
}
```

### Scenario 3: Don't Log Request Body (Passwords)
```csharp
[AutoLog(logRequestBody: false)]  // Don't log the password
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request) { }
```

### Scenario 4: Don't Log Response Body (Large Data)
```csharp
[AutoLog(logResponseBody: false)]  // Don't log huge response
[HttpGet("large-report")]
public async Task<IActionResult> GetLargeReport() { }
```

---

## 📊 View All Logs

### Option A: Query via API
```bash
# Get summary of all controllers
curl https://localhost:5001/api/loganalysis/summary

# Get errors from a specific controller
curl https://localhost:5001/api/loganalysis/controller/Customers/errors

# Get slow operations (>1 second)
curl https://localhost:5001/api/loganalysis/controller/Collection/slow?thresholdMs=1000

# Get stats for a specific action
curl https://localhost:5001/api/loganalysis/controller/Collection/action/Collect/stats
```

### Option B: Read Files Directly
```bash
# Windows - view a controller's log
type Logs\Controllers\Collection\log-2024-06-13.txt

# Windows - see all controllers with logs
dir Logs\Controllers

# Find slow operations in logs
findstr "ExecutionTimeMs" Logs\Controllers\Collection\log-2024-06-13.txt
```

---

## ⚡ Performance Tips

### For High-Traffic Endpoints
```csharp
[AutoLog(logResponseBody: false)]  // Skip response body
[HttpGet("list")]
public async Task<IActionResult> ListAll() { }
```

### For Sensitive Endpoints
```csharp
[AutoLog(logRequestBody: false)]  // Skip request body
[HttpPost("verify-password")]
public async Task<IActionResult> VerifyPassword([FromBody] VerifyRequest req) { }
```

---

## 🔍 Finding Issues

### Issue 1: Find All Errors in a Controller
```bash
# Windows PowerShell
$errors = Get-Content Logs\Controllers\Collection\log-2024-06-13.txt | 
          ConvertFrom-Json | 
          Where-Object { $_.LogLevel -eq "ERROR" }
$errors | Format-Table

# Or use the API
curl https://localhost:5001/api/loganalysis/controller/Collection/errors
```

### Issue 2: Find Slow Operations
```bash
# Windows PowerShell
$logs = Get-Content Logs\Controllers\Collection\log-*.txt | ConvertFrom-Json
$slow = $logs | Where-Object { $_.ExecutionTimeMs -gt 1000 }
$slow | Select-Object Timestamp, ActionName, ExecutionTimeMs
```

### Issue 3: Track Errors Over Time
```bash
# Check for spikes
curl https://localhost:5001/api/loganalysis/errors/summary
```

---

## 📋 Migration Checklist

Convert your existing controllers one by one:

- [ ] Add `[AutoLog]` attribute to controller
- [ ] Remove `IFileLoggerService` injection (if using old logging)
- [ ] Remove `try-catch-log` blocks (AutoLog handles it)
- [ ] Remove `LoggingHelper.StartTimer()` calls (not needed anymore)
- [ ] Test the controller
- [ ] Check logs in `Logs/Controllers/{ControllerName}/`
- [ ] Move to next controller

### Example Conversion

**Before:**
```csharp
public class UsersController : BaseApiController
{
    private readonly IFileLoggerService _fileLogger;
    
    public UsersController(IFileLoggerService fileLogger)
    {
        _fileLogger = fileLogger;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var stopwatch = LoggingHelper.StartTimer();
        try
        {
            var user = await _service.CreateUserAsync(request);
            var executionTime = LoggingHelper.StopTimer(stopwatch);
            
            await _fileLogger.LogInfoAsync(
                "Users API",
                nameof(UsersController),
                nameof(CreateUser),
                requestBody: request,
                responseData: user,
                executionTimeMs: executionTime
            );
            
            return Created($"api/users/{user.Id}", user);
        }
        catch (Exception ex)
        {
            var executionTime = LoggingHelper.StopTimer(stopwatch);
            await _fileLogger.LogErrorAsync(
                "Users API",
                nameof(UsersController),
                nameof(CreateUser),
                ex,
                requestBody: request
            );
            throw;
        }
    }
}
```

**After:**
```csharp
[AutoLog]
[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _service.CreateUserAsync(request);
        return Created($"api/users/{user.Id}", user);
    }
}
```

**Result:** 
- ✅ 40+ lines reduced to 2 lines
- ✅ All logging automatic
- ✅ Logs go to `Logs/Controllers/Users/log-yyyy-MM-dd.txt`

---

## 🆘 Troubleshooting

### Q: No logs appearing?
**A:** 
1. Check `Logs/Controllers` folder exists
2. Verify `[AutoLog]` attribute is applied
3. Make an actual API call to the method

### Q: Logs in wrong location?
**A:** 
Check folder structure: `Logs/Controllers/{ControllerName}/`  
Controller name is sanitized (without "Controller" suffix)

### Q: Too many logs?
**A:** 
Use `[AutoLog(logResponseBody: false)]` for large responses

### Q: Want to see specific errors?
**A:** 
Use API: `GET /api/loganalysis/controller/{name}/errors`

---

## 📚 Full Documentation

For complete documentation, see:
- `CONTROLLER_WISE_LOGGING_IMPLEMENTATION.md` - Complete guide
- `LOGGING_CONFIGURATION.md` - Detailed reference
- `LogAnalysisController.cs` - API endpoint documentation
- `AutoLogAttribute.cs` - Implementation details

---

## ✅ You're Done!

That's it! Your controller is now logging to controller-specific files.

**What happens next:**
1. Each controller method execution is logged
2. Logs are in JSON format (easy to parse)
3. Daily log files rotate automatically
4. Query logs via REST API
5. Monitor performance and errors

**Supported Controllers:**
All 20 existing controllers will automatically create log files when you add `[AutoLog]`:
- Auth, Collection, Customers, LoanCases, Products
- Branches, Users, Partners, Installments, Receipts
- Audit, Journal, DayEnd, Recovery, LoanClosure
- Report, CapitalAccounts, WeatherForecast, and more

---

## 🎓 Learn More

Start here:
1. Add `[AutoLog]` to one controller
2. Check logs in `Logs/Controllers/`
3. Query via `/api/loganalysis/summary`
4. Move to next controller

Questions? See `CONTROLLER_WISE_LOGGING_IMPLEMENTATION.md`
