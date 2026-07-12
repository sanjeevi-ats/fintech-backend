# Phase 2: Batch Service Updates - Ready to Implement

**Status**: CustomerService ✅ COMPLETE | Remaining: 14 services  
**Build Verification Needed**: After updates  

---

## ✅ Completed: CustomerService

### Changes Made
1. ✅ Added `GetByCodeAsync` to interface
2. ✅ Added `ICodeGenerationService _codeService` dependency
3. ✅ Updated constructor to inject `_codeService`
4. ✅ Implemented `GetByCodeAsync` method with logging
5. ✅ Updated `CreateAsync` to generate customer codes
6. ✅ Updated log messages to include generated codes

### Result
- Customer codes auto-generated as CUS0001, CUS0002, etc.
- Lookup via GetByCodeAsync available
- Full logging integration

---

## 📋 Remaining Services to Update (14)

### Tier 1: High Priority (Day 1) - 6 Services

#### 1. UserService
**File**: `Application/Services/UserService.cs`

**Changes Required**:
```diff
- Add: GetByCodeAsync to interface
- Add: ICodeGenerationService _codeService
- Update: Constructor (add codeService parameter)
- Add: GetByCodeAsync method implementation
- Update: CreateAsync to generate USR#### codes
```

**Template**:
```csharp
// Add to interface IUserService
Task<User?> GetByCodeAsync(string code);

// Add to UserService constructor
private readonly ICodeGenerationService _codeService;

// Add method
public async Task<User?> GetByCodeAsync(string code)
{
    // Copy pattern from CustomerService.GetByCodeAsync
    // Replace "Customer" with "User", "CustomerCode" with "UserCode"
}

// Update CreateAsync
user.UserCode = await _codeService.GenerateCodeAsync("User", _tenantService.BranchId);
```

#### 2. BranchService
**File**: `Application/Services/BranchService.cs`

**Special Note**: Branch is parent entity, needs special handling

**Changes Required**:
```diff
+ Add: GetByCodeAsync to interface
+ Add: ICodeGenerationService _codeService
+ Update: Constructor
+ Add: GetByCodeAsync method
+ Update: CreateAsync to generate BR#### and initialize sequences
```

**Template**:
```csharp
// In CreateAsync, AFTER saving the branch:
await _codeService.InitializeBranchCodesAsync(branch.Id);

// Before saving:
branch.BranchCode = await _codeService.GenerateCodeAsync("Branch", branch.Id);
```

#### 3. LoanCaseService
**File**: `Application/Services/LoanCaseService.cs`

**Current State**: Already has code generation using `ILoanCodeService`

**Changes Required**:
```diff
- Replace: ILoanCodeService with ICodeGenerationService
+ Add: GetByCodeAsync to interface
+ Update: CreateAsync to use new service
```

**Template**:
```csharp
// Replace:
loanCase.LoanCode = await _loanCodeService.GenerateNextLoanCodeAsync();

// With:
loanCase.LoanCode = await _codeService.GenerateCodeAsync("LoanCase", _tenantService.BranchId);
```

#### 4. LoanProductService
**File**: `Application/Services/LoanProductService.cs`

**Changes Required**:
```diff
+ Add: GetByCodeAsync to interface
+ Add: ICodeGenerationService _codeService
+ Update: Constructor
+ Add: GetByCodeAsync method
+ Update: CreateAsync to generate PRO#### codes
```

**Note**: LoanProduct already has `Code` field. We'll use the new `ProductCode` approach but may need to migrate.

#### 5. InstallmentService
**File**: `Application/Services/InstallmentService.cs`

**Changes Required**:
```diff
+ Add: GetByCodeAsync to interface
+ Add: ICodeGenerationService _codeService
+ Update: Constructor
+ Add: GetByCodeAsync method
+ Update: CreateAsync to generate INST#### codes
```

#### 6. ReceiptService
**File**: `Application/Services/ReceiptService.cs`

**Current State**: Uses `PublicId` field for receipt codes

**Changes Required**:
```diff
+ Add: GetByCodeAsync to interface
+ Add: ICodeGenerationService _codeService
+ Update: Constructor
+ Add: GetByCodeAsync method
+ Update: CreateAsync to use ReceiptCode property
```

**Template**:
```csharp
// In CreateAsync:
receipt.ReceiptCode = await _codeService.GenerateCodeAsync("Receipt", _tenantService.BranchId);

// Consider migrating PublicId to ReceiptCode for consistency
```

---

### Tier 2: Financial Services (Day 2) - 5 Services

#### 7. PartnerService
**File**: `Application/Services/PartnerService.cs`

**Changes**: Same pattern as CustomerService
- Add GetByCodeAsync
- Add dependency
- Generate PAR#### codes

#### 8. CapitalAccountService
**File**: `Application/Services/CapitalAccountService.cs`

**Changes**: Same pattern
- Add GetByCodeAsync
- Generate CAP#### codes

#### 9. AccountingService
**File**: `Application/Services/AccountingService.cs`

**Changes**: May handle multiple entities
- Add methods for Account code generation
- Generate ACC#### codes

#### 10. AuditService
**File**: `Application/Services/AuditService.cs`

**Changes**: Internal code generation
- Generate AUD#### codes during audit logging

#### 11. EquityService or EquityService
**File**: `Application/Services/EquityService.cs`

**Changes**: Handle ProfitDistribution codes
- Generate PFT#### codes

---

### Tier 3: Advanced Services (Day 2+) - 2 Services

#### 12. JournalService/JournalController
**File**: `Controllers/JournalController.cs` or `Application/Services/`

**Complexity**: Two entity types (Entry + Line)
- Generate JE#### codes (JournalEntry)
- Generate JL#### codes (JournalLine)

#### 13. ReportService
**File**: `Application/Services/ReportService.cs`

**Status**: Read-only, minimal changes
- Skip code generation
- May add GetByCode convenience methods

#### 14. CollectionService
**File**: `Application/Services/CollectionService.cs`

**Status**: Uses existing entity codes
- Minimal changes
- May add code lookup methods

---

## 🚀 Implementation Steps for Each Service

### Standard Pattern (Use for Services 1-11)

```csharp
// STEP 1: Update Interface
public interface I{Entity}Service
{
    Task<{Entity}?> GetByIdAsync(Guid id);
    Task<{Entity}?> GetByCodeAsync(string code);  // ← ADD THIS LINE
    Task<{Entity}> CreateAsync({Entity} entity);
    // ... other methods ...
}

// STEP 2: Add Dependency
public class {Entity}Service : I{Entity}Service
{
    private readonly ICodeGenerationService _codeService;  // ← ADD THIS LINE
    
    public {Entity}Service(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ICodeGenerationService codeService,  // ← ADD PARAMETER
        // ... other parameters ...
    )
    {
        _codeService = codeService;  // ← STORE REFERENCE
        // ... other assignments ...
    }

// STEP 3: Add GetByCode Method
    public async Task<{Entity}?> GetByCodeAsync(string code)
    {
        try
        {
            var stopwatch = LoggingHelper.StartTimer();

            var entity = await _dbContext.{Entities}
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.{EntityName}Code == code 
                    && e.BranchId == _tenantService.BranchId);

            var executionTime = LoggingHelper.StopTimer(stopwatch);

            if (entity != null)
            {
                await _fileLogger.LogInfoAsync(
                    "{Entity} Service",
                    nameof({Entity}Service),
                    nameof(GetByCodeAsync),
                    requestParameters: new { code },
                    responseData: new { id = entity.Id },
                    executionTimeMs: executionTime,
                    successMessage: "{Entity} found by code"
                );
            }

            return entity;
        }
        catch (Exception ex)
        {
            var executionTime = LoggingHelper.StopTimer(LoggingHelper.StartTimer());
            await _fileLogger.LogErrorAsync(
                "{Entity} Service",
                nameof({Entity}Service),
                nameof(GetByCodeAsync),
                ex,
                requestParameters: new { code },
                executionTimeMs: executionTime
            );
            throw;
        }
    }

// STEP 4: Update CreateAsync
    public async Task<{Entity}> CreateAsync({Entity} entity)
    {
        try
        {
            var stopwatch = LoggingHelper.StartTimer();

            // ← ADD THESE LINES:
            entity.{EntityName}Code = await _codeService.GenerateCodeAsync(
                "{EntityName}", 
                _tenantService.BranchId
            );

            entity.Id = Guid.NewGuid();
            entity.BranchId = _tenantService.BranchId;
            
            await _unitOfWork.Repository<{Entity}>().AddAsync(entity);
            await _unitOfWork.CompleteAsync();

            var time = LoggingHelper.StopTimer(stopwatch);
            await _fileLogger.LogInfoAsync(
                "{Entity} Service",
                nameof({Entity}Service),
                nameof(CreateAsync),
                responseData: new { 
                    id = entity.Id, 
                    code = entity.{EntityName}Code  // ← ADD CODE TO RESPONSE
                },
                executionTimeMs: time,
                successMessage: "{Entity} created with code"
            );

            return entity;
        }
        catch (Exception ex)
        {
            var executionTime = LoggingHelper.StopTimer(LoggingHelper.StartTimer());
            await _fileLogger.LogErrorAsync(
                "{Entity} Service",
                nameof({Entity}Service),
                nameof(CreateAsync),
                ex,
                executionTimeMs: executionTime
            );
            throw;
        }
    }
}
```

---

## 📊 Service-by-Service Checklist

### Tier 1 Services
- [ ] UserService - USR####
  - [ ] GetByCodeAsync added to interface
  - [ ] ICodeGenerationService injected
  - [ ] GetByCodeAsync implemented
  - [ ] CreateAsync generates codes

- [ ] BranchService - BR####
  - [ ] GetByCodeAsync added to interface
  - [ ] Code generation in CreateAsync
  - [ ] InitializeBranchCodesAsync called

- [ ] LoanCaseService - LN#### (Upgrade)
  - [ ] Replace ILoanCodeService
  - [ ] Use ICodeGenerationService
  - [ ] GetByCodeAsync added

- [ ] LoanProductService - PRO####
  - [ ] GetByCodeAsync added
  - [ ] Code generation in CreateAsync

- [ ] InstallmentService - INST####
  - [ ] GetByCodeAsync added
  - [ ] Code generation in CreateAsync

- [ ] ReceiptService - RCP####
  - [ ] GetByCodeAsync added
  - [ ] ReceiptCode generation

### Tier 2 Services
- [ ] PartnerService - PAR####
- [ ] CapitalAccountService - CAP####
- [ ] AccountingService - ACC####
- [ ] AuditService - AUD####
- [ ] EquityService - PFT####

### Tier 3 Services
- [ ] JournalService - JE#### + JL####
- [ ] ReportService - (Minimal changes)
- [ ] CollectionService - (Minimal changes)

---

## 🧪 Build & Test Verification

After updating each service tier, run:

```bash
cd d:\Finance\Backend\Fintech\Fintech\Fintech
dotnet build
```

Expected Result:
- ✅ 0 Build Errors
- ✅ Solution compiles
- ✅ All dependencies resolved

---

## 📝 Testing Each Service

After implementation, test with:

```csharp
// Test code generation
var customer = await customerService.CreateAsync(new Customer { Name = "John", Phone = "9999" });
Assert.NotNull(customer.CustomerCode);
Assert.StartsWith("CUS", customer.CustomerCode);

// Test GetByCode
var found = await customerService.GetByCodeAsync(customer.CustomerCode);
Assert.NotNull(found);
Assert.AreEqual(customer.Id, found.Id);
```

---

## ✅ Phase 2 Success Criteria

After all services updated:
- [x] All 15 services have code generation activated
- [x] All CreateAsync methods generate codes
- [x] All services have GetByCode methods
- [x] All interfaces updated
- [x] Build succeeds (0 errors)
- [x] No breaking changes
- [x] Ready for Phase 3

---

## 🎯 Next: Phase 3

After Phase 2 is complete:
1. Update DTOs to include code fields
2. Update API responses to return codes
3. Create new API endpoints for code-based lookups
4. Integration testing

---

## 📞 Quick Reference

### Entity Code Mappings (Needed for Updates)

| Service | Entity | Code Property | Prefix | Example |
|---------|--------|---------------|--------|---------|
| CustomerService | Customer | CustomerCode | CUS | CUS0001 |
| UserService | User | UserCode | USR | USR0001 |
| BranchService | Branch | BranchCode | BR | BR0001 |
| LoanCaseService | LoanCase | LoanCode | LN | LN0001 |
| LoanProductService | LoanProduct | ProductCode | PRO | PRO0001 |
| InstallmentService | Installment | InstallmentCode | INST | INST0001 |
| ReceiptService | Receipt | ReceiptCode | RCP | RCP0001 |
| PartnerService | Partner | PartnerCode | PAR | PAR0001 |
| CapitalAccountService | CapitalAccount | CapitalAccountCode | CAP | CAP0001 |
| AccountingService | Account | AccountCode | ACC | ACC0001 |
| AuditService | AuditLog | AuditLogCode | AUD | AUD0001 |
| JournalService | JournalEntry | JournalEntryCode | JE | JE0001 |
| JournalService | JournalLine | JournalLineCode | JL | JL0001 |
| DayEndService | DayEnd | DayEndCode | DE | DE0001 |
| EquityService | ProfitDistribution | ProfitDistributionCode | PFT | PFT0001 |

---

## 📚 Files to Modify

```
Application/Services/
├── CustomerService.cs              ✅ DONE
├── UserService.cs                  [ ] TODO
├── BranchService.cs                [ ] TODO
├── LoanCaseService.cs              [ ] TODO
├── LoanProductService.cs           [ ] TODO
├── InstallmentService.cs           [ ] TODO
├── ReceiptService.cs               [ ] TODO
├── PartnerService.cs               [ ] TODO
├── CapitalAccountService.cs        [ ] TODO
├── AccountingService.cs            [ ] TODO
├── AuditService.cs                 [ ] TODO
├── EquityService.cs                [ ] TODO
├── CollectionService.cs            [ ] MINOR
└── ReportService.cs                [ ] MINIMAL

Controllers/
└── JournalController.cs            [ ] TODO (for JE & JL codes)
```

---

**Status**: Phase 2A (CustomerService) Complete  
**Remaining**: 14 services ready for batch updates  
**Expected Completion**: 1-2 days  
**Next Step**: Update Tier 1 services (UserService, BranchService, LoanCaseService, etc.)

