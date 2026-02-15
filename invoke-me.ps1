param(
    [string]$BaseUrl = "https://localhost:7261"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path "session.json")) {
    Write-Host "session.json not found. Run invoke-login.ps1 and invoke-mfa.ps1 first." -ForegroundColor Red
    exit 1
}

$session = Get-Content "session.json" | ConvertFrom-Json
$headers = @{
    Authorization = "Bearer $($session.accessToken)"
}

try {
    $result = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/me" -Method GET -Headers $headers
    $result | ConvertTo-Json -Depth 10 | Set-Content "user-info.json"
    Write-Host "GET /auth/me successful. Saved to user-info.json" -ForegroundColor Green
}
catch {
    Write-Host "GET /auth/me failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
