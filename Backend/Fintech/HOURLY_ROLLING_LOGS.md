# Hourly Rolling Log Files Implementation

## 📋 Overview

The logging system has been enhanced to support **hourly rolling log files** instead of daily logs. Each controller now creates a new log file for every hour, providing better log management and easier troubleshooting.

---

## 📁 Directory Structure

### New Format
```
Logs/
├── log-2026-06-13.txt (global - backward compatible)
└── Controllers/
    ├── LoanCases/
    │   ├── LoanCases_2026-06-13_08.log
    │   ├── LoanCases_2026-06-13_09.log
    │   ├── LoanCases_2026-06-13_10.log
    │   ├── LoanCases_2026-06-13_11.log
    │   └── LoanCases_2026-06-13_12.log
    ├── Collection/
    │   ├── Collection_2026-06-13_08.log
    │   ├── Collection_2026-06-13_09.log
    │   ├── Collection_2026-06-13_10.log
    │   └── Collection_2026-06-13_11.log
    ├── Customers/
    │   ├── Customers_2026-06-13_08.log
    │   ├── Customers_2026-06-13_09.log
    │   └── Customers_2026-06-13_10.log
    ├── Auth/
    │   ├── Auth_2026-06-13_08.log
    │   ├── Auth_2026-06-13_09.log
    │   └── Auth_2026-06-13_10.log
    ├── Branch/
    ├── Products/
    ├── Users/
    └── ... (one directory per controller)
```

### Previous Format (Daily - Now Deprecated for Controller Logs)
```
Logs/
└── Controllers/
    ├── LoanCases/
    │   └── log-2026-06-13.txt (single file for entire day)
    ├── Collection/
    │   └── log-2026-06-13.txt
    └── ... (etc)
```

---

## 🔄 Log File Naming Convention

### Filename Format
```
{ControllerName}_{Date}_{Hour}.log
```

### Examples
```
LoanCases_2026-06-13_08.log     # 8:00-8:59 AM UTC
LoanCases_2026-06-13_14.log     # 2:00-2:59 PM UTC (14:00 in 24-hour)
Collection_2026-06-13_10.log    # 10:00-10:59 AM UTC
Customers_2026-06-13_23.log     # 11:00-11:59 PM UTC
Auth_2026-06-14_00.log          # Next day midnight-1 AM UTC
```

### File Naming Components
- **ControllerName**: Sanitized controller name (without "Controller" suffix)
- **Date**: UTC date in `yyyy-MM-dd` format
- **Hour**: UTC hour in 24-hour format (00-23)
- **Extension**: `.log` (previously `.txt`)

---

## ⏰ Hourly Rolling Behavior

### How It Works

1. **First request of the hour**
   - Log file created: `Collection_2026-06-13_10.log`
   - All requests at 10:00-10:59 write to this file

2. **Hour changes**
   - New log file created: `Collection_2026-06-13_11.log`
   - Requests at 11:00-11:59 write to new file
   - Previous file (`10.log`) remains untouched

3. **Day changes**
   - Timestamp changes from `2026-06-13` to `2026-06-14`
   - New files created: `Collection_2026-06-14_00.log`, etc.
   - Previous day files remain accessible

### Timeline Example
```
Timeline:
10:00 AM → Collection_2026-06-13_10.log created
10:15 AM → Request logged to 10.log
10:45 AM → Another request logged to 10.log
11:00 AM → Collection_2026-06-13_11.log created (automatic)
11:05 AM → First request of 11 AM hour logged to 11.log
11:59 AM → Last request of 11 AM hour logged to 11.log
12:00 PM → Collection_2026-06-13_12.log created (automatic)
```

---

## 📊 Benefits of Hourly Rolling Logs

### 1. Better Organization
- **Smaller files**: ~100KB-500KB per hour vs 1-5MB per day
- **Faster searching**: Query specific hour instead of entire day
- **Easier archival**: Move/compress hourly batches

### 2. Improved Performance
- **Reduced file size**: Faster to open and read
- **Better caching**: OS can cache smaller files in memory
- **Quicker searches**: Grep/find operations are faster

### 3. Operational Benefits
- **Granular troubleshooting**: Pinpoint issues to specific hour
- **Load tracking**: See request patterns by hour
- **Performance analysis**: Identify peak usage times
- **Faster debugging**: Narrow down issue to 60-minute window

### 4. Storage Management
- **Easy rotation**: Keep last 7 days (168 hourly files)
- **Predictable size**: Can calculate storage needs
- **Batch operations**: Delete/compress by date+hour
- **Quota management**: Better control over disk usage

---

## 📈 Storage Estimation

### Per Controller Per Hour
- **Light traffic**: 50-200 KB per hour
- **Normal traffic**: 200-500 KB per hour
- **Heavy traffic**: 500 KB - 2 MB per hour

### Daily per Controller
```
50 KB/hour  × 24 hours = 1.2 MB/day
500 KB/hour × 24 hours = 12 MB/day
2 MB/hour   × 24 hours = 48 MB/day
```

### Weekly per Controller (at 500 KB/hour average)
```
500 KB/hour × 24 hours × 7 days = 84 MB/week
```

### All 19 Controllers (at 500 KB/hour average)
```
500 KB/hour × 24 hours × 7 days × 19 controllers = 1.6 GB/week
```

### Archive Strategy (Keep last 90 days)
```
500 KB/hour × 24 hours × 90 days × 19 controllers = 20.5 GB
```

---

## 🔍 Querying Hourly Logs

### View Logs for Specific Hour
```bash
# Windows - View 10 AM Collection logs
type Logs\Controllers\Collection\Collection_2026-06-13_10.log

# Search for errors in specific hour
findstr ERROR Logs\Controllers\Collection\Collection_2026-06-13_10.log
```

### View All Logs for a Day
```bash
# Windows - View all Collection logs for the day
type Logs\Controllers\Collection\Collection_2026-06-13_*.log

# View last hour's logs
type Logs\Controllers\Collection\Collection_2026-06-13_11.log
```

### Search Across Hours
```bash
# Find all errors in morning hours (8-12 AM)
findstr ERROR Logs\Controllers\Collection\Collection_2026-06-13_0[8-9].log
findstr ERROR Logs\Controllers\Collection\Collection_2026-06-13_1[0-2].log
```

### Via REST API (No Changes)
```bash
# Get all logs for a controller (across all hours)
curl https://localhost:5001/api/loganalysis/controller/Collection

# Get errors for a controller
curl https://localhost:5001/api/loganalysis/controller/Collection/errors

# Get slow operations
curl https://localhost:5001/api/loganalysis/controller/Collection/slow?thresholdMs=1000

# Get summary
curl https://localhost:5001/api/loganalysis/summary
```

---

## 📝 Log Entry Example

### Hourly Log File Content
```
File: Collection_2026-06-13_10.log

{
  "Timestamp": "2026-06-13T10:15:32.1234567Z",
  "LogLevel": "INFO",
  "ControllerName": "CollectionController",
  "ActionName": "Collect",
  "RequestParameters": {
    "installmentId": 123,
    "amountPaid": 5000
  },
  "ResponseData": {
    "success": true,
    "receiptNo": "RCP2026061310001",
    "balance": 0
  },
  "ExecutionTimeMs": 245,
  "SuccessMessage": "Collect completed successfully"
}
────────────────────────────────────────────────────────────────────────────────
{
  "Timestamp": "2026-06-13T10:16:45.5678901Z",
  "LogLevel": "INFO",
  "ControllerName": "CollectionController",
  "ActionName": "Sync",
  "RequestParameters": {
    "collectionCount": 15
  },
  "ExecutionTimeMs": 512,
  "SuccessMessage": "Sync completed successfully"
}
────────────────────────────────────────────────────────────────────────────────
```

---

## 🛠️ Implementation Details

### How Hourly Rolling Works

1. **Timestamp Generation** (in UTC)
   ```csharp
   var now = DateTime.UtcNow;
   var logFileName = $"{controllerName}_{now:yyyy-MM-dd}_{now:HH}.log";
   // Result: Collection_2026-06-13_10.log
   ```

2. **File Path Construction**
   ```csharp
   var controllerLogDir = Path.Combine(_logsBaseDirectory, sanitizedControllerName);
   // Result: Logs/Controllers/Collection/
   
   var logFilePath = Path.Combine(controllerLogDir, logFileName);
   // Result: Logs/Controllers/Collection/Collection_2026-06-13_10.log
   ```

3. **Thread-Safe Append**
   ```csharp
   File.AppendAllText(logFilePath, logMessage, Encoding.UTF8);
   // Appends to file (or creates if not exists)
   ```

---

## 📊 Analyzing Logs by Hour

### Find Peak Traffic Hours
```bash
# Count log entries per hour
for /L %H in (0,1,23) do (
    echo Hour %H: && findstr /c:"" Logs\Controllers\Collection\Collection_2026-06-13_0%H.log | find /c /v "" || echo 0
)
```

### Find Error Hours
```bash
# Find which hours had errors
findstr ERROR Logs\Controllers\Collection\Collection_2026-06-13_*.log | findstr /o "^" | sort
```

### Performance by Hour
```bash
# Extract execution times for specific hour
findstr "ExecutionTimeMs" Logs\Controllers\Collection\Collection_2026-06-13_10.log
```

---

## 🔄 Migration from Daily to Hourly

### Automatic
The system automatically creates hourly files on first request of each hour.

### Backward Compatibility
- Existing daily log files remain untouched
- Global `log-*.txt` file continues (backward compatible)
- REST API works with both daily and hourly format

### Recovery
If needed to revert to daily logs:
1. Stop the application
2. Remove hourly `.log` files from `Logs/Controllers/`
3. Keep the original log structure
4. Restart application (will create new logs in hourly format)

---

## 📋 Best Practices

### 1. Regular Archival
```bash
# Weekly archival (keep only last 7 days)
# Delete logs older than 7 days
Remove-Item Logs\Controllers\*\*_*.log -Include "*_2026-06-*.log" -Where {$_.LastWriteTime -lt (Get-Date).AddDays(-7)}
```

### 2. Monthly Backup
```bash
# Compress logs from previous month
Compress-Archive -Path Logs\Controllers\*\*_2026-05-*.log -DestinationPath Backups\Logs_2026-05.zip
```

### 3. Storage Quota
```
Monitor: Total logs should not exceed 20-30GB
Daily check: Keep track of log directory size
Action: Archive when size exceeds 15GB
```

### 4. Performance
```
Recommendation: Keep last 90 days of logs (168 hours × 90 days)
For 19 controllers: ~20GB total storage
```

---

## 🔍 Troubleshooting

### No Logs Appearing?
1. Check `Logs/Controllers/{ControllerName}/` directory exists
2. Verify file pattern: `{ControllerName}_YYYY-MM-DD_HH.log`
3. Check file timestamp is from current hour
4. Verify file is being written to (not empty)

### Logs in Wrong Location?
1. Verify controller name sanitization
2. Check `[AutoLog]` attribute is applied
3. Confirm DI registration in `Program.cs`

### Files Not Rolling to New Hour?
1. Verify system clock is accurate (UTC)
2. Check file system permissions
3. Ensure disk space available

### Too Many Files?
1. Implement archival strategy
2. Delete files older than retention period
3. Compress archived logs

---

## 📞 Support

### Check Log Files
```bash
# List all log files for a controller
dir Logs\Controllers\Collection\*.log

# Count total log files
dir Logs\Controllers\*\*.log /s
```

### Query via API
```bash
# Always works regardless of log format
curl https://localhost:5001/api/loganalysis/controller/Collection
```

### Find Issues in Specific Hour
```bash
# View logs from 10 AM hour
type Logs\Controllers\Collection\Collection_2026-06-13_10.log | findstr ERROR

# View logs from 10 AM and 11 AM
type Logs\Controllers\Collection\Collection_2026-06-13_1[01].log
```

---

## ✅ Verification Checklist

After deployment, verify:

- [ ] Logs appear in `Logs/Controllers/{ControllerName}/` directory
- [ ] Filename format: `{ControllerName}_YYYY-MM-DD_HH.log`
- [ ] Logs roll to new file each hour
- [ ] JSON entries are properly formatted
- [ ] Timestamps are in UTC format
- [ ] Execution times are recorded
- [ ] Errors are properly logged
- [ ] API endpoints return data from hourly logs
- [ ] Storage size is within expected range
- [ ] File permissions allow read/write

---

## 🎯 Summary

### What Changed
- ✅ Daily logs → Hourly logs
- ✅ File format: `log-YYYY-MM-DD.txt` → `{ControllerName}_YYYY-MM-DD_HH.log`
- ✅ File size: 1-5 MB/day → 100-500 KB/hour
- ✅ Better organization and faster queries

### What Stayed the Same
- ✅ `[AutoLog]` attribute still works
- ✅ REST API unchanged
- ✅ JSON log format unchanged
- ✅ Thread-safe logging
- ✅ Controller-specific directories

### Benefits
- ✅ Smaller, more manageable files
- ✅ Faster searching and analysis
- ✅ Better granularity for troubleshooting
- ✅ Easier storage management
- ✅ Predictable file sizes

---

**Implementation Date**: June 13, 2024  
**Status**: ✅ Active  
**Format**: Hourly Rolling Logs  
**UTC-Based**: Yes (all times in UTC)
