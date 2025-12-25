#!/usr/bin/env bash
set -euo pipefail

# Run the published Release build of HbcRest.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PUBLISH_DIR="${REPO_ROOT}/publish"

# Load shared environment defaults (DB path, URLs, etc.) if available.
if [[ -f "${REPO_ROOT}/scripts/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${REPO_ROOT}/scripts/env.sh"
fi

if [ ! -d "${PUBLISH_DIR}" ]; then
  echo "[run_prod] publish directory not found at ${PUBLISH_DIR}. Run scripts/build_prod.sh first."
  exit 1
fi

cd "${PUBLISH_DIR}"
dotnet HbcRest.dll
