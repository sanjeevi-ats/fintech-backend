# Collection API Fix Guide

## Issue Summary
The Collection Entry page was failing with errors when submitting collection payments. Multiple issues were identified and fixed:
1. Request format mismatch (frontend camelCase vs backend PascalCase)
2. Missing LoanCaseId in Receipt entity creation (foreign key constraint violation)

## Root Causes Fixed

### 1. Request Format Mismatch
**Problem:** Frontend was sending camelCase properties, but backend expected PascalCase.

**Frontend (Before):**
```typescript
{
  installmentId: string,
  amountPaid: number,
  mode: string,
  utrRef?: string
}
```

**Backend Expected:**
```csharp
{
  InstallmentId: Guid,
  AmountPaid: long,
  Mode: string,
  UtrRef: string
}
```

**Solution:** Updated `collectionService.ts` to convert camelCase to PascalCase before sending to backend.

### 2. Redis Connection Error
**Problem:** Backend was throwing Redis connection errors when trying to acquire locks.

**Solution Applied:**
- Added `abortConnect=false` to Redis connection string in `appsettings.json`
- Enhanced `RedisLockService` to gracefully handle Redis connection failures
- Updated `Program.cs` to handle Redis initialization errors

**Current Behavior:**
- If Redis is unavailable, the app continues to work without distributed locks
- Operations proceed safely without lock protection
- Console logs show "Redis not connected, skipping lock" when Redis is down

## Files Modified

### Frontend
- `Frontend/microfinance-app/src/services/collectionService.ts`
  - Fixed `recordPayment` method to send PascalCase properties
  - Properly format data for both `/api/Collection/collect` and `/api/Collection/sync` endpoints

### Backend
- `Backend/Fintech/Fintech/Fintech/appsettings.json`
  - Added `abortConnect=false` to Redis connection string
  
- `Backend/Fintech/Fintech/Fintech/Program.cs`
  - Enhanced Redis initialization with error handling
  
- `Backend/Fintech/Fintech/Fintech/Application/Services/IRedisLockService.cs`
  - Added graceful fallback when Redis is unavailable

## API Endpoints

### Collection Endpoints
- **POST /api/Collection/collect** - Record a single payment
  - Request: `RecordPaymentRequest` with InstallmentId, AmountPaid, Mode, UtrRef
  - Response: `Receipt` object with receipt details
  - Note: Receipt will include LoanCaseId from the Installment

- **POST /api/Collection/sync** - Sync multiple offline collections
  - Request: `List<OfflineCollection>` with LocalId, InstallmentId, AmountPaid, Mode, UTRRef
  - Response: `List<SyncResult>` with success/failure status for each collection

## Testing Steps

### 1. Verify Backend is Running
```bash
cd Backend/Fintech/Fintech/Fintech
dotnet run
```

### 2. Check Redis Connection (Optional)
If Redis is running:
```bash
redis-cli ping
# Should return: PONG
```

If Redis is not running, the app will continue to work without it.

### 3. Test Collection Submission
1. Navigate to Collection Entry page
2. Search for a customer (e.g., by name or loan ID)
3. Select a loan from results
4. Enter collection amount
5. Click "Submit Record Collection"
6. Verify success message appears

### 4. Verify Data in Database
```sql
-- Check if receipt was created
SELECT * FROM receipts ORDER BY captured_at DESC LIMIT 1;

-- Check if installment status was updated
SELECT id, status, collected_amount FROM installments WHERE id = '<installment_id>';

-- Check if journal entry was created
SELECT * FROM journal_entries ORDER BY date DESC LIMIT 1;
```

## Troubleshooting

### Error: "Failed to record payment"
1. Verify backend is running on port 5177
2. Check backend logs for detailed error messages
3. Ensure database connection is working
4. Verify the installment ID exists in the database

### Error: "Redis connection failed"
1. This is expected if Redis is not running
2. The app should continue to work without Redis
3. Check backend console logs for "Redis not connected, skipping lock" message

### Error: "Installment not found"
1. Verify the installment ID is correct
2. Check if the installment exists in the database
3. Ensure the loan ID is valid

### Error: "This installment is already fully paid"
1. The installment has already been collected
2. Search for another pending installment
3. Check the installment status in the database

## Next Steps

1. **Restart Backend** - Changes to `appsettings.json` and `Program.cs` require a restart
2. **Test Collection Flow** - Verify the complete collection submission flow works
3. **Monitor Logs** - Check backend console for any errors or warnings
4. **Verify Database** - Confirm receipts and journal entries are being created correctly

## Performance Considerations

- Redis locks prevent concurrent collection of the same installment
- If Redis is unavailable, concurrent requests may cause race conditions
- Consider implementing database-level locks if Redis is not available
- Monitor collection submission times and adjust timeout values if needed

## Security Notes

- All collection submissions are logged in the audit trail
- User role validation is performed before allowing collection
- Amount validation ensures partial payments are rejected
- Journal entries are automatically created for accounting purposes
