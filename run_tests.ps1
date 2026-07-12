# FinTech Phase 1 Items 1-2 Automated Test Script
# Purpose: Run comprehensive tests for Loan Code Format and Duplicate Customer Validation
# Usage: powershell -ExecutionPolicy Bypass -File run_tests.ps1

param(
    [string]$BaseUrl = "http://localhost:5177",
    [string]$Token = "",
    [switch]$ShowDetails = $false
)

# ============================================================
# CONFIGURATION
# ============================================================

$ErrorActionPreference = "Continue"
$testResults = @()
$passCount = 0
$failCount = 0

# Colors
$successColor = "Green"
$failColor = "Red"
$infoColor = "Cyan"
$warningColor = "Yellow"

# Test data
$testData = @{
    customer1 = @{
        name = "Rajesh Kumar"
        phone = "9876543210"
        aadhaar = "123456789012"
        pan = "ABCDE1234F"
    }
    customer2 = @{
        name = "Priya Singh"
        phone = "9123456789"
        aadhaar = "987654321098"
        pan = "FGHIJ5678K"
    }
    customer3 = @{
        name = "Amit Patel"
        phone = "9988776655"
        aadhaar = "111222333444"
        pan = "KLMNO9876P"
    }
}

# ============================================================
# HELPER FUNCTIONS
# ============================================================

function Write-TestHeader {
    param([string]$Title)
    Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor $infoColor
    Write-Host "║ $($Title.PadRight(38)) ║" -ForegroundColor $infoColor
    Write-Host "╚════════════════════════════════════════╝" -ForegroundColor $infoColor
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = ""
    )
    
    $status = $Passed ? "✅ PASS" : "❌ FAIL"
    $color = $Passed ? $successColor : $failColor
    
    Write-Host "$status | $TestName" -ForegroundColor $color
    if ($Message -and $ShowDetails) {
        Write-Host "       → $Message" -ForegroundColor $infoColor
    }
    
    $global:testResults += @{
        name = $TestName
        passed = $Passed
        message = $Message
    }
    
    if ($Passed) { $global:passCount++ } else { $global:failCount++ }
}

function Test-API {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [int]$ExpectedStatus = 200
    )
    
    try {
        $headers = @{
            "Authorization" = "Bearer $Token"
            "Content-Type" = "application/json"
        }
        
        $params = @{
            Uri = "$BaseUrl$Endpoint"
            Method = $Method
            Headers = $headers
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
        }
        
        $response = Invoke-RestMethod @params
        return @{
            success = $true
            statusCode = 200
            data = $response
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        return @{
            success = $false
            statusCode = $statusCode
            error = $_.Exception.Message
            response = $null
        }
    }
}

function Get-AuthToken {
    Write-Host "Acquiring authentication token..." -ForegroundColor $infoColor
    
    $loginBody = @{
        email = "admin@fintech.com"
        password = "Admin@123"
    } | ConvertTo-Json
    
    $response = Test-API -Method "POST" -Endpoint "/api/v1/auth/login" -Body $loginBody -ExpectedStatus 200
    
    if ($response.success) {
        $global:Token = $response.data.token
        Write-Host "✅ Token acquired" -ForegroundColor $successColor
        return $true
    } else {
        Write-Host "❌ Failed to get token" -ForegroundColor $failColor
        Write-Host "   Error: $($response.error)" -ForegroundColor $failColor
        return $false
    }
}

# ============================================================
# TEST SUITES
# ============================================================

function Test-LoanCodeGeneration {
    Write-TestHeader "ITEM 1: LOAN CODE GENERATION"
    
    # Test 1.1: Create first loan
    Write-Host "`n→ Creating first loan..." -ForegroundColor $infoColor
    $loan1Body = @{
        customerId = "550e8400-e29b-41d4-a716-446655440000"
        principal = 1000000
        interestAmount = 150000
        processingFees = 50000
    }
    
    $response = Test-API -Method "POST" -Endpoint "/api/v1/LoanCases" -Body $loan1Body
    $passed = $response.success -and $response.data.loanCode -eq "LN00001"
    Write-TestResult "Create Loan 1" $passed
    
    if (-not $passed) {
        Write-Host "   Response: $($response.data | ConvertTo-Json)" -ForegroundColor $warningColor
        return $null
    }
    
    $loan1Id = $response.data.id
    $loan1Code = $response.data.loanCode
    
    # Test 1.2: Create second loan
    Write-Host "`n→ Creating second loan..." -ForegroundColor $infoColor
    $loan2Body = @{
        customerId = "550e8400-e29b-41d4-a716-446655440001"
        principal = 2000000
        interestAmount = 300000
        processingFees = 100000
    }
    
    $response = Test-API -Method "POST" -Endpoint "/api/v1/LoanCases" -Body $loan2Body
    $passed = $response.success -and $response.data.loanCode -eq "LN00002"
    Write-TestResult "Create Loan 2" $passed
    
    if (-not $passed) { return $null }
    
    $loan2Id = $response.data.id
    $loan2Code = $response.data.loanCode
    
    # Test 1.3: Search by loan code
    Write-Host "`n→ Searching loans by code..." -ForegroundColor $infoColor
    $response = Test-API -Method "GET" -Endpoint "/api/v1/LoanCases/search/$loan1Code"
    $passed = $response.success -and $response.data.id -eq $loan1Id
    Write-TestResult "Search by Code" $passed
    
    # Test 1.4: Search by UUID
    Write-Host "`n→ Searching loans by UUID..." -ForegroundColor $infoColor
    $response = Test-API -Method "GET" -Endpoint "/api/v1/LoanCases/search/$loan1Id"
    $passed = $response.success -and $response.data.loanCode -eq $loan1Code
    Write-TestResult "Search by UUID" $passed
    
    # Test 1.5: Search non-existent code
    Write-Host "`n→ Testing invalid search..." -ForegroundColor $infoColor
    $response = Test-API -Method "GET" -Endpoint "/api/v1/LoanCases/search/LN99999"
    $passed = -not $response.success -and ($response.statusCode -eq 404 -or $response.statusCode -eq 400)
    Write-TestResult "Invalid Code 404" $passed
    
    return @{
        loan1Id = $loan1Id
        loan1Code = $loan1Code
        loan2Id = $loan2Id
        loan2Code = $loan2Code
    }
}

function Test-DuplicateCustomerValidation {
    Write-TestHeader "ITEM 2: DUPLICATE CUSTOMER VALIDATION"
    
    # Test 2.1: Check non-existent phone
    Write-Host "`n→ Checking non-existent phone..." -ForegroundColor $infoColor
    $response = Test-API -Method "GET" -Endpoint "/api/v1/Customers/check-duplicate?phone=9999999999"
    $passed = $response.success -and $response.data.exists -eq $false
    Write-TestResult "Check Non-Existent Phone" $passed
    
    # Test 2.2: Create customer 1
    Write-Host "`n→ Creating customer 1..." -ForegroundColor $infoColor
    $cust1Body = @{
        name = $testData.customer1.name
        phone = $testData.customer1.phone
        aadhaarEncrypted = $testData.customer1.aadhaar
        panEncrypted = $testData.customer1.pan
    }
    
    $response = Test-API -Method "POST" -Endpoint "/api/v1/Customers" -Body $cust1Body
    $passed = $response.success -and $response.statusCode -eq 201
    Write-TestResult "Create Customer 1" $passed
    
    if (-not $passed) { return $null }
    
    $cust1Id = $response.data.id
    
    # Test 2.3: Check existing phone
    Write-Host "`n→ Checking existing phone..." -ForegroundColor $infoColor
    $response = Test-API -Method "GET" -Endpoint "/api/v1/Customers/check-duplicate?phone=$($testData.customer1.phone)"
    $passed = $response.success -and $response.data.exists -eq $true -and $response.data.customerId -eq $cust1Id
    Write-TestResult "Check Existing Phone" $passed
    
    # Test 2.4: Try to create duplicate (should fail)
    Write-Host "`n→ Attempting duplicate creation..." -ForegroundColor $infoColor
    $dupBody = @{
        name = "Different Name"
        phone = $testData.customer1.phone  # Same phone
        aadhaarEncrypted = "999888777666"
        panEncrypted = "XXXXX0000Y"
    }
    
    $response = Test-API -Method "POST" -Endpoint "/api/v1/Customers" -Body $dupBody
    $passed = -not $response.success -and $response.statusCode -eq 409
    Write-TestResult "Reject Duplicate (409)" $passed
    
    # Test 2.5: Create customer 2 with different phone
    Write-Host "`n→ Creating customer 2 (different phone)..." -ForegroundColor $infoColor
    $cust2Body = @{
        name = $testData.customer2.name
        phone = $testData.customer2.phone
        aadhaarEncrypted = $testData.customer2.aadhaar
        panEncrypted = $testData.customer2.pan
    }
    
    $response = Test-API -Method "POST" -Endpoint "/api/v1/Customers" -Body $cust2Body
    $passed = $response.success -and $response.statusCode -eq 201
    Write-TestResult "Create Customer 2" $passed
    
    if (-not $passed) { return $null }
    
    $cust2Id = $response.data.id
    
    # Test 2.6: Verify both customers exist
    Write-Host "`n→ Retrieving all customers..." -ForegroundColor $infoColor
    $response = Test-API -Method "GET" -Endpoint "/api/v1/Customers"
    $passed = $response.success -and ($response.data | Where-Object { $_.id -eq $cust1Id }).count -gt 0
    Write-TestResult "Get All Customers" $passed
    
    return @{
        cust1Id = $cust1Id
        cust1Phone = $testData.customer1.phone
        cust2Id = $cust2Id
        cust2Phone = $testData.customer2.phone
    }
}

function Test-EndToEndFlow {
    Write-TestHeader "END-TO-END WORKFLOW TEST"
    
    Write-Host "`n→ Creating test customer..." -ForegroundColor $infoColor
    $e2eCustBody = @{
        name = "E2E Test Customer"
        phone = "9111111111"
        aadhaarEncrypted = "555666777888"
        panEncrypted = "PPPPP1111Q"
    }
    
    $response = Test-API -Method "POST" -Endpoint "/api/v1/Customers" -Body $e2eCustBody
    $e2ePassed = $response.success -and $response.statusCode -eq 201
    Write-TestResult "E2E: Create Customer" $e2ePassed
    
    if (-not $e2ePassed) { return }
    
    $e2eCustId = $response.data.id
    
    Write-Host "`n→ Creating loan with auto-generated code..." -ForegroundColor $infoColor
    $e2eLoanBody = @{
        customerId = $e2eCustId
        principal = 500000
        interestAmount = 75000
        processingFees = 25000
    }
    
    $response = Test-API -Method "POST" -Endpoint "/api/v1/LoanCases" -Body $e2eLoanBody
    $e2ePassed = $response.success -and $response.data.loanCode
    Write-TestResult "E2E: Create Loan" $e2ePassed
    
    if (-not $e2ePassed) { return }
    
    $e2eLoanCode = $response.data.loanCode
    
    Write-Host "`n→ Searching loan by auto-generated code..." -ForegroundColor $infoColor
    $response = Test-API -Method "GET" -Endpoint "/api/v1/LoanCases/search/$e2eLoanCode"
    $e2ePassed = $response.success
    Write-TestResult "E2E: Search by Generated Code" $e2ePassed
    
    Write-Host "`n→ Verifying no duplicate customer exists..." -ForegroundColor $infoColor
    $response = Test-API -Method "GET" -Endpoint "/api/v1/Customers/check-duplicate?phone=9111111111"
    $e2ePassed = $response.success -and $response.data.exists -eq $true
    Write-TestResult "E2E: Duplicate Prevention Works" $e2ePassed
}

function Test-PerformanceAndLoad {
    Write-TestHeader "PERFORMANCE & LOAD TESTING"
    
    Write-Host "`n→ Testing duplicate check performance..." -ForegroundColor $infoColor
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    for ($i = 0; $i -lt 10; $i++) {
        $phone = "91234567" + ($i.ToString().PadLeft(2, '0'))
        $response = Test-API -Method "GET" -Endpoint "/api/v1/Customers/check-duplicate?phone=$phone"
    }
    
    $stopwatch.Stop()
    $avgTime = $stopwatch.ElapsedMilliseconds / 10
    
    $perfPassed = $avgTime -lt 100  # Should be < 100ms per check
    Write-TestResult "Duplicate Check Performance" $perfPassed "$($avgTime)ms avg"
}

function Print-TestSummary {
    Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor $infoColor
    Write-Host "║         TEST EXECUTION SUMMARY         ║" -ForegroundColor $infoColor
    Write-Host "╚════════════════════════════════════════╝" -ForegroundColor $infoColor
    
    Write-Host "`nTotal Tests: $($global:passCount + $global:failCount)" -ForegroundColor $infoColor
    Write-Host "✅ Passed:   $global:passCount" -ForegroundColor $successColor
    Write-Host "❌ Failed:   $global:failCount" -ForegroundColor $failColor
    
    if ($global:passCount -gt 0) {
        $passRate = [Math]::Round(($global:passCount / ($global:passCount + $global:failCount)) * 100, 2)
        Write-Host "Pass Rate:  $passRate%" -ForegroundColor $successColor
    }
    
    if ($global:failCount -gt 0) {
        Write-Host "`nFailed Tests:" -ForegroundColor $failColor
        foreach ($result in $global:testResults | Where-Object { $_.passed -eq $false }) {
            Write-Host "  ❌ $($result.name)" -ForegroundColor $failColor
            if ($result.message) {
                Write-Host "     → $($result.message)" -ForegroundColor $warningColor
            }
        }
    }
}

# ============================================================
# MAIN EXECUTION
# ============================================================

Write-Host "╔════════════════════════════════════════╗" -ForegroundColor $infoColor
Write-Host "║  FinTech Phase 1 Automated Test Suite  ║" -ForegroundColor $infoColor
Write-Host "╚════════════════════════════════════════╝" -ForegroundColor $infoColor

Write-Host "`nConfiguration:" -ForegroundColor $infoColor
Write-Host "  Base URL: $BaseUrl" -ForegroundColor $infoColor
Write-Host "  Show Details: $ShowDetails" -ForegroundColor $infoColor

# Get auth token
if (-not (Get-AuthToken)) {
    Write-Host "`n❌ Cannot proceed without authentication" -ForegroundColor $failColor
    exit 1
}

# Run test suites
Test-LoanCodeGeneration
Test-DuplicateCustomerValidation
Test-EndToEndFlow
Test-PerformanceAndLoad

# Print summary
Print-TestSummary

# Exit with appropriate code
if ($global:failCount -eq 0) {
    Write-Host "`n✅ ALL TESTS PASSED!" -ForegroundColor $successColor
    exit 0
} else {
    Write-Host "`n❌ SOME TESTS FAILED" -ForegroundColor $failColor
    exit 1
}
