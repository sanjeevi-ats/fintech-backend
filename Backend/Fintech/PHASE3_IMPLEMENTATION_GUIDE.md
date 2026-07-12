# Phase 3: API & DTO Implementation - Business Code Integration

**Phase**: 3 of 10  
**Status**: 📋 READY FOR IMPLEMENTATION  
**Date**: June 13, 2026  
**Estimated Duration**: 1-2 days  
**Effort**: 12-16 hours  
**Scope**: DTOs + API Controllers + Swagger Documentation  

---

## 🎯 Phase 3 Objectives

Update all API responses to include business codes alongside GUIDs. Create new API endpoints for code-based lookups.

### Primary Goals
1. ✅ Update all DTOs to include code fields
2. ✅ Update API responses to return codes
3. ✅ Create GetByCode endpoints in all controllers
4. ✅ Update Swagger documentation
5. ✅ Maintain backward compatibility
6. ✅ Verify build succeeds

---

## 📋 Work Breakdown

### Part A: DTO Updates
Update `Dtos.cs` to add code properties to all response DTOs

### Part B: Controller Updates
Update all 15+ API controllers to:
1. Return codes in responses
2. Add GetByCode endpoints
3. Add code to Create request/response

### Part C: Documentation
Update Swagger metadata for new endpoints

---

## 📝 DTO Update Pattern

### Current DTO Structure
```csharp
public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
}
```

### Updated DTO Structure
```csharp
public class CustomerDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }          // ← ADD
    public string Name { get; set; }
    public string Phone { get; set; }
}
```

### All DTOs to Update
1. CustomerDto - Add `Code` (string)
2. UserDto - Add `Code` (string)
3. BranchDto - Add `Code` (string)
4. LoanCaseDto - Add `LoanCode` (string)
5. LoanProductDto - Add `ProductCode` (string)
6. InstallmentDto - Add `Code` (string)
7. ReceiptDto - Add `Code` (string)
8. PartnerDto - Add `Code` (string)
9. CapitalAccountDto - Add `Code` (string)
10. AccountDto - Add `Code` (string)
11. AuditLogDto - Add `Code` (string)
12. JournalEntryDto - Add `Code` (string)
13. JournalLineDto - Add `Code` (string)
14. DayEndDto - Add `Code` (string)
15. ProfitDistributionDto - Add `Code` (string)

---

## 🔄 Controller Update Pattern

### Current Controller Method
```csharp
[HttpPost("create")]
public async Task<IActionResult> Create(CreateCustomerRequest request)
{
    var customer = new Customer 
    { 
        Name = request.Name, 
        Phone = request.Phone 
    };
    
    var created = await _customerService.CreateAsync(customer);
    
    return Ok(new CustomerDto 
    { 
        Id = created.Id, 
        Name = created.Name, 
        Phone = created.Phone 
    });
}
```

### Updated Controller Method
```csharp
[HttpPost("create")]
public async Task<IActionResult> Create(CreateCustomerRequest request)
{
    var customer = new Customer 
    { 
        Name = request.Name, 
        Phone = request.Phone 
    };
    
    var created = await _customerService.CreateAsync(customer);
    
    return Ok(new CustomerDto 
    { 
        Id = created.Id, 
        Code = created.CustomerCode,      // ← ADD
        Name = created.Name, 
        Phone = created.Phone 
    });
}
```

### New GetByCode Endpoint
```csharp
[HttpGet("by-code/{code}")]
public async Task<IActionResult> GetByCode(string code)
{
    var customer = await _customerService.GetByCodeAsync(code);
    
    if (customer == null)
        return NotFound(new { message = $"Customer with code {code} not found" });
    
    return Ok(new CustomerDto 
    { 
        Id = customer.Id, 
        Code = customer.CustomerCode, 
        Name = customer.Name, 
        Phone = customer.Phone 
    });
}
```

### Swagger Documentation
```csharp
/// <summary>
/// Get customer by business code
/// </summary>
/// <param name="code">Customer code (e.g., CUS0001)</param>
/// <returns>Customer details with code</returns>
[HttpGet("by-code/{code}")]
[ProduceResponseType(typeof(CustomerDto), 200)]
[ProduceResponseType(404)]
public async Task<IActionResult> GetByCode(string code)
{
    // ... implementation
}
```

---

## 🗂️ Controllers to Update (15+)

### Customer/User Management (3)
- [ ] **CustomersController** - Add `Code` to response, add GetByCode endpoint
- [ ] **UsersController** - Add `Code` to response, add GetByCode endpoint  
- [ ] **BranchController** - Add `Code` to response, add GetByCode endpoint

### Loan & Credit (6)
- [ ] **LoanCasesController** - Add `LoanCode` to response, upgrade GetByCode
- [ ] **ProductController** - Add `ProductCode` to response, add GetByCode
- [ ] **InstallmentsController** - Add `Code` to response, add GetByCode
- [ ] **LoanClosureController** - Add code support where applicable

### Financial (4)
- [ ] **ReceiptsController** - Add `Code` to response, add GetByCode
- [ ] **PartnersController** - Add `Code` to response, add GetByCode
- [ ] **CapitalAccountsController** - Add `Code` to response, add GetByCode
- [ ] **AccountingController** - Add Account `Code` to response, add GetByCode

### Accounting/Operations (2+)
- [ ] **AuditController** - Add `Code` to response, add GetByCode
- [ ] **JournalController** - Add `Code` to response, add GetByCode (dual entities)
- [ ] **DayEndController** - Add `Code` support if applicable
- [ ] **EquityController** - Add ProfitDistribution `Code` support

### Advanced (2)
- [ ] **ReportController** - Add code fields to reports
- [ ] **CollectionController** - Add codes to collection responses

---

## 📋 DTO Update Implementation

### Step 1: Open Dtos.cs
**File**: `Dtos.cs`

### Step 2: Add Code Fields to All DTOs

```csharp
namespace Fintech;

// CUSTOMER DTOS
public class CustomerDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }        // ← ADD
    public string Name { get; set; }
    public string Phone { get; set; }
}

public class CreateCustomerRequest
{
    public string Name { get; set; }
    public string Phone { get; set; }
}

// USER DTOS
public class UserDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }        // ← ADD
    public string Name { get; set; }
    public string Email { get; set; }
}

// LOAN DTOS
public class LoanCaseDto
{
    public Guid Id { get; set; }
    public string LoanCode { get; set; }    // ← ADD
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } // ← ADD (for reference)
    public long Principal { get; set; }
    public long InterestAmount { get; set; }
    public string Status { get; set; }
}

// ... similar for all 15+ DTOs
```

---

## 🔄 Step-by-Step Implementation for Each Controller

### For Each Controller File:

#### 1. Update Create Method
```csharp
[HttpPost("create")]
public async Task<IActionResult> Create(Create{Entity}Request request)
{
    var entity = new {Entity} { /* map from request */ };
    var created = await _{entity}Service.CreateAsync(entity);
    
    return Ok(new {Entity}Dto 
    { 
        Id = created.Id,
        Code = created.{EntityName}Code,  // ← ADD THIS LINE
        // ... other fields
    });
}
```

#### 2. Add GetById Method Update
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var entity = await _{entity}Service.GetByIdAsync(id);
    
    return Ok(new {Entity}Dto 
    { 
        Id = entity.Id,
        Code = entity.{EntityName}Code,  // ← ADD THIS LINE
        // ... other fields
    });
}
```

#### 3. Add New GetByCode Endpoint
```csharp
/// <summary>
/// Get {entity} by business code
/// </summary>
[HttpGet("by-code/{code}")]
[ProduceResponseType(typeof({Entity}Dto), 200)]
[ProduceResponseType(404)]
public async Task<IActionResult> GetByCode(string code)
{
    var entity = await _{entity}Service.GetByCodeAsync(code);
    
    if (entity == null)
        return NotFound(new { message = $"{Entity} with code {code} not found" });
    
    return Ok(new {Entity}Dto 
    { 
        Id = entity.Id,
        Code = entity.{EntityName}Code,
        // ... other fields
    });
}
```

#### 4. Update GetAll Method
```csharp
[HttpGet("all")]
public async Task<IActionResult> GetAll()
{
    var entities = await _{entity}Service.GetAllAsync();
    
    var dtos = entities.Select(e => new {Entity}Dto 
    { 
        Id = e.Id,
        Code = e.{EntityName}Code,  // ← ADD THIS LINE
        // ... other fields
    }).ToList();
    
    return Ok(dtos);
}
```

---

## 🎯 Priority Order

### Tier 1: Core APIs (Hours 1-2)
1. CustomersController
2. UsersController
3. BranchController

### Tier 2: Loan APIs (Hours 2-4)
1. LoanCasesController
2. ProductController
3. InstallmentsController

### Tier 3: Financial APIs (Hours 4-6)
1. ReceiptsController
2. PartnersController
3. CapitalAccountsController

### Tier 4: Advanced APIs (Hours 6-8+)
1. AuditController
2. JournalController
3. AccountingController
4. EquityController
5. ReportController
6. CollectionController

---

## 🚀 Implementation Checklist

### DTOs
- [ ] Add Code field to CustomerDto
- [ ] Add Code field to UserDto
- [ ] Add Code field to BranchDto
- [ ] Add Code field to LoanCaseDto
- [ ] Add Code field to LoanProductDto
- [ ] Add Code field to InstallmentDto
- [ ] Add Code field to ReceiptDto
- [ ] Add Code field to PartnerDto
- [ ] Add Code field to CapitalAccountDto
- [ ] Add Code field to AccountDto
- [ ] Add Code field to AuditLogDto
- [ ] Add Code field to JournalEntryDto
- [ ] Add Code field to JournalLineDto
- [ ] Add Code field to DayEndDto
- [ ] Add Code field to ProfitDistributionDto

### Controllers (Each needs 3 updates)
- [ ] Update Create response (include code)
- [ ] Update GetById response (include code)
- [ ] Update GetAll response (include code)
- [ ] Add GetByCode endpoint with [HttpGet("by-code/{code}")]

### Documentation
- [ ] Add XML comments for new GetByCode endpoints
- [ ] Update Swagger/OpenAPI documentation
- [ ] Add [ProduceResponseType] attributes
- [ ] Document error responses (404 for not found)

### Build Verification
- [ ] dotnet build succeeds (0 errors)
- [ ] No breaking changes to existing endpoints
- [ ] All new endpoints compile

---

## 📊 Expected Changes Per Controller

| Change | Lines | Impact |
|--------|-------|--------|
| DTO updates (15) | 2-3 lines each | 30-45 lines |
| Create method updates (15) | 1 line each | 15 lines |
| GetById updates (15) | 1 line each | 15 lines |
| GetAll updates (15) | 1 line each | 15 lines |
| New GetByCode endpoints (15) | 15-20 lines each | 225-300 lines |
| Total | | ~300-390 lines |

---

## 🧪 Testing Each Controller

### Test Create with Code
```bash
POST /api/customers/create
Body: { "name": "John", "phone": "9999" }
Response: { "id": "{guid}", "code": "CUS0001", "name": "John", "phone": "9999" }
```

### Test GetByCode
```bash
GET /api/customers/by-code/CUS0001
Response: { "id": "{guid}", "code": "CUS0001", "name": "John", "phone": "9999" }
```

### Test GetById (Updated)
```bash
GET /api/customers/{id}
Response: { "id": "{guid}", "code": "CUS0001", "name": "John", "phone": "9999" }
```

---

## ✅ Phase 3 Success Criteria

- [x] All DTOs updated with code fields
- [x] All Create methods include codes in responses
- [x] All GetById methods include codes in responses
- [x] All GetAll methods include codes in responses
- [x] All controllers have GetByCode endpoints
- [x] Swagger documentation updated
- [x] Build succeeds (0 errors)
- [x] No breaking changes
- [x] Backward compatible

---

## 📈 Timeline

| Task | Duration | Start | End |
|------|----------|-------|-----|
| DTO updates | 30 min | Hour 0 | Hour 0.5 |
| Tier 1 controllers | 1 hour | Hour 0.5 | Hour 1.5 |
| Tier 2 controllers | 1.5 hours | Hour 1.5 | Hour 3 |
| Tier 3 controllers | 1.5 hours | Hour 3 | Hour 4.5 |
| Tier 4 controllers | 2 hours | Hour 4.5 | Hour 6.5 |
| Testing & verification | 1 hour | Hour 6.5 | Hour 7.5 |
| Buffer | 1 hour | Hour 7.5 | Hour 8.5 |

**Total: ~8-12 hours**

---

## 🎯 API Endpoints Summary (After Phase 3)

For each entity, will have 5 endpoints:

```
GET    /api/{entities}              → List all (with codes)
GET    /api/{entities}/{id}         → Get by ID (with code)
GET    /api/{entities}/by-code/{code} → Get by CODE (NEW)
POST   /api/{entities}/create       → Create (returns code)
PUT    /api/{entities}/{id}         → Update
DELETE /api/{entities}/{id}         → Delete
```

Example for Customers:
```
GET    /api/customers               → List all customers
GET    /api/customers/550e8400...   → Get specific customer
GET    /api/customers/by-code/CUS0001 → Get by code (NEW)
POST   /api/customers/create        → Create with auto-code
PUT    /api/customers/550e8400...   → Update
DELETE /api/customers/550e8400...   → Delete
```

---

## 📞 Common Patterns

### Response with Code
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "CUS0001",
  "name": "John Doe",
  "phone": "9999-0000",
  "isActive": true
}
```

### Error Response (Code Not Found)
```json
{
  "statusCode": 404,
  "message": "Customer with code CUS0999 not found",
  "timestamp": "2026-06-13T10:00:00Z"
}
```

### List Response
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "code": "CUS0001",
    "name": "John Doe"
  },
  {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "code": "CUS0002",
    "name": "Jane Smith"
  }
]
```

---

## 🚀 Ready to Implement Phase 3?

Phase 1 & 2 are complete. Phase 3 is straightforward:
1. Add code fields to DTOs
2. Update controller methods to include codes
3. Add GetByCode endpoints
4. Verify build

**Estimated Time**: 8-12 hours  
**Complexity**: Medium (repetitive pattern)  
**Risk**: Low (no breaking changes)

---

**Next Step**: Implement Phase 3 following this guide

**Questions?** Refer to Phase 1 or Phase 2 documentation for patterns
