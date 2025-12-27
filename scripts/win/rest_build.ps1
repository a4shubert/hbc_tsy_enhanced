<#
  Production build helper for HbcRest (Windows/PowerShell):
  - loads env defaults
  - cleans/restores
  - applies migrations if dotnet-ef is available
  - publishes Release to hbc_rest/publish
#>

$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$ProjectPath = Join-Path $RepoRoot "hbc_rest/HbcRest/HbcRest.csproj"
$PublishDir = Join-Path $RepoRoot "hbc_rest/publish"

# Load env defaults.
$EnvScript = Join-Path $ScriptDir "env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

Push-Location $RepoRoot

Write-Host "[rest_build] Cleaning..."
dotnet clean $ProjectPath

Write-Host "[rest_build] Restoring packages..."
dotnet restore $ProjectPath

Write-Host "[rest_build] Applying migrations..."
if (Get-Command dotnet-ef -ErrorAction SilentlyContinue) {
    dotnet ef database update --project $ProjectPath --startup-project $ProjectPath
} else {
    Write-Host "[rest_build] dotnet-ef not found on PATH. Skipping migrations. Install via:"
    Write-Host "  dotnet tool install --global dotnet-ef"
}

Write-Host "[rest_build] Publishing Release build..."
dotnet publish $ProjectPath -c Release -o $PublishDir

Write-Host "[rest_build] Done. To run:"
Write-Host "  cd $PublishDir"
Write-Host "  HBC_DB_PATH=$Env:HBC_DB_PATH ASPNETCORE_ENVIRONMENT=$Env:ASPNETCORE_ENVIRONMENT dotnet HbcRest.dll"

Pop-Location
