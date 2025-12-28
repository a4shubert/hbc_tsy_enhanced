#!/usr/bin/env bash
set -euo pipefail

# Run the Next.js production server (requires prior web_build).

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
WEB_DIR="${REPO_ROOT}/hbc_web"

# Load env defaults (HBC_API_URL used by Next.js rewrites).
if [[ -f "${SCRIPT_DIR}/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${SCRIPT_DIR}/env.sh"
fi

if ! command -v npm >/dev/null 2>&1; then
  echo "[web_start_prod] npm not found. Please install Node.js/npm first."
  exit 1
fi

if [[ ! -d "${WEB_DIR}" ]]; then
  echo "[web_start_prod] ${WEB_DIR} not found. Did you clone the repo?"
  exit 1
fi

cd "${WEB_DIR}"
if [[ ! -d node_modules ]]; then
  echo "[web_start_prod] Installing dependencies..."
  npm install
fi

if [[ ! -d .next ]]; then
  if [[ "${HBC_WEB_REBUILD:-0}" == "1" ]]; then
    echo "[web_start_prod] .next not found. Running build (HBC_WEB_REBUILD=1)..."
    npm run build
  else
    echo "[web_start_prod] .next not found. This repo expects a committed production build for end users."
    echo "[web_start_prod] If you're developing locally, run:"
    echo "  cd ${WEB_DIR} && npm install && npm run build"
    echo "[web_start_prod] Or set HBC_WEB_REBUILD=1 to build automatically."
    exit 1
  fi
fi

echo "[web_start_prod] Starting Next.js production server..."
npm run start
