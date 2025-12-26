#!/usr/bin/env bash
set -euo pipefail

# Report row counts for all user tables in hbc.db.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
DB_PATH="${HBC_DB_PATH:-${REPO_ROOT}/hbc_db/hbc.db}"

if [[ ! -f "${DB_PATH}" ]]; then
  echo "[count_rows] DB not found at ${DB_PATH}"
  exit 1
fi

tables=$(sqlite3 "${DB_PATH}" "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;")
if [[ -z "${tables}" ]]; then
  echo "[count_rows] No user tables found."
  exit 0
fi

while IFS= read -r tbl; do
  sqlite3 "${DB_PATH}" "SELECT '$tbl = ' || COUNT(*) AS row_count FROM \"$tbl\";"
done <<< "${tables}"
