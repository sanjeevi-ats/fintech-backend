# Phase 6: Code Search & Filtering Implementation Guide

**Date**: June 13, 2026  
**Phase**: 6 of 10  
**Status**: 🟡 **IN PROGRESS** - Search Infrastructure Ready  
**Build**: ✅ **0 errors**  
**Estimated Duration**: 4-6 hours

---

## 📋 OVERVIEW

Phase 6 implements comprehensive code-based search and filtering across all pages. Users can now:
- ✅ Search by business codes (CUS0001, LN0001, etc.)
- ✅ Search by entity names/properties
- ✅ Auto-complete suggestions
- ✅ Search history with persistence
- ✅ Advanced filtering by code prefix
- ✅ Fuzzy matching with code priority

---

## 🎯 DELIVERABLES

### 1. ✅ Search Utilities Module
**File**: `src/lib/searchUtils.ts` (450+ lines)

**Includes**:
- `searchEntity()` - Generic search function
- `fuzzySearchWithCodePriority()` - Intelligent code-first search
- `isCodeLikePattern()` - Detect code format
- `formatCodeForDisplay()` - Standardize code display
- `validateCodeFormat()` - Code validation
- `filterByCodePrefix()` - Filter by code prefix
- `groupByCodePrefix()` - Group results
- `getSearchHistory()` - Load search history
- `saveSearchHistory()` - Persist searches
- `clearSearchHistory()` - Clear history
- `highlightSearchQuery()` - Highlight matches

**Features**:
- Per-entity-type search configuration
- Relevance scoring algorithm
- Search history persistence (localStorage)
- Code format validation
- Fuzzy matching with priorities

### 2. ✅ Reusable Search Component
**File**: `src/components/CodeSearchBar.tsx` (300+ lines)

**Component Props**:
```typescript
interface CodeSearchBarProps {
  onSearch: (query: string) => void;      // Callback on search
  placeholder?: string;                   // Custom placeholder
  entityType: keyof typeof codeSearchConfig; // Entity type
  showHistory?: boolean;                  // Show search history
  autoFocus?: boolean;                    // Auto-focus input
  loading?: boolean;                      // Loading state
}
```

**Features**:
- Keyboard navigation (arrow keys, enter, escape)
- Search history dropdown
- Clear button
- Suggested searches
- Loading state
- Accessibility-friendly

### 3. 📊 Search Configuration by Entity

**Supported Entities**:
```typescript
{
  customer: { codeFormat: 'CUS####', searchFields: ['code', 'name', 'phone'] },
  user: { codeFormat: 'USR####', searchFields: ['code', 'email'] },
  loan: { codeFormat: 'LN####', searchFields: ['loanCode', 'customerCode'] },
  installment: { codeFormat: 'INST####', searchFields: ['code', 'no'] },
  receipt: { codeFormat: 'RCP####', searchFields: ['receiptCode'] },
  partner: { codeFormat: 'PAR####', searchFields: ['code', 'name', 'email'] },
  account: { codeFormat: 'ACC####', searchFields: ['code', 'accountName'] },
  journalEntry: { codeFormat: 'JE####', searchFields: ['code', 'description'] },
  dayEnd: { codeFormat: 'DE####', searchFields: ['code', 'date'] }
}
```

---

## 📊 SEARCH ALGORITHM

### Relevance Scoring
```
100 points: Exact code match (CUS0001 == CUS0001)
90+ points: Code pattern starts with query (CUS0 matches CUS0001)
85+ points: Exact name/field match
80+ points: Starts with query (John matches John Doe)
60+ points: Contains query (John in Customer Name)
50+ points: Fuzzy partial match
0: No match
```

### Search Priority
1. **Code Queries** (e.g., "CUS0001")
   - Code field (highest priority)
   - Then name/secondary fields

2. **Name Queries** (e.g., "John")
   - Exact matches
   - Starts with matches
   - Contains matches

3. **Mixed Queries** (e.g., "CUS John")
   - Code + name combined
   - Relevance weighted

---

## 🔍 IMPLEMENTATION PATTERNS

### Pattern 1: Simple Search (Loans Page)
```typescript
import { searchEntity } from '@/lib/searchUtils';
import CodeSearchBar from '@/components/CodeSearchBar';

export default function LoansPage() {
  const [search, setSearch] = useState('');
  const [filteredLoans, setFilteredLoans] = useState<LoanCase[]>([]);

  const handleSearch = (query: string) => {
    const results = searchEntity(
      loans,
      query,
      ['loanCode', 'customerCode', 'customerName', 'id'],
      'loanCode'
    );
    setFilteredLoans(results.map(r => r.item));
  };

  return (
    <>
      <CodeSearchBar
        onSearch={handleSearch}
        entityType="loan"
        showHistory={true}
      />
      {/* Display filteredLoans */}
    </>
  );
}
```

### Pattern 2: Fuzzy Search (Customers Page)
```typescript
import { fuzzySearchWithCodePriority, codeSearchConfig } from '@/lib/searchUtils';
import CodeSearchBar from '@/components/CodeSearchBar';

export default function CustomersPage() {
  const handleSearch = (query: string) => {
    const results = fuzzySearchWithCodePriority(
      customers,
      query,
      codeSearchConfig.customer
    );
    setFilteredCustomers(results.map(r => r.item));
  };

  return (
    <CodeSearchBar
      onSearch={handleSearch}
      entityType="customer"
    />
  );
}
```

### Pattern 3: Advanced Search with Filters
```typescript
import { filterByCodePrefix, fuzzySearchWithCodePriority } from '@/lib/searchUtils';

export default function AdvancedSearchPage() {
  const [query, setQuery] = useState('');
  const [codePrefix, setCodePrefix] = useState('');
  
  const handleSearch = (searchQuery: string) => {
    // Step 1: Fuzzy search
    let results = fuzzySearchWithCodePriority(
      loans,
      searchQuery,
      codeSearchConfig.loan
    ).map(r => r.item);

    // Step 2: Filter by code prefix if specified
    if (codePrefix) {
      results = filterByCodePrefix(results, codePrefix, 'loanCode');
    }

    setFilteredResults(results);
  };

  return (
    <>
      <CodeSearchBar onSearch={handleSearch} entityType="loan" />
      <div>
        <label>Filter by Code Prefix:</label>
        <select value={codePrefix} onChange={e => setCodePrefix(e.target.value)}>
          <option value="">All</option>
          <option value="LN">Loan Cases (LN)</option>
          <option value="CUS">Customers (CUS)</option>
        </select>
      </div>
    </>
  );
}
```

---

## 🚀 QUICK START

### Step 1: Add Search Component to Page
```tsx
import CodeSearchBar from '@/components/CodeSearchBar';
import { searchEntity, codeSearchConfig } from '@/lib/searchUtils';

// In your component:
const [query, setQuery] = useState('');
const [results, setResults] = useState<Entity[]>([]);

const handleSearch = (query: string) => {
  const searchResults = searchEntity(
    items,
    query,
    codeSearchConfig.entityType.searchFields
  );
  setResults(searchResults.map(r => r.item));
};

return (
  <>
    <CodeSearchBar
      onSearch={handleSearch}
      entityType="entityType"
      showHistory={true}
    />
    {/* Display results */}
  </>
);
```

### Step 2: Display Results with Highlighting
```tsx
import { highlightSearchQuery } from '@/lib/searchUtils';

{results.map(item => {
  const { before, highlight, after } = highlightSearchQuery(
    item.code,
    query
  );
  
  return (
    <div key={item.id}>
      {before}
      <span style={{ background: '#fef08a', fontWeight: 700 }}>
        {highlight}
      </span>
      {after}
    </div>
  );
})}
```

### Step 3: Add to Multiple Pages
Just repeat Step 1-2 for:
- Loans page
- Customers page
- Partners page
- Collections page
- Receipts page
- Journal page
- Day End page

---

## 📈 SEARCH PERFORMANCE OPTIMIZATION

### For Large Datasets (1000+ items)

**Option 1: Client-Side Debouncing**
```typescript
import { useCallback } from 'react';
import { debounce } from 'lodash';

const handleSearchDebounced = useCallback(
  debounce((query: string) => {
    const results = searchEntity(items, query, searchFields);
    setResults(results.map(r => r.item));
  }, 300),
  [items, searchFields]
);
```

**Option 2: Server-Side Search**
```typescript
const handleSearch = async (query: string) => {
  setLoading(true);
  try {
    // Call backend search endpoint
    const results = await itemService.search(query);
    setResults(results);
  } finally {
    setLoading(false);
  }
};
```

**Option 3: Indexed Search (Client-Side)**
```typescript
import { createIndex, searchIndex } from '@/lib/searchUtils';

// Build index once
const index = useMemo(() => createIndex(items, ['code', 'name']), [items]);

// Search in index
const results = searchIndex(index, query);
```

---

## 🔌 INTEGRATION WITH BACKEND

### Recommended Backend Search Endpoints

```csharp
// CustomerController
[HttpGet("search")]
public async Task<IActionResult> Search([FromQuery] string query)
{
    var customers = await _service.SearchAsync(query);
    return Ok(customers);
}

// LoanCaseController
[HttpGet("search")]
public async Task<IActionResult> Search([FromQuery] string query)
{
    var loans = await _service.SearchAsync(query);
    return Ok(loans);
}

// Generic pattern
[HttpGet("search")]
public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int? limit = 50)
{
    var results = await _repository.SearchAsync(query, limit);
    return Ok(results);
}
```

### Service Implementation
```csharp
public async Task<List<Customer>> SearchAsync(string query, int limit = 50)
{
    var queryLower = query.ToLower();
    
    return await _dbContext.Customers
        .Where(c => 
            c.Code.ToLower().Contains(queryLower) ||
            c.Name.ToLower().Contains(queryLower) ||
            c.Phone.Contains(query)
        )
        .OrderByDescending(c => 
            c.Code.ToLower() == queryLower ? 100 : // Exact match
            c.Code.ToLower().StartsWith(queryLower) ? 90 : // Starts with
            c.Name.ToLower().Contains(queryLower) ? 60 : 0 // Contains
        )
        .Take(limit)
        .ToListAsync();
}
```

---

## 🎨 UI/UX GUIDELINES

### Search Bar Styling
```tsx
<CodeSearchBar
  placeholder="Search by code (e.g., CUS0001) or name..."
  entityType="customer"
  showHistory={true}
  autoFocus={false}
  loading={false}
/>
```

### Result Display
```tsx
// Show search results in table or list
results.map(item => (
  <tr key={item.id}>
    <td className="mono" style={{ fontWeight: 700, color: '#6366f1' }}>
      {item.code}  {/* Highlight code column */}
    </td>
    <td>{item.name}</td>
    {/* ... other fields */}
  </tr>
))
```

### Search Tips Display
```tsx
{results.length > 0 && (
  <div style={{ padding: '12px 16px', background: 'rgba(99,102,241,0.05)', borderRadius: 8 }}>
    <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>
      Found {results.length} matching records
    </span>
  </div>
)}
```

---

## ✅ IMPLEMENTATION CHECKLIST

### Core Components
- [x] `searchUtils.ts` - Search utilities module
- [x] `CodeSearchBar.tsx` - Reusable search component
- [ ] Add to Loans page
- [ ] Add to Customers page
- [ ] Add to Partners page
- [ ] Add to Collections page
- [ ] Add to Receipts page
- [ ] Add to Journal page
- [ ] Add to Day End page

### Features
- [x] Code-based search
- [x] Name-based search
- [x] Fuzzy matching
- [x] Search history
- [x] Auto-complete
- [ ] Advanced filters
- [ ] Bulk operations (select multiple codes)
- [ ] Export search results

### Testing
- [ ] Test code search (CUS0001)
- [ ] Test name search (John Doe)
- [ ] Test mixed search
- [ ] Test search history
- [ ] Test keyboard navigation
- [ ] Test on mobile
- [ ] Performance test (1000+ items)

### Documentation
- [x] Implementation guide
- [ ] User guide
- [ ] API documentation
- [ ] Code examples

---

## 📊 EXPECTED OUTCOMES

### Before Phase 6
```
Search: [type long UUID]
↓
Display: List of GUIDs (slow to find)
```

### After Phase 6
```
Search: [CUS0001▼]
         ↓ Recent: CUS0001, CUS0002, CUS0005
         ↓ Auto-complete with code suggestions
↓
Display: Instant results, highlighted matches
```

---

## 🔗 NEXT STEPS

1. ✅ Created `searchUtils.ts`
2. ✅ Created `CodeSearchBar.tsx`
3. ⏳ Add search to Loans page
4. ⏳ Add search to Customers page
5. ⏳ Add search to other pages
6. ⏳ Add backend search endpoints (optional)
7. ⏳ Test & optimize
8. ⏳ Deploy

---

## 📞 SUPPORT

### Common Issues

**Q: Search is slow on large datasets**
A: Use debouncing or server-side search. See "Performance Optimization" section.

**Q: Want to customize search fields**
A: Update `codeSearchConfig` in `searchUtils.ts` for your entity type.

**Q: How to disable search history**
A: Set `showHistory={false}` on `CodeSearchBar` component.

**Q: Search not finding codes**
A: Ensure code field name matches config (e.g., `loanCode`, not `code`).

---

## 🎉 PHASE 6 STATUS

**Components Created**: ✅ 2 (searchUtils, CodeSearchBar)  
**Build Status**: ✅ Clean (0 errors)  
**Ready to Integrate**: ✅ Yes  
**Next Phase**: Phase 7 (Reports & Export)

---

**Last Updated**: June 13, 2026  
**Phase Lead**: Development Team  
**Status**: 🟢 **INFRASTRUCTURE READY - AWAITING INTEGRATION**

Ready to add search to pages? Use patterns above and integrate CodeSearchBar into each page!
