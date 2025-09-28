#!/usr/bin/env bash
set -euo pipefail

podman run --replace --name linux-emulator -d --restart=unless-stopped \
  -p 8081:8081 -p 1234:1234 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview \
  --protocol http
