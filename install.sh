#!/usr/bin/env bash
set -euo pipefail

# One-shot installer/runner:
# - loads shared env defaults (DB path, API URL, etc.)
# - creates/activates a local venv (if none active)
# - installs the Python package from the hbc_py subfolder
# - starts the published ASP.NET Core REST API (foreground) via run_prod.sh

# Resolve script location for bash/zsh.
if [ -n "${BASH_SOURCE:-}" ]; then
  _SELF="${BASH_SOURCE[0]}"
elif [ -n "${ZSH_VERSION:-}" ]; then
  _SELF="${(%):-%N}"
else
  _SELF="$0"
fi
SCRIPT_DIR="$(cd "$(dirname "${_SELF}")" && pwd)"
REPO_ROOT="${SCRIPT_DIR}"

# Load environment defaults.
source "${REPO_ROOT}/scripts/env.sh"

# Check for optional runtimes/tools.
if ! command -v dotnet >/dev/null 2>&1; then
  echo "[install] Warning: dotnet SDK not found. Install .NET 8 SDK if you plan to run the REST API:"
  echo "  https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
fi

if ! command -v conda >/dev/null 2>&1; then
  echo "[install] Warning: conda/miniconda not found. If you prefer conda, install Miniconda:"
  echo "  https://docs.conda.io/en/latest/miniconda.html"
fi

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

VENV_DIR="${REPO_ROOT}/.venv"
echo $VENV_DIR
source "${VENV_DIR}/bin/activate"