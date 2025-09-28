#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="${RG:-cosmos-nosql-rg}"
ACCOUNT="${ACCOUNT:-cosmos$(hexdump -n3 -e '"/%02x"' /dev/urandom | tr -d /)}"
LOCATION="eastus2"                  # Resource group location (doesn't drive Cosmos)
DISPLAY_REGION="East US 2"          # Cosmos location (must be the display name)
MODE="${MODE:-free-tier}"           # "free-tier" or "serverless"
DB="${DB:-appdb}"
CONTAINER="${CONTAINER:-items}"
PARTITION_KEY="${PARTITION_KEY:-/pk}"
THROUGHPUT="${THROUGHPUT:-400}"     # used only for provisioned (free-tier)

TEARDOWN=false
for arg in "$@"; do
  [[ "$arg" == "--teardown" ]] && TEARDOWN=true
done
if $TEARDOWN; then
  echo "Tearing down resource group: $RESOURCE_GROUP"
  az group delete --name "$RESOURCE_GROUP" --yes --no-wait
  exit 0
fi

# RG can live anywhere; Cosmos region is set below via DISPLAY_REGION.
az group create -n "$RESOURCE_GROUP" -l "$LOCATION" 1>/dev/null

# IMPORTANT: pass --locations as separate args (NOT one quoted string)
create_base=(
  az cosmosdb create
  -g "$RESOURCE_GROUP" -n "$ACCOUNT"
  --kind GlobalDocumentDB
  --locations "regionName=${DISPLAY_REGION}" "failoverPriority=0" "isZoneRedundant=False"
  --default-consistency-level Session
  --public-network-access Enabled
)

case "$MODE" in
  free-tier)
    "${create_base[@]}" --enable-free-tier true
    ;;
  serverless)
    "${create_base[@]}" --capabilities EnableServerless
    ;;
  *)
    echo "Invalid MODE: $MODE"; exit 1;;
esac

az cosmosdb sql database create -g "$RESOURCE_GROUP" -a "$ACCOUNT" -n "$DB" 1>/dev/null

if [[ "$MODE" == "serverless" ]]; then
  az cosmosdb sql container create \
    -g "$RESOURCE_GROUP" -a "$ACCOUNT" -d "$DB" -n "$CONTAINER" \
    --partition-key-path "$PARTITION_KEY" 1>/dev/null
else
  az cosmosdb sql container create \
    -g "$RESOURCE_GROUP" -a "$ACCOUNT" -d "$DB" -n "$CONTAINER" \
    --partition-key-path "$PARTITION_KEY" \
    --throughput "$THROUGHPUT" 1>/dev/null
fi

echo "== Cosmos DB (NoSQL) created =="
echo "Resource Group : $RESOURCE_GROUP"
echo "Account        : $ACCOUNT"
echo "Region         : $DISPLAY_REGION"
echo "Mode           : $MODE"
echo "Database       : $DB"
echo "Container      : $CONTAINER"
echo

az cosmosdb keys list -g "$RESOURCE_GROUP" -n "$ACCOUNT" \
  --type connection-strings --query "connectionStrings[0].connectionString" -o tsv
echo
echo "Teardown later with: $0 --teardown"
