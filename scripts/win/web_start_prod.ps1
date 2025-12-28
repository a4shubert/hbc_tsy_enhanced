<#
  Run the Next.js production server (requires prior web_build).
#>

$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$WebDir = Join-Path $RepoRoot "hbc_web"

# Load env defaults (HBC_API_URL used by Next.js rewrites).
$EnvScript = Join-Path $ScriptDir "env.ps1"
if (Test-Path $EnvScript) { . $EnvScript }

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "[web_start_prod] npm not found. Please install Node.js/npm first."
    exit 1
}

if (-not (Test-Path $WebDir)) {
    Write-Host "[web_start_prod] $WebDir not found. Did you clone the repo?"
    exit 1
}

Push-Location $WebDir
if (-not (Test-Path "node_modules")) {
    Write-Host "[web_start_prod] Installing dependencies..."
    npm install
}

$NeedBuild = $false
if (-not (Test-Path ".next")) { $NeedBuild = $true }
if ($Env:HBC_WEB_REBUILD -eq "1") { $NeedBuild = $true }

if (-not $NeedBuild) {
    $BuildMarker = Join-Path $WebDir ".next/BUILD_ID"
    if (-not (Test-Path $BuildMarker)) {
        $NeedBuild = $true
    } else {
        $BuildTime = (Get-Item $BuildMarker).LastWriteTimeUtc
        $Paths = @(
            (Join-Path $WebDir "app"),
            (Join-Path $WebDir "components"),
            (Join-Path $WebDir "lib"),
            (Join-Path $WebDir "next.config.js"),
            (Join-Path $WebDir "package.json"),
            (Join-Path $WebDir "tsconfig.json")
        ) | Where-Object { Test-Path $_ }

        $Latest = $BuildTime
        foreach ($p in $Paths) {
            if (Test-Path $p -PathType Leaf) {
                $t = (Get-Item $p).LastWriteTimeUtc
                if ($t -gt $Latest) { $Latest = $t }
            } else {
                $t = Get-ChildItem -Recurse -File $p | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
                if ($t -and $t.LastWriteTimeUtc -gt $Latest) { $Latest = $t.LastWriteTimeUtc }
            }
        }

        if ($Latest -gt $BuildTime) { $NeedBuild = $true }
    }
}

if ($NeedBuild) {
    if ($Env:HBC_WEB_REBUILD -eq "1") {
        Write-Host "[web_start_prod] Running build (HBC_WEB_REBUILD=1)..."
        npm run build
    } else {
        Write-Host "[web_start_prod] .next not found or stale. This repo expects a committed production build for end users."
        Write-Host "[web_start_prod] If you're developing locally, run:"
        Write-Host "  cd $WebDir; npm install; npm run build"
        Write-Host "[web_start_prod] Or set HBC_WEB_REBUILD=1 to build automatically."
        exit 1
    }
}

Write-Host "[web_start_prod] Starting Next.js production server..."
npm run start
Pop-Location
