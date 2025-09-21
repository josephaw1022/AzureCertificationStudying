#! /usr/bin/env bash
set -euo pipefail

sku="P1v4"
resourceGroup="quick-azure-app-service-poc"
location="eastus2"
containerImageToUse="mcr.microsoft.com/appsvc/staticsite:latest"

teardown=false
for arg in "$@"; do
  [[ "$arg" == "--teardown" ]] && teardown=true
done

if $teardown; then
    echo "Tearing down resource group: $resourceGroup"
    az group delete --name "$resourceGroup" --yes --no-wait
    exit 0
fi

planName="${resourceGroup}-plan"
webappName="${resourceGroup}-web"

echo "Creating resource group: $resourceGroup in $location"
az group create --name "$resourceGroup" --location "$location" 1>/dev/null

echo "Creating app service plan ($sku): $planName"
az appservice plan create \
  --name "$planName" \
  --resource-group "$resourceGroup" \
  --location "$location" \
  --sku "$sku" \
  --is-linux 1>/dev/null

echo "Creating or updating web app: $webappName"
if ! az webapp show --name "$webappName" --resource-group "$resourceGroup" >/dev/null 2>&1; then
  az webapp create \
    --resource-group "$resourceGroup" \
    --plan "$planName" \
    --name "$webappName" \
    --deployment-container-image-name "$containerImageToUse" 1>/dev/null
else
  az webapp config container set \
    --name "$webappName" \
    --resource-group "$resourceGroup" \
    --container-image-name "$containerImageToUse" 1>/dev/null
fi

defaultHostName=$(az webapp show --name "$webappName" --resource-group "$resourceGroup" --query defaultHostName -o tsv 2>/dev/null || echo "")
echo "App: https://$defaultHostName"
echo "Image: $containerImageToUse"
echo "Region: $location"
