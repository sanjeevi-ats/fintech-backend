# ID Refactoring Implementation Plan - Keep GUIDs + Add Business Codes

## 📋 Overview

This is a **non-breaking refactoring** that adds human-readable business codes while keeping all existing GUIDs intact. The business codes will be auto-generated, sequential, and displayed to users instead of GUIDs.

**Key Principle**: Hybrid approach - GUID as database PK (unchanged), Business Code for user display (new).

---

## 🎯 Scope: All 15 Business Entities

| Entity | Business Code | Format | Example |
|--------|---------------|--------|---------|
| Customer | CustomerCode | CUS{0000} | CUS0001, CUS0002 |
| LoanCase | LoanCode | LN{0000} | LN0001, LN0002 |
| LoanProduct | ProductCode | PRO{0000} | PRO0001, PRO0002 |
| Installment | InstallmentCode | INST{0000} | INST0001, INST0002 |
| Receipt | ReceiptCode | RCP{0000} | RCP0001, RCP0002 |
| Partner | PartnerCode | PAR{0000} | PAR0001, PAR0002 |
| CapitalAccount | CapitalAcctCode | CAP{0000} | CAP0001, CAP0002 |
| JournalEntry | JournalCode | JE{0000} | JE0001, JE0002 |
| JournalLine | JournalLineCode | JL{0000} | JL0001, JL0002 |
| Account | AccountCode | ACC{0000} | ACC0001, ACC0002 |
| DayEnd | DayEndCode | DE{0000} | DE0001, DE0002 |
| ProfitDistribution | ProfitCode | PFT{0000} | PFT0001, PFT0002 |
| Branch | BranchCode | BR{0000} | BR0001, BR0002 |
| User | UserCode | USR{0000} | USR0001, USR0002 |
| AuditLog | AuditCode | AUD{0000} | AUD0001, AUD0002 |

---

## 🔧 Implementation Phases

### Phase 1: Database Schema Updates (SQL Migrations)

#### 1.1 Add Code Columns

```sql
-- Customers Table
ALTER TABLE customers ADD COLUMN customer_code VARCHAR(20) UNIQUE;

-- Loan Cases Table
ALTER TABLE loan_cases ADD COLUMN loan_code_new VARCHAR(20) UNIQUE;
-- Note: Keep existing loan_code for backward compatibility

-- Loan Products Table
ALTER TABLE loan_products ADD COLUMN product_code VARCHAR(20) UNIQUE;

-- Installments Table
ALTER TABLE installments ADD COLUMN installment_code VARCHAR(20) UNIQUE;

-- Receipts Table
ALTER TABLE receipts ADD COLUMN receipt_code VARCHAR(20) UNIQUE;

-- Partners Table
ALTER TABLE partners ADD COLUMN partner_code VARCHAR(20) UNIQUE;

-- Capital Accounts Table
ALTER TABLE capital_accounts ADD COLUMN capital_account_code VARCHAR(20) UNIQUE;

-- Journal Entries Table
ALTER TABLE journal_entries ADD COLUMN journal_code VARCHAR(20) UNIQUE;

-- Journal Lines Table
ALTER TABLE journal_lines ADD COLUMN journal_line_code VARCHAR(20) UNIQUE;

-- Accounts Table
ALTER TABLE accounts ADD COLUMN account_code VARCHAR(20) UNIQUE;

-- Day Ends Table
ALTER TABLE day_ends ADD COLUMN day_end_code VARCHAR(20) UNIQUE;

-- Profit Distributions Table
ALTER TABLE profit_distributions ADD COLUMN profit_code VARCHAR(20) UNIQUE;

-- Branches Table
ALTER TABLE branches ADD COLUMN branch_code VARCHAR(20) UNIQUE;

-- Users Table
ALTER TABLE users ADD COLUMN user_code VARCHAR(20) UNIQUE;

-- Audit Logs Table
ALTER TABLE audit_logs ADD COLUMN audit_code VARCHAR(20) UNIQUE;

-- Create sequence table for tracking code generation
CREATE TABLE IF NOT EXISTS code_sequences (
    id SERIAL PRIMARY KEY,
    entity_name VARCHAR(100) UNIQUE NOT NULL,
    branch_id UUID NOT NULL,
    next_sequence_number INTEGER DEFAULT 1,
    code_prefix VARCHAR(10) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Index for faster lookups
CREATE INDEX idx_code_sequences_entity_branch 
ON code_sequences(entity_name, branch_id);

-- Create indices for business code lookups
CREATE INDEX idx_customers_code ON customers(customer_code);
CREATE INDEX idx_loan_cases_code ON loan_cases(loan_code_new);
CREATE INDEX idx_loan_products_code ON loan_products(product_code);
CREATE INDEX idx_installments_code ON installments(installment_code);
CREATE INDEX idx_receipts_code ON receipts(receipt_code);
CREATE INDEX idx_partners_code ON partners(partner_code);
CREATE INDEX idx_capital_accounts_code ON capital_accounts(capital_account_code);
CREATE INDEX idx_journal_entries_code ON journal_entries(journal_code);
CREATE INDEX idx_journal_lines_code ON journal_lines(journal_line_code);
CREATE INDEX idx_accounts_code ON accounts(account_code);
CREATE INDEX idx_day_ends_code ON day_ends(day_end_code);
CREATE INDEX idx_profit_distributions_code ON profit_distributions(profit_code);
CREATE INDEX idx_branches_code ON branches(branch_code);
CREATE INDEX idx_users_code ON users(user_code);
CREATE INDEX idx_audit_logs_code ON audit_logs(audit_code);
```

#### 1.2 Migrate Existing Data

```sql
-- Function to generate code for existing records
CREATE OR REPLACE FUNCTION generate_sequential_code(p_prefix VARCHAR, p_branch_id UUID, p_entity_name VARCHAR)
RETURNS VARCHAR AS $$
DECLARE
    v_next_seq INTEGER;
    v_code VARCHAR;
BEGIN
    -- Get and increment sequence for this entity and branch
    UPDATE code_sequences 
    SET next_sequence_number = next_sequence_number + 1,
        updated_at = CURRENT_TIMESTAMP
    WHERE entity_name = p_entity_name AND branch_id = p_branch_id
    RETURNING next_sequence_number INTO v_next_seq;
    
    IF v_next_seq IS NULL THEN
        -- First record for this entity in this branch
        INSERT INTO code_sequences (entity_name, branch_id, next_sequence_number, code_prefix)
        VALUES (p_entity_name, p_branch_id, 2, p_prefix)
        RETURNING next_sequence_number INTO v_next_seq;
        v_next_seq := 1;
    END IF;
    
    -- Generate code with zero-padded number
    v_code := p_prefix || LPAD(v_next_seq::VARCHAR, 4, '0');
    RETURN v_code;
END;
$$ LANGUAGE plpgsql;

-- Migrate Customer Codes
UPDATE customers c
SET customer_code = generate_sequential_code('CUS', c.branch_id, 'Customer')
WHERE customer_code IS NULL;

-- Migrate Loan Codes
UPDATE loan_cases lc
SET loan_code_new = generate_sequential_code('LN', lc.branch_id, 'LoanCase')
WHERE loan_code_new IS NULL;

-- Similar migrations for other entities...
UPDATE loan_products lp
SET product_code = generate_sequential_code('PRO', lp.branch_id, 'LoanProduct')
WHERE product_code IS NULL;

UPDATE installments i
SET installment_code = generate_sequential_code('INST', i.branch_id, 'Installment')
WHERE installment_code IS NULL;

-- ... continue for all other entities
```

---

### Phase 2: Entity Model Updates (C# Domain)

#### 2.1 Update Entity Classes

Add code properties to each entity without removing ID:

```csharp
// Customer.cs
public class Customer
{
    public Guid Id { get; set; }  // ← Keep GUID PK (unchanged)
    public Guid BranchId { get; set; }
    
    // NEW: Business code for display
    [Column("customer_code")]
    public string? CustomerCode { get; set; }  // ← Add this
    
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Aadhaar_Encrypted { get; set; } = string.Empty;
    public string PAN_Encrypted { get; set; } = string.Empty;
}

// LoanCase.cs
public class LoanCase
{
    public Guid Id { get; set; }  // ← Keep GUID PK (unchanged)
    public Guid BranchId { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    
    // EXISTING: Original code field (for backward compatibility)
    [Column("loan_code")]
    public string? LoanCode { get; set; }
    
    // NEW: Standardized code field
    [Column("loan_code_new")]
    public string? LoanCodeNew { get; set; }  // ← Rename existing to this eventually
    
    public long Principal { get; set; }
    public long InterestAmount { get; set; }
    public long TotalReceivable { get; set; }
    public long ProcessingFees { get; set; }
    public LoanStatus Status { get; set; }
    [ConcurrencyCheck]
    public long Version { get; set; }
    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
}

// Similar updates for all entities...
```

#### 2.2 Create CodeSequence Entity

```csharp
// CodeSequence.cs
public class CodeSequence
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public int NextSequenceNumber { get; set; } = 1;
    public string CodePrefix { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

---

### Phase 3: Service Implementation

#### 3.1 Create CodeGenerationService

```csharp
// CodeGenerationService.cs
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Application.Services;

public interface ICodeGenerationService
{
    Task<string> GenerateCodeAsync(string entityName, Guid branchId);
    Task InitializeSequencesAsync(Guid branchId);
}

public class CodeGenerationService : ICodeGenerationService
{
    private readonly FinVedaDbContext _context;
    private readonly ILogger<CodeGenerationService> _logger;
    
    private static readonly Dictionary<string, string> EntityPrefixes = new()
    {
        { "Customer", "CUS" },
        { "LoanCase", "LN" },
        { "LoanProduct", "PRO" },
        { "Installment", "INST" },
        { "Receipt", "RCP" },
        { "Partner", "PAR" },
        { "CapitalAccount", "CAP" },
        { "JournalEntry", "JE" },
        { "JournalLine", "JL" },
        { "Account", "ACC" },
        { "DayEnd", "DE" },
        { "ProfitDistribution", "PFT" },
        { "Branch", "BR" },
        { "User", "USR" },
        { "AuditLog", "AUD" }
    };

    public CodeGenerationService(FinVedaDbContext context, ILogger<CodeGenerationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateCodeAsync(string entityName, Guid branchId)
    {
        if (!EntityPrefixes.TryGetValue(entityName, out var prefix))
        {
            throw new ArgumentException($"Unknown entity: {entityName}");
        }

        try
        {
            // Get or create sequence for this entity and branch
            var sequence = await _context.CodeSequences
                .FirstOrDefaultAsync(cs => 
                    cs.EntityName == entityName && cs.BranchId == branchId);

            if (sequence == null)
            {
                sequence = new CodeSequence
                {
                    EntityName = entityName,
                    BranchId = branchId,
                    CodePrefix = prefix,
                    NextSequenceNumber = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CodeSequences.Add(sequence);
            }

            // Generate code
            var code = $"{prefix}{sequence.NextSequenceNumber:D4}";
            
            // Increment for next
            sequence.NextSequenceNumber++;
            sequence.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Generated code {Code} for entity {Entity} in branch {BranchId}", 
                code, entityName, branchId);
            
            return code;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code for entity {Entity} in branch {BranchId}", 
                entityName, branchId);
            throw;
        }
    }

    public async Task InitializeSequencesAsync(Guid branchId)
    {
        foreach (var (entityName, prefix) in EntityPrefixes)
        {
            var existing = await _context.CodeSequences
                .AnyAsync(cs => cs.EntityName == entityName && cs.BranchId == branchId);

            if (!existing)
            {
                _context.CodeSequences.Add(new CodeSequence
                {
                    EntityName = entityName,
                    BranchId = branchId,
                    CodePrefix = prefix,
                    NextSequenceNumber = 1,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}
```

#### 3.2 Register Service in DI

```csharp
// Program.cs
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
```

---

### Phase 4: Update Business Services

Update each service to auto-generate codes on entity creation:

```csharp
// CustomerService.cs
public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICodeGenerationService _codeGenerator;
    private readonly ITenantService _tenantService;

    public async Task<Customer> CreateAsync(Customer customer)
    {
        try
        {
            // Auto-generate business code
            customer.CustomerCode = await _codeGenerator.GenerateCodeAsync("Customer", _tenantService.BranchId);
            
            var repo = _unitOfWork.Repository<Customer>();
            repo.Add(customer);
            await _unitOfWork.SaveChangesAsync();
            
            return customer;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}

// LoanCaseService.cs
public class LoanCaseService : ILoanCaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICodeGenerationService _codeGenerator;
    private readonly ITenantService _tenantService;

    public async Task<LoanCase> CreateLoanAsync(LoanCase loanCase)
    {
        try
        {
            // Auto-generate loan code
            loanCase.LoanCodeNew = await _codeGenerator.GenerateCodeAsync("LoanCase", _tenantService.BranchId);
            
            var repo = _unitOfWork.Repository<LoanCase>();
            repo.Add(loanCase);
            await _unitOfWork.SaveChangesAsync();
            
            return loanCase;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}

// Similar updates for all other services...
```

---

### Phase 5: DTO Updates

Update DTOs to include business codes:

```csharp
// CustomerDto.cs
public class CustomerDto
{
    public Guid Id { get; set; }  // ← Keep GUID for API responses
    public string? CustomerCode { get; set; }  // ← NEW: Display code
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Aadhaar { get; set; } = string.Empty;
    public string Pan { get; set; } = string.Empty;
}

// LoanCaseDto.cs
public class LoanCaseDto
{
    public Guid Id { get; set; }  // ← Keep GUID for internal reference
    public string? LoanCode { get; set; }  // ← Display code
    public Guid CustomerId { get; set; }  // ← Keep GUID for internal FK
    public string? CustomerCode { get; set; }  // ← NEW: Customer's display code
    public string CustomerName { get; set; } = string.Empty;
    public long Principal { get; set; }
    public long InterestAmount { get; set; }
    public long TotalReceivable { get; set; }
    public long ProcessingFees { get; set; }
    public string Status { get; set; } = string.Empty;
}

// API responses now look like:
/*
{
    "id": "0802b316-b265-4b1e-9189-0c93dca6e6fc",  ← GUID (internal)
    "loanCode": "LN0001",  ← Business Code (display)
    "customerId": "e82ce855-8e17-4eef-aa35-90579daee839",  ← GUID (internal)
    "customerCode": "CUS0001",  ← Business Code (display)
    "customerName": "John Doe"
}
*/
```

---

### Phase 6: API Updates

#### 6.1 Update Endpoints to Accept Business Codes

```csharp
// CustomersController.cs
[AutoLog]
[ApiController]
[Route("api/[controller]")]
public class CustomersController : BaseApiController
{
    // Search by code or name
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? code, [FromQuery] string? name)
    {
        var customers = await _service.SearchAsync(code, name);
        return Ok(customers);
    }

    // Get by code
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var customer = await _service.GetByCodeAsync(code);
        if (customer == null) return NotFound();
        return Ok(_mapper.Map<CustomerDto>(customer));
    }

    // Get by GUID (backward compatible)
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await _service.GetByIdAsync(id);
        if (customer == null) return NotFound();
        return Ok(_mapper.Map<CustomerDto>(customer));
    }
}

// LoansController.cs
[AutoLog]
[ApiController]
[Route("api/[controller]")]
public class LoansController : BaseApiController
{
    // Search by loan code
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? loanCode)
    {
        var loans = await _service.SearchByCodeAsync(loanCode);
        return Ok(loans);
    }

    // Get by code
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var loan = await _service.GetByCodeAsync(code);
        if (loan == null) return NotFound();
        return Ok(_mapper.Map<LoanCaseDto>(loan));
    }
}
```

#### 6.2 Update Existing Endpoints

```csharp
// When returning customer in responses
public async Task<IActionResult> GetCustomer(Guid id)
{
    var customer = await _service.GetByIdAsync(id);
    
    var dto = new CustomerDto
    {
        Id = customer.Id,  // ← Include GUID
        CustomerCode = customer.CustomerCode,  // ← Include business code
        Name = customer.Name,
        Phone = customer.Phone
    };
    
    return Ok(dto);
}
```

---

### Phase 7: Frontend Updates

#### 7.1 Display Codes Instead of GUIDs

**Before:**
```typescript
// Old: Showing GUIDs to user
<td>Customer: e82ce855-8e17-4eef-aa35-90579daee839</td>
<td>Loan: 0802b316-b265-4b1e-9189-0c93dca6e6fc</td>
```

**After:**
```typescript
// New: Showing business codes to user
<td>Customer: {customer.customerCode} ({customer.name})</td>
<td>Loan: {loan.loanCode}</td>

// Example:
<td>Customer: CUS0001 (John Doe)</td>
<td>Loan: LN0001</td>
```

#### 7.2 Search Implementation

```typescript
// customerService.ts
async searchCustomers(searchTerm: string): Promise<Customer[]> {
  // Can search by code or name
  return this.http.get<Customer[]>(
    `/api/customers/search?code=${searchTerm}&name=${searchTerm}`
  ).toPromise();
}

async getCustomerByCode(code: string): Promise<Customer> {
  return this.http.get<Customer>(`/api/customers/code/${code}`).toPromise();
}

// Usage in component
onSearchCustomer(term: string) {
  this.customerService.searchCustomers(term).then(results => {
    // Results now show codes instead of GUIDs
    // Can also search: "CUS0001" or "John Doe"
  });
}
```

#### 7.3 Update UI Components

```typescript
// loansComponent.tsx
export const LoansPage = () => {
  return (
    <table>
      <thead>
        <tr>
          <th>Loan Code</th>  {/* Changed from "Loan ID" */}
          <th>Customer Code</th>  {/* Changed from "Customer ID" */}
          <th>Customer Name</th>
          <th>Amount</th>
        </tr>
      </thead>
      <tbody>
        {loans.map(loan => (
          <tr key={loan.id}>
            <td>{loan.loanCode}</td>  {/* LN0001 */}
            <td>{loan.customerCode}</td>  {/* CUS0001 */}
            <td>{loan.customerName}</td>
            <td>₹{loan.principal / 100}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};
```

---

### Phase 8: Search Implementation

#### 8.1 Add Search Methods to Services

```csharp
// CustomerService.cs
public interface ICustomerService
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByCodeAsync(string code);  // ← NEW
    Task<IEnumerable<Customer>> SearchAsync(string? code, string? name);  // ← NEW
}

public class CustomerService : ICustomerService
{
    public async Task<Customer?> GetByCodeAsync(string code)
    {
        var repo = _unitOfWork.Repository<Customer>();
        return await repo.FirstOrDefaultAsync(c => c.CustomerCode == code.ToUpper());
    }

    public async Task<IEnumerable<Customer>> SearchAsync(string? code, string? name)
    {
        var repo = _unitOfWork.Repository<Customer>();
        var query = repo.AsQueryable();

        if (!string.IsNullOrWhiteSpace(code))
            query = query.Where(c => c.CustomerCode!.Contains(code.ToUpper()));

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        return await query.ToListAsync();
    }
}

// LoanCaseService.cs
public interface ILoanCaseService
{
    Task<LoanCase?> GetByIdAsync(Guid id);
    Task<LoanCase?> GetByCodeAsync(string code);  // ← NEW
    Task<IEnumerable<LoanCase>> SearchAsync(string? code, string? customerCode);  // ← NEW
}

public class LoanCaseService : ILoanCaseService
{
    public async Task<LoanCase?> GetByCodeAsync(string code)
    {
        var repo = _unitOfWork.Repository<LoanCase>();
        return await repo.FirstOrDefaultAsync(lc => lc.LoanCodeNew == code.ToUpper());
    }

    public async Task<IEnumerable<LoanCase>> SearchAsync(string? code, string? customerCode)
    {
        var repo = _unitOfWork.Repository<LoanCase>();
        var query = repo.AsQueryable().Include(lc => lc.Customer);

        if (!string.IsNullOrWhiteSpace(code))
            query = query.Where(lc => lc.LoanCodeNew!.Contains(code.ToUpper()));

        if (!string.IsNullOrWhiteSpace(customerCode))
            query = query.Where(lc => lc.Customer.CustomerCode!.Contains(customerCode.ToUpper()));

        return await query.ToListAsync();
    }
}
```

---

### Phase 9: Reports

Update all reports to display business codes:

```csharp
// ReportService.cs
public async Task<LoanReport> GenerateLoanReportAsync(DateTime startDate, DateTime endDate)
{
    var loans = await _context.LoanCases
        .Include(lc => lc.Customer)
        .Include(lc => lc.Installments)
        .Where(lc => lc.CreatedAt >= startDate && lc.CreatedAt <= endDate)
        .ToListAsync();

    return new LoanReport
    {
        GeneratedDate = DateTime.UtcNow,
        Loans = loans.Select(lc => new LoanReportItem
        {
            LoanCode = lc.LoanCodeNew,  // ← Display code instead of GUID
            LoanId = lc.Id,  // ← Keep GUID for internal reference
            CustomerCode = lc.Customer.CustomerCode,  // ← Display code
            CustomerId = lc.Customer.Id,  // ← Keep GUID
            CustomerName = lc.Customer.Name,
            Principal = lc.Principal,
            InterestAmount = lc.InterestAmount,
            Status = lc.Status.ToString()
        }).ToList()
    };
}
```

---

### Phase 10: Testing & Validation

#### 10.1 Unit Tests

```csharp
[TestClass]
public class CodeGenerationServiceTests
{
    [TestMethod]
    public async Task GenerateCode_ShouldCreateUniqueSequentialCodes()
    {
        var service = new CodeGenerationService(_context, _logger);
        var branchId = Guid.NewGuid();

        var code1 = await service.GenerateCodeAsync("Customer", branchId);
        var code2 = await service.GenerateCodeAsync("Customer", branchId);
        var code3 = await service.GenerateCodeAsync("Customer", branchId);

        Assert.AreEqual("CUS0001", code1);
        Assert.AreEqual("CUS0002", code2);
        Assert.AreEqual("CUS0003", code3);
    }

    [TestMethod]
    public async Task GenerateCode_ShouldRespectBranchIsolation()
    {
        var service = new CodeGenerationService(_context, _logger);
        var branch1 = Guid.NewGuid();
        var branch2 = Guid.NewGuid();

        var code1_branch1 = await service.GenerateCodeAsync("Customer", branch1);
        var code1_branch2 = await service.GenerateCodeAsync("Customer", branch2);
        var code2_branch1 = await service.GenerateCodeAsync("Customer", branch1);

        Assert.AreEqual("CUS0001", code1_branch1);
        Assert.AreEqual("CUS0001", code1_branch2);  // Different branch gets same sequence
        Assert.AreEqual("CUS0002", code2_branch1);
    }
}
```

#### 10.2 Integration Tests

```csharp
[TestClass]
public class CustomerServiceIntegrationTests
{
    [TestMethod]
    public async Task CreateCustomer_ShouldAutoGenerateCode()
    {
        var service = new CustomerService(_unitOfWork, _codeGenerator, _tenantService);
        var customer = new Customer { Name = "Test Customer", Phone = "9876543210" };

        var created = await service.CreateAsync(customer);

        Assert.IsNotNull(created.CustomerCode);
        Assert.IsTrue(created.CustomerCode.StartsWith("CUS"));
    }

    [TestMethod]
    public async Task SearchCustomer_ByCode()
    {
        var service = new CustomerService(_unitOfWork, _codeGenerator, _tenantService);
        var customer = new Customer { Name = "Test", Phone = "9876543210" };
        await service.CreateAsync(customer);

        var found = await service.GetByCodeAsync(customer.CustomerCode!);

        Assert.IsNotNull(found);
        Assert.AreEqual(customer.Id, found.Id);
    }
}
```

---

## 📊 Migration Checklist

### Phase-by-Phase Execution Order

- [ ] **Phase 1**: Database Schema Updates
  - [ ] Create SQL migration for all new columns
  - [ ] Create code_sequences table
  - [ ] Add indices
  - [ ] Generate codes for existing records
  
- [ ] **Phase 2**: Entity Model Updates
  - [ ] Add code properties to all entities
  - [ ] Create CodeSequence entity
  - [ ] Add DbSet for CodeSequence in DbContext
  
- [ ] **Phase 3**: Service Implementation
  - [ ] Create CodeGenerationService
  - [ ] Register in DI container
  - [ ] Unit test code generation
  
- [ ] **Phase 4**: Update Business Services
  - [ ] Update all service Create methods
  - [ ] Add search methods
  - [ ] Add GetByCode methods
  - [ ] Integration testing
  
- [ ] **Phase 5**: DTO Updates
  - [ ] Update all DTOs to include code fields
  - [ ] Update mappers
  - [ ] Verify AutoMapper configurations
  
- [ ] **Phase 6**: API Updates
  - [ ] Update endpoints to return codes
  - [ ] Add new search endpoints
  - [ ] Add code-based lookup endpoints
  - [ ] Update API documentation
  
- [ ] **Phase 7**: Frontend Updates
  - [ ] Update components to display codes
  - [ ] Update search UI
  - [ ] Update forms
  - [ ] Update tables/lists
  
- [ ] **Phase 8**: Search Implementation
  - [ ] Add search UI component
  - [ ] Implement search service calls
  - [ ] Add auto-complete for codes
  
- [ ] **Phase 9**: Reports
  - [ ] Update all reports to show codes
  - [ ] PDF/Excel exports
  - [ ] Dashboard displays
  
- [ ] **Phase 10**: Testing & Deployment
  - [ ] Unit tests
  - [ ] Integration tests
  - [ ] E2E tests
  - [ ] Staging validation
  - [ ] Production deployment

---

## 🔄 Data Structure Examples

### Before & After

**Database Record - Before:**
```sql
SELECT * FROM customers;
-- id: e82ce855-8e17-4eef-aa35-90579daee839
-- name: John Doe
-- phone: 9876543210
```

**Database Record - After:**
```sql
SELECT id, customer_code, name, phone FROM customers;
-- id: e82ce855-8e17-4eef-aa35-90579daee839
-- customer_code: CUS0001
-- name: John Doe
-- phone: 9876543210
```

**API Response - Before:**
```json
{
    "id": "e82ce855-8e17-4eef-aa35-90579daee839",
    "name": "John Doe",
    "phone": "9876543210"
}
```

**API Response - After:**
```json
{
    "id": "e82ce855-8e17-4eef-aa35-90579daee839",
    "customerCode": "CUS0001",
    "name": "John Doe",
    "phone": "9876543210"
}
```

**UI Display - Before:**
```
Customer ID: e82ce855-8e17-4eef-aa35-90579daee839
```

**UI Display - After:**
```
Customer: CUS0001 (John Doe)
```

---

## ✅ Success Criteria

- ✅ All 15 entities have business codes
- ✅ GUIDs preserved in database (no data loss)
- ✅ Codes auto-generated on entity creation
- ✅ Backward compatible (GUIDs still in API responses)
- ✅ Search by code works
- ✅ UI displays codes instead of GUIDs
- ✅ Reports show codes
- ✅ Existing data migrated with codes
- ✅ Multi-branch isolation maintained
- ✅ All tests passing

---

**Total Estimated Effort**: 80-100 hours (3-4 weeks)
**Risk Level**: Medium (data migration, comprehensive refactoring)
**Breaking Changes**: None (backward compatible)

Ready to start implementation? Which phase would you like to begin with?
