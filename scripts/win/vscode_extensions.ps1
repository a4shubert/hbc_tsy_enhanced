<#
  Install recommended VS Code extensions for Next.js / TypeScript / Tailwind.
#>

$extensions = @(
  "bradlc.vscode-tailwindcss",
  "dsznajder.es7-react-js-snippets",
  "mkhl.direnv",
  "ms-vscode.vscode-typescript-next",
  "ms-vscode.vscode-eslint",
  "esbenp.prettier-vscode",
  "usernamehw.errorlens",
  "streetsidesoftware.code-spell-checker"
  # Optional productivity:
  # "GitHub.copilot",
  # "GitHub.copilot-chat"
)

if (-not (Get-Command code -ErrorAction SilentlyContinue)) {
    Write-Host "[vscode_ext] 'code' CLI not found. In VS Code, run: 'Shell Command: Install 'code' command in PATH'."
    exit 1
}

foreach ($ext in $extensions) {
    Write-Host "[vscode_ext] Installing $ext..."
    code --install-extension $ext --force | Out-Null
}

Write-Host "[vscode_ext] Done."
