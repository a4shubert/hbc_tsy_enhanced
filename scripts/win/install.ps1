<#
  One-shot installer/runner for Windows PowerShell:
  - loads env defaults
  - creates/activates .venv if not active
  - installs hbc_py in editable mode
  - starts published REST API via scripts\win\rest_start_prod.ps1
#>

$RepoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path

# Load env defaults
. "$PSScriptRoot\env.ps1"

# Check prerequisites (do not proceed if missing).
$Missing = $false

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "[install] Missing required dependency: .NET SDK (8.x)."
    Write-Host "  https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
    $Missing = $true
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "[install] Missing required dependency: npm (Node.js)."
    Write-Host "  https://nodejs.org/en/download"
    $Missing = $true
}

# Optional tools.
if (-not (Get-Command conda -ErrorAction SilentlyContinue)) {
    Write-Host "[install] Note: conda/miniconda not found. If you prefer conda, install Miniconda:"
    Write-Host "  https://docs.conda.io/en/latest/miniconda.html"
}

# Resolve a Python executable
$PythonCandidates = @()
if ($Env:PYTHON) { $PythonCandidates += $Env:PYTHON }
$PythonCandidates += @("python", "py", "python3")
$Python = $PythonCandidates | Where-Object { (Get-Command $_ -ErrorAction SilentlyContinue) } | Select-Object -First 1
if (-not $Python) {
    Write-Host "[install] Missing required dependency: python (3.10+ recommended)."
    Write-Host "  https://www.python.org/downloads/"
    $Missing = $true
}

if ($Missing) {
    Write-Host "[install] Aborting: install missing dependencies and retry."
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

Write-Host "[install] env + venv activated in this session. You can now run jobs or the REST API here."
