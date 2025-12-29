#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

# Load env defaults (HBC_API_URL, etc).
if [[ -f "${SCRIPT_DIR}/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${SCRIPT_DIR}/env.sh"
fi

# Activate venv (required for pytest + deps).
if [[ -f "${SCRIPT_DIR}/activate_venv.sh" ]]; then
  # shellcheck source=/dev/null
  source "${SCRIPT_DIR}/activate_venv.sh"
else
  if [[ -f "${REPO_ROOT}/.venv/bin/activate" ]]; then
    # shellcheck source=/dev/null
    source "${REPO_ROOT}/.venv/bin/activate"
  fi
fi

export HBC_INTEGRATION=1
echo "[py_run_tests] HBC_INTEGRATION=${HBC_INTEGRATION}"

cd "${REPO_ROOT}/hbc_py"
pytest -vv -s "$@"

