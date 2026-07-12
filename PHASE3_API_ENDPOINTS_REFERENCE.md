# Phase 3: Complete API GetByCode Endpoints Reference

## Overview
All 11 controllers now expose GetByCode endpoints for querying entities by their business codes. This document provides complete API endpoint specifications.

---

## 1. Customers API
### Get Customer by Code
**Endpoint**: `GET /api/v1/customers/by-code/{code}`
**Authorization**: Bearer token required (VIEW_USERS permission)
**Parameters**:
- `code` (string, path): Customer business code (e.g., "CUS00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "CUS00001",
  "name": "John Doe",
  "aadhaar": "XXXX XXXX XXXX 1234",
  "pan": "AAAP1234A"
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Customer with code {code} not found" }`
- `500 Internal Server Error`: Standard error response with exception details

---

## 2. Users API
### Get User by Code
**Endpoint**: `GET /api/v1/users/by-code/{code}`
**Authorization**: Bearer token required (VIEW_USERS permission)
**Parameters**:
- `code` (string, path): User business code (e.g., "USR00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "USR00001",
  "name": "Agent Name",
  "email": "agent@company.com",
  "role": "agent",
  "isActive": true
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "User with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## 3. Branch API
### Get Branch by Code
**Endpoint**: `GET /api/v1/branch/by-code/{code}`
**Authorization**: None (public endpoint)
**Parameters**:
- `code` (string, path): Branch business code (e.g., "BR0001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "branchCode": "BR0001",
  "name": "Main Branch",
  "city": "New Delhi",
  "address": "123 Main Street",
  "phoneNumber": "+91-11-1234-5678",
  "managerEmail": "manager@branch.com",
  "settings": "{...}"
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Branch with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## 4. Loan Cases API
### Get Loan by Code
**Endpoint**: `GET /api/v1/loancases/by-code/{code}`
**Authorization**: Bearer token required
**Parameters**:
- `code` (string, path): Loan business code (e.g., "LN00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "loanCode": "LN00001",
  "customerCode": "CUS00001",
  "customerId": "550e8400-e29b-41d4-a716-446655440001",
  "customerName": "John Doe",
  "principal": 100000,
  "interestAmount": 20000,
  "totalReceivable": 120000,
  "processingFees": 5000,
  "status": "Active"
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Loan with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## 5. Product API
### Get Product by Code
**Endpoint**: `GET /api/v1/product/by-code/{code}`
**Authorization**: Role required (super_admin or branch_manager)
**Parameters**:
- `code` (string, path): Product business code (e.g., "PRD00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "branchId": "550e8400-e29b-41d4-a716-446655440001",
  "name": "Short Term Loan",
  "code": "PRD00001",
  "interestRate": 18.5,
  "defaultTenureMonths": 12,
  "isActive": true
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Product with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## 6. Installments API
### Get Installment by Code
**Endpoint**: `GET /api/v1/installments/by-code/{code}`
**Authorization**: Bearer token required
**Parameters**:
- `code` (string, path): Installment business code (e.g., "INS00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "INS00001",
  "no": 1,
  "dueDate": "2025-02-15T00:00:00Z",
  "amount": 12000,
  "status": "Due"
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Installment with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## 7. Receipts API
### Get Receipt by Code
**Endpoint**: `GET /api/v1/receipts/by-code/{code}`
**Authorization**: Bearer token required
**Parameters**:
- `code` (string, path): Receipt business code (e.g., "RCP00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "RCP00001",
  "amountPaid": 12000,
  "mode": "cash",
  "utrRef": "UTR123456",
  "capturedAt": "2025-01-15T10:30:00Z"
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Receipt with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## 8. Partners API
### Get Partner by Code
**Endpoint**: `GET /api/v1/partners/by-code/{code}`
**Authorization**: Bearer token required
**Parameters**:
- `code` (string, path): Partner business code (e.g., "PAR00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "PAR00001",
  "userId": "550e8400-e29b-41d4-a716-446655440001",
  "equityPct": 25.5,
  "name": "Partner Name",
  "email": "partner@company.com",
  "phone": "+91-9876-543210",
  "isActive": true
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Partner with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## 9. Capital Accounts API
### Get Capital Account by Code
**Endpoint**: `GET /api/v1/capitalaccounts/by-code/{code}`
**Authorization**: Bearer token required
**Parameters**:
- `code` (string, path): Capital account business code (e.g., "CAP00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "partnerId": "550e8400-e29b-41d4-a716-446655440001",
  "capitalAccountCode": "CAP00001",
  "totalInvestment": 1000000,
  "totalProfit": 250000,
  "totalWithdrawal": 100000,
  "currentBalance": 1150000
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Capital account with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## 10. Audit API
### Get Audit Log by Code
**Endpoint**: `GET /api/v1/audit/by-code/{code}`
**Authorization**: Role required (super_admin or accountant)
**Parameters**:
- `code` (string, path): Audit log business code (e.g., "AUD00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "auditLogCode": "AUD00001",
  "entityName": "LoanCase",
  "recordId": "550e8400-e29b-41d4-a716-446655440001",
  "action": "Created",
  "changes": "{...}",
  "userId": "550e8400-e29b-41d4-a716-446655440002",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Audit log with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## 11. Day End API
### Get Day End Record by Code
**Endpoint**: `GET /api/dayend/by-code/{code}`
**Authorization**: None (internal endpoint)
**Parameters**:
- `code` (string, path): Day end business code (e.g., "DAY00001")

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "dayEndCode": "DAY00001",
  "date": "2025-01-15T00:00:00Z",
  "expectedCash": 500000,
  "verifiedCash": 500000,
  "variance": 0,
  "status": "Closed",
  "closedBy": "manager@branch.com",
  "closedAt": "2025-01-15T18:30:00Z"
}
```

**Error Responses**:
- `404 Not Found`: `{ "message": "Day end record with code {code} not found" }`
- `500 Internal Server Error`: Standard error response

---

## Special Cases

### Journal API (Already Had GetByCode)
**Note**: JournalController already had GetByCode endpoints before Phase 3. No changes made.

#### Get Journal Entry by Code
**Endpoint**: `GET /api/journal/entries/by-code/{code}`
**Parameters**: `code` = Journal entry code

#### Get Journal Line by Code
**Endpoint**: `GET /api/journal/lines/by-code/{code}`
**Parameters**: `code` = Journal line code

---

## Common Error Response Format

### 500 Internal Server Error
```json
{
  "success": false,
  "message": "An unexpected error occurred while retrieving the {entity} by code.",
  "error": "Exception message details"
}
```

### 404 Not Found
```json
{
  "message": "{Entity} with code {code} not found"
}
```

---

## Authentication & Authorization

### Token Format
All authenticated endpoints require:
```
Authorization: Bearer {jwt_token}
```

### Permissions Required
- **VIEW_USERS**: CustomersController, UsersController
- **super_admin or branch_manager**: ProductController, ReportController (some endpoints)
- **super_admin or accountant**: AuditController, ReportController (some endpoints)

---

## Code Format Specifications

| Entity | Code Format | Example | Length | Characters |
|--------|------------|---------|--------|-----------|
| Customer | CUS + Sequential | CUS00001 | 8 | Alphanumeric |
| User | USR + Sequential | USR00001 | 8 | Alphanumeric |
| Branch | BR + Sequential | BR0001 | 6 | Alphanumeric |
| Loan | LN + Sequential | LN00001 | 8 | Alphanumeric |
| Product | PRD + Sequential | PRD00001 | 8 | Alphanumeric |
| Installment | INS + Sequential | INS00001 | 8 | Alphanumeric |
| Receipt | RCP + Sequential | RCP00001 | 8 | Alphanumeric |
| Partner | PAR + Sequential | PAR00001 | 8 | Alphanumeric |
| Capital Account | CAP + Sequential | CAP00001 | 8 | Alphanumeric |
| Audit Log | AUD + Sequential | AUD00001 | 8 | Alphanumeric |
| Day End | DAY + Sequential | DAY00001 | 8 | Alphanumeric |

---

## Usage Examples

### cURL - Get Customer by Code
```bash
curl -X GET "http://localhost:5000/api/v1/customers/by-code/CUS00001" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json"
```

### JavaScript/Fetch - Get Loan by Code
```javascript
fetch('/api/v1/loancases/by-code/LN00001', {
  method: 'GET',
  headers: {
    'Authorization': 'Bearer ' + token,
    'Content-Type': 'application/json'
  }
})
.then(response => response.json())
.then(data => console.log(data));
```

### PowerShell - Get User by Code
```powershell
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}
$response = Invoke-WebRequest -Uri "http://localhost:5000/api/v1/users/by-code/USR00001" `
    -Headers $headers -Method Get
$response.Content | ConvertFrom-Json
```

---

## Response Time Expectations
- GetByCode endpoints: < 100ms (indexed database queries)
- Typical API response: ~50-150ms including serialization

---

## Database Indexes
All code columns are indexed for optimal performance:
- `Customer.CustomerCode` - Unique Index
- `User.UserCode` - Unique Index
- `Branch.BranchCode` - Unique Index
- `LoanCase.LoanCode` - Unique Index
- `LoanProduct.Code` - Unique Index
- `Installment.InstallmentCode` - Unique Index
- `Receipt.ReceiptCode` - Unique Index
- `Partner.PartnerCode` - Unique Index
- `CapitalAccount.CapitalAccountCode` - Index
- `AuditLog.AuditLogCode` - Index
- `Account.Code` - Index

---

## Rate Limiting
No rate limiting currently implemented on GetByCode endpoints. 
Recommended: Implement rate limiting for production deployment.

---

## Pagination
GetByCode endpoints return single entities. For paginated lists, use:
- `GET /api/v1/{entity}` - Returns all entities
- Add pagination parameters as needed

---

## Version Information
- **API Version**: v1
- **Phase**: 3 (API & DTO Implementation)
- **Status**: Complete
- **Build**: Succeeded (0 errors, 4 pre-existing warnings)

---

## Related Documentation
- `PHASE3_API_DTO_IMPLEMENTATION_COMPLETE.md` - Implementation summary
- `ID_REFACTORING_SUMMARY.md` - Overall refactoring plan
- Domain Models - Core/Domain/*.cs files
- Services - Application/Services/*.cs files
