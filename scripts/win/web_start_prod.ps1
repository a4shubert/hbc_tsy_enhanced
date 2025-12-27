<#
  Run the Next.js production server (requires prior web_build).
#>

$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$WebDir = Join-Path $RepoRoot "hbc_web"

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "[web_start_prod] npm not found. Please install Node.js/npm first."
    exit 1
}

if (-not (Test-Path $WebDir)) {
    Write-Host "[web_start_prod] $WebDir not found. Did you clone the repo?"
    exit 1
}

Push-Location $WebDir
if (-not (Test-Path "node_modules")) {
    Write-Host "[web_start_prod] Installing dependencies..."
    npm install
}

if (-not (Test-Path ".next")) {
    Write-Host "[web_start_prod] .next not found. Running build first..."
    npm run build
}

Write-Host "[web_start_prod] Starting Next.js production server..."
npm run start
Pop-Location
