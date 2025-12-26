<#
  Run the published Release build of HbcRest on Windows.
  Assumes publish output is in /publish under the repo root.
#>

$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$PublishDir = Join-Path $RepoRoot "publish"

# Load env defaults if available.
$EnvScript = Join-Path $ScriptDir "env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

if (-not (Test-Path $PublishDir)) {
    Write-Host "[rest_start_prod] publish directory not found at $PublishDir. Run scripts/linux/rest_build.sh or scripts/win/rest_build.ps1 first."
    exit 1
}

Push-Location $PublishDir
dotnet HbcRest.dll
Pop-Location
