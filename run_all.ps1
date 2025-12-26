<#
  Start REST API (published build) and demo notebook in background,
  leaving this session free for running jobs.
#>

$RepoRoot = $PSScriptRoot

# Load env defaults.
$EnvScript = Join-Path $RepoRoot "scripts/env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

# Ensure venv exists (basic install check) and activate.
$VenvDir = Join-Path $RepoRoot ".venv"
$VenvPy = Join-Path $VenvDir "Scripts\python.exe"
if (-not (Test-Path $VenvPy)) {
    Write-Host "[run_all] .venv not found. Please run .\install.ps1 first."
    exit 1
}
$Activate = Join-Path $VenvDir "Scripts\Activate.ps1"
if (Test-Path $Activate) { . $Activate }

$LogDir = Join-Path $RepoRoot "logs"
if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir | Out-Null }

Write-Host "[run_all] Starting REST API (logs: $LogDir\rest.log)"
$restProc = Start-Process -FilePath "powershell" -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-File", (Join-Path $RepoRoot "hbc_rest\scripts\run_prod.ps1") -RedirectStandardOutput "$LogDir\rest.log" -RedirectStandardError "$LogDir\rest.log" -PassThru -WindowStyle Hidden

Write-Host "[run_all] Starting demo notebook (logs: $LogDir\notebook.log)"
$nbProc = Start-Process -FilePath "powershell" -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-File", (Join-Path $RepoRoot "hbc_py\scripts\run_demo_notebook.ps1") -RedirectStandardOutput "$LogDir\notebook.log" -RedirectStandardError "$LogDir\notebook.log" -PassThru -WindowStyle Hidden

Write-Host "[run_all] PIDs -> REST: $($restProc.Id), Notebook: $($nbProc.Id)"
Write-Host "[run_all] Use 'Stop-Process <PID>' to stop services. Shell remains free for job commands (venv activated)."
