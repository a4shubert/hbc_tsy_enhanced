<#
  Production build helper for HbcRest (Windows/PowerShell):
  - loads env defaults
  - cleans/restores
  - applies migrations if dotnet-ef is available
  - publishes Release to /publish under repo root
#>

$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$ProjectPath = Join-Path $RepoRoot "hbc_rest/HbcRest/HbcRest.csproj"

# Load env defaults.
$EnvScript = Join-Path $RepoRoot "scripts/env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

Push-Location $RepoRoot

Write-Host "[build_prod] Cleaning..."
dotnet clean $ProjectPath

Write-Host "[build_prod] Restoring packages..."
dotnet restore $ProjectPath

Write-Host "[build_prod] Applying migrations..."
if (Get-Command dotnet-ef -ErrorAction SilentlyContinue) {
    dotnet ef database update --project $ProjectPath --startup-project $ProjectPath
} else {
    Write-Host "[build_prod] dotnet-ef not found on PATH. Skipping migrations. Install via:"
    Write-Host "  dotnet tool install --global dotnet-ef"
}

Write-Host "[build_prod] Publishing Release build..."
dotnet publish $ProjectPath -c Release -o (Join-Path $RepoRoot "publish")

Write-Host "[build_prod] Done. To run:"
Write-Host "  cd $(Join-Path $RepoRoot "publish")"
Write-Host "  HBC_DB_PATH=$Env:HBC_DB_PATH ASPNETCORE_ENVIRONMENT=$Env:ASPNETCORE_ENVIRONMENT dotnet HbcRest.dll"

Pop-Location
