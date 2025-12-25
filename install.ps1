<#
  One-shot installer/runner for Windows PowerShell:
  - loads env defaults
  - creates/activates .venv if not active
  - installs hbc_py in editable mode
  - starts published REST API via hbc_rest\scripts\run_prod.ps1
#>

$RepoRoot = $PSScriptRoot

# Load env defaults
. "$RepoRoot\scripts\env.ps1"

$Python = $Env:PYTHON
if (-not $Python) { $Python = "python" }

if (-not $Env:VIRTUAL_ENV) {
    $VenvDir = Join-Path $RepoRoot ".venv"
    if (-not (Test-Path $VenvDir)) {
        Write-Host "[install] Creating virtualenv at $VenvDir"
        & $Python -m venv $VenvDir
    }
    & "$VenvDir\Scripts\Activate.ps1"
}

Write-Host "[install] Upgrading pip and installing hbc_py (editable)"
pip install --upgrade pip
pip install -e "$RepoRoot\hbc_py"

Write-Host "[install] Starting REST API from published build..."
& "$RepoRoot\hbc_rest\scripts\run_prod.ps1"
