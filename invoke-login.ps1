param(
    [string]$BaseUrl = "https://localhost:7261",
    [string]$UserName = "admin@govportal.com",
    [string]$Password = "GovPortal@2026!Secure",
    [string]$DeviceInfo = "PowerShell Invoke Test"
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

Write-Host "=== Login Test ===" -ForegroundColor Green
Write-Host "URL: $BaseUrl/api/v1/auth/login" -ForegroundColor Cyan
Write-Host "User: $UserName" -ForegroundColor Cyan

$headers = @{
    "Content-Type" = "application/json"
}

$bodyObject = @{
    userName = $UserName
    password = $Password
    deviceInfo = $DeviceInfo
}

$bodyJson = $bodyObject | ConvertTo-Json -Compress

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method POST -Headers $headers -Body $bodyJson

    $response | ConvertTo-Json -Depth 10 | Set-Content -Path "login-response.json"

    Write-Host "Login request accepted." -ForegroundColor Green
    Write-Host "MFA Required: $($response.mfaRequired)" -ForegroundColor Yellow
    Write-Host "Message: $($response.message)" -ForegroundColor Yellow
    Write-Host "Response saved to: login-response.json" -ForegroundColor Cyan

    if ($response.mfaRequired -eq $true) {
        Write-Host ""
        Write-Host "Next step:" -ForegroundColor Green
        Write-Host "1) Get OTP from SQL (Notifications table)." -ForegroundColor Cyan
        Write-Host "2) Run: .\invoke-mfa.ps1 -OtpCode <OTP>" -ForegroundColor Cyan
    } else {
        Write-Host ""
        Write-Host "Access token received. You can call protected endpoints." -ForegroundColor Green
    }
}
catch {
    $status = Get-StatusCodeFromError -ErrorRecord $_
    Write-Host "Login failed. Status: $status" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
