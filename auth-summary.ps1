# Authentication summary (ASCII-only to avoid console encoding issues)

$ErrorActionPreference = "Stop"

Write-Host "=== NCIEMS Authentication Summary ===" -ForegroundColor Green
Write-Host ""

if (-not (Test-Path "session.json")) {
    Write-Host "No session found. Run .\invoke-login.ps1 then .\invoke-mfa.ps1 first." -ForegroundColor Red
    exit 1
}

$session = Get-Content "session.json" | ConvertFrom-Json
$expiryUtc = [DateTime]::Parse($session.accessTokenExpiresAtUtc)
$remaining = $expiryUtc - [DateTime]::UtcNow

Write-Host "Status: AUTHENTICATED" -ForegroundColor Green
Write-Host "User: $($session.userName)" -ForegroundColor Cyan
Write-Host "Roles: $($session.roles -join ', ')" -ForegroundColor Cyan
Write-Host "Token Expiry (UTC): $($expiryUtc.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Cyan
Write-Host ("Time Remaining: {0}m {1}s" -f $remaining.Minutes, $remaining.Seconds) -ForegroundColor Cyan
Write-Host ""

$headers = @{
    Authorization = "Bearer $($session.accessToken)"
}

try {
    Invoke-RestMethod -Uri "https://localhost:7261/api/v1/auth/me" -Method GET -Headers $headers | Out-Null
    Write-Host "Token validity check: OK" -ForegroundColor Green
}
catch {
    Write-Host "Token validity check: FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Files:" -ForegroundColor Yellow
if (Test-Path "login-response.json") { Write-Host "  - login-response.json" }
if (Test-Path "mfa-response.json") { Write-Host "  - mfa-response.json" }
if (Test-Path "session.json") { Write-Host "  - session.json" }
if (Test-Path "user-info.json") { Write-Host "  - user-info.json" }
