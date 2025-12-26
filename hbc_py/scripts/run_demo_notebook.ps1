<#
  Launch classic Jupyter (nbclassic) in the notebooks folder and open Demo.ipynb.
  Requires notebook/nbclassic installed (included in hbc_py deps).
#>

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$NotebookDir = Join-Path $RepoRoot "hbc_py/notebooks"
$DefaultNotebook = "Demo.ipynb"

# Load env defaults if available.
$EnvScript = Join-Path $RepoRoot "scripts/env.ps1"
if (Test-Path $EnvScript) {
    . $EnvScript
}

# Resolve Python to run jupyter from (prefer repo venv).
# Ensure repo venv exists; create if missing.
$VenvPy = Join-Path $RepoRoot ".venv\Scripts\python.exe"
if (-not (Test-Path $VenvPy)) {
    $BootPy = ""
    if (Get-Command py -ErrorAction SilentlyContinue) { $BootPy = "py" }
    elseif (Get-Command python -ErrorAction SilentlyContinue) { $BootPy = "python" }
    if (-not $BootPy) {
        Write-Host "[demo] Python not found. Install Python 3.10+ and rerun."
        exit 1
    }
    Write-Host "[demo] Creating venv at $($RepoRoot)\.venv using $BootPy"
    & $BootPy -m venv (Join-Path $RepoRoot ".venv")
}

$Python = $VenvPy

try {
    & $Python -c "import jupyter" 2>$null | Out-Null
} catch {
    Write-Host "[demo] jupyter not found in $Python. Installing notebook/nbclassic/ipykernel..."
    & $Python -m pip install notebook nbclassic ipykernel
}

# Register kernel for this venv (best-effort).
try {
    & $Python -m ipykernel install --user --name hbc-venv --display-name "Python (hbc)" 2>$null | Out-Null
} catch {}

Write-Host "[demo] Starting nbclassic in $NotebookDir"
& $Python -m jupyter nbclassic `
  --NotebookApp.open_browser=True `
  --ServerApp.root_dir="$NotebookDir" `
  --NotebookApp.default_url="/tree/$DefaultNotebook"
