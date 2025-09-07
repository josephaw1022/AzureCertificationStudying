# Azure Terraform Starter

Minimal project structure using the azurerm provider.


# Infra

## Requirements

| Name | Version |
|------|---------|
| <a name="requirement_terraform"></a> [terraform](#requirement\_terraform) | >= 1.6.0 |
| <a name="requirement_azurerm"></a> [azurerm](#requirement\_azurerm) | ~> 4.0 |

## Providers

| Name | Version |
|------|---------|
| <a name="provider_azurerm"></a> [azurerm](#provider\_azurerm) | 4.43.0 |

## Modules

No modules.

## Resources

| Name | Type |
|------|------|
| [azurerm_container_registry.basic_acr](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/container_registry) | resource |
| [azurerm_container_registry_cache_rule.ecr_public](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/container_registry_cache_rule) | resource |
| [azurerm_container_registry_cache_rule.quay_argoproj](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/container_registry_cache_rule) | resource |
| [azurerm_container_registry_task.import_nginx_latest](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/container_registry_task) | resource |
| [azurerm_eventhub.acr_diag](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/eventhub) | resource |
| [azurerm_eventhub_namespace.diag_ns](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/eventhub_namespace) | resource |
| [azurerm_eventhub_namespace_authorization_rule.diag_send](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/eventhub_namespace_authorization_rule) | resource |
| [azurerm_monitor_diagnostic_setting.acr_to_eventhub](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/monitor_diagnostic_setting) | resource |
| [azurerm_resource_group.this](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/resource_group) | resource |
| [azurerm_location.this](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/data-sources/location) | data source |

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| <a name="input_location"></a> [location](#input\_location) | Azure region | `string` | `"East US"` | no |
| <a name="input_rg_name"></a> [rg\_name](#input\_rg\_name) | Resource group name | `string` | `"demo-rg"` | no |
| <a name="input_subscription_id"></a> [subscription\_id](#input\_subscription\_id) | Subscription to deploy to | `string` | `null` | no |

## Outputs

No outputs.



# Supply Chain Workflow To Terraform

Prereqs

- Applied terraform
- ran docker/podman pull commands to get images into the registry
- invoke the nginx pull task

Goals

- Implement workflow describe in these [acr docs](https://learn.microsoft.com/en-us/azure/container-registry/how-to-continuous-patching)
- Then use azure cli to extra info about the acr tasks created
- Then recreate the acr tasks for the registry via terraform 



Create the config file 


```json
{
  "version": "v1",
  "tag-convention": "incremental",
  "repositories": [
    { "repository": "argoproj/argocd",   "tags": ["*"], "enabled": true },
    { "repository": "argoproj/argocli",  "tags": ["*"], "enabled": true },
    { "repository": "argoproj/argoexec", "tags": ["*"], "enabled": true },
    { "repository": "ecr/docker/library/nginx", "tags": ["*"], "enabled": true }
  ]
}
```


See what ACR will patch

```bash
az acr supply-chain workflow create \
  -r basicacr -g learning-acr \
  -t continuouspatchv1 \
  --config ./continuouspatching.json \
  --schedule 1d \
  --dry-run
```


Setup scheduled tasks

```bash
az acr supply-chain workflow create \
  -r basicacr -g learning-acr \
  -t continuouspatchv1 \
  --config ./continuouspatching.json \
  --schedule 7d \
  --run-immediately
```




to extract info about it, do this 

```bash
# Dump the YAML for all 3 tasks
for t in cssc-trigger-workflow cssc-scan-image cssc-patch-image; do
  az acr task show -r basicacr -n "$t" \
    --query "step.encodedTaskContent" -o tsv | base64 --decode > "${t}.yaml"
done

```


and now you can just make this be tf 


```hcl

resource "azurerm_container_registry_task" "cssc_trigger_workflow" {
  name                  = "cssc-trigger-workflow"
  container_registry_id = azurerm_container_registry.basic_acr.id

  platform { os = "Linux" }
  agent_setting { cpu = 2 }

  encoded_step {
    task_content = <<-YAML
      ...
    YAML
  }

  timer_trigger {
    name     = "azcli_defined_schedule"
    schedule = "19 18 */7 * *"
    enabled  = true
  }

  identity { type = "SystemAssigned" }

  tags = {
    clienttracking = "true"
    cssc           = "true"
  }
}

resource "azurerm_container_registry_task" "cssc_scan_image" {
  name                  = "cssc-scan-image"
  container_registry_id = azurerm_container_registry.basic_acr.id

  platform { os = "Linux" }
  agent_setting { cpu = 2 }

  encoded_step {
    task_content = <<-YAML
        ...
    YAML
  }

  identity { type = "SystemAssigned" }

  tags = {
    cssc = "true"
  }
}

resource "azurerm_container_registry_task" "cssc_patch_image" {
  name                  = "cssc-patch-image"
  container_registry_id = azurerm_container_registry.basic_acr.id

  platform { os = "Linux" }
  agent_setting { cpu = 2 }

  encoded_step {
    task_content = <<-YAML
      ...
    YAML
  }

  tags = {
    clienttracking = "true"
    cssc           = "true"
  }
}



```

