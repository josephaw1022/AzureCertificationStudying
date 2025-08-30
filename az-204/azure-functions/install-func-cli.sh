#!/usr/bin/env bash
set -euo pipefail

# Bail out if func already exists in PATH
if command -v func >/dev/null 2>&1; then
  echo "✅ Azure Functions CLI (func) is already installed at: $(command -v func)"
  exit 0
fi

cd ~
curl -LO https://github.com/Azure/azure-functions-core-tools/releases/download/4.0.5611/Azure.Functions.Cli.linux-x64.4.0.5611.zip
unzip -d azure-functions-cli Azure.Functions.Cli.linux-x64.*.zip
rm Azure.Functions.Cli.linux-x64.*.zip
cd azure-functions-cli
chmod +x func gozip
echo 'export PATH="$HOME/azure-functions-cli:$PATH"' > ~/.bashrc.d/azure-functions-cli.sh
source ~/.bashrc
func --version
