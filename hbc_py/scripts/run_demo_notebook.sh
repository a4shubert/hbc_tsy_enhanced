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

# Resolve a Python to run jupyter from (prefer repo venv).
PY_BIN=""
if [[ -n "${VIRTUAL_ENV:-}" && -x "${VIRTUAL_ENV}/bin/python" ]]; then
  PY_BIN="${VIRTUAL_ENV}/bin/python"
elif [[ -x "${REPO_ROOT}/.venv/bin/python" ]]; then
  PY_BIN="${REPO_ROOT}/.venv/bin/python"
elif command -v python3 >/dev/null 2>&1; then
  PY_BIN="$(command -v python3)"
elif command -v python >/dev/null 2>&1; then
  PY_BIN="$(command -v python)"
fi

if [[ -z "${PY_BIN}" ]]; then
  echo "[demo] Python not found. Install Python 3.10+ and rerun."
  exit 1
fi

if ! "${PY_BIN}" -c "import jupyter" >/dev/null 2>&1; then
  echo "[demo] jupyter not found in ${PY_BIN}. Install with: ${PY_BIN} -m pip install notebook nbclassic"
  exit 1
fi

echo "[demo] Starting nbclassic in ${NOTEBOOK_DIR}"
exec "${PY_BIN}" -m jupyter nbclassic \
  --NotebookApp.open_browser=True \
  --ServerApp.root_dir="${NOTEBOOK_DIR}" \
  --NotebookApp.default_url="/tree/${DEFAULT_NOTEBOOK}"
