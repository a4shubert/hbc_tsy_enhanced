#!/usr/bin/env bash
set -euo pipefail

# Start REST API (published build) and demo notebook in separate Terminal windows,
# leaving this shell free for running jobs inside the venv.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${SCRIPT_DIR}"

# Load env defaults.
if [[ -f "${REPO_ROOT}/scripts/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${REPO_ROOT}/scripts/env.sh"
fi

# Ensure venv exists (basic install check).
if [[ ! -f "${REPO_ROOT}/.venv/bin/activate" ]]; then
  echo "[run_all] .venv not found. Please run ./install.sh first."
  exit 1
fi

rest_cmd="cd \"${REPO_ROOT}\"; source scripts/env.sh; source .venv/bin/activate; bash hbc_rest/scripts/run_prod.sh"
nb_cmd="cd \"${REPO_ROOT}\"; source scripts/env.sh; source .venv/bin/activate; bash hbc_py/scripts/run_demo_notebook.sh"

# Escape double quotes for AppleScript.
rest_cmd_escaped=${rest_cmd//\"/\\\"}
nb_cmd_escaped=${nb_cmd//\"/\\\"}

echo "[run_all] Opening REST API in new Terminal window..."
osascript <<APPLESCRIPT
tell application "Terminal"
  do script "${rest_cmd_escaped}"
end tell
APPLESCRIPT

echo "[run_all] Opening demo notebook in new Terminal window..."
osascript <<APPLESCRIPT
tell application "Terminal"
  do script "${nb_cmd_escaped}"
end tell
APPLESCRIPT

echo "[run_all] Two Terminal windows started (REST API + notebook). This shell remains free for job commands."
