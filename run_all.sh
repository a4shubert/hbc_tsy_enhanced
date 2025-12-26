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

# Ensure venv exists (basic install check).
if [[ ! -x "${REPO_ROOT}/.venv/bin/python" ]]; then
  echo "[run_all] .venv not found. Please run ./install.sh first."
  exit 1
fi
# shellcheck source=/dev/null
source "${REPO_ROOT}/.venv/bin/activate"

LOG_DIR="${REPO_ROOT}/logs"
mkdir -p "${LOG_DIR}"

start_rest_cmd="cd \"${REPO_ROOT}\" && source .venv/bin/activate && source scripts/env.sh && bash hbc_rest/scripts/run_prod.sh"
start_nb_cmd="cd \"${REPO_ROOT}\" && source .venv/bin/activate && source scripts/env.sh && bash hbc_py/scripts/run_demo_notebook.sh"

echo "[run_all] Starting REST API (logs: ${LOG_DIR}/rest.log)"
nohup bash -c "${start_rest_cmd}" >"${LOG_DIR}/rest.log" 2>&1 &
REST_PID=$!
echo "[run_all] Starting demo notebook (logs: ${LOG_DIR}/notebook.log)"
nohup bash -c "${start_nb_cmd}" >"${LOG_DIR}/notebook.log" 2>&1 &
NB_PID=$!
echo "[run_all] PIDs -> REST: ${REST_PID}, Notebook: ${NB_PID}"
echo "[run_all] Use 'kill <pid>' to stop services. Terminal is free for job commands (venv activated)."
