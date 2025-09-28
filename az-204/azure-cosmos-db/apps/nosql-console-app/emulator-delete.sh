#!/usr/bin/env bash
set -euo pipefail

podman rm -f linux-emulator
echo "Deleted container: linux-emulator"
