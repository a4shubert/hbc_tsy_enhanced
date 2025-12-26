<#
  Source this file to activate the project virtualenv and env vars in the current PowerShell session.
  Usage: .\scripts\activate_venv.ps1   (from repo root)
#>

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Resolve-Path (Join-Path $ScriptDir "..")

# Load env defaults if present.
$EnvScript = Join-Path $RepoRoot "scripts/env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

$VenvDir   = Join-Path $RepoRoot ".venv"
$Activate  = Join-Path $VenvDir "Scripts\Activate.ps1"

if (-not (Test-Path $Activate)) {
    Write-Host "[activate_venv] .venv not found. Run .\install.ps1 first."
    exit 1
}

. $Activate
Write-Host "[activate_venv] Activated $env:VIRTUAL_ENV"
