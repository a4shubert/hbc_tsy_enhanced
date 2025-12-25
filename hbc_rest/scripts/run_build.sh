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
source "${REPO_ROOT}/scripts/env.sh"

cd "${REPO_ROOT}"

echo "[build_prod] Cleaning..."
dotnet clean "${PROJECT_PATH}"

echo "[build_prod] Restoring packages..."
dotnet restore "${PROJECT_PATH}"

echo "[build_prod] Applying migrations..."
if command -v dotnet-ef >/dev/null 2>&1; then
  dotnet ef database update --project "${PROJECT_PATH}" --startup-project "${PROJECT_PATH}"
else
  echo "[build_prod] dotnet-ef not found on PATH. Skipping migrations. Install via:"
  echo "  dotnet tool install --global dotnet-ef"
fi

echo "[build_prod] Publishing Release build..."
dotnet publish "${PROJECT_PATH}" -c Release -o "${REPO_ROOT}/publish"

echo "[build_prod] Done. To run:"
echo "  cd ${REPO_ROOT}/publish && HBC_DB_PATH=${HBC_DB_PATH} ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT} dotnet HbcRest.dll"
