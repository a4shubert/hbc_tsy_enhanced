<#
  Start REST API (published build) and demo notebook in separate PowerShell windows,
  leaving this session free for running jobs.
#>

$RepoRoot = $PSScriptRoot

# Load env defaults.
$EnvScript = Join-Path $RepoRoot "scripts/env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

# Ensure venv exists and activate.
$VenvDir = Join-Path $RepoRoot ".venv"
$VenvPy = Join-Path $VenvDir "Scripts\python.exe"
if (-not (Test-Path $VenvPy)) {
    $BootPy = ""
    if (Get-Command py -ErrorAction SilentlyContinue) { $BootPy = "py" }
    elseif (Get-Command python -ErrorAction SilentlyContinue) { $BootPy = "python" }
    if (-not $BootPy) {
        Write-Host "[run_all] Python not found. Install Python 3.10+ and rerun."
        exit 1
    }
    & $BootPy -m venv $VenvDir
}
$Activate = Join-Path $VenvDir "Scripts\Activate.ps1"
if (Test-Path $Activate) { . $Activate }

$restScript = Join-Path $RepoRoot "hbc_rest\scripts\run_prod.ps1"
$nbScript = Join-Path $RepoRoot "hbc_py\scripts\run_demo_notebook.ps1"
$envCall = ". `"$RepoRoot\scripts\env.ps1`"; . `"$RepoRoot\.venv\Scripts\Activate.ps1`";"

Write-Host "[run_all] Opening REST API in new PowerShell window..."
$restProc = Start-Process -FilePath "powershell" -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-NoExit","-Command","cd `"$RepoRoot`"; $envCall & `"$restScript`"" -PassThru

Write-Host "[run_all] Opening notebook in new PowerShell window..."
$nbProc = Start-Process -FilePath "powershell" -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-NoExit","-Command","cd `"$RepoRoot`"; $envCall & `"$nbScript`"" -PassThru

Write-Host "[run_all] REST PID: $($restProc.Id); Notebook PID: $($nbProc.Id)"
Write-Host "[run_all] Close the spawned windows or Stop-Process to stop services. This shell stays free."
