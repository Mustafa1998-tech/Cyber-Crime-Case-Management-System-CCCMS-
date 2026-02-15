param(
    [string]$CertPath = "backend/src/Nciems.Api/certs/nciems-dev.pfx",
    [string]$Password = "NciemsDevCert@2026"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$absolutePath = Join-Path $root $CertPath
$certDir = Split-Path -Parent $absolutePath

if (-not (Test-Path $certDir)) {
    New-Item -ItemType Directory -Path $certDir -Force | Out-Null
}

dotnet dev-certs https -ep $absolutePath -p $Password | Out-Null
dotnet dev-certs https --trust | Out-Null

Write-Host "HTTPS certificate exported to: $absolutePath"
Write-Host "Certificate trusted for local development."
