$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..\\..")

# Load env defaults (HBC_API_URL, etc).
$envFile = Join-Path $ScriptDir "env.ps1"
if (Test-Path $envFile) {
  . $envFile
}

# Activate venv (required for pytest + deps).
$activate = Join-Path $ScriptDir "activate_venv.ps1"
if (Test-Path $activate) {
  . $activate
} else {
  $fallback = Join-Path $RepoRoot ".venv\\Scripts\\Activate.ps1"
  if (Test-Path $fallback) {
    . $fallback
  }
}

$env:HBC_INTEGRATION = "1"
Write-Host "[py_run_tests] HBC_INTEGRATION=$($env:HBC_INTEGRATION)"

Push-Location (Join-Path $RepoRoot "hbc_py")
try {
  pytest -vv -s @args
} finally {
  Pop-Location
}

