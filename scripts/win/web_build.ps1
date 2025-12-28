<#
  Build the Next.js app for production.
#>

$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$WebDir = Join-Path $RepoRoot "hbc_web"

# Load env defaults (HBC_API_URL is used by Next.js rewrites at build time).
$EnvScript = Join-Path $ScriptDir "env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "[web_build] npm not found. Please install Node.js/npm first."
    exit 1
}

if (-not (Test-Path $WebDir)) {
    Write-Host "[web_build] $WebDir not found. Did you clone the repo?"
    exit 1
}

Push-Location $WebDir
if (-not (Test-Path "node_modules")) {
    Write-Host "[web_build] Installing dependencies..."
    npm install
}

Write-Host "[web_build] Building Next.js production bundle..."
npm run build
Pop-Location
