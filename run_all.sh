#!/usr/bin/env bash
set -euo pipefail

# Start REST API (published build) and demo notebook in separate terminals
# (macOS) or background (other Unix), leaving this shell free for running jobs
# inside the venv.

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

start_rest_cmd="cd \"${REPO_ROOT}\" && source .venv/bin/activate && source scripts/env.sh && bash hbc_rest/scripts/run_prod.sh"
start_nb_cmd="cd \"${REPO_ROOT}\" && source .venv/bin/activate && source scripts/env.sh && bash hbc_py/scripts/run_demo_notebook.sh"

if [[ "$(uname)" == "Darwin" ]]; then
  rest_cmd_escaped=${start_rest_cmd//\\/\\\\}      # escape backslashes
  rest_cmd_escaped=${rest_cmd_escaped//\'/\'"\'"\'} # escape single quotes
  nb_cmd_escaped=${start_nb_cmd//\\/\\\\}
  nb_cmd_escaped=${nb_cmd_escaped//\'/\'"\'"\'}
  echo "[run_all] Opening REST API in new Terminal window..."
  osascript >/dev/null <<OSA
tell application "Terminal"
  do script "bash -lc '${rest_cmd_escaped}'"
end tell
OSA
  echo "[run_all] Opening notebook in new Terminal window..."
  osascript >/dev/null <<OSA
tell application "Terminal"
  do script "bash -lc '${nb_cmd_escaped}'"
end tell
OSA
  echo "[run_all] Terminals started for REST and notebook. This shell stays free for commands (venv active)."
else
  echo "[run_all] Starting REST API (logs: ${LOG_DIR}/rest.log)"
  nohup bash -c "${start_rest_cmd}" >"${LOG_DIR}/rest.log" 2>&1 &
  REST_PID=$!
  echo "[run_all] Starting demo notebook (logs: ${LOG_DIR}/notebook.log)"
  nohup bash -c "${start_nb_cmd}" >"${LOG_DIR}/notebook.log" 2>&1 &
  NB_PID=$!
  echo "[run_all] PIDs -> REST: ${REST_PID}, Notebook: ${NB_PID}"
  echo "[run_all] Use 'kill <pid>' to stop services. Terminal is free for job commands (venv activated)."
fi
