
#region boilerplate
data "azurerm_location" "this" {
  location = var.location
}

resource "azurerm_resource_group" "this" {
  name     = var.rg_name
  location = data.azurerm_location.this.location
}


locals {
  location = data.azurerm_location.this.location
  rg       = azurerm_resource_group.this.name
}

#endregion


#region basic-acr


resource "azurerm_container_registry" "basic_acr" {
  resource_group_name = local.rg
  location            = local.location
  name                = "basicacr"
  sku                 = "Basic"
}

resource "azurerm_container_registry_cache_rule" "quay_argoproj" {
  name                  = "quay-argoproj"
  container_registry_id = azurerm_container_registry.basic_acr.id

  source_repo = "quay.io/argoproj/*"
  target_repo = "argoproj/*"
}


resource "azurerm_container_registry_cache_rule" "ecr_public" {
  name                  = "ecr-public"
  container_registry_id = azurerm_container_registry.basic_acr.id
  source_repo           = "public.ecr.aws/*"
  target_repo           = "ecr/*"
}


resource "azurerm_container_registry_task" "import_nginx_latest" {
  name                  = "import-nginx-latest"
  container_registry_id = azurerm_container_registry.basic_acr.id

  platform {
    os = "Linux"
  }

  encoded_step {
    task_content = <<-YAML
      version: v1.1.0
      steps:
        - cmd: docker pull $Registry/ecr/docker/library/nginx:latest
    YAML
  }
}





#endregion



#region continous patching
# resource "azurerm_container_registry_task" "cssc_trigger_workflow" {
#   name                  = "cssc-trigger-workflow"
#   container_registry_id = azurerm_container_registry.basic_acr.id

#   platform { os = "Linux" }

#   encoded_step {
#     task_content = <<-YAML
#       version: v1.1.0
#       alias:
#         values:
#           ScanImageAndSchedulePatchTask: cssc-scan-image
#           cssc : mcr.microsoft.com/acr/cssc:cbcf692
#           maxLimit: 100
#       steps:
#         - cmd: bash -c 'echo "Inside cssc-trigger-workflow task, getting list of images to be patched based on --filter-policy for Registry {{.Run.Registry}}."'
#         - cmd: cssc acr cssc patch --filter-policy csscpolicies/patchpolicy:v1 --dry-run > filterRepos.txt
#           env:
#             - ACR_EXPERIMENTAL_CSSC=true
#         - cmd: bash -c 'sed -n "/^Validating/,/^Total/ {/^Validating/b;/^Total/b;p}" filterRepos.txt' > filterReposToDisplay.txt
#         - cmd: |
#             bash -c '
#             echo "Below images will be scanned and patched (if any os vulnerabilities found) based on --filter-policy.\n$(cat filterReposToDisplay.txt)"
#             totalImages=$(sed -n "s/^Matches found://p" filterReposToDisplay.txt | tr -d "[:space:]")
#             if [ $totalImages -gt $maxLimit ]; then
#               echo "You have exceeded the maximum limit of $maxLimit images that can be scheduled for continuous patching. Adjust the JSON filter to limit the number of images. Failing the workflow."
#               exit 1
#             fi'
#         - cmd: cssc acr cssc patch --filter-policy csscpolicies/patchpolicy:v1 --show-patch-tags --dry-run> filterReposWithPatchTags.txt
#           env:
#             - ACR_EXPERIMENTAL_CSSC=true
#         - cmd: bash -c 'sed -n "/^Listing/,/^Matches/ {/^Listing/b;/^Matches/b;/^Repo/b;p}" filterReposWithPatchTags.txt' > filteredReposAndTags.txt
#         - cmd: bash -c 'sed -n "/^Configured Tag Convention:/p" filterReposWithPatchTags.txt' > tagConvention.txt
#         - cmd: az login --identity --allow-no-subscriptions
#         - id: scan-and-schedule-patch
#           timeout: 1800
#           cmd: |
#               az -c '
#               counter=0; \
#               batchSize=10; \
#               sleepDuration=30; \
#               RegistryName={{.Run.Registry}}; \
#               while read line;do \
#               IFS=',' read -r -a array <<< "$${line}"
#               RepoName=$${array[0]}
#               OriginalTag=$${array[1]}
#               TagName=$${array[2]}
#               IncrementedTagNumber=""
#               echo "Tag Convention details: $(cat tagConvention.txt)"
#               if grep -q "floating" tagConvention.txt; then
#                 IncrementedTagNumber="patched"
#               else
#                 IncrementedTagNumber="1"
#               fi

#               if [ $TagName == "N/A" ]; then
#                 TagName=$OriginalTag
#               elif [[ $TagName =~ -([0-9]{1,3})$ ]]; then
#                 TagNumber=$${BASH_REMATCH[1]}
#                 IncrementedTagNumber=$((TagNumber+1))
#               fi
#               echo "Scheduling $ScanImageAndSchedulePatchTask for $RegistryName/$RepoName, Tag:$TagName, OriginalTag:$OriginalTag, PatchTag:$OriginalTag-$IncrementedTagNumber"; \
#               az acr task run --name $ScanImageAndSchedulePatchTask --registry $RegistryName --set SOURCE_REPOSITORY=$RepoName --set SOURCE_IMAGE_TAG=$TagName --set SOURCE_IMAGE_ORIGINAL_TAG=$OriginalTag --set SOURCE_IMAGE_NEWPATCH_TAG=$IncrementedTagNumber --no-wait; \
#               counter=$((counter+1)); \
#               if [ $((counter%batchSize)) -eq 0 ]; then \
#                 echo "Waiting for $sleepDuration seconds before scheduling scans for next batch of images"; \
#                 sleep $sleepDuration; \
#               fi; \
#               done < filteredReposAndTags.txt;'
#           entryPoint: /bin/bash
#     YAML
#   }

#   timer_trigger {
#     name     = "azcli_defined_schedule"
#     schedule = "19 18 */7 * *"
#     enabled  = true
#   }

#   identity { type = "SystemAssigned" }

#   tags = {
#     clienttracking = "true"
#     cssc           = "true"
#   }
# }

# resource "azurerm_container_registry_task" "cssc_scan_image" {
#   name                  = "cssc-scan-image"
#   container_registry_id = azurerm_container_registry.basic_acr.id

#   platform { os = "Linux" }

#   encoded_step {
#     task_content = <<-YAML
#       version: v1.1.0
#       alias:
#         values:
#           patchimagetask: cssc-patch-image
#           DATE: $(date "+%Y-%m-%d")
#           cssc : mcr.microsoft.com/acr/cssc:cbcf692
#       steps:
#         - id: print-inputs
#           cmd: |
#               bash -c 'echo "Scanning image for vulnerability {{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}} for tag {{.Values.SOURCE_IMAGE_ORIGINAL_TAG}}"'
#               bash -c 'echo "Scanning repo: {{.Values.SOURCE_REPOSITORY}}, Tag:{{.Values.SOURCE_IMAGE_TAG}}, OriginalTag:{{.Values.SOURCE_IMAGE_ORIGINAL_TAG}}"'
#         - id: setup-data-dir
#           cmd: bash mkdir ./data

#         - id: generate-trivy-report
#           retries: 3
#           retryDelay: 5
#           timeout: 1800
#           cmd: |
#             cssc trivy image \
#             {{.Run.Registry}}/{{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}} \
#             --pkg-types os \
#             --ignore-unfixed \
#             --format json \
#             --timeout 30m \
#             --scanners vuln \
#             --db-repository "ghcr.io/aquasecurity/trivy-db:2","public.ecr.aws/aquasecurity/trivy-db" \
#             --output /workspace/data/vulnerability-report_trivy_$DATE.json

#         - cmd: cssc jq 'if .Results == null or (.Results | length) == 0 then 0 else [.Results[] | select(.Vulnerabilities != null) | .Vulnerabilities | length] | add end' /workspace/data/vulnerability-report_trivy_$DATE.json > /workspace/data/vulCount.txt
#         - cmd: cssc jq 'if .Metadata.OS.EOSL == null then false else .Metadata.OS.EOSL end' /workspace/data/vulnerability-report_trivy_$DATE.json > /workspace/data/eosl.txt
#         - cmd: |
#               bash -c 'echo "Vulnerabilities found for image {{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}} -> $(cat /workspace/data/vulCount.txt)"'
#               bash -c 'echo "EOSL for the image {{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}} -> $(cat /workspace/data/eosl.txt)"'

#         - id: trigger-patch-task
#           retries: 3
#           retryDelay: 10
#           timeout: 1800
#           cmd: |
#             az -c '
#             vulCount=$(cat /workspace/data/vulCount.txt) && \
#             eoslValue=$(cat /workspace/data/eosl.txt) && \
#             if ! [[ "$vulCount" =~ ^[0-9]+$ ]]; then vulCount=0; fi && \
#             if [ "$eoslValue" = "true" ]; then \
#               echo "PATCHING will be skipped as EOSL is $eoslValue for image {{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}}"; \
#             elif [ $vulCount -gt 0 ]; then \
#                 az login --identity --allow-no-subscriptions; \
#                 echo "Total OS vulnerabilities found -> $vulCount"; \
#                 echo "PATCHING task scheduled for image {{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}}, new patch tag will be {{.Values.SOURCE_IMAGE_ORIGINAL_TAG}}-{{.Values.SOURCE_IMAGE_NEWPATCH_TAG}}"; \
#                 az acr task run --name $patchimagetask --registry $RegistryName --set SOURCE_REPOSITORY={{.Values.SOURCE_REPOSITORY}} --set SOURCE_IMAGE_TAG={{.Values.SOURCE_IMAGE_ORIGINAL_TAG}} --set SOURCE_IMAGE_NEWPATCH_TAG={{.Values.SOURCE_IMAGE_NEWPATCH_TAG}} --no-wait; \
#               else \
#                 echo "PATCHING will be skipped as no vulnerability found in the image {{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}}"; \
#             fi'
#           entryPoint: /bin/bash
#     YAML
#   }

#   identity { type = "SystemAssigned" }

#   tags = { cssc = "true" }
# }

# resource "azurerm_container_registry_task" "cssc_patch_image" {
#   name                  = "cssc-patch-image"
#   container_registry_id = azurerm_container_registry.basic_acr.id

#   platform { os = "Linux" }

#   encoded_step {
#     task_content = <<-YAML
#       version: v1.1.0
#       alias:
#         values:
#           ScanReport : os-vulnerability-report_trivy_{{ regexReplaceAll "[^a-zA-Z0-9]" .Values.SOURCE_REPOSITORY "-" }}_{{.Values.SOURCE_IMAGE_TAG}}_$(date "+%Y-%m-%d").json
#           cssc : mcr.microsoft.com/acr/cssc:cbcf692
#       steps:
#         - id: print-inputs
#           cmd: |
#               bash -c 'echo "Patching OS vulnerabilities for image {{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}}"'
#               bash -c 'echo "Patching repo: {{.Values.SOURCE_REPOSITORY}}, Tag:{{.Values.SOURCE_IMAGE_TAG}}, NewPatchTag:{{.Values.SOURCE_IMAGE_NEWPATCH_TAG}}"'

#         - id: check-patch-tag
#           cmd: |
#              bash -c 'echo "New Patch tag is {{.Values.SOURCE_IMAGE_NEWPATCH_TAG}}"
#              if [ "{{.Values.SOURCE_IMAGE_NEWPATCH_TAG}}" != "patched" ] && [ {{.Values.SOURCE_IMAGE_NEWPATCH_TAG}} -gt 999 ]; then
#                 echo "New Patch tag is greater than 999. No more than 1000 patches can be created for a tag. Exiting the patching workflow."
#                 exit 1
#              fi'

#         - id: setup-data-dir
#           cmd: bash mkdir ./data
#         - id: generate-trivy-report
#           retries: 3
#           retryDelay: 5
#           timeout: 1800
#           cmd: |
#             cssc trivy image \
#             {{.Run.Registry}}/{{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}} \
#             --pkg-types os \
#             --ignore-unfixed \
#             --format json \
#             --timeout 30m \
#             --scanners vuln \
#             --db-repository "ghcr.io/aquasecurity/trivy-db:2","public.ecr.aws/aquasecurity/trivy-db" \
#             --output /workspace/data/$ScanReport

#         - id: buildkitd
#           cmd: mobybuildkit --addr tcp://0.0.0.0:8888
#           entrypoint: buildkitd
#           detach: true
#           privileged: true
#           ports: ["127.0.0.1:8888:8888/tcp"]

#         - id: patch-image
#           retries: 3
#           retryDelay: 5
#           timeout: 1800
#           cmd: |
#             cssc copa patch \
#             -i "{{.Run.Registry}}/{{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}}" \
#             -r ./data/$ScanReport \
#             -t "{{.Values.SOURCE_IMAGE_TAG}}-{{.Values.SOURCE_IMAGE_NEWPATCH_TAG}}" \
#             --timeout 30m \
#             --addr tcp://127.0.0.1:8888
#           network: host

#         - id: push-image
#           retries: 3
#           retryDelay: 5
#           timeout: 1800
#           cmd: docker push {{.Run.Registry}}/{{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}}-{{.Values.SOURCE_IMAGE_NEWPATCH_TAG}}
#         - cmd: bash echo "Patched image pushed to {{.Run.Registry}}/{{.Values.SOURCE_REPOSITORY}}:{{.Values.SOURCE_IMAGE_TAG}}-{{.Values.SOURCE_IMAGE_NEWPATCH_TAG}}"
#     YAML
#   }

#   tags = {
#     clienttracking = "true"
#     cssc           = "true"
#   }
# }



#endregion


#region eventhub

resource "azurerm_eventhub_namespace" "diag_ns" {
  name                = "acrdiag-ns"
  location            = local.location
  resource_group_name = local.rg
  sku                 = "Basic"
  capacity            = 1
}

resource "azurerm_eventhub" "acr_diag" {
  name                = "acr-diagnostics"
  namespace_name      = azurerm_eventhub_namespace.diag_ns.name
  resource_group_name = local.rg

  partition_count   = 2
  message_retention = 1
}

resource "azurerm_eventhub_namespace_authorization_rule" "diag_send" {
  name                = "acr-diag-send"
  namespace_name      = azurerm_eventhub_namespace.diag_ns.name
  resource_group_name = local.rg

  listen = false
  send   = true
  manage = false
}


resource "azurerm_monitor_diagnostic_setting" "acr_to_eventhub" {
  name                           = "acr-to-eventhub"
  target_resource_id             = azurerm_container_registry.basic_acr.id
  eventhub_name                  = azurerm_eventhub.acr_diag.name
  eventhub_authorization_rule_id = azurerm_eventhub_namespace_authorization_rule.diag_send.id

  // All logs
  enabled_log {
    category_group = "AllLogs"
  }

  // All metrics
  enabled_metric {
    category = "AllMetrics"
  }
}

#endregion 
