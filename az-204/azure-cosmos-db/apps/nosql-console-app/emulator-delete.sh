#!/usr/bin/env bash
set -euo pipefail

podman rm -f linux-emulator || true
podman volume rm cosmosdb-data || true

echo "Deleted container: linux-emulator"
