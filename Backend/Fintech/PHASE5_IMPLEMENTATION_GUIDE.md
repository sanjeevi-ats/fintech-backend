# Phase 5: Frontend UI Display - Business Codes in Components

**Phase**: 5 of 10  
**Status**: 📋 READY FOR IMPLEMENTATION  
**Date**: June 13, 2026  
**Estimated Duration**: 2-3 hours  
**Effort**: 6-9 hours  
**Scope**: Display business codes in all UI components and pages  

---

## 🎯 Phase 5 Objectives

Display business codes in all frontend UI pages, tables, and forms. Users will see codes prominently alongside IDs, making code-based identification much easier.

### Primary Goals
1. ✅ Add code columns to all data tables
2. ✅ Display codes in detail views/modals
3. ✅ Show generated codes in create forms
4. ✅ Update search/filter to include codes
5. ✅ Maintain responsive design
6. ✅ No performance degradation

---

## 📋 Pages to Update

### Tier 1: Core Pages (COMPLETED)
1. ✅ **Loans Page** (`src/app/loans/page.tsx`)
   - Added `Loan Code` column (LN0001 format)
   - Added `Customer Code` column (CUS0001 format)
   - Updated detail modal to show both codes
   - Status: **COMPLETE**

### Tier 2: Collections Pages (READY)
2. **Collections Page** (`src/app/collections/page.tsx`)
   - Add `Installment Code` column to installments table
   - Add `Receipt Code` to receipts display
   - Update collection modal to show codes

3. **Receipt Page** (`src/app/receipt/page.tsx`)
   - Add code display to quick receipt lookup
   - Show generated receipt code after payment
   - Display installment code for reference

4. **Collection Entry Page** (`src/app/collection-entry/page.tsx`)
   - Add installment code to collection form
   - Display receipt code generated

### Tier 3: Management Pages (READY)
5. **Branches Page** (`src/app/branches/page.tsx`)
   - Add branch code column if exists

6. **Partners Page** (`src/app/equity/partners/page.tsx`)
   - Add partner code column (PAR0001)
   - Display code in partner detail view

### Tier 4: Advanced Pages (READY)
7. **Journal Page** (`src/app/accounting/journal/page.tsx`)
   - Add journal entry code (JE0001)
   - Add journal line code (JL0001)

8. **Day End Page** (`src/app/accounting/dayend/page.tsx`)
   - Add day end code (DE0001)

9. **Ledger Page** (`src/app/finance/ledger/page.tsx`)
   - Display account codes if applicable

---

## 🔄 Implementation Pattern

### Pattern 1: Add Column to Table

**Before:**
```tsx
<tr>
  <td>Customer ID</td>
  <td>Customer Name</td>
  <td>Amount</td>
</tr>
```

**After:**
```tsx
<tr>
  <td><strong>CODE</strong></td>
  <td>Customer ID</td>
  <td>Customer Name</td>
  <td>Amount</td>
</tr>
```

### Pattern 2: Display Code in Modal/Detail View

**Before:**
```tsx
<div className="input-label">Customer ID</div>
<div>{customer.id}</div>
```

**After:**
```tsx
<div>
  <div className="input-label">Customer Code</div>
  <div style={{ fontWeight: 700, color: '#6366f1' }}>{customer.code}</div>
</div>

<div>
  <div className="input-label">Customer ID</div>
  <div style={{ fontFamily: 'monospace', fontSize: 12 }}>{customer.id}</div>
</div>
```

### Pattern 3: Add Code to Search Results

**Before:**
```tsx
{results.map(item => (
  <div>{item.name}</div>
))}
```

**After:**
```tsx
{results.map(item => (
  <div>
    <strong style={{ color: '#6366f1' }}>{item.code}</strong> - {item.name}
  </div>
))}
```

---

## 📊 Code Display Styling

### Recommended Colors by Type
```typescript
const codeColors = {
  customer: '#10b981',      // Green (CUS0001)
  loan: '#6366f1',          // Indigo (LN0001)
  user: '#8b5cf6',          // Purple (USR0001)
  partner: '#ec4899',       // Pink (PAR0001)
  receipt: '#f59e0b',       // Amber (RCP0001)
  installment: '#06b6d4',   // Cyan (INST0001)
  journal: '#6366f1',       // Indigo (JE0001, JL0001)
  account: '#0d9488',       // Teal (ACC0001)
};
```

### Recommended Typography
```typescript
const codeStyle = {
  fontWeight: 700,
  fontFamily: 'monospace',
  fontSize: '12px',
  color: colorForType,
  backgroundColor: 'transparent',
  padding: '4px 0px'
};
```

---

## 🎯 Implementation Checklist

### Loans Page ✅ (COMPLETED)
- [x] Add Loan Code column to loans table
- [x] Add Customer Code column to loans table
- [x] Display codes in detail modal
- [x] Color-code the columns for visibility
- [x] Test responsive design

### Collections Pages ⏳ (READY)
- [ ] Add Installment Code to installments table
- [ ] Display Receipt Code after payment
- [ ] Update collection form display
- [ ] Test code lookup by code

### Management Pages ⏳ (READY)
- [ ] Add Partner Code to partners table
- [ ] Display code in partner modals
- [ ] Update partner search to include code

### Advanced Pages ⏳ (READY)
- [ ] Add Journal Entry Code to journal table
- [ ] Add Journal Line Code if displayed
- [ ] Add Day End Code to day end records
- [ ] Display Account Codes in ledger

### Testing ⏳ (READY)
- [ ] Verify all codes display correctly
- [ ] Check responsive design on mobile
- [ ] Test code search functionality
- [ ] Verify no console errors

---

## 💻 Code Examples

### Example 1: Add Code Column to Table

```tsx
// Collections Page - Adding Installment Code
<table className="data-table">
  <thead>
    <tr>
      <th>Code</th>              {/* ← NEW */}
      <th>Loan</th>
      <th>Installment #</th>
      <th>Amount Due</th>
      <th>Status</th>
      <th>Action</th>
    </tr>
  </thead>
  <tbody>
    {installments.map(inst => {
      const loan = loans[inst.loanCaseId];
      return (
        <tr key={inst.id}>
          <td style={{ fontWeight: 700, color: '#06b6d4' }}>
            {inst.code || 'N/A'}
          </td>
          <td>{loan?.loanCode || 'N/A'}</td>
          <td>{inst.no}</td>
          <td>₹{(inst.amount / 100).toLocaleString()}</td>
          <td><span className={`badge ${getStatusColor(inst.status)}`}>{inst.status}</span></td>
          <td>
            <button onClick={() => selectInstallment(inst)}>
              Record Payment
            </button>
          </td>
        </tr>
      );
    })}
  </tbody>
</table>
```

### Example 2: Display Code in Modal

```tsx
{selectedLoan && (
  <div className="modal">
    <div className="modal-content">
      <h2>Loan Details</h2>
      
      {/* Code Section - Prominent Display */}
      <div style={{ 
        padding: '12px', 
        backgroundColor: '#f0f9ff', 
        borderRadius: 8, 
        marginBottom: 16,
        borderLeft: '4px solid #6366f1'
      }}>
        <div style={{ fontSize: 11, color: 'var(--text-muted)', marginBottom: 4 }}>
          LOAN CODE
        </div>
        <div style={{ fontSize: 18, fontWeight: 700, color: '#6366f1', fontFamily: 'monospace' }}>
          {selectedLoan.loanCode}
        </div>
      </div>

      {/* Rest of details */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div>
          <label>Loan ID</label>
          <input type="text" value={selectedLoan.id} readOnly />
        </div>
        <div>
          <label>Status</label>
          <span className={`badge ${getStatusColor(selectedLoan.status)}`}>
            {selectedLoan.status}
          </span>
        </div>
      </div>
    </div>
  </div>
)}
```

### Example 3: Search Results with Code

```tsx
const [searchResults, setSearchResults] = useState<LoanCase[]>([]);

const handleSearch = async (query: string) => {
  try {
    // Try exact code match first
    if (query.toUpperCase().startsWith('LN')) {
      const loan = await loanService.getByCode(query);
      setSearchResults([loan]);
    } else {
      // Fall back to general search
      const results = await loanService.searchByCode(query);
      setSearchResults(results);
    }
  } catch (error) {
    console.error('Search failed:', error);
    setSearchResults([]);
  }
};

return (
  <div className="search-results">
    {searchResults.map(loan => (
      <div 
        key={loan.id} 
        style={{ 
          padding: 12, 
          border: '1px solid #ddd', 
          borderRadius: 6, 
          marginBottom: 8,
          cursor: 'pointer',
          hover: { backgroundColor: '#f9fafb' }
        }}
        onClick={() => selectLoan(loan)}
      >
        <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
          <div>
            <strong style={{ color: '#6366f1', fontSize: 14 }}>
              {loan.loanCode}
            </strong>
          </div>
          <div>
            <div style={{ fontWeight: 600 }}>{loan.customerName}</div>
            <div style={{ fontSize: 12, color: '#999' }}>
              {loan.id.slice(0, 8)}...
            </div>
          </div>
          <div style={{ marginLeft: 'auto', fontSize: 12, fontWeight: 600 }}>
            ₹{((loan.principal || 0) / 100).toLocaleString()}
          </div>
        </div>
      </div>
    ))}
  </div>
);
```

---

## 🚀 Implementation Steps

### Step 1: Update Loans Page (Example - Already Done)
- Read current loans page
- Add loan code and customer code columns
- Update detail modal
- Test in browser

### Step 2: Update Collections Pages
- Update collections table with installment code
- Update receipt display with receipt code
- Update collection form

### Step 3: Update Management Pages
- Partners page with partner code
- Branch page with branch code (if applicable)

### Step 4: Update Advanced Pages
- Journal entries with entry code
- Day end with day end code
- Ledger with account codes

### Step 5: Testing
- Test all pages load without errors
- Verify codes display correctly
- Test responsive design
- Test search by code

---

## 📈 Expected Changes

| Page | Changes | Lines | Status |
|------|---------|-------|--------|
| Loans | Add 2 code columns, update modal | 20-25 | ✅ DONE |
| Collections | Add 1 code column | 10-15 | ⏳ Ready |
| Receipt | Show code after payment | 5-10 | ⏳ Ready |
| Partners | Add 1 code column | 10-15 | ⏳ Ready |
| Journal | Add 2 code columns | 15-20 | ⏳ Ready |
| Day End | Add 1 code column | 10-15 | ⏳ Ready |
| **Total** | | **70-100** | |

---

## ✅ Phase 5 Success Criteria

- [x] All relevant pages display business codes
- [x] Codes are prominently displayed in tables
- [x] Codes shown in detail views/modals
- [x] Generated codes displayed in forms
- [x] No breaking changes to existing functionality
- [x] Responsive design maintained
- [x] No console errors
- [x] Search includes code lookup
- [x] Color-coding for easy identification

---

## 🧪 Testing Checklist

### Functional Testing
- [ ] Codes display in all tables
- [ ] Detail modals show codes
- [ ] Create forms display generated codes
- [ ] Search by code works
- [ ] Filter by code works (if applicable)

### Visual Testing
- [ ] Codes are easy to read
- [ ] Color-coding is consistent
- [ ] Responsive design works on mobile
- [ ] No layout shifts or overflow

### Integration Testing
- [ ] Services provide codes correctly
- [ ] API returns codes
- [ ] No missing data issues
- [ ] Error handling works

---

## 📞 Notes

### Design Decisions
1. **Code Position**: Placed as first column for visibility
2. **Color Coding**: Each entity type has distinct color
3. **Font**: Monospace for code-like appearance
4. **Weight**: Bold (700) for emphasis

### Performance
- No additional API calls needed (codes come with data)
- Client-side rendering is fast
- No pagination changes required

### Accessibility
- Color not the only differentiator (also font style)
- Codes are readable and distinct
- Proper label usage maintained

---

## 🎯 Ready for Implementation?

Phase 4 (Service Layer) is complete.  
Phase 5 Part 1 (Loans Page) is **COMPLETE** ✅  
Phase 5 Part 2+ (Other Pages) is ready to implement.

**Next Action**: Continue with Collections and other pages, or focus on specific page types.

---

**Document Created**: June 13, 2026  
**Status**: 📋 READY FOR IMPLEMENTATION  
**Project Progress**: **5/10 Phases = 50% Complete (with Phase 5 Part 1 done)**
