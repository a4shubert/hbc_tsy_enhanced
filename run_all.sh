#!/usr/bin/env bash
set -euo pipefail

# Start REST API (published build) and demo notebook in background,
# leaving this shell free for running jobs inside the venv.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${SCRIPT_DIR}"

# Load env defaults.
if [[ -f "${REPO_ROOT}/scripts/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${REPO_ROOT}/scripts/env.sh"
fi

# Ensure venv exists for notebook and activate it.
if [[ ! -x "${REPO_ROOT}/.venv/bin/python" ]]; then
  PY_BOOT=""
  if command -v python3 >/dev/null 2>&1; then PY_BOOT="$(command -v python3)"; elif command -v python >/dev/null 2>&1; then PY_BOOT="$(command -v python)"; fi
  if [[ -z "${PY_BOOT}" ]]; then
    echo "[run_all] Python not found. Install Python 3.10+ and rerun."
    exit 1
  fi
  "${PY_BOOT}" -m venv "${REPO_ROOT}/.venv"
fi
# shellcheck source=/dev/null
source "${REPO_ROOT}/.venv/bin/activate"

LOG_DIR="${REPO_ROOT}/logs"
mkdir -p "${LOG_DIR}"

echo "[run_all] Starting REST API (logs: ${LOG_DIR}/rest.log)"
nohup bash "${REPO_ROOT}/hbc_rest/scripts/run_prod.sh" >"${LOG_DIR}/rest.log" 2>&1 &
REST_PID=$!

echo "[run_all] Starting demo notebook (logs: ${LOG_DIR}/notebook.log)"
nohup bash "${REPO_ROOT}/hbc_py/scripts/run_demo_notebook.sh" >"${LOG_DIR}/notebook.log" 2>&1 &
NB_PID=$!

echo "[run_all] PIDs -> REST: ${REST_PID}, Notebook: ${NB_PID}"
echo "[run_all] Use 'kill <pid>' to stop services. Terminal is free for job commands (venv activated)."
