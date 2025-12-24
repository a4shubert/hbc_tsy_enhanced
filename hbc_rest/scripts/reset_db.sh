#!/usr/bin/env bash
set -euo pipefail

# Reset the SQLite database:
# - removes the existing DB file
# - ensures dotnet-ef is available
# - adds an initial migration (if none exist) or uses the existing ones
# - updates the database

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PROJECT_PATH="${REPO_ROOT}/hbc_rest/HbcRest/HbcRest.csproj"
DB_PATH="${HBC_DB_PATH:-${REPO_ROOT}/hbc_db/hbc.db}"

echo "[reset_db] Using DB path: ${DB_PATH}"

echo "[reset_db] Removing existing database..."
rm -f "${DB_PATH}"

cd "${REPO_ROOT}"

if ! command -v dotnet-ef >/dev/null 2>&1; then
  echo "[reset_db] dotnet-ef not found. Install with:"
  echo "  dotnet tool install --global dotnet-ef"
  exit 1
fi

MIGRATIONS_DIR="${REPO_ROOT}/hbc_rest/HbcRest/Migrations"

echo "[reset_db] Removing existing migrations..."
rm -rf "${MIGRATIONS_DIR}"

echo "[reset_db] Creating initial migration..."
dotnet ef migrations add InitialCreate --project "${PROJECT_PATH}" --startup-project "${PROJECT_PATH}"

echo "[reset_db] Updating database..."
dotnet ef database update --project "${PROJECT_PATH}" --startup-project "${PROJECT_PATH}"

echo "[reset_db] Done. DB recreated at ${DB_PATH}"
