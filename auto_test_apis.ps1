$VerbosePreference = "Continue"
$baseUrl = "http://localhost:5177"

# 1. Login to get token
$loginBody = @{
    email = "super_admin@finveda.com"
    password = "Admin@123"
} | ConvertTo-Json

Write-Verbose "Logging in as super_admin..."
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/Auth/login" -Method POST -ContentType "application/json" -Body $loginBody
$token = $loginResponse.token

if (-not $token) {
    Write-Error "Failed to get token"
    exit 1
}

$headers = @{
    Authorization = "Bearer $token"
}

# 2. Get Swagger
$swagger = Get-Content "C:\Users\sanjeevi\Downloads\Fintech\swagger.json" | ConvertFrom-Json

$getEndpoints = @()
foreach ($pathObj in $swagger.paths.PSObject.Properties) {
    $path = $pathObj.Name
    $methods = $pathObj.Value
    
    if ($methods.PSObject.Properties.Name -contains "get") {
        # Skip endpoints that have explicit path parameters (like {id}) for this basic automated test
        if ($path -notmatch "\{.*?\}") {
            $getEndpoints += $path
        }
    }
}

Write-Verbose "Found $($getEndpoints.Count) GET endpoints without path parameters."

$report = @()

# 3. Test Endpoints
foreach ($endpoint in $getEndpoints) {
    $url = "$baseUrl$endpoint"
    Write-Verbose "Testing GET $endpoint"
    
    try {
        $response = Invoke-WebRequest -Uri $url -Method GET -Headers $headers -ErrorAction Stop
        
        $report += [PSCustomObject]@{
            Endpoint = $endpoint
            StatusCode = $response.StatusCode
            Status = "OK"
        }
    }
    catch {
        $errResponse = $_.Exception.Response
        $statusCode = if ($errResponse) { $errResponse.StatusCode.value__ } else { "Unknown" }
        
        $report += [PSCustomObject]@{
            Endpoint = $endpoint
            StatusCode = $statusCode
            Status = "Error: $($_.Exception.Message)"
        }
    }
}

$report | Format-Table -AutoSize
$successCount = ($report | Where-Object { $_.StatusCode -eq 200 }).Count
$errorCount = $report.Count - $successCount

Write-Host "`nTest Result: $successCount Succeeded, $errorCount Failed / Warnings"
