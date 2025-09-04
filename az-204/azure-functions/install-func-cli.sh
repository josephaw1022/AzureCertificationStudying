#!/usr/bin/env bash
set -euo pipefail

if command -v func >/dev/null 2>&1; then
  echo "✅ Azure Functions CLI (func) already installed at: $(command -v func)"
  exit 0
fi

VERSION="4.0.5611"
ZIP="Azure.Functions.Cli.linux-x64.${VERSION}.zip"
URL="https://github.com/Azure/azure-functions-core-tools/releases/download/${VERSION}/${ZIP}"

# Install location
INSTALL_DIR="${HOME}/azure-functions-cli"

mkdir -p "${INSTALL_DIR}"
cd "${HOME}"

# Download
curl -fL -o "${ZIP}" "${URL}"

# Unzip fresh
rm -rf "${INSTALL_DIR:?}/"* || true
unzip -q -d "${INSTALL_DIR}" "${ZIP}"
rm -f "${ZIP}"

# Ensure binaries are executable
chmod +x "${INSTALL_DIR}/func" "${INSTALL_DIR}/gozip" || true

# Make available in this shell immediately (no sourcing .bashrc)
export PATH="${INSTALL_DIR}:${PATH}"

# Persist for future shells without touching /etc/bashrc
BASHRC_D="${HOME}/.bashrc.d"
mkdir -p "${BASHRC_D}"
PROFILE_SNIPPET='export PATH="$HOME/azure-functions-cli:$PATH"'
SNIPPET_FILE="${BASHRC_D}/azure-functions-cli.sh"

# Write/overwrite the snippet idempotently
printf '%s\n' "${PROFILE_SNIPPET}" > "${SNIPPET_FILE}"

# Also append a loader to ~/.bashrc if it doesn’t already load ~/.bashrc.d/*
if ! grep -q 'bashrc\.d' "${HOME}/.bashrc" 2>/dev/null; then
  {
    echo ''
    echo '# Load per-tool snippets'
    echo 'if [ -d "$HOME/.bashrc.d" ]; then'
    echo '  for f in "$HOME/.bashrc.d/"*.sh; do [ -r "$f" ] && . "$f"; done'
    echo 'fi'
  } >> "${HOME}/.bashrc"
fi

# Show version
func --version
