<#
  Run the app in Development using the launch profile "HbcRest".
#>

$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$ProjectDir = Join-Path $RepoRoot "hbc_rest/HbcRest"

# Load env defaults if available.
$EnvScript = Join-Path $ScriptDir "env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

Push-Location $ProjectDir
dotnet run --launch-profile "HbcRest"
Pop-Location
