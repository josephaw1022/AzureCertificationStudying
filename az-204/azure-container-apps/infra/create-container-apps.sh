#!/usr/bin/env bash
set -euo pipefail


RESOURCE_GROUP="containerapps-demo-rg"
INFRA_RESOURCE_GROUP="containerapps-demo-infra"

CONTAINER_APP_ENV_NAME="demo-container-app-env"
CONTAINER_APP_NAME="containerapp"


TEARDOWN=false
for arg in "$@"; do
  [[ "$arg" == "--teardown" ]] && TEARDOWN=true
done

if $TEARDOWN; then
  az containerapp env delete -n $CONTAINER_APP_ENV_NAME -g $RESOURCE_GROUP -y 
  az group delete --name "$RESOURCE_GROUP" --yes --no-wait
  az group delete --name "$INFRA_RESOURCE_GROUP" --yes --no-wait
  echo "resource group deleted"
  exit 0
fi


if ! az group exists --name "$RESOURCE_GROUP" | grep -q true; then
    az group create --location "eastus" --name "$RESOURCE_GROUP"
fi



vnetName=ContainerAppsVNetTutorial
subnetName=TutorialSubnet1
vnetAddressPrefix=10.0.0.0/16
subnetAddressPrefix=10.0.0.0/24


# Create a vnet in the rg
if ! az network vnet show --name $vnetName -g $RESOURCE_GROUP >/dev/null 2>&1; then
  echo "Network being created"
  az network vnet create --name $vnetName -g $RESOURCE_GROUP \
  --address-prefixes $vnetAddressPrefix --subnet-name $subnetName \
  --subnet-prefixes $subnetAddressPrefix -l "eastus"

else 
  echo "Network already created"
fi


subnetID=$(az network vnet show --name $vnetName -g $RESOURCE_GROUP | jq -r '.subnets[0].id')
echo "subnet id = ${subnetID}"

if [[ "$(az network vnet subnet show -g "$RESOURCE_GROUP" --vnet-name "$vnetName" -n "$subnetName" \
  --query "length(delegations[?serviceName=='Microsoft.App/environments'])" -o tsv)" != "1" ]]; then
  echo "delegating the subnet to container apps"
  az network vnet subnet update -g "$RESOURCE_GROUP" --vnet-name "$vnetName" -n "$subnetName" \
    --delegations Microsoft.App/environments >/dev/null
fi





if ! az containerapp env show -n "$CONTAINER_APP_ENV_NAME" -g "$RESOURCE_GROUP" >/dev/null 2>&1; then
  echo "creating the container app env"
  az containerapp env create -n "$CONTAINER_APP_ENV_NAME" -g "$RESOURCE_GROUP" -l eastus \
    --logs-destination none  \
    -i $INFRA_RESOURCE_GROUP -s $subnetID \
    -o tsv
else
  echo "container app env already exists"
fi




# --- Create nginx container app ---
NGINX_APP_NAME="nginx-app"
NGINX_IMAGE="mcr.microsoft.com/azurelinux/base/nginx:1.25"

if ! az containerapp show -n "$NGINX_APP_NAME" -g "$RESOURCE_GROUP" >/dev/null 2>&1; then
  echo "creating nginx container app"
  az containerapp create \
    -n "$NGINX_APP_NAME" \
    -g "$RESOURCE_GROUP" \
    --environment "$CONTAINER_APP_ENV_NAME" \
    --image "$NGINX_IMAGE" \
    --ingress external \
    --target-port 80 \
    -o table
else
  echo "nginx container app already exists (updating image)"
  az containerapp update \
    -n "$NGINX_APP_NAME" \
    -g "$RESOURCE_GROUP" \
    --image "$NGINX_IMAGE" \
    -o table
fi


