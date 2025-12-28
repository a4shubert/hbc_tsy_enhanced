#!/usr/bin/env bash
set -euo pipefail

# Start REST API (published build), web app (prod), and demo notebook in separate Terminal windows,
# leaving this shell free for running jobs inside the venv.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

# Load env defaults.
if [[ -f "${SCRIPT_DIR}/env.sh" ]]; then
  # shellcheck source=/dev/null
  source "${SCRIPT_DIR}/env.sh"
fi

# Ensure venv exists (basic install check).
if [[ ! -f "${REPO_ROOT}/.venv/bin/activate" ]]; then
    echo "[run_all] .venv not found. Please run scripts/linux/install.sh first."
  exit 1
fi

rest_cmd="cd \"${REPO_ROOT}\"; source scripts/linux/env.sh; source .venv/bin/activate; bash scripts/linux/rest_start_prod.sh"
web_cmd="cd \"${REPO_ROOT}\"; source scripts/linux/env.sh; bash scripts/linux/web_start_prod.sh"
nb_cmd="cd \"${REPO_ROOT}\"; source scripts/linux/env.sh; source .venv/bin/activate; bash scripts/linux/run_demo_notebook.sh"

# Escape double quotes for AppleScript.
rest_cmd_escaped=${rest_cmd//\"/\\\"}
web_cmd_escaped=${web_cmd//\"/\\\"}
nb_cmd_escaped=${nb_cmd//\"/\\\"}

echo "[run_all] Opening REST API in new Terminal window..."
osascript <<APPLESCRIPT
tell application "Terminal"
  do script "${rest_cmd_escaped}"
end tell
APPLESCRIPT

echo "[run_all] Opening web app (prod) in new Terminal window..."
osascript <<APPLESCRIPT
tell application "Terminal"
  do script "${web_cmd_escaped}"
end tell
APPLESCRIPT

web_url="${HBC_WEB_URL:-http://localhost:3000}"
echo "[run_all] Opening browser at ${web_url}..."
(
  sleep 3
  if command -v open >/dev/null 2>&1; then
    open "${web_url}"
  elif command -v xdg-open >/dev/null 2>&1; then
    xdg-open "${web_url}" >/dev/null 2>&1 || true
  else
    echo "[run_all] Could not auto-open browser (missing open/xdg-open)."
  fi
) &

echo "[run_all] Opening demo notebook in new Terminal window..."
osascript <<APPLESCRIPT
tell application "Terminal"
  do script "${nb_cmd_escaped}"
end tell
APPLESCRIPT

echo "[run_all] Three Terminal windows started (REST API + web + notebook). This shell remains free for job commands."
