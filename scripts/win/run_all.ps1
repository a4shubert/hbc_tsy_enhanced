<#
  Start REST API (published build), web app (prod), and demo notebook in separate PowerShell windows,
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
$webScript  = "Set-Location '$RepoRoot'; . scripts/win/env.ps1; & 'scripts/win/web_start_prod.ps1'"
$nbScript   = "Set-Location '$RepoRoot'; . scripts/win/env.ps1; & '$Activate'; & 'scripts/win/run_demo_notebook.ps1'"

Write-Host "[run_all] Starting REST API in new window..."
Start-Process -FilePath "powershell" -ArgumentList "-NoExit","-NoProfile","-ExecutionPolicy","Bypass","-Command", $restScript -WindowStyle Normal | Out-Null

Write-Host "[run_all] Starting web app (prod) in new window..."
Start-Process -FilePath "powershell" -ArgumentList "-NoExit","-NoProfile","-ExecutionPolicy","Bypass","-Command", $webScript -WindowStyle Normal | Out-Null

if ($Env:HBC_WEB_URL) {
    Start-Sleep -Seconds 3
    Write-Host "[run_all] Opening browser at $Env:HBC_WEB_URL..."
    Start-Process $Env:HBC_WEB_URL | Out-Null
}

Write-Host "[run_all] Starting demo notebook in new window..."
Start-Process -FilePath "powershell" -ArgumentList "-NoExit","-NoProfile","-ExecutionPolicy","Bypass","-Command", $nbScript -WindowStyle Normal | Out-Null

Write-Host "[run_all] Three PowerShell windows started (REST API + web + notebook). This window remains free for job commands."
