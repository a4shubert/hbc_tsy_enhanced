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
$Python = ""
$VenvPy = Join-Path $RepoRoot ".venv\Scripts\python.exe"
if ($Env:VIRTUAL_ENV -and (Test-Path (Join-Path $Env:VIRTUAL_ENV "Scripts\python.exe"))) {
    $Python = Join-Path $Env:VIRTUAL_ENV "Scripts\python.exe"
} elseif (Test-Path $VenvPy) {
    $Python = $VenvPy
} elseif ($Env:CONDA_PYTHON_EXE -and (Test-Path $Env:CONDA_PYTHON_EXE)) {
    $Python = $Env:CONDA_PYTHON_EXE
} elseif (Get-Command conda -ErrorAction SilentlyContinue) {
    try {
        $base = (& conda info --base)
        $condaPy = Join-Path $base "python.exe"
        if (Test-Path $condaPy) { $Python = $condaPy }
    } catch {}
} elseif (Get-Command python -ErrorAction SilentlyContinue) {
    $Python = "python"
} elseif (Get-Command py -ErrorAction SilentlyContinue) {
    $Python = "py"
}

if (-not $Python) {
    Write-Host "[demo] Python not found. Install Python 3.10+ and rerun."
    exit 1
}

try {
    & $Python -c "import jupyter" 2>$null | Out-Null
} catch {
    Write-Host "[demo] jupyter not found in $Python. Installing notebook/nbclassic..."
    & $Python -m pip install notebook nbclassic
}

Write-Host "[demo] Starting nbclassic in $NotebookDir"
& $Python -m jupyter nbclassic `
  --NotebookApp.open_browser=True `
  --ServerApp.root_dir="$NotebookDir" `
  --NotebookApp.default_url="/tree/$DefaultNotebook"
