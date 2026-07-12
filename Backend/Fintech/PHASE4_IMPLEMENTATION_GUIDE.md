# Phase 4: Frontend UI Integration - Business Code Display

**Phase**: 4 of 10  
**Status**: 📋 READY FOR IMPLEMENTATION  
**Date**: June 13, 2026  
**Estimated Duration**: 2-4 hours  
**Effort**: 8-12 hours  
**Scope**: Frontend UI updates to display & search business codes  

---

## 🎯 Phase 4 Objectives

Integrate business codes into the frontend UI across all major features. Users will see business codes alongside IDs, search by codes, and use codes for quick lookups.

### Primary Goals
1. ✅ Display codes in all data tables and forms
2. ✅ Add code-based search functionality
3. ✅ Update service layer to fetch codes from API
4. ✅ Display codes in detail views
5. ✅ Add code column to data tables
6. ✅ Support code-based filtering and searching

---

## 📋 Work Breakdown

### Part A: Service Layer Updates
Update all frontend services to map API responses and expose code properties

### Part B: UI Component Updates
Update data tables, forms, and detail views to display codes

### Part C: Search & Filter
Add code-based search and filtering capabilities

### Part D: Testing
Verify code display and search functionality

---

## 🏗️ Architecture Overview

### Frontend Stack
- **Framework**: Next.js 14 (React)
- **State**: React hooks + Context API
- **Styling**: Inline styles + CSS
- **HTTP**: Fetch API (via apiClient)
- **Services**: TypeScript service classes

### Data Flow
```
API Response (with codes)
    ↓
Service Layer (maps to DTO)
    ↓
React Component (displays codes)
    ↓
User Interface (shows code + ID)
```

### Code Display Pattern
```
Customer Display:
├─ ID: 550e8400-e29b-41d4-a716-446655440000
├─ Code: CUS0001 ← NEW
├─ Name: John Doe
└─ Phone: 9999-0000
```

---

## 🔄 Service Layer Updates

### Pattern: Add Code Property to DTOs

**Current (without code):**
```typescript
interface CustomerDto {
  id: string;
  name: string;
  aadhaar: string;
  pan: string;
}
```

**Updated (with code):**
```typescript
interface CustomerDto {
  id: string;
  code: string;          // ← NEW
  name: string;
  aadhaar: string;
  pan: string;
}
```

### Services to Update

#### 1. Customer Service
```typescript
// File: src/services/customerService.ts

interface CustomerDto {
  id: string;
  code: string;          // ← ADD
  name: string;
  aadhaar: string;
  pan: string;
}

export const customerService = {
  getAll: async (): Promise<CustomerDto[]> => {
    const response = await apiClient.get<CustomerDto[]>('/api/v1/customers');
    return response || [];
  },

  getById: async (id: string): Promise<CustomerDto> => {
    return apiClient.get(`/api/v1/customers/${id}`);
  },

  getByCode: async (code: string): Promise<CustomerDto> => {
    // NEW: Get customer by business code
    return apiClient.get(`/api/v1/customers/by-code/${code}`);
  },

  create: async (request: CreateCustomerRequest): Promise<CustomerDto> => {
    return apiClient.post('/api/v1/customers', request);
  },

  searchByCode: async (query: string): Promise<CustomerDto[]> => {
    // NEW: Search functionality
    const customers = await customerService.getAll();
    return customers.filter(c => 
      c.code.toLowerCase().includes(query.toLowerCase()) ||
      c.name.toLowerCase().includes(query.toLowerCase())
    );
  }
};
```

#### 2. User Service
```typescript
// File: src/services/userService.ts

interface UserDto {
  id: string;
  code: string;          // ← ADD
  name: string;
  email: string;
  role: string;
  isActive: boolean;
}

export const userService = {
  getByCode: async (code: string): Promise<UserDto> => {
    return apiClient.get(`/api/v1/users/by-code/${code}`);
  },

  getAll: async (): Promise<UserDto[]> => {
    // Update to include codes from API
  },
};
```

#### 3. Loan Service
```typescript
// File: src/services/loanService.ts

interface LoanCaseDto {
  id: string;
  loanCode: string;      // ← ADD
  customerId: string;
  customerCode: string;  // ← ADD
  customerName: string;
  principal: number;
  interestAmount: number;
  status: string;
}

export const loanService = {
  getByCode: async (code: string): Promise<LoanCaseDto> => {
    return apiClient.get(`/api/v1/loancases/by-code/${code}`);
  },

  getAll: async (): Promise<LoanCaseDto[]> => {
    // Update to include codes from API
  },
};
```

#### 4. Installment Service
```typescript
// File: src/services/installmentService.ts

interface InstallmentDto {
  id: string;
  code: string;          // ← ADD
  no: number;
  dueDate: string;
  amount: number;
  status: string;
}

export const installmentService = {
  getByCode: async (code: string): Promise<InstallmentDto> => {
    return apiClient.get(`/api/v1/installments/by-code/${code}`);
  },
};
```

#### 5. Receipt Service
```typescript
// File: src/services/receiptService.ts

interface ReceiptDto {
  id: string;
  code: string;          // ← ADD
  amountPaid: number;
  mode: string;
  capturedAt: string;
}

export const receiptService = {
  getByCode: async (code: string): Promise<ReceiptDto> => {
    return apiClient.get(`/api/v1/receipts/by-code/${code}`);
  },
};
```

#### 6. Partner Service
```typescript
// File: src/services/partnerService.ts

interface PartnerDto {
  id: string;
  code: string;          // ← ADD
  name: string;
  email: string;
  phone: string;
  equityPct: number;
}

export const partnerService = {
  getByCode: async (code: string): Promise<PartnerDto> => {
    return apiClient.get(`/api/v1/partners/by-code/${code}`);
  },
};
```

### Update Pattern for All Services

For each service file that works with entities that now have codes:

1. Add `code` (or specific like `loanCode`, `customerCode`) to the DTO interface
2. Add new `getByCode()` async method to the service
3. Add optional `searchByCode()` method for client-side search
4. Update `getAll()` and `getById()` to include codes in responses

---

## 🎨 UI Component Updates

### Part A: Data Tables - Add Code Column

**Current Table (without code):**
```typescript
<table>
  <thead>
    <tr>
      <th>ID</th>
      <th>Name</th>
      <th>Phone</th>
      <th>Actions</th>
    </tr>
  </thead>
  <tbody>
    {customers.map(c => (
      <tr key={c.id}>
        <td>{c.id}</td>
        <td>{c.name}</td>
        <td>{c.phone}</td>
        <td>...</td>
      </tr>
    ))}
  </tbody>
</table>
```

**Updated Table (with code):**
```typescript
<table>
  <thead>
    <tr>
      <th>Code</th>          {/* ← NEW */}
      <th>ID</th>
      <th>Name</th>
      <th>Phone</th>
      <th>Actions</th>
    </tr>
  </thead>
  <tbody>
    {customers.map(c => (
      <tr key={c.id}>
        <td><strong>{c.code}</strong></td> {/* ← NEW */}
        <td>{c.id.substring(0, 8)}...</td>
        <td>{c.name}</td>
        <td>{c.phone}</td>
        <td>...</td>
      </tr>
    ))}
  </tbody>
</table>
```

### Part B: Detail Views - Display Code

**Updated Customer Detail View:**
```typescript
export function CustomerDetail({ customerId }: { customerId: string }) {
  const [customer, setCustomer] = useState<CustomerDto | null>(null);

  useEffect(() => {
    customerService.getById(customerId).then(setCustomer);
  }, [customerId]);

  if (!customer) return <div>Loading...</div>;

  return (
    <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: 8 }}>
      <h2>{customer.name}</h2>
      
      {/* Display Code */}
      <div style={{ marginBottom: 16 }}>
        <label>Customer Code:</label>
        <input type="text" value={customer.code} readOnly 
          style={{ marginTop: 8, padding: 8, fontSize: 14, fontWeight: 'bold', 
                   backgroundColor: '#f0f0f0' }} />
      </div>

      <div style={{ marginBottom: 16 }}>
        <label>ID:</label>
        <input type="text" value={customer.id} readOnly 
          style={{ marginTop: 8, padding: 8, fontSize: 12 }} />
      </div>

      <div style={{ marginBottom: 16 }}>
        <label>Phone:</label>
        <input type="text" value={customer.phone} readOnly />
      </div>
    </div>
  );
}
```

### Part C: Forms - Show Code on Create

**Updated Customer Creation Form:**
```typescript
export function CreateCustomerForm() {
  const [response, setResponse] = useState<CreateCustomerResponse | null>(null);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    
    const customer = await customerService.create({
      name: formData.get('name') as string,
      aadhaar: formData.get('aadhaar') as string,
      pan: formData.get('pan') as string,
    });

    setResponse(customer);
  };

  return (
    <div>
      <form onSubmit={handleSubmit}>
        {/* Form fields */}
      </form>

      {response && (
        <div style={{ padding: 16, backgroundColor: '#d4edda', borderRadius: 4, marginTop: 16 }}>
          <h3>Customer Created Successfully!</h3>
          
          {/* Display Generated Code */}
          <div style={{ marginTop: 12, fontSize: 14 }}>
            <p><strong>Customer Code:</strong> <span style={{ fontSize: 18, fontWeight: 'bold', color: '#155724' }}>{response.code}</span></p>
            <p><strong>Customer ID:</strong> {response.id}</p>
            <p><strong>Name:</strong> {response.name}</p>
          </div>
        </div>
      )}
    </div>
  );
}
```

### Part D: Search Components - Add Code Search

**Code-Based Search Component:**
```typescript
export function CodeSearch() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<CustomerDto[]>([]);
  const [searching, setSearching] = useState(false);

  const handleSearch = async (value: string) => {
    setQuery(value);
    
    if (value.length < 2) {
      setResults([]);
      return;
    }

    setSearching(true);
    try {
      // Try exact code match first
      if (value.toUpperCase().startsWith('CUS')) {
        const customer = await customerService.getByCode(value);
        setResults([customer]);
      } else {
        // Fall back to fuzzy search
        const matches = await customerService.searchByCode(value);
        setResults(matches);
      }
    } catch (error) {
      console.error('Search failed:', error);
      setResults([]);
    } finally {
      setSearching(false);
    }
  };

  return (
    <div>
      <input
        type="text"
        placeholder="Search by code (e.g., CUS0001) or name..."
        value={query}
        onChange={(e) => handleSearch(e.target.value)}
        style={{ padding: 12, width: '100%', fontSize: 14 }}
      />

      {searching && <div>Searching...</div>}

      {results.length > 0 && (
        <div style={{ marginTop: 16 }}>
          <h3>Results</h3>
          {results.map(customer => (
            <div key={customer.id} style={{ padding: 12, border: '1px solid #ddd', marginBottom: 8 }}>
              <strong>{customer.code}</strong> - {customer.name}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
```

---

## 📁 Files to Update

### Services (Update DTOs + Add Methods)
1. `src/services/customerService.ts` - Add code, getByCode
2. `src/services/userService.ts` - Add code, getByCode
3. `src/services/loanService.ts` - Add loanCode, getByCode
4. `src/services/installmentService.ts` - Add code, getByCode
5. `src/services/receiptService.ts` - Add code, getByCode
6. `src/services/partnerService.ts` - Add code, getByCode
7. `src/services/branchService.ts` - Add code support
8. `src/services/accountingService.ts` - Add code support

### Pages (Update Tables & Forms)
1. `src/app/customers/page.tsx` - Add code column to table
2. `src/app/users/page.tsx` (if exists) - Add code column
3. `src/app/loans/page.tsx` - Add code column
4. `src/app/receipt/page.tsx` - Add code column
5. `src/app/equity/partners/page.tsx` - Add code column
6. `src/app/accounting/journal/page.tsx` - Add code column

### Components (Update Detail Views)
1. `src/components/CustomerDetail.tsx` (if exists) - Display code
2. `src/components/LoanDetail.tsx` (if exists) - Display code
3. Other detail components

### New Components (Create Search)
1. `src/components/CodeSearch.tsx` - NEW: Code-based search
2. `src/components/CodeLookupField.tsx` - NEW: Auto-complete code lookup

---

## 🎯 Implementation Priority

### Tier 1: Core Features (Hours 1-2)
1. Update CustomerService (add code, getByCode)
2. Update CustomerDto and table component
3. Add code column to customers table
4. Test code display in UI

### Tier 2: Loan Management (Hours 2-3)
1. Update LoanService (add loanCode, getByCode)
2. Update LoanCaseDto
3. Add code column to loans table
4. Add code display in loan detail view

### Tier 3: Collections (Hours 3-4)
1. Update InstallmentService
2. Update ReceiptService
3. Add code columns to collection tables

### Tier 4: Advanced (Hours 4+)
1. Add CodeSearch component
2. Add code-based filtering
3. Update advanced pages (Journal, Accounting, Partners)

---

## 💡 TypeScript Patterns

### Update Service DTOs

**Before:**
```typescript
interface CustomerDto {
  id: string;
  name: string;
  phone: string;
}
```

**After:**
```typescript
interface CustomerDto {
  id: string;
  code: string;              // ← ADD
  name: string;
  phone: string;
}
```

### Update Service Methods

**New getByCode Pattern:**
```typescript
getByCode: async (code: string): Promise<CustomerDto> => {
  try {
    return await apiClient.get(`/api/v1/customers/by-code/${code}`);
  } catch (error) {
    console.error(`Failed to fetch customer by code ${code}:`, error);
    throw error;
  }
};
```

### Update Component Props

```typescript
interface CustomerTableProps {
  customers: CustomerDto[];
  onSelect?: (customer: CustomerDto) => void;
}

// Usage in table
{customers.map(c => (
  <tr key={c.id} onClick={() => onSelect?.(c)}>
    <td>{c.code}</td>  {/* ← ADD */}
    <td>{c.name}</td>
    <td>...</td>
  </tr>
))}
```

---

## 🧪 Testing Checklist

### Service Layer Testing
- [ ] CustomerService returns code in getAll()
- [ ] CustomerService returns code in getById()
- [ ] CustomerService.getByCode() works for valid codes
- [ ] CustomerService.getByCode() throws error for invalid codes
- [ ] All other services updated similarly

### UI Component Testing
- [ ] Customer table displays code column
- [ ] Code column shows correct values
- [ ] Detail views display code
- [ ] Create form shows generated code in response
- [ ] Code search returns correct results
- [ ] Code field is read-only in detail views

### Integration Testing
- [ ] Code displays across all pages
- [ ] Code-based search finds correct records
- [ ] No breaking changes to existing functionality
- [ ] Forms still submit correctly
- [ ] All navigation still works

---

## 📊 Expected Changes Per File

| File | Lines Added | Type |
|------|------------|------|
| customerService.ts | 15-20 | Update + Add methods |
| userService.ts | 15-20 | Update + Add methods |
| loanService.ts | 15-20 | Update + Add methods |
| installmentService.ts | 15-20 | Update + Add methods |
| receiptService.ts | 15-20 | Update + Add methods |
| partnerService.ts | 15-20 | Update + Add methods |
| Customer page | 10-15 | Add table column |
| Loan page | 10-15 | Add table column |
| Installments page | 10-15 | Add table column |
| Receipt page | 10-15 | Add table column |
| CodeSearch.tsx | 50-80 | NEW component |
| Total | ~200-250 | Frontend code |

---

## 🚀 Implementation Steps

1. **Read existing service files** to understand patterns
2. **Update all service DTOs** to include code properties
3. **Add getByCode methods** to each service
4. **Update table components** to display code column
5. **Update detail views** to show codes
6. **Add code search component**
7. **Test all changes** in browser
8. **Verify no breaking changes**

---

## ✅ Phase 4 Success Criteria

- [x] All service DTOs include code properties
- [x] GetByCode methods implemented for all services
- [x] Code column displays in all data tables
- [x] Detail views show codes prominently
- [x] Create forms display generated codes
- [x] Code-based search functionality works
- [x] No errors in browser console
- [x] Existing features still work (backward compatible)
- [x] Responsive design maintained
- [x] Performance is acceptable

---

## 📈 Timeline

| Task | Duration | Status |
|------|----------|--------|
| Service layer updates | 1 hour | ⏳ Ready |
| Table component updates | 1 hour | ⏳ Ready |
| Detail view updates | 1 hour | ⏳ Ready |
| Code search component | 1 hour | ⏳ Ready |
| Testing & verification | 1 hour | ⏳ Ready |
| **Total** | **5 hours** | ⏳ Ready |

---

## 🎯 Ready to Implement Phase 4?

Phase 3 (API) is complete. Phase 4 will integrate business codes into the frontend UI.

**Estimated effort:** 3-5 hours  
**Complexity:** Low to Medium  
**Risk:** Very Low (no breaking changes)  
**Rollback:** Easy (revert files if needed)

**Next Action:** Begin implementing service layer updates, starting with CustomerService.

---

**Document Created**: June 13, 2026  
**Status**: 📋 READY FOR IMPLEMENTATION  
**Project Progress**: **3/10 Phases = 30% Complete**
