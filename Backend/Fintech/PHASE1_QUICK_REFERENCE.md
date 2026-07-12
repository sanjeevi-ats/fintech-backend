# Phase 1 Quick Reference - Database Schema Implementation

**Status**: ✅ Complete | **Build**: ✅ Success | **Date**: June 13, 2026

---

## 🎯 What Happened in Phase 1?

The database schema was prepared to support business codes for all 15 entities. GUIDs remain unchanged; code columns are now available.

---

## 📊 What Changed?

### New Table
- `code_sequences` - Tracks sequential code generation per entity per branch

### New Columns Added to These Tables
| Table | Code Column | Example |
|-------|------------|---------|
| customers | `customer_code` | CUS0001 |
| loan_cases | `loan_code_new` | LN0001 |
| loan_products | `product_code` | PRO0001 |
| installments | `installment_code` | INST0001 |
| receipts | `receipt_code` | RCP0001 |
| partners | `partner_code` | PAR0001 |
| capital_accounts | `capital_account_code` | CAP0001 |
| journal_entries | `journal_entry_code` | JE0001 |
| journal_lines | `journal_line_code` | JL0001 |
| accounts | `account_code` | ACC0001 |
| day_ends | `day_end_code` | DE0001 |
| profit_distributions | `profit_distribution_code` | PFT0001 |
| branches | `branch_code` | BR0001 |
| users | `user_code` | USR0001 |
| audit_logs | `audit_log_code` | AUD0001 |

---

## 🔧 Implementation Details

### Entity Models
All 15 entity classes now have code properties:
```csharp
public class Customer
{
    public Guid Id { get; set; }
    // ... existing fields ...
    public string? CustomerCode { get; set; }  // ← NEW
}
```

### Service: CodeGenerationService
```csharp
// Location: Application/Services/CodeGenerationService.cs

var service = serviceProvider.GetRequiredService<ICodeGenerationService>();

// Generate a unique code
var code = await service.GenerateCodeAsync("Customer", branchId);
// Returns: "CUS0001", "CUS0002", etc.

// Initialize codes for new branch
await service.InitializeBranchCodesAsync(branchId);

// Check next sequence (info only)
var nextNum = await service.GetNextSequenceNumberAsync("Customer", branchId);
// Returns: 1, 2, 3, etc.
```

### Database Configuration
```csharp
// Location: Infrastructure/Persistence/FinVedaDbContext.cs
// Updated in OnModelCreating()

modelBuilder.Entity<Customer>(entity =>
{
    entity.HasIndex(e => e.CustomerCode).IsUnique();
});

modelBuilder.Entity<CodeSequence>(entity =>
{
    entity.HasIndex(e => new { e.EntityName, e.BranchId }).IsUnique();
});
// ... similar for all 15 entities
```

---

## 🚀 How to Apply Phase 1

### Apply EF Core Migration
```bash
cd d:\Finance\Backend\Fintech\Fintech\Fintech
dotnet ef database update
```

### Or Run Raw SQL
Execute `Migrations/003_AddBusinessCodes.sql` in your PostgreSQL client.

---

## ✅ Verify Phase 1

```sql
-- Check code_sequences table
SELECT COUNT(*) FROM code_sequences;

-- Check new columns exist
SELECT column_name FROM information_schema.columns 
WHERE table_name = 'customers' AND column_name = 'customer_code';

-- Verify unique indices
SELECT indexname FROM pg_indexes 
WHERE tablename = 'customers' AND indexname LIKE '%code%';
```

---

## 💾 Test Code Generation

```csharp
// In a test or startup code
var codeService = serviceProvider.GetRequiredService<ICodeGenerationService>();
var branchId = Guid.Parse("your-branch-id");

// Generate a customer code
var code1 = await codeService.GenerateCodeAsync("Customer", branchId);
Console.WriteLine(code1); // Output: CUS0001

// Generate another
var code2 = await codeService.GenerateCodeAsync("Customer", branchId);
Console.WriteLine(code2); // Output: CUS0002

// Generate for different entity type
var loanCode = await codeService.GenerateCodeAsync("LoanCase", branchId);
Console.WriteLine(loanCode); // Output: LN0001
```

---

## 🔄 Per-Branch Isolation

Each branch maintains its own code sequences:
```
Branch-A → CUS0001, CUS0002, CUS0003, ...
Branch-B → CUS0001, CUS0002, CUS0003, ...
           (Independent sequences)
```

---

## 🚫 What NOT to Do in Phase 1

1. ❌ Don't manually set code values
   - Codes should be auto-generated only
   
2. ❌ Don't modify CodeGenerationService
   - It's thread-safe by design; changes may break it
   
3. ❌ Don't delete code_sequences rows
   - They track the state; deletion will break sequence
   
4. ❌ Don't skip initialization for new branches
   - Call `InitializeBranchCodesAsync()` when creating branches

---

## 📋 Code Prefixes & Format

```
Entity          Prefix  Format   Example
Customer        CUS     ####     CUS0001
LoanCase        LN      ####     LN0001
LoanProduct     PRO     ####     PRO0001
Installment     INST    ####     INST0001
Receipt         RCP     ####     RCP0001
Partner         PAR     ####     PAR0001
CapitalAccount  CAP     ####     CAP0001
JournalEntry    JE      ####     JE0001
JournalLine     JL      ####     JL0001
Account         ACC     ####     ACC0001
DayEnd          DE      ####     DE0001
ProfitDistribution PFT  ####     PFT0001
Branch          BR      ####     BR0001
User            USR     ####     USR0001
AuditLog        AUD     ####     AUD0001

Format: PREFIX + Zero-Padded Number (4 digits)
```

---

## 🧵 Thread Safety

The CodeGenerationService is thread-safe:
- Uses database transactions
- Acquires row locks on code_sequences
- Ensures unique sequential codes even under high concurrency
- Returns immediately after code generation

---

## 🔙 Rollback (if needed)

```sql
-- Drop table
DROP TABLE IF EXISTS code_sequences;

-- Remove columns
ALTER TABLE customers DROP COLUMN IF EXISTS customer_code;
ALTER TABLE loan_cases DROP COLUMN IF EXISTS loan_code_new;
-- ... repeat for all tables

-- Remove indices
DROP INDEX IF EXISTS idx_customers_customer_code;
-- ... repeat for all indices
```

Or use EF Core:
```bash
dotnet ef database update 20260321112710_InitialCreate
```

---

## 📞 Key Files

| File | Purpose |
|------|---------|
| `Core/Domain/CodeSequence.cs` | Entity definition |
| `Application/Services/CodeGenerationService.cs` | Code generation logic |
| `Infrastructure/Persistence/FinVedaDbContext.cs` | Database config |
| `Program.cs` | DI registration |
| `Migrations/20260613_AddBusinessCodeFields.cs` | EF Core migration |
| `Migrations/003_AddBusinessCodes.sql` | Raw SQL migration |
| `ID_REFACTORING_PHASE1_COMPLETE.md` | Full documentation |

---

## ✅ Phase 1 Status

- ✅ CodeSequence entity created
- ✅ Code columns added to all 15 tables
- ✅ CodeGenerationService implemented & registered
- ✅ Migrations created (EF Core & SQL)
- ✅ Build successful (0 errors)
- ✅ Thread-safe code generation
- ✅ Per-branch isolation
- ✅ Backward compatible

---

## 🚀 Next: Phase 2

Phase 2 will activate code generation in services, so codes are automatically assigned to new records.

**Timeline**: 1-2 days  
**Files to Modify**: All 15 service classes  

---

**Last Updated**: June 13, 2026  
**Build Status**: ✅ SUCCESS
