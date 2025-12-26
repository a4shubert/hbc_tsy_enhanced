<#
  Start the Next.js dev server for the web portal.
#>

$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$WebDir = Join-Path $RepoRoot "hbc_web"

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "[web_start_dev] npm not found. Please install Node.js/npm first."
    exit 1
}

if (-not (Test-Path $WebDir)) {
    Write-Host "[web_start_dev] $WebDir not found. Did you clone the repo?"
    exit 1
}

Push-Location $WebDir
if (-not (Test-Path "node_modules")) {
    Write-Host "[web_start_dev] Installing dependencies..."
    npm install
}

Write-Host "[web_start_dev] Starting Next.js dev server..."
npm run dev
Pop-Location
