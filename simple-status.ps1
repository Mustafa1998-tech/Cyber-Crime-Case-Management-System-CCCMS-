# Simple API Status Test
Write-Host "=== NCIEMS API Status ===" -ForegroundColor Green

$headers = @{
    'Content-Type' = 'application/json'
}

# Test login
$loginBody = @{
    userName = "admin@govportal.com"
    password = "GovPortal@2026!Secure"
    deviceInfo = "PowerShell Status Check"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "https://localhost:7261/api/v1/auth/login" -Method POST -Headers $headers -Body $loginBody
    Write-Host "Login: SUCCESS" -ForegroundColor Green
    Write-Host "User: $($loginResponse.userName)" -ForegroundColor Cyan
    Write-Host "Roles: $($loginResponse.roles -join ', ')" -ForegroundColor Cyan
    Write-Host "MFA Required: $($loginResponse.mfaRequired)" -ForegroundColor Cyan
} catch {
    Write-Host "Login: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}

# Test OpenAPI
try {
    $openApiResponse = Invoke-RestMethod -Uri "https://localhost:7261/openapi/v1.json" -Method GET -Headers $headers
    Write-Host "OpenAPI: AVAILABLE - $($openApiResponse.info.title)" -ForegroundColor Green
} catch {
    Write-Host "OpenAPI: NOT AVAILABLE" -ForegroundColor Red
}

Write-Host "API URL: https://localhost:7261" -ForegroundColor Cyan
Write-Host "Frontend URL: http://localhost:4201" -ForegroundColor Cyan
