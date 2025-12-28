#!/usr/bin/env bash
set -euo pipefail

# Start the Next.js dev server for the web portal.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
WEB_DIR="${REPO_ROOT}/hbc_web"

# Load env defaults (HBC_API_URL used by Next.js rewrites).
if [[ -f "${SCRIPT_DIR}/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${SCRIPT_DIR}/env.sh"
fi

if ! command -v npm >/dev/null 2>&1; then
  echo "[web_start_dev] npm not found. Please install Node.js/npm first."
  exit 1
fi

if [[ ! -d "${WEB_DIR}" ]]; then
  echo "[web_start_dev] ${WEB_DIR} not found. Did you clone the repo?"
  exit 1
fi

cd "${WEB_DIR}"
if [[ ! -d node_modules ]]; then
  echo "[web_start_dev] Installing dependencies..."
  npm install
fi

echo "[web_start_dev] Starting Next.js dev server..."
npm run dev
