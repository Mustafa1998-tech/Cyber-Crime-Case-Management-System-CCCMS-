# Test API endpoints with PowerShell
$headers = @{
    'Content-Type' = 'application/json'
}

# Test 1: Login endpoint
$loginBody = @{
    userName = "admin@govportal.com"
    password = "GovPortal@2026!Secure"
    deviceInfo = "PowerShell Test"
} | ConvertTo-Json

Write-Host "Testing login endpoint..."
try {
    $loginResponse = Invoke-RestMethod -Uri "https://localhost:7261/api/v1/auth/login" -Method POST -Headers $headers -Body $loginBody
    Write-Host "Login Response:"
    $loginResponse | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Login Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
        $errorBody = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorBody)
        $errorText = $reader.ReadToEnd()
        Write-Host "Error Details: $errorText"
    }
}

# Test 2: Protected endpoint (should fail without auth)
Write-Host "`nTesting protected endpoint without auth..."
try {
    $meResponse = Invoke-RestMethod -Uri "https://localhost:7261/api/v1/auth/me" -Method GET -Headers $headers
    Write-Host "ME Response: $meResponse"
} catch {
    Write-Host "Expected auth error: $($_.Exception.Message)"
}
