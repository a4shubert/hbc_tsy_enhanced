#!/usr/bin/env bash
# Common environment setup for local runs (API + notebooks).

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

# Paths/URLs
export HBC_DB_PATH="${HBC_DB_PATH:-${REPO_ROOT}/hbc_db/hbc.db}"
export HBC_API_URL="${HBC_API_URL:-http://localhost:5047}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://localhost:5047}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"

echo "[env] HBC_DB_PATH=${HBC_DB_PATH}"
echo "[env] HBC_API_URL=${HBC_API_URL}"
echo "[env] ASPNETCORE_URLS=${ASPNETCORE_URLS}"
echo "[env] ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}"
