#!/usr/bin/env bash
set -euo pipefail

# Launch classic Jupyter pointing at the project notebooks folder and open Demo.ipynb.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
NOTEBOOK_DIR="${REPO_ROOT}/hbc_py/notebooks"
DEFAULT_NOTEBOOK="Demo.ipynb"

# Load shared env defaults (HBC_API_URL, HBC_DB_PATH, etc.) if available.
if [[ -f "${SCRIPT_DIR}/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${SCRIPT_DIR}/env.sh"
fi

# Ensure repo venv exists; create if missing, then activate it so PATH/python point to it.
VENV_DIR="${REPO_ROOT}/.venv"
VENV_PY="${VENV_DIR}/bin/python"
if [[ ! -x "${VENV_PY}" ]]; then
  BOOT_PY=""
  if command -v python3 >/dev/null 2>&1; then
    BOOT_PY="$(command -v python3)"
  elif command -v python >/dev/null 2>&1; then
    BOOT_PY="$(command -v python)"
  fi
  if [[ -z "${BOOT_PY}" ]]; then
    echo "[demo] Python not found. Install Python 3.10+ and rerun."
    exit 1
  fi
  echo "[demo] Creating venv at ${VENV_DIR} using ${BOOT_PY}"
  "${BOOT_PY}" -m venv "${VENV_DIR}"
fi

# Activate venv for this shell (affects PATH), then use its python explicitly.
# shellcheck source=/dev/null
source "${VENV_DIR}/bin/activate"
PY_BIN="${VENV_PY}"

# Ensure jupyter is installed in the chosen interpreter.
if ! "${PY_BIN}" -c "import jupyter" >/dev/null 2>&1; then
  echo "[demo] jupyter not found in ${PY_BIN}. Installing notebook/nbclassic/ipykernel..."
  "${PY_BIN}" -m pip install --quiet notebook nbclassic ipykernel
fi

# Ensure kernel registered for this venv.
"${PY_BIN}" -m ipykernel install --user --name hbc-venv --display-name "Python (hbc)" >/dev/null 2>&1 || true

echo "[demo] Using python: ${PY_BIN}"

echo "[demo] Starting nbclassic in ${NOTEBOOK_DIR}"
exec "${PY_BIN}" -m jupyter nbclassic \
  --NotebookApp.open_browser=True \
  --ServerApp.root_dir="${NOTEBOOK_DIR}" \
  --NotebookApp.default_url="/tree/${DEFAULT_NOTEBOOK}"
