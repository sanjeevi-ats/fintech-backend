# PowerShell script to generate Excel test cases document

# Create Excel COM object
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$workbook = $excel.Workbooks.Add()
$worksheet = $workbook.Worksheets.Item(1)
$worksheet.Name = "API Test Cases"

# Define headers
$headers = @(
    "Test Case ID",
    "Endpoint Name", 
    "Test Case Description",
    "Request Payload",
    "Expected Status Code",
    "Expected Response",
    "Status",
    "Comments"
)

# Sample GUIDs
$SAMPLE_BRANCH_ID = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
$SAMPLE_CUSTOMER_ID = "b2c3d4e5-f6a7-8901-bcde-f12345678901"
$SAMPLE_LOAN_ID = "c3d4e5f6-a7b8-9012-cdef-123456789012"
$SAMPLE_INSTALLMENT_ID = "d4e5f6a7-b8c9-0123-def1-234567890123"
$SAMPLE_PARTNER_ID = "e5f6a7b8-c9d0-1234-ef12-345678901234"
$SAMPLE_USER_ID = "f6a7b8c9-d0e1-2345-f123-456789012345"
$SAMPLE_PRODUCT_ID = "a7b8c9d0-e1f2-3456-1234-567890123456"

# Write headers with formatting
for ($i = 0; $i -lt $headers.Count; $i++) {
    $cell = $worksheet.Cells.Item(1, $i + 1)
    $cell.Value2 = $headers[$i]
    $cell.Font.Bold = $true
    $cell.Font.Color = 16777215  # White
    $cell.Interior.Color = 4474068  # Blue
    $cell.HorizontalAlignment = -4108  # Center
    $cell.VerticalAlignment = -4108  # Center
}

# Test cases data
$testCases = @(
    # AUTH CONTROLLER
    @{
        id = "TS_API_001"
        endpoint = "POST /api/v1/auth/login"
        description = "Verify whether the login endpoint returns 200 response while hitting it with valid credentials"
        payload = @"
{
  "email": "admin@fintech.com",
  "password": "Admin@123"
}
"@
        statusCode = 200
        response = @"
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "admin@fintech.com",
  "name": "Admin User",
  "role": "super_admin"
}
"@
    },
    @{
        id = "TS_API_002"
        endpoint = "POST /api/v1/auth/login"
        description = "Verify whether the login endpoint returns 401 response when invalid email is provided"
        payload = @"
{
  "email": "invalid@fintech.com",
  "password": "Admin@123"
}
"@
        statusCode = 401
        response = @"
{
  "statusCode": 401,
  "errorCode": "UNAUTHORIZED",
  "message": "Invalid email or password"
}
"@
    },
    @{
        id = "TS_API_003"
        endpoint = "POST /api/v1/auth/login"
        description = "Verify whether the login endpoint returns 401 response when invalid password is provided"
        payload = @"
{
  "email": "admin@fintech.com",
  "password": "WrongPassword"
}
"@
        statusCode = 401
        response = @"
{
  "statusCode": 401,
  "errorCode": "UNAUTHORIZED",
  "message": "Invalid email or password"
}
"@
    },
    @{
        id = "TS_API_004"
        endpoint = "POST /api/v1/auth/login"
        description = "Verify whether the login endpoint returns 400 response when email field is missing"
        payload = @"
{
  "password": "Admin@123"
}
"@
        statusCode = 400
        response = @"
{
  "errors": {
    "Email": ["The Email field is required."]
  }
}
"@
    },
    @{
        id = "TS_API_005"
        endpoint = "POST /api/v1/auth/login"
        description = "Verify whether the login endpoint returns 400 response when password field is missing"
        payload = @"
{
  "email": "admin@fintech.com"
}
"@
        statusCode = 400
        response = @"
{
  "errors": {
    "Password": ["The Password field is required."]
  }
}
"@
    },
    @{
        id = "TS_API_006"
        endpoint = "POST /api/v1/auth/register"
        description = "Verify whether the register endpoint returns 200 response with valid registration data"
        payload = @"
{
  "name": "John Doe",
  "email": "john.doe@fintech.com",
  "password": "SecurePass@123",
  "branchId": "$SAMPLE_BRANCH_ID",
  "role": "agent"
}
"@
        statusCode = 200
        response = @"
{
  "id": "new-user-guid-here",
  "message": "User registered successfully"
}
"@
    },
    @{
        id = "TS_API_007"
        endpoint = "POST /api/v1/auth/register"
        description = "Verify whether the register endpoint returns 400 response when email format is invalid"
        payload = @"
{
  "name": "John Doe",
  "email": "invalid-email",
  "password": "SecurePass@123",
  "branchId": "$SAMPLE_BRANCH_ID",
  "role": "agent"
}
"@
        statusCode = 400
        response = @"
{
  "errors": {
    "Email": ["The Email field is not a valid e-mail address."]
  }
}
"@
    },
    @{
        id = "TS_API_008"
        endpoint = "POST /api/v1/auth/register"
        description = "Verify whether the register endpoint returns 400 response when password is less than 6 characters"
        payload = @"
{
  "name": "John Doe",
  "email": "john.doe@fintech.com",
  "password": "12345",
  "branchId": "$SAMPLE_BRANCH_ID",
  "role": "agent"
}
"@
        statusCode = 400
        response = @"
{
  "errors": {
    "Password": ["The field Password must be a string with a minimum length of 6."]
  }
}
"@
    },
    @{
        id = "TS_API_009"
        endpoint = "POST /api/v1/auth/refresh-token"
        description = "Verify whether the refresh-token endpoint returns 200 response with valid token"
        payload = @"
{
  "token": "valid-refresh-token-here"
}
"@
        statusCode = 200
        response = @"
{
  "token": "new-jwt-token-here",
  "refreshToken": "new-refresh-token-here"
}
"@
    },
    @{
        id = "TS_API_010"
        endpoint = "POST /api/v1/auth/refresh-token"
        description = "Verify whether the refresh-token endpoint returns 401 response with invalid token"
        payload = @"
{
  "token": "invalid-token"
}
"@
        statusCode = 401
        response = @"
{
  "statusCode": 401,
  "message": "Invalid or expired token"
}
"@
    }
)

Write-Host "Writing test cases to Excel..."

# Write test cases
$row = 2
foreach ($tc in $testCases) {
    $worksheet.Cells.Item($row, 1).Value2 = $tc.id
    $worksheet.Cells.Item($row, 2).Value2 = $tc.endpoint
    $worksheet.Cells.Item($row, 3).Value2 = $tc.description
    $worksheet.Cells.Item($row, 4).Value2 = $tc.payload
    $worksheet.Cells.Item($row, 5).Value2 = $tc.statusCode
    $worksheet.Cells.Item($row, 6).Value2 = $tc.response
    $worksheet.Cells.Item($row, 7).Value2 = ""
    $worksheet.Cells.Item($row, 8).Value2 = ""
    
    # Apply borders and wrap text
    for ($col = 1; $col -le 8; $col++) {
        $cell = $worksheet.Cells.Item($row, $col)
        $cell.WrapText = $true
        $cell.VerticalAlignment = -4160  # Top
        $cell.Borders.Weight = 2
    }
    
    $row++
}

Write-Host "Formatting columns..."

# Set column widths
$worksheet.Columns.Item(1).ColumnWidth = 15   # Test Case ID
$worksheet.Columns.Item(2).ColumnWidth = 40   # Endpoint Name
$worksheet.Columns.Item(3).ColumnWidth = 60   # Test Case Description
$worksheet.Columns.Item(4).ColumnWidth = 50   # Request Payload
$worksheet.Columns.Item(5).ColumnWidth = 20   # Expected Status Code
$worksheet.Columns.Item(6).ColumnWidth = 50   # Expected Response
$worksheet.Columns.Item(7).ColumnWidth = 15   # Status
$worksheet.Columns.Item(8).ColumnWidth = 30   # Comments

# Freeze header row
$excel.ActiveWindow.SplitRow = 1
$excel.ActiveWindow.FreezePanes = $true

# Apply borders to all cells
$usedRange = $worksheet.UsedRange
$usedRange.Borders.Weight = 2

Write-Host "Saving file..."

# Save and close
$filePath = Join-Path (Get-Location) "Fintech_API_Test_Cases_Part1.xlsx"
$workbook.SaveAs($filePath)
$workbook.Close()
$excel.Quit()

# Release COM objects
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($worksheet) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($workbook) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
[System.GC]::Collect()
[System.GC]::WaitForPendingFinalizers()

Write-Host "Excel file created successfully at: $filePath"
Write-Host "Total test cases in Part 1: $($testCases.Count)"
Write-Host ""
Write-Host "NOTE: This is Part 1 with first 10 test cases. Creating remaining test cases..."
