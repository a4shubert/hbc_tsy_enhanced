#!/usr/bin/env bash
set -euo pipefail

# Launch classic Jupyter pointing at the project notebooks folder and open Demo.ipynb.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
NOTEBOOK_DIR="${REPO_ROOT}/hbc_py/notebooks"
DEFAULT_NOTEBOOK="Demo.ipynb"

# Load shared env defaults (HBC_API_URL, HBC_DB_PATH, etc.) if available.
if [[ -f "${REPO_ROOT}/scripts/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${REPO_ROOT}/scripts/env.sh"
fi

if ! command -v jupyter >/dev/null 2>&1; then
  echo "[demo] jupyter not found on PATH. Install with: pip install notebook nbclassic"
  exit 1
fi

echo "[demo] Starting nbclassic in ${NOTEBOOK_DIR}"
exec jupyter nbclassic \
  --NotebookApp.open_browser=True \
  --ServerApp.root_dir="${NOTEBOOK_DIR}" \
  --NotebookApp.default_url="/tree/${DEFAULT_NOTEBOOK}"
