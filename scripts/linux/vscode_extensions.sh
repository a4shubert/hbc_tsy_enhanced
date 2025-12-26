#!/usr/bin/env bash
set -euo pipefail

# Install recommended VS Code extensions for Next.js / TypeScript / Tailwind.

EXTENSIONS=(
  "bradlc.vscode-tailwindcss"
  "dsznajder.es7-react-js-snippets"
  "mkhl.direnv"
  "ms-vscode.vscode-typescript-next"
  "ms-vscode.vscode-eslint"
  "esbenp.prettier-vscode"
  "usernamehw.errorlens"
  "streetsidesoftware.code-spell-checker"
  # Optional productivity:
  # "GitHub.copilot"
  # "GitHub.copilot-chat"
)

if ! command -v code >/dev/null 2>&1; then
  echo "[vscode_ext] 'code' CLI not found. Open VS Code and run 'Shell Command: Install 'code' command in PATH'."
  exit 1
fi

for ext in "${EXTENSIONS[@]}"; do
  echo "[vscode_ext] Installing ${ext}..."
  code --install-extension "${ext}" --force >/dev/null
done

echo "[vscode_ext] Done."
