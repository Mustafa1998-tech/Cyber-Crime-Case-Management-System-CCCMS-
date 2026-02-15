# Test MFA and other endpoints
$headers = @{
    'Content-Type' = 'application/json'
}

# Test MFA verification (will fail without valid OTP)
$mfaBody = @{
    userName = "admin@govportal.com"
    otpCode = "123456"  # Invalid OTP for testing
} | ConvertTo-Json

Write-Host "Testing MFA verification with invalid OTP..."
try {
    $mfaResponse = Invoke-RestMethod -Uri "https://localhost:7261/api/v1/auth/verify-mfa" -Method POST -Headers $headers -Body $mfaBody
    Write-Host "MFA Response: $($mfaResponse | ConvertTo-Json -Depth 10)"
} catch {
    Write-Host "Expected MFA error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
    }
}

# Test registration endpoint
$registerBody = @{
    userName = "testuser"
    email = "test@example.com"
    password = "TestPassword123!"
    mfaEnabled = $false
    roles = @("Investigator")
} | ConvertTo-Json

Write-Host "`nTesting registration endpoint..."
try {
    $registerResponse = Invoke-RestMethod -Uri "https://localhost:7261/api/v1/auth/register" -Method POST -Headers $headers -Body $registerBody
    Write-Host "Registration Response: User ID created - $($registerResponse)"
} catch {
    Write-Host "Registration Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $errorBody = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorBody)
        $errorText = $reader.ReadToEnd()
        Write-Host "Error Details: $errorText"
    }
}

# Test refresh token endpoint (will fail without valid refresh token)
$refreshBody = @{
    refreshToken = "invalid-refresh-token"
} | ConvertTo-Json

Write-Host "`nTesting refresh token endpoint with invalid token..."
try {
    $refreshResponse = Invoke-RestMethod -Uri "https://localhost:7261/api/v1/auth/refresh" -Method POST -Headers $headers -Body $refreshBody
    Write-Host "Refresh Response: $($refreshResponse | ConvertTo-Json -Depth 10)"
} catch {
    Write-Host "Expected refresh error: $($_.Exception.Message)"
}
