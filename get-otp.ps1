# Helper script to extract OTP from SQL query output
# Run the SQL query first, then pipe the output to this script

param(
    [Parameter(ValueFromPipeline = $true)]
    [string]$InputLine
)

begin {
    $otpPattern = 'Your OTP code is: (\d{6})'
    $foundOtp = $null
}

process {
    if ($InputLine -match $otpPattern) {
        $foundOtp = $matches[1]
        Write-Host "Latest OTP found: $foundOtp" -ForegroundColor Green
        Write-Host "Run this command:" -ForegroundColor Yellow
        Write-Host "powershell -ExecutionPolicy Bypass -File .\invoke-mfa.ps1 -OtpCode $foundOtp" -ForegroundColor Cyan
    }
}

end {
    if (-not $foundOtp) {
        Write-Host "No OTP found in input. Make sure to run the SQL query:" -ForegroundColor Yellow
        Write-Host "USE [NciemsDb.Dev];" -ForegroundColor Gray
        Write-Host "SELECT TOP 1 n.Message, n.CreatedAtUtc" -ForegroundColor Gray
        Write-Host "FROM Notifications n" -ForegroundColor Gray
        Write-Host "JOIN Users u ON u.Id = n.UserId" -ForegroundColor Gray
        Write-Host "WHERE u.UserName = 'admin@govportal.com' OR u.Email = 'admin@govportal.com'" -ForegroundColor Gray
        Write-Host "ORDER BY n.CreatedAtUtc DESC;" -ForegroundColor Gray
    }
}
