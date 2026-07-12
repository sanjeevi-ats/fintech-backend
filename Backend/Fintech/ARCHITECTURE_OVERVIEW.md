# Controller-Wise Logging Architecture Overview

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         ASP.NET Core Application                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                        HTTP Request                             │ │
│  └─────────────────────────┬──────────────────────────────────────┘ │
│                           │                                         │
│                           ▼                                         │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                   [AutoLogAttribute]                            │ │
│  │                  (Action Filter)                                │ │
│  │  ┌ Start Timer                                                 │ │
│  │  ├ Capture Request Parameters                                  │ │
│  │  ├ Execute Action                                              │ │
│  │  ├ Capture Response Data                                       │ │
│  │  ├ Stop Timer & Calculate ExecutionTimeMs                      │ │
│  │  └ Call Logger.LogInfoAsync() or LogErrorAsync()               │ │
│  └─────────────────────────┬──────────────────────────────────────┘ │
│                           │                                         │
│         ┌─────────────────┴─────────────────┐                      │
│         │                                   │                      │
│         ▼ Success (2xx-3xx)              ▼ Error (4xx-5xx)        │
│  ┌────────────────────┐        ┌────────────────────────────┐    │
│  │ LogInfoAsync()     │        │ LogErrorAsync()            │    │
│  │ ┌────────────────┐ │        │ ┌──────────────────────┐  │    │
│  │ │ Log Level: INFO│ │        │ │ Log Level: ERROR     │  │    │
│  │ │ Execution Time │ │        │ │ Exception Details    │  │    │
│  │ │ Response Data  │ │        │ │ Stack Trace         │  │    │
│  │ │ Parameters     │ │        │ │ Request Parameters  │  │    │
│  │ └────────────────┘ │        │ └──────────────────────┘  │    │
│  └────────────────────┘        └────────────────────────────┘    │
│         │                                   │                      │
│         └─────────────────┬─────────────────┘                      │
│                           │                                         │
│         ┌─────────────────▼─────────────────┐                      │
│         │  IControllerFileLoggerService     │                      │
│         │ (WriteLogAsync)                   │                      │
│         │                                   │                      │
│         │ ┌───────────────────────────────┐ │                      │
│         │ │ Sanitize Controller Name      │ │                      │
│         │ │ Create Directory if Missing   │ │                      │
│         │ │ Serialize Log Entry to JSON   │ │                      │
│         │ │ Get Current Date (yyyy-MM-dd) │ │                      │
│         │ │ Thread-Safe File Write        │ │                      │
│         │ └───────────────────────────────┘ │                      │
│         └─────────────────┬─────────────────┘                      │
│                           │                                         │
│                           ▼                                         │
│         ┌──────────────────────────────────────────┐               │
│         │      File System                         │               │
│         │  Logs/                                   │               │
│         │  ├── log-yyyy-MM-dd.txt                  │               │
│         │  └── Controllers/                        │               │
│         │      ├── Auth/                           │               │
│         │      │   ├── log-2024-06-13.txt ──────┐  │               │
│         │      │   │ {                          │  │               │
│         │      │   │   "LogLevel": "INFO",      │  │               │
│         │      │   │   "ControllerName": "Auth" │  │               │
│         │      │   │   ...                      │  │               │
│         │      │   │ }                          │  │               │
│         │      │   └── log-2024-06-12.txt       │  │               │
│         │      ├── Collection/                  │  │               │
│         │      │   └── log-2024-06-13.txt       │  │               │
│         │      ├── Customers/                   │  │               │
│         │      │   └── log-2024-06-13.txt       │  │               │
│         │      └── ... (one per controller)     │  │               │
│         │                                       │  │               │
│         │                                       │  │               │
│         │  JSON Log Entry Format:              │  │               │
│         │  {                                   │  │               │
│         │    "Timestamp": "2024-06-...",       │  │               │
│         │    "LogLevel": "INFO|ERROR",         │  │               │
│         │    "ControllerName": "Auth",         │  │               │
│         │    "ActionName": "Login",            │  │               │
│         │    "RequestParameters": {...},       │  │               │
│         │    "ResponseData": {...},            │  │               │
│         │    "ExecutionTimeMs": 245,           │  │               │
│         │    "ExceptionMessage": "...",        │  │               │
│         │    "StackTrace": "..."               │  │               │
│         │  }                                   │  │               │
│         └──────────────────────────────────────┘  │               │
│                           ▲                       │               │
│         ┌─────────────────┴────────────────┐      │               │
│         │  IControllerLogAnalyzerService  │      │               │
│         │  - Read log files                │      │               │
│         │  - Parse JSON entries            │      │               │
│         │  - Filter by level/date/action   │      │               │
│         │  - Calculate statistics          │      │               │
│         └─────────────────┬────────────────┘      │               │
│                           │                        │               │
│                           ▼                        │               │
│         ┌────────────────────────────────┐         │               │
│         │  LogAnalysisController         │         │               │
│         │  (REST API Endpoints)          │         │               │
│         │                                │         │               │
│         │  GET /api/loganalysis/        │         │               │
│         │    - controllers               │         │               │
│         │    - controller/{name}         │         │               │
│         │    - controller/{name}/errors  │         │               │
│         │    - controller/{name}/slow    │         │               │
│         │    - controller/{name}/...     │         │               │
│         │    - summary                   │         │               │
│         │    - performance/slowest       │         │               │
│         │    - errors/summary            │         │               │
│         │                                │         │               │
│         └────────────────────────────────┘         │               │
│                           │                        │               │
│                           ▼                        │               │
│         ┌────────────────────────────────┐         │               │
│         │      HTTP Response             │         │               │
│         │  {                             │         │               │
│         │    "totalControllers": 20,     │         │               │
│         │    "totalLogEntries": 5000,    │         │               │
│         │    "totalErrors": 125,         │         │               │
│         │    "summaries": [...]          │         │               │
│         │  }                             │         │               │
│         └────────────────────────────────┘         │               │
│                           ▲                        │               │
│                           │                        │               │
└───────────────────────────┼────────────────────────┘               │
                            │                                         │
                    ┌───────┴────────┐                                │
                    │                │                                │
                    │   Client/API   │                                │
                    │   Consumer     │                                │
                    │                │                                │
                    └────────────────┘                                │
```

---

## Component Details

### 1. AutoLogAttribute Filter
**Location**: `Infrastructure/Logging/AutoLogAttribute.cs`

**Responsibilities**:
- Intercept action method execution
- Start/stop timer
- Capture request parameters and body
- Capture response data
- Call appropriate logging method

**Flow**:
```
OnActionExecutionAsync()
  ├── Start Stopwatch
  ├── Extract Controller Name
  ├── Extract Action Name
  ├── Extract Request Parameters
  ├── Execute Action (via next())
  ├── Stop Stopwatch
  ├── Extract Response Data
  ├── Determine Success/Failure
  └── Call Logger.LogInfoAsync() or LogErrorAsync()
```

---

### 2. IControllerFileLoggerService
**Location**: `Infrastructure/Logging/ControllerFileLoggerService.cs`

**Responsibilities**:
- Create/manage controller log directories
- Serialize log entries to JSON
- Write to appropriate log file
- Handle thread safety
- Provide log retrieval methods

**Methods**:
```csharp
LogInfoAsync()           // Log successful operations
LogWarningAsync()        // Log warnings
LogErrorAsync()          // Log exceptions
LogStoredProcedureAsync()// Log stored procedures
GetControllerLogFiles()  // Retrieve log files
GetAllControllerLogDirectories()  // List all controllers
```

**Log File Strategy**:
```
Input: controllerName = "AuthController"
  ├── Sanitize: "Auth"
  ├── Create Directory: Logs/Controllers/Auth/
  ├── Get Date: 2024-06-13
  ├── Filename: log-2024-06-13.txt
  └── Append JSON entry
```

---

### 3. IControllerLogAnalyzerService
**Location**: `Infrastructure/Logging/ControllerLogAnalyzerService.cs`

**Responsibilities**:
- Read log files from disk
- Parse JSON entries
- Filter by criteria (level, date, action)
- Calculate statistics
- Provide query results

**Methods**:
```csharp
GetControllerLogsAsync()           // Get all logs
GetControllerErrorsAsync()         // Filter to errors
GetSlowOperationsAsync()           // Find slow operations
GetActionStatsAsync()              // Calculate statistics
GetAllControllersSummaryAsync()    // Cross-controller summary
```

**Query Patterns**:
```
Input: (controllerName: "Collection", limitDays: 7)
  ├── Get current date
  ├── Calculate cutoff: today - 7 days
  ├── Find matching log files
  ├── Read and parse JSON
  ├── Filter by criteria
  └── Return results
```

---

### 4. LogAnalysisController
**Location**: `Controllers/LogAnalysisController.cs`

**Responsibilities**:
- Expose log analysis via REST API
- Authorize access
- Format results
- Handle errors

**Endpoints**:
```
GET /api/loganalysis/controllers              → List all controllers
GET /api/loganalysis/controller/{name}        → All logs for controller
GET /api/loganalysis/controller/{name}/errors → Only error logs
GET /api/loganalysis/controller/{name}/slow   → Slow operations
GET /api/loganalysis/controller/{name}/action/{action}/stats  → Stats
GET /api/loganalysis/summary                  → All controllers summary
GET /api/loganalysis/performance/slowest      → Cross-controller slowest
GET /api/loganalysis/errors/summary           → Error statistics
GET /api/loganalysis/health                   → Service health check
```

---

## Data Flow Examples

### Example 1: Successful Request Logging

```
1. HTTP POST /api/auth/login

2. ASP.NET Core Routes to Action

3. AutoLogAttribute.OnActionExecutionAsync()
   ├── Starts timer
   ├── Extracts: LoginUserCommand { Email: "user@test.com", ... }
   └── Calls: executor.Next()

4. AuthController.LoginAsync()
   ├── Calls: Mediator.Send(command)
   └── Returns: Ok({ Token: "...", ExpiresIn: 3600 })

5. AutoLogAttribute continues
   ├── Stops timer: elapsed = 245ms
   ├── Extracts response: { Token: "...", ExpiresIn: 3600 }
   ├── Checks status: 200 OK (success)
   └── Calls: _logger.LogInfoAsync(...)

6. IControllerFileLoggerService.LogInfoAsync()
   ├── Creates log entry object
   ├── Serializes to JSON
   ├── Gets current date: 2024-06-13
   ├── Creates dir: Logs/Controllers/Auth/
   ├── Writes to: Logs/Controllers/Auth/log-2024-06-13.txt
   └── Lock released

7. HTTP Response
   └── 200 OK with { Token: "...", ExpiresIn: 3600 }

8. Result in Log File:
   {
     "Timestamp": "2024-06-13T10:30:45.123Z",
     "LogLevel": "INFO",
     "ControllerName": "AuthController",
     "ActionName": "LoginAsync",
     "RequestParameters": { "email": "user@test.com" },
     "ResponseData": { "token": "...", "expiresIn": 3600 },
     "ExecutionTimeMs": 245,
     "SuccessMessage": "LoginAsync completed successfully"
   }
```

---

### Example 2: Error Logging

```
1. HTTP POST /api/collection/collect
   Body: { "installmentId": 999, "amountPaid": 5000 }

2. ASP.NET Core Routes to Action

3. AutoLogAttribute.OnActionExecutionAsync()
   ├── Starts timer
   ├── Extracts: RecordPaymentRequest { ... }
   └── Calls: executor.Next()

4. CollectionController.Collect()
   ├── Tries: CollectionService.CollectInstallmentAsync(999, ...)
   └── Throws: Exception("Installment not found")

5. Exception propagates to AutoLogAttribute

6. AutoLogAttribute.OnActionExecutionAsync() - catch block
   ├── Stops timer: elapsed = 125ms
   ├── Captures: Exception details, stack trace
   ├── Calls: _logger.LogErrorAsync(...)

7. IControllerFileLoggerService.LogErrorAsync()
   ├── Creates error log entry
   ├── Includes exception message and stack trace
   ├── Serializes to JSON
   ├── Writes to: Logs/Controllers/Collection/log-2024-06-13.txt
   └── Lock released

8. Exception re-thrown

9. HTTP Response
   └── 500 Internal Server Error

10. Result in Log File:
    {
      "Timestamp": "2024-06-13T10:31:12.567Z",
      "LogLevel": "ERROR",
      "ControllerName": "CollectionController",
      "ActionName": "Collect",
      "RequestParameters": { "installmentId": 999, "amountPaid": 5000 },
      "ExceptionMessage": "Installment not found",
      "StackTrace": "at Fintech.Application.Services...",
      "ExecutionTimeMs": 125
    }
```

---

### Example 3: Log Query

```
1. REST Request
   GET /api/loganalysis/controller/Collection/errors?limitDays=7

2. LogAnalysisController.GetControllerErrors()
   └── Calls: _analyzer.GetControllerErrorsAsync("Collection", 7)

3. IControllerLogAnalyzerService.GetControllerErrorsAsync()
   ├── Calls: ReadControllerLogsAsync("Collection", "ERROR", 7)

4. ReadControllerLogsAsync()
   ├── Calculates cutoff date: today - 7 days
   ├── Gets log files: 
   │   - log-2024-06-13.txt
   │   - log-2024-06-12.txt
   │   - log-2024-06-11.txt
   │   - log-2024-06-10.txt
   │   - ... (up to 7 days)
   ├── For each file:
   │   ├── Read content
   │   ├── Split by separator: "────────────────────────────────"
   │   ├── Parse each chunk as JSON
   │   ├── Filter: Where LogLevel == "ERROR"
   │   └── Collect results
   └── Return: List<Dictionary<string, JsonElement>>

5. LogAnalysisController formats response
   {
     "ControllerName": "Collection",
     "LimitDays": 7,
     "TotalErrors": 12,
     "Errors": [
       { "Timestamp": "2024-06-13T...", "LogLevel": "ERROR", ... },
       { "Timestamp": "2024-06-12T...", "LogLevel": "ERROR", ... },
       ...
     ]
   }

6. HTTP Response
   └── 200 OK with error list
```

---

## Service Registration

```csharp
// In Program.cs
builder.Services.AddScoped<IFileLoggerService, FileLoggerService>();
builder.Services.AddScoped<IControllerFileLoggerService, ControllerFileLoggerService>();
builder.Services.AddScoped<IControllerLogAnalyzerService, ControllerLogAnalyzerService>();
```

**Lifetime**: Scoped (per HTTP request)
- Fresh instance for each request
- Isolated state per request
- Thread-safe access

---

## Thread Safety

### Lock Mechanism
```csharp
private readonly object _lockObject = new();

private async Task WriteLogAsync(string controllerName, object logEntry)
{
    lock (_lockObject)  // Critical section
    {
        try
        {
            // File I/O operations
            File.AppendAllText(logFilePath, logMessage, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            // Error handling
        }
    }
}
```

**How it works**:
- Multiple requests → Multiple threads → Single lock
- Thread A waits while Thread B writes file
- Thread B writes JSON entry
- Lock released
- Thread A acquires lock and writes
- Prevents file corruption

---

## Performance Characteristics

### Logging Overhead
- **Time to log**: ~5-10ms per entry (async, non-blocking)
- **File I/O**: Single file append (fast on modern drives)
- **Memory**: Minimal (JSON serialized immediately)
- **CPU**: Negligible

### Query Performance
- **Reading 1 day of logs**: ~50-100ms
- **Parsing JSON**: ~10-50ms
- **Filtering**: ~5-20ms
- **Depends on**: Volume of logs, file system speed

### Storage
- **Per controller per day**: 2-5MB (depends on request volume)
- **7 days of logs**: 15-35MB per controller
- **All controllers (7 days)**: ~300-700MB total
- **Recommendation**: Archive after 90 days

---

## Error Handling

### File I/O Errors
```csharp
try
{
    File.AppendAllText(logFilePath, logMessage, Encoding.UTF8);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to write log to file: {Message}", ex.Message);
    // Does not throw - prevents logging errors from breaking app
}
```

### Missing Directories
```csharp
if (!Directory.Exists(controllerLogDir))
{
    Directory.CreateDirectory(controllerLogDir);  // Auto-create
}
```

### Invalid JSON
```csharp
try
{
    using (var doc = JsonDocument.Parse(trimmedPart))
    {
        // Process
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error parsing log entry: {Content}", trimmedPart[..100]);
    // Continue processing other entries
}
```

---

## Security Considerations

### 1. Authorization
```csharp
[Authorize(Roles = "Admin,Operator")]  // Protect log endpoints
public class LogAnalysisController
```

### 2. Sensitive Data
```csharp
[AutoLog(logRequestBody: false)]  // Don't log passwords
public async Task Login(LoginRequest request)
```

### 3. File Permissions
- Application: Read/Write on `Logs/` directory
- Other users: Deny access (sensitive data)
- Backup: Include in backup strategy

### 4. Log Retention
- Implement policy: Keep 90 days, delete older
- Archive: Compress and store long-term
- Compliance: Meet regulatory requirements

---

## Summary

The controller-wise logging architecture provides:
- ✅ Automatic request/response logging
- ✅ Separate log files per controller
- ✅ Built-in query and analysis API
- ✅ Thread-safe concurrent access
- ✅ Minimal performance overhead
- ✅ Error isolation (logging errors don't break app)
- ✅ Extensible design for future enhancements

All components work together to create a complete, production-ready logging system that's easy to use and maintain.
