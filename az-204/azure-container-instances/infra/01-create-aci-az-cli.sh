#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="quick-lab"
LOCATION="eastus"

CONTAINER_NAME="quick-container"

CONTAINER2_NAME="securetest"
CONTAINER2_FILE="secure-env.yaml"

# container-3 (MCR SQL Server + Azure Files)
CONTAINER3_NAME="quick-container-3"
CONTAINER3_DNS_LABEL="aci-sql-demo-$RANDOM"
IMAGE3="mcr.microsoft.com/mssql/server:2022-latest"
MSSQL_PID="${MSSQL_PID:-Developer}"
MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:-P@ssw0rd123!Strong}"
ACCEPT_EULA="Y"
STORAGE_ACCOUNT="acilabstorage"
SHARE_NAME="acishare"
MSSQL_MOUNT="/var/opt/mssql"


TEARDOWN=false
for arg in "$@"; do
  [[ "$arg" == "--teardown" ]] && TEARDOWN=true
done


if $TEARDOWN; then
  az group delete --name "$RESOURCE_GROUP" --yes --no-wait
  exit 0
fi

if ! az group exists --name "$RESOURCE_GROUP" | grep -q true; then
  az group create --location "$LOCATION" --name "$RESOURCE_GROUP" >/dev/null
fi

if ! az container show --resource-group "$RESOURCE_GROUP" --name "$CONTAINER_NAME" >/dev/null 2>&1; then
  DNS_NAME="aci-example-$RANDOM"
  az container create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$CONTAINER_NAME" \
    --image mcr.microsoft.com/azuredocs/aci-helloworld \
    --ports 80 \
    --dns-name-label "$DNS_NAME" \
    --location "$LOCATION" \
    --os-type Linux \
    --cpu 1 \
    --memory 1.5 >/dev/null
fi

az container show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CONTAINER_NAME" \
  --query "{FQDN:ipAddress.fqdn,ProvisioningState:provisioningState}" \
  --out table

if ! az container show --resource-group "$RESOURCE_GROUP" --name "$CONTAINER2_NAME" >/dev/null 2>&1; then
  az container create \
    --resource-group "$RESOURCE_GROUP" \
    --file "$CONTAINER2_FILE" >/dev/null
fi

az container show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CONTAINER2_NAME" \
  --query "{Name:name,ProvisioningState:provisioningState}" \
  --out table

if ! az storage account show --name "$STORAGE_ACCOUNT" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
  az storage account create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$STORAGE_ACCOUNT" \
    --location "$LOCATION" \
    --sku Standard_LRS >/dev/null
fi

STORAGE_KEY=$(az storage account keys list \
  --resource-group "$RESOURCE_GROUP" \
  --account-name "$STORAGE_ACCOUNT" \
  --query '[0].value' -o tsv)

if ! az storage share-rm show --resource-group "$RESOURCE_GROUP" --storage-account "$STORAGE_ACCOUNT" --name "$SHARE_NAME" >/dev/null 2>&1; then
  az storage share-rm create \
    --resource-group "$RESOURCE_GROUP" \
    --storage-account "$STORAGE_ACCOUNT" \
    --name "$SHARE_NAME" >/dev/null
fi

if ! az container show --resource-group "$RESOURCE_GROUP" --name "$CONTAINER3_NAME" >/dev/null 2>&1; then
  az container create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$CONTAINER3_NAME" \
    --image "$IMAGE3" \
    --os-type Linux \
    --cpu 2 \
    --memory 4 \
    --ports 1433 \
    --ip-address Public \
    --dns-name-label "$CONTAINER3_DNS_LABEL" \
    --environment-variables \
      ACCEPT_EULA="$ACCEPT_EULA" \
      MSSQL_PID="$MSSQL_PID" \
      MSSQL_SA_PASSWORD="$MSSQL_SA_PASSWORD" \
    --azure-file-volume-account-name "$STORAGE_ACCOUNT" \
    --azure-file-volume-account-key "$STORAGE_KEY" \
    --azure-file-volume-share-name "$SHARE_NAME" \
    --azure-file-volume-mount-path "$MSSQL_MOUNT" \
    --restart-policy Always >/dev/null
fi

az container show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CONTAINER3_NAME" \
  --query "{FQDN:ipAddress.fqdn,ProvisioningState:provisioningState}" \
  --out table
