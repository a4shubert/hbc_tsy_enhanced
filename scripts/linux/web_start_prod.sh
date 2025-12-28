#!/usr/bin/env bash
set -euo pipefail

# Run the Next.js production server (requires prior web_build).

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
WEB_DIR="${REPO_ROOT}/hbc_web"

if ! command -v npm >/dev/null 2>&1; then
  echo "[web_start_prod] npm not found. Please install Node.js/npm first."
  exit 1
fi

if [[ ! -d "${WEB_DIR}" ]]; then
  echo "[web_start_prod] ${WEB_DIR} not found. Did you clone the repo?"
  exit 1
fi

cd "${WEB_DIR}"
if [[ ! -d node_modules ]]; then
  echo "[web_start_prod] Installing dependencies..."
  npm install
fi

if [[ ! -d .next ]]; then
  need_build=1
else
  need_build=0
fi

if [[ "${HBC_WEB_REBUILD:-0}" == "1" ]]; then
  need_build=1
fi

build_marker=".next/BUILD_ID"
if [[ "${need_build}" -eq 0 && ! -f "${build_marker}" ]]; then
  need_build=1
fi

if [[ "${need_build}" -eq 0 ]]; then
  build_mtime=""
  if stat -f "%m" "${build_marker}" >/dev/null 2>&1; then
    build_mtime="$(stat -f "%m" "${build_marker}")"
  else
    build_mtime="$(stat -c "%Y" "${build_marker}" 2>/dev/null || echo "")"
  fi

  latest_src=0
  while IFS= read -r -d '' f; do
    mt=""
    if stat -f "%m" "$f" >/dev/null 2>&1; then
      mt="$(stat -f "%m" "$f")"
    else
      mt="$(stat -c "%Y" "$f" 2>/dev/null || echo "")"
    fi
    if [[ -n "$mt" && "$mt" -gt "$latest_src" ]]; then
      latest_src="$mt"
    fi
  done < <(find app components lib next.config.js package.json tsconfig.json -type f -print0 2>/dev/null || true)

  if [[ -n "${build_mtime}" && "${latest_src}" -gt "${build_mtime}" ]]; then
    need_build=1
  fi
fi

if [[ "${need_build}" -eq 1 ]]; then
  echo "[web_start_prod] Running build..."
  npm run build
fi

echo "[web_start_prod] Starting Next.js production server..."
npm run start
