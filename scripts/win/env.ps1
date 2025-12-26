<#
  Common environment setup for Windows/PowerShell runs (API + notebooks).
  Usage:
    .\scripts\win\env.ps1
#>

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

if (-not $Env:HBC_DB_PATH -or [string]::IsNullOrWhiteSpace($Env:HBC_DB_PATH)) {
    $Env:HBC_DB_PATH = Join-Path $RepoRoot "hbc_db/hbc.db"
}
if (-not $Env:HBC_API_URL -or [string]::IsNullOrWhiteSpace($Env:HBC_API_URL)) {
    $Env:HBC_API_URL = "http://localhost:5047"
}
if (-not $Env:ASPNETCORE_URLS -or [string]::IsNullOrWhiteSpace($Env:ASPNETCORE_URLS)) {
    $Env:ASPNETCORE_URLS = $Env:HBC_API_URL
}
if (-not $Env:ASPNETCORE_ENVIRONMENT -or [string]::IsNullOrWhiteSpace($Env:ASPNETCORE_ENVIRONMENT)) {
    $Env:ASPNETCORE_ENVIRONMENT = "Production"
}

Write-Host "[env] HBC_DB_PATH=$Env:HBC_DB_PATH"
Write-Host "[env] HBC_API_URL=$Env:HBC_API_URL"
Write-Host "[env] ASPNETCORE_URLS=$Env:ASPNETCORE_URLS"
Write-Host "[env] ASPNETCORE_ENVIRONMENT=$Env:ASPNETCORE_ENVIRONMENT"
