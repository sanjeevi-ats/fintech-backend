# Verify and Insert Missing Seed Data
# This script checks if all seed data from seed_data_v2.sql is present in the database

param(
    [string]$Server = "localhost",
    [string]$Port = "5432",
    [string]$Database = "Fintech",
    [string]$Username = "postgres",
    [string]$Password = "Test123"
)

# Results tracking
$script:Results = @{
    AlreadyPresent = @()
    Inserted = @()
    Errors = @()
}

function Execute-PostgresQuery {
    param(
        [string]$Query,
        [switch]$NonQuery
    )
    
    $env:PGPASSWORD = $Password
    
    if ($NonQuery) {
        $output = & psql -h $Server -p $Port -U $Username -d $Database -c $Query 2>&1
        return $output
    } else {
        $output = & psql -h $Server -p $Port -U $Username -d $Database -t -A -c $Query 2>&1
        return $output
    }
}

function Check-RecordExists {
    param(
        [string]$Table,
        [string]$IdColumn,
        [string]$IdValue
    )
    
    $query = "SELECT COUNT(*) FROM $Table WHERE $IdColumn = '$IdValue';"
    $result = Execute-PostgresQuery -Query $query
    
    return ($result -match "^\d+$" -and [int]$result -gt 0)
}

function Insert-Record {
    param(
        [string]$Table,
        [string]$InsertQuery,
        [string]$RecordDescription
    )
    
    try {
        $result = Execute-PostgresQuery -Query $InsertQuery -NonQuery
        
        if ($LASTEXITCODE -eq 0) {
            $script:Results.Inserted += "$Table - $RecordDescription"
            Write-Host "[INSERTED] $Table - $RecordDescription" -ForegroundColor Green
            return $true
        } else {
            $script:Results.Errors += "$Table - $RecordDescription : $result"
            Write-Host "[ERROR] $Table - $RecordDescription : $result" -ForegroundColor Red
            return $false
        }
    } catch {
        $script:Results.Errors += "$Table - $RecordDescription : $_"
        Write-Host "[ERROR] $Table - $RecordDescription : $_" -ForegroundColor Red
        return $false
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SEED DATA VERIFICATION STARTED" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. BRANCHES
Write-Host "Checking BRANCHES..." -ForegroundColor Yellow
$branchId = "11111111-1111-1111-1111-111111111111"
if (Check-RecordExists -Table "branches" -IdColumn "id" -IdValue $branchId) {
    $script:Results.AlreadyPresent += "branches - FinVeda Main Office"
    Write-Host "[EXISTS] branches - FinVeda Main Office" -ForegroundColor Gray
} else {
    $query = @"
ALTER TABLE branches DISABLE TRIGGER ALL;
INSERT INTO branches (id, name, city, is_active, settings_json) VALUES 
('$branchId', 'FinVeda Main Office', 'Mumbai', true, '{}');
ALTER TABLE branches ENABLE TRIGGER ALL;
"@
    Insert-Record -Table "branches" -InsertQuery $query -RecordDescription "FinVeda Main Office"
}

# 2. USERS
Write-Host "Checking USERS..." -ForegroundColor Yellow
$users = @(
    @{Id='22222222-2222-2222-2222-222222222221'; Name='Super Admin User'; Email='super_admin@finveda.com'; Role='super_admin'},
    @{Id='22222222-2222-2222-2222-222222222222'; Name='Branch Manager User'; Email='branch_manager@finveda.com'; Role='branch_manager'},
    @{Id='22222222-2222-2222-2222-222222222223'; Name='Partner User'; Email='partner@finveda.com'; Role='partner'},
    @{Id='22222222-2222-2222-2222-222222222224'; Name='Accountant User'; Email='accountant@finveda.com'; Role='accountant'},
    @{Id='22222222-2222-2222-2222-222222222225'; Name='Collection Officer User'; Email='collection_officer@finveda.com'; Role='collection_officer'},
    @{Id='22222222-2222-2222-2222-222222222226'; Name='Recovery Specialist User'; Email='recovery_specialist@finveda.com'; Role='recovery_specialist'},
    @{Id='22222222-2222-2222-2222-222222222227'; Name='Loan Officer User'; Email='loan_officer@finveda.com'; Role='loan_officer'},
    @{Id='22222222-2222-2222-2222-222222222228'; Name='Customer User'; Email='customer@finveda.com'; Role='customer'},
    @{Id='22222222-2222-2222-2222-222222222229'; Name='Agent User'; Email='agent@finveda.com'; Role='agent'}
)

foreach ($user in $users) {
    if (Check-RecordExists -Table "users" -IdColumn "id" -IdValue $user.Id) {
        $script:Results.AlreadyPresent += "users - $($user.Name)"
        Write-Host "[EXISTS] users - $($user.Name)" -ForegroundColor Gray
    } else {
        $query = "INSERT INTO users (id, branch_id, name, email, password_hash, role, is_active, totp_enabled, refresh_token) VALUES ('$($user.Id)', '$branchId', '$($user.Name)', '$($user.Email)', 'Admin@123', '$($user.Role)', true, false, '');"
        Insert-Record -Table "users" -InsertQuery $query -RecordDescription $user.Name
    }
}

# 3. ACCOUNTS
Write-Host "Checking ACCOUNTS..." -ForegroundColor Yellow
$accounts = @(
    @{Id='33333333-3333-3333-3333-333333333331'; Name='Cash Office'},
    @{Id='33333333-3333-3333-3333-333333333332'; Name='HDFC Bank Account'},
    @{Id='33333333-3333-3333-3333-333333333333'; Name='Loan Portfolio'},
    @{Id='33333333-3333-3333-3333-333333333334'; Name='Interest Revenue'},
    @{Id='33333333-3333-3333-3333-333333333335'; Name='Equity Capital'}
)

foreach ($account in $accounts) {
    if (Check-RecordExists -Table "accounts" -IdColumn "id" -IdValue $account.Id) {
        $script:Results.AlreadyPresent += "accounts - $($account.Name)"
        Write-Host "[EXISTS] accounts - $($account.Name)" -ForegroundColor Gray
    } else {
        $query = "INSERT INTO accounts (id, branch_id, name) VALUES ('$($account.Id)', '$branchId', '$($account.Name)');"
        Insert-Record -Table "accounts" -InsertQuery $query -RecordDescription $account.Name
    }
}

# 4. CUSTOMERS
Write-Host "Checking CUSTOMERS..." -ForegroundColor Yellow
$customers = @(
    @{Id='44444444-4444-4444-4444-444444444441'; Name='Amit Sharma'; Phone='9876543210'; Aadhaar='ENCRYPTED_AD123'; Pan='ENCRYPTED_PAN123'},
    @{Id='44444444-4444-4444-4444-444444444442'; Name='Meera Patel'; Phone='9876543211'; Aadhaar='ENCRYPTED_AD456'; Pan='ENCRYPTED_PAN456'}
)

foreach ($customer in $customers) {
    if (Check-RecordExists -Table "customers" -IdColumn "id" -IdValue $customer.Id) {
        $script:Results.AlreadyPresent += "customers - $($customer.Name)"
        Write-Host "[EXISTS] customers - $($customer.Name)" -ForegroundColor Gray
    } else {
        $query = "INSERT INTO customers (id, branch_id, name, phone, is_active, aadhaar_encrypted, pan_encrypted) VALUES ('$($customer.Id)', '$branchId', '$($customer.Name)', '$($customer.Phone)', true, '$($customer.Aadhaar)', '$($customer.Pan)');"
        Insert-Record -Table "customers" -InsertQuery $query -RecordDescription $customer.Name
    }
}

# 5. LOAN PRODUCTS
Write-Host "Checking LOAN PRODUCTS..." -ForegroundColor Yellow
$loanProducts = @(
    @{Id='55555555-5555-5555-5555-555555555551'; Name='Personal Loan'; Frequency='monthly'; MinAmount=1000000; MaxAmount=50000000; InterestRate=12.0},
    @{Id='55555555-5555-5555-5555-555555555552'; Name='Biz Loan'; Frequency='weekly'; MinAmount=5000000; MaxAmount=250000000; InterestRate=18.0}
)

foreach ($product in $loanProducts) {
    if (Check-RecordExists -Table "loan_products" -IdColumn "id" -IdValue $product.Id) {
        $script:Results.AlreadyPresent += "loan_products - $($product.Name)"
        Write-Host "[EXISTS] loan_products - $($product.Name)" -ForegroundColor Gray
    } else {
        $query = "INSERT INTO loan_products (id, branch_id, name, frequency, min_amount, max_amount, interest_rate_pct, is_active) VALUES ('$($product.Id)', '$branchId', '$($product.Name)', '$($product.Frequency)', $($product.MinAmount), $($product.MaxAmount), $($product.InterestRate), true);"
        Insert-Record -Table "loan_products" -InsertQuery $query -RecordDescription $product.Name
    }
}

# 6. PARTNERS
Write-Host "Checking PARTNERS..." -ForegroundColor Yellow
$partnerId = "66666666-6666-6666-6666-666666666661"
$partnerUserId = "22222222-2222-2222-2222-222222222223"
if (Check-RecordExists -Table "partners" -IdColumn "id" -IdValue $partnerId) {
    $script:Results.AlreadyPresent += "partners - Partner User (25% equity)"
    Write-Host "[EXISTS] partners - Partner User (25% equity)" -ForegroundColor Gray
} else {
    $query = "INSERT INTO partners (id, branch_id, user_id, equity_pct, is_active) VALUES ('$partnerId', '$branchId', '$partnerUserId', 25.0, true);"
    Insert-Record -Table "partners" -InsertQuery $query -RecordDescription "Partner User (25% equity)"
}

# 7. CAPITAL ACCOUNTS
Write-Host "Checking CAPITAL ACCOUNTS..." -ForegroundColor Yellow
$capitalAccountId = "77777777-7777-7777-7777-777777777771"
if (Check-RecordExists -Table "capital_accounts" -IdColumn "id" -IdValue $capitalAccountId) {
    $script:Results.AlreadyPresent += "capital_accounts - Investment 500000000"
    Write-Host "[EXISTS] capital_accounts - Investment 500000000" -ForegroundColor Gray
} else {
    $query = "INSERT INTO capital_accounts (id, branch_id, partner_id, amount, transaction_type, created_at) VALUES ('$capitalAccountId', '$branchId', '$partnerId', 500000000, 'investment', NOW());"
    Insert-Record -Table "capital_accounts" -InsertQuery $query -RecordDescription "Investment 500000000"
}

# 8. LOAN CASES
Write-Host "Checking LOAN CASES..." -ForegroundColor Yellow
$loanCaseId = "88888888-8888-8888-8888-888888888881"
$customerId = "44444444-4444-4444-4444-444444444441"
if (Check-RecordExists -Table "loan_cases" -IdColumn "id" -IdValue $loanCaseId) {
    $script:Results.AlreadyPresent += "loan_cases - Loan Case 5000000"
    Write-Host "[EXISTS] loan_cases - Loan Case 5000000" -ForegroundColor Gray
} else {
    $query = "INSERT INTO loan_cases (id, branch_id, customer_id, finance_amount, interest_amount, total_receivable, file_charges_amount, status, version) VALUES ('$loanCaseId', '$branchId', '$customerId', 5000000, 600000, 5600000, 50000, 'active', 1);"
    Insert-Record -Table "loan_cases" -InsertQuery $query -RecordDescription "Loan Case 5000000"
}

# 9. INSTALLMENTS
Write-Host "Checking INSTALLMENTS..." -ForegroundColor Yellow
$installments = @(
    @{Id='99999999-9999-9999-9999-999999999991'; No=1; Amount=933333; Status='paid'; DueDate="NOW() - INTERVAL '1 month'"},
    @{Id='99999999-9999-9999-9999-999999999992'; No=2; Amount=933333; Status='pending'; DueDate="NOW()"}
)

foreach ($installment in $installments) {
    if (Check-RecordExists -Table "installments" -IdColumn "id" -IdValue $installment.Id) {
        $script:Results.AlreadyPresent += "installments - Installment $($installment.No)"
        Write-Host "[EXISTS] installments - Installment $($installment.No)" -ForegroundColor Gray
    } else {
        $query = "INSERT INTO installments (id, branch_id, loan_case_id, installment_no, due_date, amount, status) VALUES ('$($installment.Id)', '$branchId', '$loanCaseId', $($installment.No), $($installment.DueDate), $($installment.Amount), '$($installment.Status)');"
        Insert-Record -Table "installments" -InsertQuery $query -RecordDescription "Installment $($installment.No)"
    }
}

# 10. RECEIPTS
Write-Host "Checking RECEIPTS..." -ForegroundColor Yellow
$receiptId = "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA1"
$installmentId = "99999999-9999-9999-9999-999999999991"
if (Check-RecordExists -Table "receipts" -IdColumn "id" -IdValue $receiptId) {
    $script:Results.AlreadyPresent += "receipts - RCP-1001"
    Write-Host "[EXISTS] receipts - RCP-1001" -ForegroundColor Gray
} else {
    $query = "INSERT INTO receipts (id, branch_id, public_id, installment_id, loan_case_id, amount_paid, mode, utr_ref, captured_at) VALUES ('$receiptId', '$branchId', 'RCP-1001', '$installmentId', '$loanCaseId', 933333, 'cash', 'CASH001', NOW() - INTERVAL '1 month');"
    Insert-Record -Table "receipts" -InsertQuery $query -RecordDescription "RCP-1001"
}

# 11. JOURNAL ENTRIES
Write-Host "Checking JOURNAL ENTRIES..." -ForegroundColor Yellow
$journalEntryId = "BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBB1"
if (Check-RecordExists -Table "journal_entries" -IdColumn "id" -IdValue $journalEntryId) {
    $script:Results.AlreadyPresent += "journal_entries - JE-3001"
    Write-Host "[EXISTS] journal_entries - JE-3001" -ForegroundColor Gray
} else {
    $query = "INSERT INTO journal_entries (id, branch_id, public_id, entry_date, description, reference, is_manual, is_posted) VALUES ('$journalEntryId', '$branchId', 'JE-3001', NOW(), 'Initial Capital Injection', 'VC-01', true, true);"
    Insert-Record -Table "journal_entries" -InsertQuery $query -RecordDescription "JE-3001"
}

# 12. JOURNAL LINES
Write-Host "Checking JOURNAL LINES..." -ForegroundColor Yellow
$journalLines = @(
    @{Id='CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCC1'; AccountName='Cash Office'; EntryType='debit'; Amount=500000000},
    @{Id='CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCC2'; AccountName='Equity Capital'; EntryType='credit'; Amount=500000000}
)

foreach ($line in $journalLines) {
    if (Check-RecordExists -Table "journal_lines" -IdColumn "id" -IdValue $line.Id) {
        $script:Results.AlreadyPresent += "journal_lines - $($line.AccountName) $($line.EntryType)"
        Write-Host "[EXISTS] journal_lines - $($line.AccountName) $($line.EntryType)" -ForegroundColor Gray
    } else {
        $query = "INSERT INTO journal_lines (id, branch_id, journal_entry_id, account_name, entry_type, amount) VALUES ('$($line.Id)', '$branchId', '$journalEntryId', '$($line.AccountName)', '$($line.EntryType)', $($line.Amount));"
        Insert-Record -Table "journal_lines" -InsertQuery $query -RecordDescription "$($line.AccountName) $($line.EntryType)"
    }
}

# 13. AUDIT LOGS
Write-Host "Checking AUDIT LOGS..." -ForegroundColor Yellow
$auditLogId = "DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDD1"
if (Check-RecordExists -Table "audit_logs" -IdColumn "id" -IdValue $auditLogId) {
    $script:Results.AlreadyPresent += "audit_logs - Initial Setup"
    Write-Host "[EXISTS] audit_logs - Initial Setup" -ForegroundColor Gray
} else {
    $query = "INSERT INTO audit_logs (id, branch_id, table_name, record_id, action, before_val, after_val, timestamp) VALUES ('$auditLogId', '$branchId', 'branches', '$branchId', 'INSERT', '{}', '{""name"": ""Initial Setup""}', NOW());"
    Insert-Record -Table "audit_logs" -InsertQuery $query -RecordDescription "Initial Setup"
}

# 14. DAY ENDS
Write-Host "Checking DAY ENDS..." -ForegroundColor Yellow
$dayEndId = "EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEE1"
if (Check-RecordExists -Table "day_ends" -IdColumn "id" -IdValue $dayEndId) {
    $script:Results.AlreadyPresent += "day_ends - Day End Record"
    Write-Host "[EXISTS] day_ends - Day End Record" -ForegroundColor Gray
} else {
    $query = "INSERT INTO day_ends (id, branch_id, date, is_closed, journals_locked, total_collected, discrepancy, discrepancy_resolved) VALUES ('$dayEndId', '$branchId', NOW() - INTERVAL '1 day', true, 12, 4500000, 0, true);"
    Insert-Record -Table "day_ends" -InsertQuery $query -RecordDescription "Day End Record"
}

# 15. PROFIT DISTRIBUTIONS
Write-Host "Checking PROFIT DISTRIBUTIONS..." -ForegroundColor Yellow
$profitDistId = "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFF1"
if (Check-RecordExists -Table "profit_distributions" -IdColumn "id" -IdValue $profitDistId) {
    $script:Results.AlreadyPresent += "profit_distributions - March 2026"
    Write-Host "[EXISTS] profit_distributions - March 2026" -ForegroundColor Gray
} else {
    $query = "INSERT INTO profit_distributions (id, branch_id, period, payout_amount, status, processed_at, partner_id) VALUES ('$profitDistId', '$branchId', 'March 2026', 2500000, 'processed', NOW(), '$partnerId');"
    Insert-Record -Table "profit_distributions" -InsertQuery $query -RecordDescription "March 2026"
}

# Generate Summary Report
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SEED DATA VERIFICATION SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "ALREADY PRESENT IN DATABASE:" -ForegroundColor Green
Write-Host "Total: $($script:Results.AlreadyPresent.Count)" -ForegroundColor Green
foreach ($item in $script:Results.AlreadyPresent) {
    Write-Host "  ✓ $item" -ForegroundColor Gray
}

Write-Host ""
Write-Host "NEWLY INSERTED RECORDS:" -ForegroundColor Yellow
Write-Host "Total: $($script:Results.Inserted.Count)" -ForegroundColor Yellow
foreach ($item in $script:Results.Inserted) {
    Write-Host "  + $item" -ForegroundColor Green
}

Write-Host ""
Write-Host "ERRORS ENCOUNTERED:" -ForegroundColor Red
Write-Host "Total: $($script:Results.Errors.Count)" -ForegroundColor Red
foreach ($item in $script:Results.Errors) {
    Write-Host "  ✗ $item" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "VERIFICATION COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Save detailed report to file
$reportPath = "seed_data_verification_report.txt"
$reportLines = @()
$reportLines += "SEED DATA VERIFICATION REPORT"
$reportLines += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$reportLines += "Database: $Database at $Server`:$Port"
$reportLines += ""
$reportLines += "========================================"
$reportLines += "SUMMARY"
$reportLines += "========================================"
$reportLines += "Already Present: $($script:Results.AlreadyPresent.Count)"
$reportLines += "Newly Inserted: $($script:Results.Inserted.Count)"
$reportLines += "Errors: $($script:Results.Errors.Count)"
$reportLines += ""
$reportLines += "========================================"
$reportLines += "ALREADY PRESENT IN DATABASE"
$reportLines += "========================================"
foreach ($item in $script:Results.AlreadyPresent) {
    $reportLines += "  [OK] $item"
}
$reportLines += ""
$reportLines += "========================================"
$reportLines += "NEWLY INSERTED RECORDS"
$reportLines += "========================================"
foreach ($item in $script:Results.Inserted) {
    $reportLines += "  [NEW] $item"
}
$reportLines += ""
$reportLines += "========================================"
$reportLines += "ERRORS ENCOUNTERED"
$reportLines += "========================================"
foreach ($item in $script:Results.Errors) {
    $reportLines += "  [ERROR] $item"
}

$reportLines | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host ""
Write-Host "Detailed report saved to: $reportPath" -ForegroundColor Cyan
arch 2026"
}

# Generate Summary Report
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SEED DATA VERIFICATION SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "ALREADY PRESENT IN DATABASE:" -ForegroundColor Green
Write-Host "Total: $($script:Results.AlreadyPresent.Count)" -ForegroundColor Green
foreach ($item in $script:Results.AlreadyPresent) {
    Write-Host "  [OK] $item" -ForegroundColor Gray
}

Write-Host ""
Write-Host "NEWLY INSERTED RECORDS:" -ForegroundColor Yellow
Write-Host "Total: $($script:Results.Inserted.Count)" -ForegroundColor Yellow
foreach ($item in $script:Results.Inserted) {
    Write-Host "  [NEW] $item" -ForegroundColor Green
}

Write-Host ""
Write-Host "ERRORS ENCOUNTERED:" -ForegroundColor Red
Write-Host "Total: $($script:Results.Errors.Count)" -ForegroundColor Red
foreach ($item in $script:Results.Errors) {
    Write-Host "  [ERR] $item" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "VERIFICATION COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Save detailed report to file
$reportPath = "seed_data_verification_report.txt"
$reportLines = @()
$reportLines += "SEED DATA VERIFICATION REPORT"
$reportLines += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$reportLines += "Database: $Database at ${Server}:${Port}"
$reportLines += ""
$reportLines += "========================================"
$reportLines += "SUMMARY"
$reportLines += "========================================"
$reportLines += "Already Present: $($script:Results.AlreadyPresent.Count)"
$reportLines += "Newly Inserted: $($script:Results.Inserted.Count)"
$reportLines += "Errors: $($script:Results.Errors.Count)"
$reportLines += ""
$reportLines += "========================================"
$reportLines += "ALREADY PRESENT IN DATABASE"
$reportLines += "========================================"
foreach ($item in $script:Results.AlreadyPresent) {
    $reportLines += "  [OK] $item"
}
$reportLines += ""
$reportLines += "========================================"
$reportLines += "NEWLY INSERTED RECORDS"
$reportLines += "========================================"
foreach ($item in $script:Results.Inserted) {
    $reportLines += "  [NEW] $item"
}
$reportLines += ""
$reportLines += "========================================"
$reportLines += "ERRORS ENCOUNTERED"
$reportLines += "========================================"
foreach ($item in $script:Results.Errors) {
    $reportLines += "  [ERROR] $item"
}

$reportLines | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host ""
Write-Host "Detailed report saved to: $reportPath" -ForegroundColor Cyan
