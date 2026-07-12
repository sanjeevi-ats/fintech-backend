# API Test Automation Script
param(
    [string]$BaseUrl = "http://localhost:5177"
)

$ErrorActionPreference = "Continue"
$testResults = @()
$authToken = $null

function Write-TestResult {
    param(
        [string]$TestId,
        [string]$Endpoint,
        [string]$Description,
        [int]$ExpectedStatus,
        [int]$ActualStatus,
        [object]$Response,
        [bool]$Passed,
        [string]$ErrorMsg = ""
    )
    
    $result = [PSCustomObject]@{
        TestId = $TestId
        Endpoint = $Endpoint
        Description = $Description
        ExpectedStatus = $ExpectedStatus
        ActualStatus = $ActualStatus
        Response = $Response
        Passed = $Passed
        ErrorMsg = $ErrorMsg
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    $script:testResults += $result
    
    $icon = if ($Passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($Passed) { "Green" } else { "Red" }
    
    Write-Host "$icon $TestId`: $Description" -ForegroundColor $color
    if (-not $Passed) {
        Write-Host "  Expected: $ExpectedStatus, Got: $ActualStatus" -ForegroundColor Yellow
        if ($ErrorMsg) {
            Write-Host "  Error: $ErrorMsg" -ForegroundColor Red
        }
    }
}

function Invoke-APIRequest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [bool]$IncludeAuth = $true
    )
    
    $url = "$BaseUrl$Endpoint"
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    if ($IncludeAuth -and $script:authToken) {
        $headers["Authorization"] = "Bearer $($script:authToken)"
    }
    
    try {
        $params = @{
            Uri = $url
            Method = $Method
            Headers = $headers
            TimeoutSec = 30
        }
        
        if ($Body) {
            $params["Body"] = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-RestMethod @params -ErrorAction Stop
        return @{
            StatusCode = 200
            Response = $response
        }
    }
    catch {
        $statusCode = 0
        $responseBody = $null
        
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            try {
                $stream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $responseBody = $reader.ReadToEnd() | ConvertFrom-Json
            }
            catch {
                $responseBody = @{ error = $_.Exception.Message }
            }
        }
        else {
            $responseBody = @{ error = $_.Exception.Message }
        }
        
        return @{
            StatusCode = $statusCode
            Response = $responseBody
        }
    }
}

Write-Host "=" * 60
Write-Host "Starting API Test Automation - Positive Flow Tests"
Write-Host "Base URL: $BaseUrl"
Write-Host "=" * 60
Write-Host ""

# Generate sample GUIDs
$SAMPLE_BRANCH_ID = [guid]::NewGuid().ToString()
$SAMPLE_CUSTOMER_ID = [guid]::NewGuid().ToString()
$SAMPLE_LOAN_ID = [guid]::NewGuid().ToString()
$SAMPLE_PARTNER_ID = [guid]::NewGuid().ToString()
$SAMPLE_USER_ID = [guid]::NewGuid().ToString()

# TS_API_001: Login with valid credentials
Write-Host "Testing Authentication Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "POST" -Endpoint "/api/v1/auth/login" `
    -Body @{ email = "super_admin@finveda.com"; password = "Admin@123" } `
    -IncludeAuth $false

$passed = ($result.StatusCode -eq 200) -and ($result.Response.token)
Write-TestResult -TestId "TS_API_001" -Endpoint "POST /api/v1/auth/login" `
    -Description "Verify login with valid credentials returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed `
    -ErrorMsg $(if (-not $passed) { "Login failed or token not returned" } else { "" })

if ($passed -and $result.Response.token) {
    $script:authToken = $result.Response.token
    Write-Host "  Auth token obtained successfully" -ForegroundColor Green
}

# TS_API_006: Register new user
$timestamp = [DateTimeOffset]::Now.ToUnixTimeSeconds()
$result = Invoke-APIRequest -Method "POST" -Endpoint "/api/v1/auth/register" `
    -Body @{
        name = "John Doe"
        email = "john.doe.$timestamp@fintech.com"
        password = "SecurePass@123"
        branchId = $SAMPLE_BRANCH_ID
        role = 8  # agent enum value
    } -IncludeAuth $false

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_006" -Endpoint "POST /api/v1/auth/register" `
    -Description "Verify register with valid data returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_015: Get all branches
Write-Host "`nTesting Branch Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/branch"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_015" -Endpoint "GET /api/v1/branch" `
    -Description "Verify get all branches returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

if ($passed -and $result.Response -and $result.Response.Count -gt 0) {
    $SAMPLE_BRANCH_ID = $result.Response[0].id
    Write-Host "  Using existing branch ID: $SAMPLE_BRANCH_ID" -ForegroundColor Gray
}

# TS_API_019: Create branch
$timestamp = [DateTimeOffset]::Now.ToUnixTimeSeconds()
$result = Invoke-APIRequest -Method "POST" -Endpoint "/api/v1/branch" `
    -Body @{
        name = "Test Branch $timestamp"
        city = "Delhi"
        isActive = $true
    }

$passed = $result.StatusCode -in @(200, 201)
Write-TestResult -TestId "TS_API_019" -Endpoint "POST /api/v1/branch" `
    -Description "Verify create branch returns 201" `
    -ExpectedStatus 201 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

if ($passed -and $result.Response.id) {
    $SAMPLE_BRANCH_ID = $result.Response.id
    Write-Host "  Created branch ID: $SAMPLE_BRANCH_ID" -ForegroundColor Gray
}

# TS_API_016: Get branch by ID
if ($SAMPLE_BRANCH_ID) {
    $result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/branch/$SAMPLE_BRANCH_ID"
    
    $passed = $result.StatusCode -eq 200
    Write-TestResult -TestId "TS_API_016" -Endpoint "GET /api/v1/branch/$SAMPLE_BRANCH_ID" `
        -Description "Verify get branch by ID returns 200" `
        -ExpectedStatus 200 -ActualStatus $result.StatusCode `
        -Response $result.Response -Passed $passed
}

# TS_API_027: Get all users
Write-Host "`nTesting User Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/users"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_027" -Endpoint "GET /api/v1/users" `
    -Description "Verify get all users returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

if ($passed -and $result.Response -and $result.Response.Count -gt 0) {
    $SAMPLE_USER_ID = $result.Response[0].id
}

# TS_API_030: Create user
$timestamp = [DateTimeOffset]::Now.ToUnixTimeSeconds()
$result = Invoke-APIRequest -Method "POST" -Endpoint "/api/v1/users" `
    -Body @{
        name = "Jane Smith"
        email = "jane.smith.$timestamp@fintech.com"
        password = "SecurePass@123"
        branchId = $SAMPLE_BRANCH_ID
        role = "loan_officer"
    }

$passed = $result.StatusCode -in @(200, 201)
Write-TestResult -TestId "TS_API_030" -Endpoint "POST /api/v1/users" `
    -Description "Verify create user returns 201" `
    -ExpectedStatus 201 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

if ($passed -and $result.Response.id) {
    $SAMPLE_USER_ID = $result.Response.id
}

# TS_API_028: Get user by ID
if ($SAMPLE_USER_ID) {
    $result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/users/$SAMPLE_USER_ID"
    
    $passed = $result.StatusCode -eq 200
    Write-TestResult -TestId "TS_API_028" -Endpoint "GET /api/v1/users/$SAMPLE_USER_ID" `
        -Description "Verify get user by ID returns 200" `
        -ExpectedStatus 200 -ActualStatus $result.StatusCode `
        -Response $result.Response -Passed $passed
}

# TS_API_034: Get all customers
Write-Host "`nTesting Customer Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/customers"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_034" -Endpoint "GET /api/v1/customers" `
    -Description "Verify get all customers returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_037: Create customer
$result = Invoke-APIRequest -Method "POST" -Endpoint "/api/v1/customers" `
    -Body @{
        name = "Priya Sharma"
        aadhaar = "123456789012"
        pan = "ABCDE1234F"
        branchId = $SAMPLE_BRANCH_ID
    }

$passed = $result.StatusCode -in @(200, 201)
Write-TestResult -TestId "TS_API_037" -Endpoint "POST /api/v1/customers" `
    -Description "Verify create customer returns 201" `
    -ExpectedStatus 201 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

if ($passed -and $result.Response.id) {
    $SAMPLE_CUSTOMER_ID = $result.Response.id
}

# TS_API_035: Get customer by ID
if ($SAMPLE_CUSTOMER_ID) {
    $result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/customers/$SAMPLE_CUSTOMER_ID"
    
    $passed = $result.StatusCode -eq 200
    Write-TestResult -TestId "TS_API_035" -Endpoint "GET /api/v1/customers/$SAMPLE_CUSTOMER_ID" `
        -Description "Verify get customer by ID returns 200" `
        -ExpectedStatus 200 -ActualStatus $result.StatusCode `
        -Response $result.Response -Passed $passed
}

# TS_API_042: Get all loan cases
Write-Host "`nTesting Loan Case Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/loancases"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_042" -Endpoint "GET /api/v1/loancases" `
    -Description "Verify get all loan cases returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_045: Create loan case
if ($SAMPLE_CUSTOMER_ID) {
    $result = Invoke-APIRequest -Method "POST" -Endpoint "/api/v1/loancases" `
        -Body @{
            customerId = $SAMPLE_CUSTOMER_ID
            principal = 10000000
            interestAmount = 2000000
            processingFees = 100000
        }
    
    $passed = $result.StatusCode -in @(200, 201)
    Write-TestResult -TestId "TS_API_045" -Endpoint "POST /api/v1/loancases" `
        -Description "Verify create loan case returns 201" `
        -ExpectedStatus 201 -ActualStatus $result.StatusCode `
        -Response $result.Response -Passed $passed
    
    if ($passed -and $result.Response.id) {
        $SAMPLE_LOAN_ID = $result.Response.id
    }
}

# TS_API_043: Get loan case by ID
if ($SAMPLE_LOAN_ID) {
    $result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/loancases/$SAMPLE_LOAN_ID"
    
    $passed = $result.StatusCode -eq 200
    Write-TestResult -TestId "TS_API_043" -Endpoint "GET /api/v1/loancases/$SAMPLE_LOAN_ID" `
        -Description "Verify get loan case by ID returns 200" `
        -ExpectedStatus 200 -ActualStatus $result.StatusCode `
        -Response $result.Response -Passed $passed
}

# TS_API_066: Get all partners
Write-Host "`nTesting Partner Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/partners"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_066" -Endpoint "GET /api/v1/partners" `
    -Description "Verify get all partners returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_069: Create partner
$timestamp = [DateTimeOffset]::Now.ToUnixTimeSeconds()
$result = Invoke-APIRequest -Method "POST" -Endpoint "/api/v1/partners" `
    -Body @{
        userId = $SAMPLE_USER_ID
        equityPct = 25.0
    }

$passed = $result.StatusCode -in @(200, 201)
Write-TestResult -TestId "TS_API_069" -Endpoint "POST /api/v1/partners" `
    -Description "Verify create partner returns 201" `
    -ExpectedStatus 201 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

if ($passed -and $result.Response.id) {
    $SAMPLE_PARTNER_ID = $result.Response.id
}

# TS_API_067: Get partner by ID
if ($SAMPLE_PARTNER_ID) {
    $result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/partners/$SAMPLE_PARTNER_ID"
    
    $passed = $result.StatusCode -eq 200
    Write-TestResult -TestId "TS_API_067" -Endpoint "GET /api/v1/partners/$SAMPLE_PARTNER_ID" `
        -Description "Verify get partner by ID returns 200" `
        -ExpectedStatus 200 -ActualStatus $result.StatusCode `
        -Response $result.Response -Passed $passed
}

# TS_API_074: Add investment
Write-Host "`nTesting Capital Account Endpoints..." -ForegroundColor Cyan
if ($SAMPLE_PARTNER_ID) {
    $result = Invoke-APIRequest -Method "POST" -Endpoint "/api/v1/capitalaccounts/investment" `
        -Body @{
            partnerId = $SAMPLE_PARTNER_ID
            amount = 50000000
        }
    
    $passed = $result.StatusCode -eq 200
    Write-TestResult -TestId "TS_API_074" -Endpoint "POST /api/v1/capitalaccounts/investment" `
        -Description "Verify add investment returns 200" `
        -ExpectedStatus 200 -ActualStatus $result.StatusCode `
        -Response $result.Response -Passed $passed
}

# TS_API_081: Get capital summary
if ($SAMPLE_PARTNER_ID) {
    $result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/capitalaccounts/summary/$SAMPLE_PARTNER_ID"
    
    $passed = $result.StatusCode -eq 200
    Write-TestResult -TestId "TS_API_081" -Endpoint "GET /api/v1/capitalaccounts/summary/$SAMPLE_PARTNER_ID" `
        -Description "Verify get capital summary returns 200" `
        -ExpectedStatus 200 -ActualStatus $result.StatusCode `
        -Response $result.Response -Passed $passed
}

# TS_API_083: Get active products
Write-Host "`nTesting Product Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/product/active"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_083" -Endpoint "GET /api/v1/product/active" `
    -Description "Verify get active products returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_091: Get PAR report
Write-Host "`nTesting Report Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/report/par?start=2026-01-01&end=2026-04-11"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_091" -Endpoint "GET /api/v1/report/par" `
    -Description "Verify get PAR report returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_092: Get PAR report without dates
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/report/par"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_092" -Endpoint "GET /api/v1/report/par" `
    -Description "Verify get PAR report without dates returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_093: Get collection efficiency
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/report/efficiency?start=2026-01-01&end=2026-04-11"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_093" -Endpoint "GET /api/v1/report/efficiency" `
    -Description "Verify get collection efficiency returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_095: Get dashboard stats
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/report/dashboard-stats"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_095" -Endpoint "GET /api/v1/report/dashboard-stats" `
    -Description "Verify get dashboard stats returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_097: Get audit logs
Write-Host "`nTesting Audit Endpoints..." -ForegroundColor Cyan
if ($SAMPLE_LOAN_ID) {
    $result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/audit/LoanCase/$SAMPLE_LOAN_ID"
    
    $passed = $result.StatusCode -eq 200
    Write-TestResult -TestId "TS_API_097" -Endpoint "GET /api/v1/audit/LoanCase/$SAMPLE_LOAN_ID" `
        -Description "Verify get entity history returns 200" `
        -ExpectedStatus 200 -ActualStatus $result.StatusCode `
        -Response $result.Response -Passed $passed
}

# TS_API_099: Get recent logs
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/v1/audit/recent?count=50"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_099" -Endpoint "GET /api/v1/audit/recent" `
    -Description "Verify get recent logs returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_102: Get overdue loans
Write-Host "`nTesting Recovery Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/recovery/overdue"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_102" -Endpoint "GET /api/recovery/overdue" `
    -Description "Verify get overdue loans returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_108: Get journal entries
Write-Host "`nTesting Journal Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/journal/entries"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_108" -Endpoint "GET /api/journal/entries" `
    -Description "Verify get journal entries returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_109: Get trial balance
Write-Host "`nTesting Ledger Endpoints..." -ForegroundColor Cyan
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/ledger/trial-balance"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_109" -Endpoint "GET /api/ledger/trial-balance" `
    -Description "Verify get trial balance returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# TS_API_110: Get P&L
$result = Invoke-APIRequest -Method "GET" -Endpoint "/api/ledger/pnl?start=2026-01-01&end=2026-04-11"

$passed = $result.StatusCode -eq 200
Write-TestResult -TestId "TS_API_110" -Endpoint "GET /api/ledger/pnl" `
    -Description "Verify get PnL returns 200" `
    -ExpectedStatus 200 -ActualStatus $result.StatusCode `
    -Response $result.Response -Passed $passed

# Generate Summary
Write-Host "`n" + ("=" * 60)
Write-Host "Test Execution Completed!"
Write-Host ("=" * 60)

$total = $testResults.Count
$passed = ($testResults | Where-Object { $_.Passed }).Count
$failed = $total - $passed
$passRate = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 1) } else { 0 }

Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "  Total Tests: $total"
Write-Host "  Passed: $passed" -ForegroundColor Green
Write-Host "  Failed: $failed" -ForegroundColor Red
Write-Host "  Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 80) { "Green" } else { "Yellow" })

# Generate HTML Report
$htmlReport = @"
<!DOCTYPE html>
<html>
<head>
    <title>API Test Automation Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .header { background-color: #2c3e50; color: white; padding: 20px; border-radius: 5px; margin-bottom: 20px; }
        .summary { display: flex; gap: 20px; margin-bottom: 20px; }
        .summary-card { flex: 1; padding: 20px; border-radius: 5px; color: white; text-align: center; }
        .total { background-color: #3498db; }
        .passed { background-color: #27ae60; }
        .failed { background-color: #e74c3c; }
        .pass-rate { background-color: #9b59b6; }
        .summary-card h2 { margin: 0; font-size: 36px; }
        .summary-card p { margin: 5px 0 0 0; font-size: 14px; }
        table { width: 100%; border-collapse: collapse; background-color: white; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        th { background-color: #34495e; color: white; padding: 12px; text-align: left; }
        td { padding: 10px; border-bottom: 1px solid #ddd; }
        tr:hover { background-color: #f5f5f5; }
        .pass { color: #27ae60; font-weight: bold; }
        .fail { color: #e74c3c; font-weight: bold; }
        .error-msg { color: #e74c3c; font-size: 12px; font-style: italic; }
    </style>
</head>
<body>
    <div class="header">
        <h1>API Test Automation Report</h1>
        <p>Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
    </div>
    
    <div class="summary">
        <div class="summary-card total">
            <h2>$total</h2>
            <p>Total Tests</p>
        </div>
        <div class="summary-card passed">
            <h2>$passed</h2>
            <p>Passed</p>
        </div>
        <div class="summary-card failed">
            <h2>$failed</h2>
            <p>Failed</p>
        </div>
        <div class="summary-card pass-rate">
            <h2>$passRate%</h2>
            <p>Pass Rate</p>
        </div>
    </div>
    
    <table>
        <thead>
            <tr>
                <th>Test ID</th>
                <th>Endpoint</th>
                <th>Description</th>
                <th>Expected</th>
                <th>Actual</th>
                <th>Status</th>
                <th>Details</th>
            </tr>
        </thead>
        <tbody>
"@

foreach ($result in $testResults) {
    $statusClass = if ($result.Passed) { "pass" } else { "fail" }
    $statusText = if ($result.Passed) { "PASS" } else { "FAIL" }
    $errorHtml = if ($result.ErrorMsg) { "<div class='error-msg'>$($result.ErrorMsg)</div>" } else { "" }
    
    $htmlReport += @"
            <tr>
                <td>$($result.TestId)</td>
                <td>$($result.Endpoint)</td>
                <td>$($result.Description)</td>
                <td>$($result.ExpectedStatus)</td>
                <td>$($result.ActualStatus)</td>
                <td class="$statusClass">$statusText</td>
                <td>$errorHtml</td>
            </tr>
"@
}

$htmlReport += @"
        </tbody>
    </table>
</body>
</html>
"@

$htmlReport | Out-File -FilePath "api_test_report.html" -Encoding UTF8
Write-Host "`nHTML Report generated: api_test_report.html" -ForegroundColor Green

# Export results to JSON
$testResults | ConvertTo-Json -Depth 10 | Out-File -FilePath "test_results.json" -Encoding UTF8
Write-Host "JSON Results exported: test_results.json" -ForegroundColor Green
