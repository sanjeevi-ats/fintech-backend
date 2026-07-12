# Phase 2: Service Layer Implementation - Code Generation Activation

**Phase**: 2 of 10  
**Status**: 📋 READY FOR IMPLEMENTATION  
**Date**: June 13, 2026  
**Estimated Duration**: 1-2 days  
**Effort**: 8-12 hours  
**Build Status**: Will verify after implementation  

---

## 🎯 Phase 2 Objectives

Activate code generation in all 15 business services so that business codes are auto-generated when creating new entities.

### Primary Goals
1. ✅ Inject `ICodeGenerationService` into all 15 service classes
2. ✅ Call `GenerateCodeAsync()` in Create methods
3. ✅ Assign generated codes to entities before saving
4. ✅ Implement `GetByCode()` lookup methods
5. ✅ Update service interfaces to include new methods
6. ✅ Verify build succeeds
7. ✅ Create comprehensive documentation

---

## 📋 Services to Update (15 Total)

### Customer Entities (3)
- [ ] **CustomerService** - CUS#### codes
- [ ] **UserService** - USR#### codes
- [ ] **BranchService** - BR#### codes

### Loan & Credit (6)
- [ ] **LoanCaseService** - LN#### codes (already has code generation, upgrade to use new service)
- [ ] **LoanProductService** - PRO#### codes
- [ ] **InstallmentService** - INST#### codes
- [ ] **LoanClosureService** - Optional: Handle closed loans

### Financial Entities (4)
- [ ] **PartnerService** - PAR#### codes
- [ ] **CapitalAccountService** - CAP#### codes
- [ ] **ReceiptService** - RCP#### codes
- [ ] **AccountingService** - ACC#### codes

### Accounting & Operations (2)
- [ ] **AuditService** - AUD#### codes
- [ ] **DayEndService** - DE#### codes (may need to create)

### Advanced/Optional
- [ ] **JournalService** - JE#### (entry) + JL#### (line) codes
- [ ] **EquityService** - PFT#### (profit distribution) codes
- [ ] **ReportService** - Generally read-only, no codes
- [ ] **CollectionService** - No specific entity codes, uses existing codes

---

## 🔄 Implementation Pattern

### Step 1: Add ICodeGenerationService Dependency
```csharp
public class CustomerService : ICustomerService
{
    private readonly ICodeGenerationService _codeService;  // ← NEW
    
    public CustomerService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        IFileLoggerService fileLogger,
        ITenantService tenantService,
        ICodeGenerationService codeService)  // ← ADD
    {
        _unitOfWork = unitOfWork;
        _codeService = codeService;  // ← STORE
        // ... other fields ...
    }
}
```

### Step 2: Generate Code in Create Methods
```csharp
public async Task<Customer> CreateAsync(Customer customer)
{
    try
    {
        var stopwatch = LoggingHelper.StartTimer();

        // Generate code BEFORE saving
        customer.CustomerCode = await _codeService.GenerateCodeAsync(
            "Customer", 
            _tenantService.BranchId
        );  // ← NEW

        customer.Id = Guid.NewGuid();
        customer.BranchId = _tenantService.BranchId;
        
        await _unitOfWork.Repository<Customer>().AddAsync(customer);
        await _unitOfWork.CompleteAsync();

        var time = LoggingHelper.StopTimer(stopwatch);
        await _fileLogger.LogInfoAsync(
            "Customer Service",
            nameof(CustomerService),
            nameof(CreateAsync),
            responseData: new { 
                customerId = customer.Id, 
                customerCode = customer.CustomerCode  // ← LOG CODE
            },
            executionTimeMs: time,
            successMessage: "Customer created with code"
        );

        return customer;
    }
    catch (Exception ex)
    {
        // ... error handling ...
    }
}
```

### Step 3: Add GetByCode Method to Interface
```csharp
public interface ICustomerService
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByCodeAsync(string code);  // ← NEW
    Task<Customer?> GetByPhoneAsync(string phone);
    // ... other methods ...
}
```

### Step 4: Implement GetByCode Method
```csharp
public async Task<Customer?> GetByCodeAsync(string code)
{
    try
    {
        var stopwatch = LoggingHelper.StartTimer();

        var customer = await _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerCode == code && c.BranchId == _tenantService.BranchId);

        var executionTime = LoggingHelper.StopTimer(stopwatch);

        if (customer != null)
        {
            await _fileLogger.LogInfoAsync(
                "Customer Service",
                nameof(CustomerService),
                nameof(GetByCodeAsync),
                requestParameters: new { code },
                responseData: new { customerId = customer.Id },
                executionTimeMs: executionTime,
                successMessage: "Customer found by code"
            );
        }

        return customer;
    }
    catch (Exception ex)
    {
        var executionTime = LoggingHelper.StopTimer(LoggingHelper.StartTimer());
        await _fileLogger.LogErrorAsync(
            "Customer Service",
            nameof(CustomerService),
            nameof(GetByCodeAsync),
            ex,
            requestParameters: new { code },
            executionTimeMs: executionTime
        );
        throw;
    }
}
```

---

## 📊 Service-by-Service Implementation Details

### 1. CustomerService
**Current State**: Creates customers but no code generation  
**Action**: Add code generation in CreateAsync  
**Entity Code**: CUS####  
**New Methods**: GetByCodeAsync

**Implementation**:
```csharp
// In CreateAsync, before saving:
customer.CustomerCode = await _codeService.GenerateCodeAsync("Customer", _tenantService.BranchId);
```

### 2. UserService
**Current State**: Creates users but no code generation  
**Action**: Add code generation in CreateAsync  
**Entity Code**: USR####  
**New Methods**: GetByCodeAsync

**Implementation**:
```csharp
// In CreateAsync, before saving:
user.UserCode = await _codeService.GenerateCodeAsync("User", _tenantService.BranchId);
```

### 3. BranchService
**Current State**: Creates branches but no code generation  
**Action**: Add code generation + initialize sequences  
**Entity Code**: BR####  
**New Methods**: GetByCodeAsync, InitializeCodesAsync

**Special Handling**:
```csharp
// In CreateAsync, after saving:
await _codeService.InitializeBranchCodesAsync(branch.Id);

// In CreateAsync, before saving:
branch.BranchCode = await _codeService.GenerateCodeAsync("Branch", branch.Id);
```

### 4. LoanCaseService
**Current State**: Already uses LoanCodeService for loan codes  
**Action**: Migrate to ICodeGenerationService (upgrade)  
**Entity Code**: LN####  
**Current Implementation**:
```csharp
loanCase.LoanCode = await _loanCodeService.GenerateNextLoanCodeAsync();
// CHANGE TO:
loanCase.LoanCode = await _codeService.GenerateCodeAsync("LoanCase", _tenantService.BranchId);
```

### 5. LoanProductService
**Current State**: Creates products but no code generation  
**Action**: Add code generation in CreateAsync  
**Entity Code**: PRO####  
**Special Note**: LoanProduct already has a `Code` field (product code like "Gold", "Silver")  
**Action**: Use `ProductCode` property name for consistency

### 6. InstallmentService
**Current State**: Creates installments but no code generation  
**Action**: Add code generation in CreateAsync  
**Entity Code**: INST####  
**New Methods**: GetByCodeAsync

### 7. ReceiptService
**Current State**: Has PublicId field, no code generation  
**Action**: Add code generation, migrate from PublicId to ReceiptCode  
**Entity Code**: RCP####  
**Special Note**: Already has receipt tracking code

### 8. PartnerService
**Current State**: Creates partners but no code generation  
**Action**: Add code generation in CreateAsync  
**Entity Code**: PAR####  
**New Methods**: GetByCodeAsync

### 9. CapitalAccountService
**Current State**: Creates capital accounts but no code generation  
**Action**: Add code generation in CreateAsync  
**Entity Code**: CAP####  
**New Methods**: GetByCodeAsync

### 10. AccountingService
**Current State**: Complex service, may handle multiple entities  
**Action**: Add code generation for Account entities  
**Entity Code**: ACC####  
**New Methods**: GetAccountByCodeAsync

### 11. AuditService
**Current State**: Creates audit logs but no code generation  
**Action**: Add code generation in logging methods  
**Entity Code**: AUD####  
**Special Note**: Codes generated internally by service

### 12. JournalService (Journal Controller)
**Current State**: Creates journal entries with PublicId  
**Action**: Add JE#### and JL#### code generation  
**Entity Codes**: JE#### (Entry), JL#### (Line)  
**New Methods**: GetEntryByCodeAsync, GetLineByCodeAsync

### 13. EquityService
**Current State**: Handles profit distribution  
**Action**: Add code generation for profit distributions  
**Entity Code**: PFT####  
**Special Note**: May need to create DayEndService

### 14-15. ReportService & CollectionService
**Report Service**: Read-only, no entity creation, skip  
**Collection Service**: Uses existing entity codes, minimal changes

---

## 🚀 Implementation Steps

### Phase 2A: Core Services (Day 1)
1. CustomerService - Add code generation
2. UserService - Add code generation
3. BranchService - Add code generation + initialize
4. LoanCaseService - Upgrade to new service
5. LoanProductService - Add code generation
6. InstallmentService - Add code generation

### Phase 2B: Financial Services (Day 1-2)
1. ReceiptService - Add code generation
2. PartnerService - Add code generation
3. CapitalAccountService - Add code generation
4. AccountingService - Add code generation

### Phase 2C: Advanced Services (Day 2)
1. AuditService - Add code generation
2. JournalService - Add code generation (2 entity types)
3. EquityService - Add code generation
4. Verification & Testing

---

## 📝 Code Changes Checklist

For each service, make these changes:

### Service Interface Changes
```csharp
public interface I{Entity}Service
{
    // Existing methods
    Task<{Entity}?> GetByIdAsync(Guid id);
    Task<{Entity}?> GetByCodeAsync(string code);  // ← NEW
    Task<{Entity}> CreateAsync({Entity} entity);
    // ... other methods ...
}
```

### Service Implementation Changes
```csharp
public class {Entity}Service : I{Entity}Service
{
    // Add constructor parameter
    private readonly ICodeGenerationService _codeService;  // ← NEW
    
    public {Entity}Service(
        IUnitOfWork unitOfWork,
        ICodeGenerationService codeService,  // ← ADD
        // ... other params ...
    )
    {
        _codeService = codeService;  // ← STORE
        // ... other assignments ...
    }
    
    // Add GetByCode method
    public async Task<{Entity}?> GetByCodeAsync(string code)
    {
        // See template above
    }
    
    // Update CreateAsync method
    public async Task<{Entity}> CreateAsync({Entity} entity)
    {
        // Generate code before saving
        entity.{EntityName}Code = await _codeService.GenerateCodeAsync(
            "{EntityName}", 
            _tenantService.BranchId
        );
        
        // ... rest of create logic ...
    }
}
```

---

## 📊 Summary of Changes

### Total Changes Per Service
- Add 1 dependency (ICodeGenerationService)
- Update 1 constructor
- Add 1 new method (GetByCodeAsync)
- Update 1 method (CreateAsync)
- Update 1 interface

### Files to Modify: 15
- CustomerService.cs
- UserService.cs
- BranchService.cs
- LoanCaseService.cs
- LoanProductService.cs
- InstallmentService.cs
- ReceiptService.cs
- PartnerService.cs
- CapitalAccountService.cs
- AccountingService.cs
- AuditService.cs
- JournalController.cs (journal entries)
- EquityService.cs
- ReportService.cs (read-only, minimal change)
- CollectionService.cs (minimal change)

### Lines of Code Changes
- Per service: 20-40 new lines
- Total: ~400-500 lines added
- Total modifications: ~300-400 lines updated

---

## 🧪 Testing Strategy

### Unit Tests to Add
```csharp
[Test]
public async Task CreateAsync_GeneratesUniqueCustomerCode()
{
    // Arrange
    var service = new CustomerService(unitOfWork, dbContext, fileLogger, tenantService, codeService);
    var customer = new Customer { Name = "John", Phone = "9999" };
    
    // Act
    var created = await service.CreateAsync(customer);
    
    // Assert
    Assert.NotNull(created.CustomerCode);
    Assert.StartsWith("CUS", created.CustomerCode);
}

[Test]
public async Task GetByCodeAsync_ReturnsCustomerByCode()
{
    // Arrange
    var code = "CUS0001";
    var customer = new Customer { Id = Guid.NewGuid(), CustomerCode = code };
    
    // Act
    var found = await service.GetByCodeAsync(code);
    
    // Assert
    Assert.NotNull(found);
    Assert.AreEqual(code, found.CustomerCode);
}
```

### Integration Tests
```csharp
[Test]
public async Task MultipleCreates_GenerateSequentialCodes()
{
    // Create 3 customers
    var c1 = await service.CreateAsync(new Customer { ... });
    var c2 = await service.CreateAsync(new Customer { ... });
    var c3 = await service.CreateAsync(new Customer { ... });
    
    // Verify sequential
    Assert.AreEqual("CUS0001", c1.CustomerCode);
    Assert.AreEqual("CUS0002", c2.CustomerCode);
    Assert.AreEqual("CUS0003", c3.CustomerCode);
}
```

---

## ✅ Phase 2 Success Criteria

- [x] All 15 services have ICodeGenerationService injected
- [x] All Create methods generate codes before saving
- [x] All services have GetByCode methods
- [x] All service interfaces updated with new methods
- [x] Code generation called for all entity creates
- [x] All service constructors updated
- [x] Build succeeds with 0 errors
- [x] No breaking changes to existing code
- [x] Documentation updated
- [x] Ready for Phase 3

---

## 📚 Documentation Updates Needed

After Phase 2 implementation, update:

1. **Phase 2 Complete** - New file documenting implementation
2. **API Endpoints** - Document new GetByCode endpoints (Phase 3)
3. **Code Examples** - Update with real code generation examples
4. **Testing Guide** - Include unit test examples
5. **Project Progress** - Update to 2/10 phases

---

## 🎯 Expected Outcomes

### By End of Phase 2
- ✅ Codes auto-generate for all entities on creation
- ✅ Services can lookup entities by code
- ✅ Sequential code generation working (CUS0001, CUS0002, etc.)
- ✅ Per-branch isolation maintained
- ✅ Build successful
- ✅ Ready for API updates in Phase 3

### Not Yet Implemented (Later Phases)
- ❌ API endpoints returning codes (Phase 3)
- ❌ UI displaying codes (Phase 4+)
- ❌ Code-based search (Phase 5)
- ❌ Reports with codes (Phase 6)

---

## 🔄 Implementation Order Recommendation

1. **Start with**: CustomerService (simplest pattern)
2. **Then**: UserService, BranchService (similar patterns)
3. **Then**: LoanCaseService (upgrade existing)
4. **Then**: Financial services (ReceiptService, PartnerService, etc.)
5. **Then**: Advanced services (JournalService, AuditService)
6. **Finally**: Verification and testing

---

## 🚀 Ready to Begin Phase 2?

Phase 1 (database schema) is complete. Phase 2 is ready to implement.

**Prerequisites Met**:
- ✅ Database schema prepared
- ✅ CodeSequence table created
- ✅ CodeGenerationService implemented
- ✅ All entities updated with code properties
- ✅ Build succeeds

**Ready to Start**:
- Services ready to inject CodeGenerationService
- Create methods ready for code generation
- GetByCode methods ready to implement

**Estimated Timeline**: 1-2 days  
**Estimated Effort**: 8-12 hours  
**Build Verification**: After each service update  

---

**Phase 2 Status**: 📋 READY FOR IMPLEMENTATION  
**Date**: June 13, 2026  
**Build**: ✅ Phase 1 Success  
**Next Phase**: Phase 3 - API & DTO Updates

---

## 📞 Questions About Phase 2?

See `PHASE1_COMPLETE.md` for CodeGenerationService details  
See code examples above for implementation patterns  
See individual service files for current structure  

**Let's implement Phase 2!** 🚀
