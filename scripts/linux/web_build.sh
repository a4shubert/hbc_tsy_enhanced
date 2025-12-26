#!/usr/bin/env bash
set -euo pipefail

# Build the Next.js app for production.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
WEB_DIR="${REPO_ROOT}/hbc_web"

if ! command -v npm >/dev/null 2>&1; then
  echo "[web_build] npm not found. Please install Node.js/npm first."
  exit 1
fi

if [[ ! -d "${WEB_DIR}" ]]; then
  echo "[web_build] ${WEB_DIR} not found. Did you clone the repo?"
  exit 1
fi

cd "${WEB_DIR}"
if [[ ! -d node_modules ]]; then
  echo "[web_build] Installing dependencies..."
  npm install
fi

echo "[web_build] Building Next.js production bundle..."
npm run build
