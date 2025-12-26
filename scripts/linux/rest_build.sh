#!/usr/bin/env bash
set -euo pipefail

# Production build helper for HbcRest.
# - Ensures HBC_DB_PATH is set (defaults to ../hbc_db/hbc.db).
# - Runs migrations on that DB (if dotnet-ef is available).
# - Publishes Release artifacts to ./publish under the repo root.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PROJECT_PATH="${REPO_ROOT}/hbc_rest/HbcRest/HbcRest.csproj"

# Load shared env defaults (defines HBC_DB_PATH, ASPNETCORE_ENVIRONMENT, etc.).
source "${SCRIPT_DIR}/env.sh"

cd "${REPO_ROOT}"

echo "[rest_build] Cleaning..."
dotnet clean "${PROJECT_PATH}"

echo "[rest_build] Restoring packages..."
dotnet restore "${PROJECT_PATH}"

echo "[rest_build] Applying migrations..."
if command -v dotnet-ef >/dev/null 2>&1; then
  dotnet ef database update --project "${PROJECT_PATH}" --startup-project "${PROJECT_PATH}"
else
  echo "[rest_build] dotnet-ef not found on PATH. Skipping migrations. Install via:"
  echo "  dotnet tool install --global dotnet-ef"
fi

echo "[rest_build] Publishing Release build..."
dotnet publish "${PROJECT_PATH}" -c Release -o "${REPO_ROOT}/publish"

echo "[rest_build] Done. To run:"
echo "  cd ${REPO_ROOT}/publish && HBC_DB_PATH=${HBC_DB_PATH} ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT} dotnet HbcRest.dll"
