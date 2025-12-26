<#
  Start REST API (published build) and demo notebook in separate PowerShell windows,
  leaving this session free for running jobs.
#>

$RepoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path

# Load env defaults.
$EnvScript = Join-Path $PSScriptRoot "env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

# Ensure venv exists (basic install check) and activate.
$VenvDir = Join-Path $RepoRoot ".venv"
$VenvPy = Join-Path $VenvDir "Scripts\python.exe"
if (-not (Test-Path $VenvPy)) {
    Write-Host "[run_all] .venv not found. Please run .\scripts\win\install.ps1 first."
    exit 1
}
$Activate = Join-Path $VenvDir "Scripts\Activate.ps1"
if (Test-Path $Activate) { . $Activate }

$LogDir = Join-Path $RepoRoot "logs"
if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir | Out-Null }

# Build commands to run in new windows.
$restScript = "Set-Location '$RepoRoot'; . scripts/win/env.ps1; & '$Activate'; & 'scripts/win/rest_start_prod.ps1'"
$nbScript   = "Set-Location '$RepoRoot'; . scripts/win/env.ps1; & '$Activate'; & 'scripts/win/run_demo_notebook.ps1'"

Write-Host "[run_all] Starting REST API in new window..."
Start-Process -FilePath "powershell" -ArgumentList "-NoExit","-NoProfile","-ExecutionPolicy","Bypass","-Command", $restScript -WindowStyle Normal | Out-Null

Write-Host "[run_all] Starting demo notebook in new window..."
Start-Process -FilePath "powershell" -ArgumentList "-NoExit","-NoProfile","-ExecutionPolicy","Bypass","-Command", $nbScript -WindowStyle Normal | Out-Null

Write-Host "[run_all] Two PowerShell windows started (REST API + notebook). This window remains free for job commands."
