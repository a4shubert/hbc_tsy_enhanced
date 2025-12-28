#!/usr/bin/env bash
set -euo pipefail

# One-shot installer/runner:
# - loads shared env defaults (DB path, API URL, etc.)
# - creates a local venv under .venv if missing
# - installs the Python package from the hbc_py subfolder (editable)
# - starts the published ASP.NET Core REST API (foreground) via rest_start_prod.sh

# Resolve script location for bash/zsh.
if [ -n "${BASH_SOURCE:-}" ]; then
  _SELF="${BASH_SOURCE[0]}"
elif [ -n "${ZSH_VERSION:-}" ]; then
  _SELF="${(%):-%N}"
else
  _SELF="$0"
fi
SCRIPT_DIR="$(cd "$(dirname "${_SELF}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

# Load environment defaults.
# shellcheck source=/dev/null
source "${SCRIPT_DIR}/env.sh"

PYTHON_BIN="${PYTHON:-python3}"

VENV_DIR="${REPO_ROOT}/.venv"
VENV_PY="${VENV_DIR}/bin/python"

# Check prerequisites (do not proceed if missing).
missing=0

if ! command -v "${PYTHON_BIN}" >/dev/null 2>&1; then
  echo "[install] Missing required dependency: python (3.10+ recommended)."
  echo "  https://www.python.org/downloads/"
  missing=1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[install] Missing required dependency: .NET SDK (8.x)."
  echo "  https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
  missing=1
fi

if ! command -v npm >/dev/null 2>&1; then
  echo "[install] Missing required dependency: npm (Node.js)."
  echo "  https://nodejs.org/en/download"
  missing=1
fi

if [[ "${missing}" -ne 0 ]]; then
  echo "[install] Aborting: install missing dependencies and retry."
  exit 1
fi

# Optional tools.
if ! command -v conda >/dev/null 2>&1; then
  echo "[install] Note: conda/miniconda not found. If you prefer conda, install Miniconda:"
  echo "  https://docs.conda.io/en/latest/miniconda.html"
fi

if [[ ! -x "${VENV_PY}" ]]; then
  echo "[install] Creating virtualenv at ${VENV_DIR}"
  "${PYTHON_BIN}" -m venv "${VENV_DIR}"
fi

echo "[install] Upgrading pip and installing hbc_py (editable) using venv python"
"${VENV_PY}" -m pip install --upgrade pip
"${VENV_PY}" -m pip install -e "${REPO_ROOT}/hbc_py"
