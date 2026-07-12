# Complete PowerShell script to generate comprehensive API test cases Excel file

Write-Host "Creating Fintech API Test Cases Excel Document..." -ForegroundColor Green
Write-Host ""

# Create Excel application
try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
} catch {
    Write-Host "Error: Could not create Excel COM object. Please ensure Microsoft Excel is installed." -ForegroundColor Red
    exit 1
}

$workbook = $excel.Workbooks.Add()
$worksheet = $workbook.Worksheets.Item(1)
$worksheet.Name = "API Test Cases"

# Sample GUIDs for testing
$BRANCH_ID = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
$CUSTOMER_ID = "b2c3d4e5-f6a7-8901-bcde-f12345678901"
$LOAN_ID = "c3d4e5f6-a7b8-9012-cdef-123456789012"
$INSTALLMENT_ID = "d4e5f6a7-b8c9-0123-def1-234567890123"
$PARTNER_ID = "e5f6a7b8-c9d0-1234-ef12-345678901234"
$USER_ID = "f6a7b8c9-d0e1-2345-f123-456789012345"
$PRODUCT_ID = "a7b8c9d0-e1f2-3456-1234-567890123456"

# Headers
$headers = @("Test Case ID", "Endpoint Name", "Test Case Description", "Request Payload", "Expected Status Code", "Expected Response", "Status", "Comments")

# Format header row
for ($i = 1; $i -le $headers.Count; $i++) {
    $cell = $worksheet.Cells.Item(1, $i)
    $cell.Value2 = $headers[$i - 1]
    $cell.Font.Bold = $true
    $cell.Font.Size = 11
    $cell.Font.Color = 16777215  # White
    $cell.Interior.Color = 4474068  # Blue
    $cell.HorizontalAlignment = -4108  # Center
    $cell.VerticalAlignment = -4108  # Center
}

Write-Host "Adding test cases..." -ForegroundColor Yellow

# Function to add test case
function Add-TestCase {
    param($row, $id, $endpoint, $description, $payload, $statusCode, $response)
    
    $worksheet.Cells.Item($row, 1).Value2 = $id
    $worksheet.Cells.Item($row, 2).Value2 = $endpoint
    $worksheet.Cells.Item($row, 3).Value2 = $description
    $worksheet.Cells.Item($row, 4).Value2 = $payload
    $worksheet.Cells.Item($row, 5).Value2 = $statusCode
    $worksheet.Cells.Item($row, 6).Value2 = $response
    $worksheet.Cells.Item($row, 7).Value2 = ""
    $worksheet.Cells.Item($row, 8).Value2 = ""
    
    # Format cells
    for ($col = 1; $col -le 8; $col++) {
        $cell = $worksheet.Cells.Item($row, $col)
        $cell.WrapText = $true
        $cell.VerticalAlignment = -4160  # Top
        $cell.Borders.Weight = 2
    }
}

$row = 2

# AUTH CONTROLLER TEST CASES
Add-TestCase $row "TS_API_001" "POST /api/v1/auth/login" "Verify whether the login endpoint returns 200 response while hitting it with valid credentials" '{"email":"admin@fintech.com","password":"Admin@123"}' 200 '{"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...","email":"admin@fintech.com","name":"Admin User","role":"super_admin"}'
$row++

Add-TestCase $row "TS_API_002" "POST /api/v1/auth/login" "Verify whether the login endpoint returns 401 response when invalid email is provided" '{"email":"invalid@fintech.com","password":"Admin@123"}' 401 '{"statusCode":401,"errorCode":"UNAUTHORIZED","message":"Invalid email or password"}'
$row++

Add-TestCase $row "TS_API_003" "POST /api/v1/auth/login" "Verify whether the login endpoint returns 401 response when invalid password is provided" '{"email":"admin@fintech.com","password":"WrongPassword"}' 401 '{"statusCode":401,"errorCode":"UNAUTHORIZED","message":"Invalid email or password"}'
$row++

Add-TestCase $row "TS_API_004" "POST /api/v1/auth/login" "Verify whether the login endpoint returns 400 response when email field is missing" '{"password":"Admin@123"}' 400 '{"errors":{"Email":["The Email field is required."]}}'
$row++

Add-TestCase $row "TS_API_005" "POST /api/v1/auth/login" "Verify whether the login endpoint returns 400 response when password field is missing" '{"email":"admin@fintech.com"}' 400 '{"errors":{"Password":["The Password field is required."]}}'
$row++

Add-TestCase $row "TS_API_006" "POST /api/v1/auth/register" "Verify whether the register endpoint returns 200 response with valid registration data" "{`"name`":`"John Doe`",`"email`":`"john.doe@fintech.com`",`"password`":`"SecurePass@123`",`"branchId`":`"$BRANCH_ID`",`"role`":`"agent`"}" 200 '{"id":"new-user-guid","message":"User registered successfully"}'
$row++

Add-TestCase $row "TS_API_007" "POST /api/v1/auth/register" "Verify whether the register endpoint returns 400 response when email format is invalid" "{`"name`":`"John Doe`",`"email`":`"invalid-email`",`"password`":`"SecurePass@123`",`"branchId`":`"$BRANCH_ID`",`"role`":`"agent`"}" 400 '{"errors":{"Email":["The Email field is not a valid e-mail address."]}}'
$row++

Add-TestCase $row "TS_API_008" "POST /api/v1/auth/register" "Verify whether the register endpoint returns 400 response when password is less than 6 characters" "{`"name`":`"John Doe`",`"email`":`"john.doe@fintech.com`",`"password`":`"12345`",`"branchId`":`"$BRANCH_ID`",`"role`":`"agent`"}" 400 '{"errors":{"Password":["The field Password must be a string with a minimum length of 6."]}}'
$row++

Add-TestCase $row "TS_API_009" "POST /api/v1/auth/refresh-token" "Verify whether the refresh-token endpoint returns 200 response with valid token" '{"token":"valid-refresh-token-here"}' 200 '{"token":"new-jwt-token","refreshToken":"new-refresh-token"}'
$row++

Add-TestCase $row "TS_API_010" "POST /api/v1/auth/refresh-token" "Verify whether the refresh-token endpoint returns 401 response with invalid token" '{"token":"invalid-token"}' 401 '{"statusCode":401,"message":"Invalid or expired token"}'
$row++

Add-TestCase $row "TS_API_011" "POST /api/v1/auth/enable-totp" "Verify whether the enable-totp endpoint returns 200 response with valid user ID" "{`"userId`":`"$USER_ID`"}" 200 '{"qrCode":"data:image/png;base64,iVBORw0KGgo...","secret":"JBSWY3DPEHPK3PXP"}'
$row++

Add-TestCase $row "TS_API_012" "POST /api/v1/auth/enable-totp" "Verify whether the enable-totp endpoint returns 404 response with non-existent user ID" '{"userId":"00000000-0000-0000-0000-000000000000"}' 404 '{"statusCode":404,"message":"User not found"}'
$row++

Add-TestCase $row "TS_API_013" "POST /api/v1/auth/change-password" "Verify whether the change-password endpoint returns 200 response with valid credentials" "{`"userId`":`"$USER_ID`",`"oldPassword`":`"OldPass@123`",`"newPassword`":`"NewPass@123`"}" 200 '{"message":"Password changed successfully"}'
$row++

Add-TestCase $row "TS_API_014" "POST /api/v1/auth/change-password" "Verify whether the change-password endpoint returns 401 response when old password is incorrect" "{`"userId`":`"$USER_ID`",`"oldPassword`":`"WrongOldPass`",`"newPassword`":`"NewPass@123`"}" 401 '{"statusCode":401,"message":"Old password is incorrect"}'
$row++

# BRANCH CONTROLLER
Add-TestCase $row "TS_API_015" "GET /api/v1/branch" "Verify whether the get all branches endpoint returns 200 response with list of branches" "N/A" 200 "[{`"id`":`"$BRANCH_ID`",`"name`":`"Main Branch`",`"city`":`"Mumbai`",`"isActive`":true}]"
$row++

Add-TestCase $row "TS_API_016" "GET /api/v1/branch/$BRANCH_ID" "Verify whether the get branch by ID endpoint returns 200 response with branch details" "N/A" 200 "{`"id`":`"$BRANCH_ID`",`"name`":`"Main Branch`",`"city`":`"Mumbai`",`"isActive`":true}"
$row++

Add-TestCase $row "TS_API_017" "GET /api/v1/branch/00000000-0000-0000-0000-000000000000" "Verify whether the get branch by ID endpoint returns 404 response when branch does not exist" "N/A" 404 '{"statusCode":404,"message":"Branch not found"}'
$row++

Add-TestCase $row "TS_API_018" "GET /api/v1/branch/search?query=Mumbai" "Verify whether the search branches endpoint returns 200 response with matching branches" "N/A" 200 "[{`"id`":`"$BRANCH_ID`",`"name`":`"Main Branch`",`"city`":`"Mumbai`",`"isActive`":true}]"
$row++

Add-TestCase $row "TS_API_019" "POST /api/v1/branch" "Verify whether the create branch endpoint returns 201 response with valid branch data" '{"name":"New Branch","city":"Delhi","isActive":true}' 201 '{"id":"new-branch-guid","name":"New Branch","city":"Delhi","isActive":true}'
$row++

Add-TestCase $row "TS_API_020" "POST /api/v1/branch" "Verify whether the create branch endpoint returns 400 response when name is missing" '{"city":"Delhi","isActive":true}' 400 '{"errors":{"Name":["The Name field is required."]}}'
$row++

Write-Host "Test cases 1-20 added..." -ForegroundColor Cyan

# Continue with more test cases...
