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

cd "${PUBLISH_DIR}"
dotnet HbcRest.dll
