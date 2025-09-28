#!/usr/bin/env bash
set -euo pipefail


podman run --replace --name linux-emulator -d --restart=unless-stopped \
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=250 \
  -e AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE=127.0.0.1 \
  -p 8081:8081 \
  -p 10250-10255:10250-10255 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest