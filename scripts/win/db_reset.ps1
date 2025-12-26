<#
  Reset the SQLite database on Windows:
  - removes existing DB file
  - removes migrations folder
  - adds initial migration
  - updates the database
#>

$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$ProjectPath = Join-Path $RepoRoot "hbc_rest/HbcRest/HbcRest.csproj"
$DbPath = if ($Env:HBC_DB_PATH) { $Env:HBC_DB_PATH } else { Join-Path $RepoRoot "hbc_db/hbc.db" }
$MigrationsDir = Join-Path $RepoRoot "hbc_rest/HbcRest/Migrations"

Write-Host "[db_reset] Using DB path: $DbPath"

Write-Host "[db_reset] Removing existing database..."
Remove-Item -Force -ErrorAction SilentlyContinue $DbPath

# Load env defaults if available.
$EnvScript = Join-Path $ScriptDir "env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    Write-Host "[db_reset] dotnet-ef not found. Install with:"
    Write-Host "  dotnet tool install --global dotnet-ef"
    exit 1
}

Write-Host "[db_reset] Removing existing migrations..."
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $MigrationsDir

Push-Location $RepoRoot

Write-Host "[db_reset] Creating initial migration..."
dotnet ef migrations add InitialCreate --project $ProjectPath --startup-project $ProjectPath

Write-Host "[db_reset] Updating database..."
dotnet ef database update --project $ProjectPath --startup-project $ProjectPath

Pop-Location

Write-Host "[db_reset] Done. DB recreated at $DbPath"
