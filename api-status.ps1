# NCIEMS API Status Test (PowerShell 5/7 compatible)

Write-Host "=== NCIEMS API Status Report ===" -ForegroundColor Green
Write-Host ""

$baseUrl = "https://localhost:7261"
$headers = @{
    "Content-Type" = "application/json"
}

# Test 1: Login
Write-Host "1) Testing login endpoint..." -ForegroundColor Yellow
$loginBody = @{
    userName = "admin@govportal.com"
    password = "GovPortal@2026!Secure"
    deviceInfo = "PowerShell Status Check"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" -Method POST -Headers $headers -Body $loginBody
    Write-Host "   OK: login request accepted" -ForegroundColor Green
    Write-Host "   User: $($loginResponse.userName)" -ForegroundColor Cyan
    Write-Host "   Roles: $($loginResponse.roles -join ', ')" -ForegroundColor Cyan
    Write-Host "   MFA Required: $($loginResponse.mfaRequired)" -ForegroundColor Cyan
}
catch {
    Write-Host "   FAIL: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Protected endpoint without token
Write-Host ""
Write-Host "2) Testing protected endpoint without token..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/me" -Method GET -Headers $headers | Out-Null
    Write-Host "   FAIL: expected unauthorized, got success" -ForegroundColor Red
}
catch {
    Write-Host "   OK: unauthorized as expected" -ForegroundColor Green
}

# Test 3: OpenAPI
Write-Host ""
Write-Host "3) Testing OpenAPI..." -ForegroundColor Yellow
try {
    $openApi = Invoke-RestMethod -Uri "$baseUrl/openapi/v1.json" -Method GET -Headers $headers
    Write-Host "   OK: OpenAPI reachable" -ForegroundColor Green
    Write-Host "   API: $($openApi.info.title)" -ForegroundColor Cyan
    Write-Host "   Version: $($openApi.info.version)" -ForegroundColor Cyan
}
catch {
    Write-Host "   FAIL: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Method probes
Write-Host ""
Write-Host "4) Probing methods on /auth/login..." -ForegroundColor Yellow
$methods = @("GET", "POST", "PUT", "DELETE", "OPTIONS")
foreach ($method in $methods) {
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/auth/login" -Method $method -Headers $headers -UseBasicParsing
        Write-Host ("   {0,-7} -> {1}" -f $method, $response.StatusCode) -ForegroundColor Green
    }
    catch {
        $status = $null
        try { $status = $_.Exception.Response.StatusCode.value__ } catch {}
        if ($null -eq $status) { $status = "ERR" }
        Write-Host ("   {0,-7} -> {1}" -f $method, $status) -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Green
Write-Host "API Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host "Frontend URL: http://localhost:4200" -ForegroundColor Cyan
Write-Host "Admin User: admin@govportal.com" -ForegroundColor Cyan
Write-Host "OpenAPI: $baseUrl/openapi/v1.json" -ForegroundColor Cyan
