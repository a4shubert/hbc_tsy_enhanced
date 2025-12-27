#!/usr/bin/env bash
set -euo pipefail

# Run the Next.js production server (requires prior web_build).

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
WEB_DIR="${REPO_ROOT}/hbc_web"

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
  echo "[web_start_prod] .next not found. Running build first..."
  npm run build
fi

echo "[web_start_prod] Starting Next.js production server..."
npm run start
