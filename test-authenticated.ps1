# Test protected endpoints using session.json

$ErrorActionPreference = "Stop"

if (-not (Test-Path "session.json")) {
    Write-Host "session.json not found. Run invoke-login.ps1 + invoke-mfa.ps1 first." -ForegroundColor Red
    exit 1
}

$session = Get-Content "session.json" | ConvertFrom-Json
$headers = @{
    Authorization = "Bearer $($session.accessToken)"
    "Content-Type" = "application/json"
}

Write-Host "=== Authenticated Endpoint Test ===" -ForegroundColor Green
Write-Host "User: $($session.userName)" -ForegroundColor Cyan
Write-Host "Roles: $($session.roles -join ', ')" -ForegroundColor Cyan
Write-Host ""

Write-Host "1) GET /api/v1/auth/me" -ForegroundColor Yellow
try {
    $me = Invoke-RestMethod -Uri "https://localhost:7261/api/v1/auth/me" -Method GET -Headers $headers
    $me | ConvertTo-Json -Depth 10 | Set-Content "user-info.json"
    Write-Host "   OK (saved: user-info.json)" -ForegroundColor Green
}
catch {
    Write-Host "   FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "2) Other protected endpoints" -ForegroundColor Yellow
$endpoints = @(
    "/api/v1/complaints",
    "/api/v1/cases",
    "/api/v1/reports/case/1",
    "/api/v1/search"
)

foreach ($ep in $endpoints) {
    try {
        Invoke-RestMethod -Uri ("https://localhost:7261{0}" -f $ep) -Method GET -Headers $headers | Out-Null
        Write-Host ("   {0} -> OK" -f $ep) -ForegroundColor Green
    }
    catch {
        $status = ""
        try { $status = [int]$_.Exception.Response.StatusCode } catch {}
        if ([string]::IsNullOrWhiteSpace($status)) { $status = "ERR" }
        Write-Host ("   {0} -> {1}" -f $ep, $status) -ForegroundColor Yellow
    }
}
