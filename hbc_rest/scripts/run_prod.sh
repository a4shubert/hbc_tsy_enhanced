#!/usr/bin/env bash
set -euo pipefail

# Run the published Release build of HbcRest.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PUBLISH_DIR="${REPO_ROOT}/publish"

if [ ! -d "${PUBLISH_DIR}" ]; then
  echo "[run_prod] publish directory not found at ${PUBLISH_DIR}. Run scripts/build_prod.sh first."
  exit 1
fi

export HBC_DB_PATH="${HBC_DB_PATH:-${REPO_ROOT}/hbc_db/hbc.db}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:5047}"

echo "[run_prod] Using HBC_DB_PATH=${HBC_DB_PATH}"
echo "[run_prod] ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}"
echo "[run_prod] ASPNETCORE_URLS=${ASPNETCORE_URLS}"

cd "${PUBLISH_DIR}"
dotnet HbcRest.dll
