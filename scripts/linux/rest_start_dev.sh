#!/usr/bin/env bash
set -euo pipefail

# Run the app in Development using the launch profile "HbcRest".

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PROJECT_DIR="${REPO_ROOT}/hbc_rest/HbcRest"

cd "${PROJECT_DIR}"
dotnet run --launch-profile "HbcRest"
