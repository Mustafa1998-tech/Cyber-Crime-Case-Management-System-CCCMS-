param(
    [Parameter(Mandatory = $true)]
    [string]$OtpCode,
    [string]$BaseUrl = "https://localhost:7261",
    [string]$UserName = "admin@govportal.com"
)

$ErrorActionPreference = "Stop"

function Get-StatusCodeFromError {
    param([Parameter(Mandatory = $true)]$ErrorRecord)
    try {
        if ($ErrorRecord.Exception.Response -and $ErrorRecord.Exception.Response.StatusCode) {
            return [int]$ErrorRecord.Exception.Response.StatusCode
        }
    } catch {}
    return -1
}

Write-Host "=== MFA Verification ===" -ForegroundColor Green
Write-Host "URL: $BaseUrl/api/v1/auth/verify-mfa" -ForegroundColor Cyan
Write-Host "User: $UserName" -ForegroundColor Cyan

$headers = @{
    "Content-Type" = "application/json"
}

$bodyObject = @{
    userName = $UserName
    otpCode = $OtpCode
}

$bodyJson = $bodyObject | ConvertTo-Json -Compress

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/verify-mfa" -Method POST -Headers $headers -Body $bodyJson

    $response | ConvertTo-Json -Depth 10 | Set-Content -Path "mfa-response.json"

    if ([string]::IsNullOrWhiteSpace($response.accessToken)) {
        Write-Host "MFA call succeeded but no token was returned." -ForegroundColor Yellow
        Write-Host "Response saved to: mfa-response.json" -ForegroundColor Cyan
        exit 1
    }

    $session = @{
        accessToken = $response.accessToken
        refreshToken = $response.refreshToken
        userName = $response.userName
        roles = $response.roles
        accessTokenExpiresAtUtc = $response.accessTokenExpiresAtUtc
    }

    $session | ConvertTo-Json -Depth 10 | Set-Content -Path "session.json"

    Write-Host "MFA verified. Tokens issued." -ForegroundColor Green
    Write-Host "Saved files: mfa-response.json, session.json" -ForegroundColor Cyan
    Write-Host "Use token from session.json for protected endpoints." -ForegroundColor Cyan
}
catch {
    $status = Get-StatusCodeFromError -ErrorRecord $_
    Write-Host "MFA verification failed. Status: $status" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
