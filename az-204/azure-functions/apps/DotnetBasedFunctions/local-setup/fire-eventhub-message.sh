#!/usr/bin/env bash
set -euo pipefail

MESSAGE=${MESSAGE:-"Hello eh1 from Java!"}
TOPIC=${TOPIC:-"eh1"}
BROKER=${BROKER:-"eventhubs-emulator:9092"}
NETWORK=${NETWORK:-"azure-emulators-stack_bus-net"}
KC_IMAGE="docker.io/edenhill/kcat:1.7.1"

# Default Event Hubs emulator connection string
CONNECTION_STRING=${CONNECTION_STRING:-"Endpoint=sb://eventhubs-emulator;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6AzrWRlE8kAzsYz0=;UseDevelopmentEmulator=true;"}

printf '%s\n' "$MESSAGE" | docker run --rm -i \
  --network "$NETWORK" \
  "$KC_IMAGE" \
  -b "$BROKER" \
  -t "$TOPIC" \
  -P \
  -X security.protocol=SASL_PLAINTEXT \
  -X sasl.mechanisms=PLAIN \
  -X sasl.username='$ConnectionString' \
  -X sasl.password="$CONNECTION_STRING"
