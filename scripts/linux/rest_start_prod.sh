#!/usr/bin/env bash
set -euo pipefail

# Run the published Release build of HbcRest.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PUBLISH_DIR="${REPO_ROOT}/hbc_rest/publish"

# Load shared environment defaults (DB path, URLs, etc.) if available.
if [[ -f "${SCRIPT_DIR}/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${SCRIPT_DIR}/env.sh"
fi

if [ ! -d "${PUBLISH_DIR}" ]; then
  echo "[rest_start_prod] publish directory not found at ${PUBLISH_DIR}. Run scripts/linux/rest_build.sh first."
  exit 1
fi

cd "${PUBLISH_DIR}"
dotnet HbcRest.dll
