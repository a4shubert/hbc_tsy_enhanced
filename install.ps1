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

# Check for optional tools.
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "[install] Warning: dotnet SDK not found. Install .NET 8 SDK if you plan to run the REST API:"
    Write-Host "  https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
}

if (-not (Get-Command conda -ErrorAction SilentlyContinue)) {
    Write-Host "[install] Warning: conda/miniconda not found. If you prefer conda, install Miniconda:"
    Write-Host "  https://docs.conda.io/en/latest/miniconda.html"
}

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

Write-Host "[install] Opening a new PowerShell with env + venv activated..."
$LaunchCmd = "cd `"$RepoRoot`"; . `"$RepoRoot\scripts\env.ps1`"; . `"$RepoRoot\.venv\Scripts\Activate.ps1`"; Write-Host 'env + venv activated; run jobs here.'"
Start-Process -FilePath "powershell" -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-NoExit","-Command",$LaunchCmd
Write-Host "[install] New window started. Use that window for running jobs."
