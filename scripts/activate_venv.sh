#!/usr/bin/env bash
# Source this file to activate the project virtualenv and env vars in your current shell.
# Usage: source scripts/activate_venv.sh

set -euo pipefail

# Resolve script path in bash or zsh.
if [ -n "${BASH_SOURCE:-}" ]; then
  _SRC="${BASH_SOURCE[0]}"
elif [ -n "${ZSH_VERSION:-}" ]; then
  _SRC="${(%):-%N}"
else
  _SRC="$0"
fi
SCRIPT_DIR="$(cd "$(dirname "${_SRC}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

if [[ -f "${REPO_ROOT}/scripts/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${REPO_ROOT}/scripts/env.sh"
fi

VENV_DIR="${REPO_ROOT}/.venv"
if [[ ! -f "${VENV_DIR}/bin/activate" ]]; then
  echo "[activate_venv] .venv not found. Run ./install.sh first."
  return 1 2>/dev/null || exit 1
fi

# shellcheck source=/dev/null
source "${VENV_DIR}/bin/activate"
echo "[activate_venv] Activated ${VIRTUAL_ENV}"
