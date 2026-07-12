# Collection Entry Sheet - Backend Implementation & Fixes

## 🚨 Critical Issues to Fix

### 1. Redis Connection Error
**Error**: `StackExchange.Redis.RedisConnectionException: 'It was not possible to connect to the redis server(s)'`

**Solution**: Update Redis connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

**Alternative**: If Redis is not needed, disable Redis caching in the application.

---

## 🔧 Backend Endpoint Requirements

### Current Endpoint: `/api/Collection/collect`
**Method**: POST  
**Access**: Admin and Collection Officer only

### Required Request Body:
```json
{
  "installmentId": "uuid-string",
  "amountPaid": 50000,  // In Paise (₹500.00)
  "mode": "Cash",       // Cash, UPI, Bank Transfer, Cheque
  "utrRef": "",         // Optional for Cash, Required for digital payments
  "remarks": "Collection via Entry Sheet"
}
```

### Expected Response:
```json
{
  "success": true,
  "message": "Collection recorded successfully",
  "installmentId": "uuid-string",
  "amountPaid": 50000,
  "newStatus": "paid" // or "partially_paid"
}
```

---

## 🗄️ Database Schema Verification

### Ensure these tables exist and have proper structure:

#### 1. Installments Table
```sql
-- Verify installments table structure
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'installments';

-- Required columns:
-- id (UUID, Primary Key)
-- loan_case_id (UUID, Foreign Key)
-- no (INT, Installment number)
-- due_date (DATE)
-- amount (BIGINT, in Paise)
-- status (VARCHAR: 'pending', 'partially_paid', 'paid')
-- collected_amount (BIGINT, default 0)
-- collected_date (DATE, nullable)
-- collected_by (VARCHAR, nullable)
-- payment_mode (VARCHAR, nullable)
-- remarks (TEXT, nullable)
```

#### 2. Collections Table (if separate tracking needed)
```sql
CREATE TABLE IF NOT EXISTS collections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    installment_id UUID NOT NULL REFERENCES installments(id),
    loan_case_id UUID NOT NULL,
    amount_paid BIGINT NOT NULL,
    payment_mode VARCHAR(50) NOT NULL DEFAULT 'Cash',
    utr_reference VARCHAR(100),
    collected_by VARCHAR(255),
    collected_date DATE NOT NULL DEFAULT CURRENT_DATE,
    remarks TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255)
);
```

---

## 🔒 Authorization Implementation

### Controller Authorization:
```csharp
[HttpPost("collect")]
[Authorize(Roles = "super_admin,collection_officer")]
public async Task<IActionResult> RecordCollection([FromBody] CollectionRequest request)
{
    try 
    {
        // Validate user role
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "super_admin" && userRole != "collection_officer") 
        {
            return Forbid("Only Admins and Collection Officers can record collections");
        }

        // Validate request
        if (request.AmountPaid <= 0)
        {
            return BadRequest("Collection amount must be greater than 0");
        }

        // Process collection
        var result = await _collectionService.RecordPayment(request);
        
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error recording collection");
        return StatusCode(500, new { message = "Failed to record collection", error = ex.Message });
    }
}
```

---

## 📊 Service Implementation

### Collection Service Method:
```csharp
public async Task<CollectionResult> RecordPayment(CollectionRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try 
    {
        // Get installment
        var installment = await _context.Installments
            .FirstOrDefaultAsync(i => i.Id == request.InstallmentId);
            
        if (installment == null)
        {
            throw new NotFoundException("Installment not found");
        }

        // Update installment
        var previousCollected = installment.CollectedAmount ?? 0;
        var newCollectedAmount = previousCollected + request.AmountPaid;
        
        installment.CollectedAmount = newCollectedAmount;
        installment.CollectedDate = DateTime.Today;
        installment.CollectedBy = request.CollectedBy ?? "System";
        installment.PaymentMode = request.Mode;
        installment.Remarks = request.Remarks;
        
        // Update status
        if (newCollectedAmount >= installment.Amount)
        {
            installment.Status = "paid";
        }
        else if (newCollectedAmount > 0)
        {
            installment.Status = "partially_paid";
        }

        // Create collection record
        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            InstallmentId = request.InstallmentId,
            LoanCaseId = installment.LoanCaseId,
            AmountPaid = request.AmountPaid,
            PaymentMode = request.Mode,
            UtrReference = request.UtrRef,
            CollectedBy = request.CollectedBy,
            CollectedDate = DateTime.Today,
            Remarks = request.Remarks,
            CreatedAt = DateTime.UtcNow
        };

        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CollectionResult
        {
            Success = true,
            Message = "Collection recorded successfully",
            InstallmentId = installment.Id,
            AmountPaid = request.AmountPaid,
            NewStatus = installment.Status
        };
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## 🧪 Testing Endpoints

### Test with Postman/curl:

```bash
# Test Collection Recording
curl -X POST "http://localhost:5177/api/Collection/collect" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "installmentId": "your-installment-id",
    "amountPaid": 50000,
    "mode": "Cash",
    "utrRef": "",
    "remarks": "Test collection via Entry Sheet"
  }'
```

### Expected Success Response:
```json
{
  "success": true,
  "message": "Collection recorded successfully",
  "installmentId": "uuid-string",
  "amountPaid": 50000,
  "newStatus": "paid"
}
```

---

## 🔧 Configuration Updates

### 1. appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-postgres-connection-string",
    "Redis": "localhost:6379,abortConnect=false"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 2. Program.cs / Startup.cs
```csharp
// Add Redis with proper configuration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
    options.ConfigurationOptions = new ConfigurationOptions
    {
        AbortOnConnectFail = false,
        ConnectTimeout = 5000,
        SyncTimeout = 5000
    };
});

// Or disable Redis if not needed
// services.AddMemoryCache(); // Use in-memory cache instead
```

---

## 🚀 Deployment Checklist

### Before Testing:
- [ ] Update Redis connection string with `abortConnect=false`
- [ ] Verify database schema has all required columns
- [ ] Ensure JWT authentication is working
- [ ] Test role-based authorization
- [ ] Verify installments data exists in database

### Testing Steps:
1. **Start Backend**: Ensure no Redis errors in logs
2. **Test Authentication**: Login with admin/collection officer account
3. **Test Collection Endpoint**: Use Postman to test `/api/Collection/collect`
4. **Verify Database**: Check that installment status updates correctly
5. **Test Frontend**: Use Entry Sheet to record collections

---

## 🐛 Common Issues & Solutions

### Issue 1: Redis Connection Fails
**Solution**: Add `abortConnect=false` to connection string or disable Redis

### Issue 2: Authorization Fails
**Solution**: Ensure JWT token includes correct role claims

### Issue 3: Database Update Fails
**Solution**: Check foreign key constraints and column data types

### Issue 4: Amount Validation Fails
**Solution**: Ensure amounts are in Paise (multiply by 100)

---

## 📋 SQL Scripts for Missing Data

### Create Test Installments:
```sql
-- Insert test installments if missing
INSERT INTO installments (id, loan_case_id, no, due_date, amount, status)
VALUES 
  (gen_random_uuid(), 'existing-loan-id', 1, CURRENT_DATE, 50000, 'pending'),
  (gen_random_uuid(), 'existing-loan-id', 2, CURRENT_DATE + INTERVAL '30 days', 50000, 'pending');
```

### Update Installment Schema:
```sql
-- Add missing columns if they don't exist
ALTER TABLE installments ADD COLUMN IF NOT EXISTS collected_amount BIGINT DEFAULT 0;
ALTER TABLE installments ADD COLUMN IF NOT EXISTS collected_date DATE;
ALTER TABLE installments ADD COLUMN IF NOT EXISTS collected_by VARCHAR(255);
ALTER TABLE installments ADD COLUMN IF NOT EXISTS payment_mode VARCHAR(50);
ALTER TABLE installments ADD COLUMN IF NOT EXISTS remarks TEXT;
```

This implementation provides a complete Collection Entry Sheet that works with your existing backend endpoint while addressing the Redis connection issue and ensuring proper role-based access control.