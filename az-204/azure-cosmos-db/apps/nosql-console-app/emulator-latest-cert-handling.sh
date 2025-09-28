#!/usr/bin/env bash
set -euo pipefail

CERT_FILE="/etc/pki/ca-trust/source/anchors/cosmos-emulator.crt"
CERT_URL="https://localhost:8081/_explorer/emulator.pem"

if [ -f "$CERT_FILE" ]; then
  echo "Removing existing certificate at $CERT_FILE"
  sudo rm -f "$CERT_FILE"
fi

echo "Downloading Cosmos DB Emulator certificate..."
sudo curl --insecure "$CERT_URL" --output "$CERT_FILE"

echo "Updating CA trust store..."
sudo update-ca-trust

echo "Certificate installed to $CERT_FILE"
