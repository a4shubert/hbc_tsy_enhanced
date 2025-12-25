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

if (-not (Get-Command jupyter -ErrorAction SilentlyContinue)) {
    Write-Host "[demo] jupyter not found on PATH. Install with: pip install notebook nbclassic"
    exit 1
}

Write-Host "[demo] Starting nbclassic in $NotebookDir"
jupyter nbclassic `
  --NotebookApp.open_browser=True `
  --ServerApp.root_dir="$NotebookDir" `
  --NotebookApp.default_url="/tree/$DefaultNotebook"
