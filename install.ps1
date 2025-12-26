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

# Resolve a Python executable
$PythonCandidates = @()
if ($Env:PYTHON) { $PythonCandidates += $Env:PYTHON }
$PythonCandidates += @("python", "py", "python3")
$Python = $PythonCandidates | Where-Object { (Get-Command $_ -ErrorAction SilentlyContinue) } | Select-Object -First 1
if (-not $Python) {
    Write-Host "[install] Python not found on PATH. Install Python 3.10+ and retry."
    exit 1
}

if (-not $Env:VIRTUAL_ENV) {
    $VenvDir = Join-Path $RepoRoot ".venv"
    if (-not (Test-Path $VenvDir)) {
        Write-Host "[install] Creating virtualenv at $VenvDir"
        & $Python -m venv $VenvDir
    }
    $Activate = Join-Path $VenvDir "Scripts\Activate.ps1"
    if (-not (Test-Path $Activate)) {
        Write-Host "[install] venv activation script not found at $Activate"
        exit 1
    }
    & $Activate
}

Write-Host "[install] Upgrading pip and installing hbc_py (editable)"
& $Python -m pip install --upgrade pip
& $Python -m pip install -e "$RepoRoot\hbc_py"

Write-Host "[install] Starting REST API from published build..."
# Ensure dotnet is available before launching API
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "[install] dotnet SDK not found. Install .NET 8 SDK and retry."
    exit 1
}

& "$RepoRoot\hbc_rest\scripts\run_prod.ps1"
