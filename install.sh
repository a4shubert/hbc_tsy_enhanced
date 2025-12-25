#!/usr/bin/env bash
set -euo pipefail

# One-shot installer/runner:
# - loads shared env defaults (DB path, API URL, etc.)
# - creates/activates a local venv (if none active)
# - installs the Python package from the hbc_py subfolder
# - starts the published ASP.NET Core REST API (foreground) via run_prod.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${SCRIPT_DIR}"

# Load environment defaults.
source "${REPO_ROOT}/scripts/env.sh"

PYTHON_BIN="${PYTHON:-python3}"

# Use existing venv if already active; otherwise create one under repo.
if [[ -z "${VIRTUAL_ENV:-}" ]]; then
  VENV_DIR="${REPO_ROOT}/.venv"
  if [[ ! -d "${VENV_DIR}" ]]; then
    echo "[install] Creating virtualenv at ${VENV_DIR}"
    "${PYTHON_BIN}" -m venv "${VENV_DIR}"
  fi
  # shellcheck source=/dev/null
  source "${VENV_DIR}/bin/activate"
fi

echo "[install] Upgrading pip and installing hbc_py (editable)"
pip install --upgrade pip
pip install -e "${REPO_ROOT}/hbc_py"

echo "[install] Starting REST API from published build..."
exec "${REPO_ROOT}/hbc_rest/scripts/run_prod.sh"
