# Day 23 — Bicep IaC

MarbleCraft Imports Pvt Ltd — Distributor Order & Stock Allocation platform (MarbleCraftOMS).  
This folder contains the complete Azure Bicep infrastructure-as-code for the MarbleCraftOMS API.

---

## Folder structure

```
infra/
├── main.bicep                  # Orchestrates all three modules
├── modules/
│   ├── sql.bicep               # Azure SQL Server + Database
│   ├── servicebus.bicep        # Service Bus namespace, topics, subscriptions
│   └── api.bicep               # Container App Environment + Container App
├── dev.parameters.json         # Dev environment parameter values
└── prod.parameters.json        # Prod environment parameter values
```

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Azure CLI | `az --version` to verify |
| Bicep CLI | `az bicep install` or `az bicep upgrade` |
| Resource group | Must exist before deployment |
| Key Vault | Pre-existing; must hold secret `sql-admin-password` and `sql-connection-string` |

Create the resource group if it does not exist:

```bash
az group create --name marblecraft-dev-rg --location eastasia
az group create --name marblecraft-prod-rg --location eastasia
```

---

## One-command deployment

**Dev:**
```bash
az deployment group create \
  --resource-group marblecraft-dev-rg \
  --template-file main.bicep \
  --parameters dev.parameters.json
```

**Prod:**
```bash
az deployment group create \
  --resource-group marblecraft-prod-rg \
  --template-file main.bicep \
  --parameters prod.parameters.json
```

---

## Dry run (what-if)

```bash
az deployment group what-if \
  --resource-group marblecraft-dev-rg \
  --template-file main.bicep \
  --parameters dev.parameters.json
```

### What-if output (dev)

![What-if output part 1](../screenshots/what-if%20output_whatif-dev-01.png)
![What-if output part 2](../screenshots/what-if%20output_whatif-dev-02.png)

```
Note: The result may contain false positive predictions (noise).
You can help us improve the accuracy of the result by opening an issue here: https://aka.ms/WhatIfIssues

Resource and property changes are indicated with this symbol:
  + Create

The deployment will update the following scope:

Scope: /subscriptions/32fa43c7-320e-4315-bbf7-e4c4593a899d/resourceGroups/marblecraft-dev-rg

  + Microsoft.App/containerApps/marblecraft-dev-api [2023-05-01]

      apiVersion:                                     "2023-05-01"
      identity.type:                                  "SystemAssigned"
      location:                                       "eastasia"
      name:                                           "marblecraft-dev-api"
      properties.configuration.ingress.allowInsecure: false
      properties.configuration.ingress.external:      true
      properties.configuration.ingress.targetPort:    8080
      properties.configuration.ingress.transport:     "auto"
      properties.configuration.secrets: [
        0:
          identity:    "system"
          keyVaultUrl: "https://marblecraft-dev-kv.vault.azure.net/secrets/sql-connection-string"
          name:        "*******"
      ]
      properties.template.containers: [
        0:
          env: [
            0:
              name:      "ConnectionStrings__DefaultConnection"
              secretRef: "*******"
            1:
              name:  "ASPNETCORE_ENVIRONMENT"
              value: "Development"
          ]
          image:            "mcr.microsoft.com/dotnet/samples:aspnetapp"
          name:             "api"
          resources.cpu:    "0.5"
          resources.memory: "1Gi"
      ]
      properties.template.scale.maxReplicas: 2
      properties.template.scale.minReplicas: 0
      tags.environment:                      "dev"
      type:                                  "Microsoft.App/containerApps"

  + Microsoft.App/managedEnvironments/marblecraft-dev-cae [2023-05-01]

      location:                 "eastasia"
      name:                     "marblecraft-dev-cae"
      properties.zoneRedundant: false
      tags.environment:         "dev"
      type:                     "Microsoft.App/managedEnvironments"

  + Microsoft.KeyVault/vaults/marblecraft-dev-kv/providers/Microsoft.Authorization/roleAssignments/15e22041-a185-5b7a-8179-0fe462d9ecd5 [2022-04-01]

      properties.roleDefinitionId: "/subscriptions/.../Microsoft.Authorization/roleDefinitions/4633458b-17de-408a-b874-0445c86b69e6"
      type:                        "Microsoft.Authorization/roleAssignments"

  + Microsoft.ServiceBus/namespaces/marblecraft-dev-sb [2021-11-01]

      location:         "eastasia"
      name:             "marblecraft-dev-sb"
      sku.name:         "Standard"
      tags.environment: "dev"
      type:             "Microsoft.ServiceBus/namespaces"

  + Microsoft.ServiceBus/namespaces/marblecraft-dev-sb/topics/low-stock-events [2021-11-01]

      properties.defaultMessageTimeToLive: "P14D"
      properties.enablePartitioning:       false
      properties.maxSizeInMegabytes:       1024
      type:                                "Microsoft.ServiceBus/namespaces/topics"

  + Microsoft.ServiceBus/namespaces/marblecraft-dev-sb/topics/low-stock-events/subscriptions/low-stock-handler [2021-11-01]

      properties.deadLetteringOnMessageExpiration: true
      properties.lockDuration:                     "PT1M"
      properties.maxDeliveryCount:                 10
      type:                                        "Microsoft.ServiceBus/namespaces/topics/subscriptions"

  + Microsoft.ServiceBus/namespaces/marblecraft-dev-sb/topics/order-status-changed [2021-11-01]

      properties.defaultMessageTimeToLive: "P14D"
      properties.enablePartitioning:       false
      properties.maxSizeInMegabytes:       1024
      type:                                "Microsoft.ServiceBus/namespaces/topics"

  + Microsoft.ServiceBus/namespaces/marblecraft-dev-sb/topics/order-status-changed/subscriptions/order-status-handler [2021-11-01]

      properties.deadLetteringOnMessageExpiration: true
      properties.lockDuration:                     "PT1M"
      properties.maxDeliveryCount:                 10
      type:                                        "Microsoft.ServiceBus/namespaces/topics/subscriptions"

  + Microsoft.Sql/servers/marblecraft-dev-sqlserver [2022-11-01-preview]

      location:                              "eastasia"
      name:                                  "marblecraft-dev-sqlserver"
      properties.administratorLogin:         "*******"
      properties.administratorLoginPassword: "*******"
      properties.minimalTlsVersion:          "1.2"
      properties.publicNetworkAccess:        "Enabled"
      tags.environment:                      "dev"
      type:                                  "Microsoft.Sql/servers"

  + Microsoft.Sql/servers/marblecraft-dev-sqlserver/databases/marblecraft-dev-db [2022-11-01-preview]

      location:                "eastasia"
      name:                    "marblecraft-dev-db"
      properties.collation:    "SQL_Latin1_General_CP1_CI_AS"
      properties.maxSizeBytes: 2147483648
      sku.name:                "Basic"
      tags.environment:        "dev"
      type:                    "Microsoft.Sql/servers/databases"

Resource changes: 10 to create.
```

---

## Resource inventory

All resources follow the naming pattern `marblecraft-{env}-{resource}`.

| Resource type | Dev name | Prod name |
|---|---|---|
| SQL Server | `marblecraft-dev-sqlserver` | `marblecraft-prod-sqlserver` |
| SQL Database | `marblecraft-dev-db` | `marblecraft-prod-db` |
| Service Bus Namespace | `marblecraft-dev-sb` | `marblecraft-prod-sb` |
| SB Topic | `low-stock-events` | `low-stock-events` |
| SB Topic | `order-status-changed` | `order-status-changed` |
| Container App Env | `marblecraft-dev-cae` | `marblecraft-prod-cae` |
| Container App | `marblecraft-dev-api` | `marblecraft-prod-api` |

---

## Dev vs Prod comparison

| Setting | Dev | Prod |
|---|---|---|
| SQL SKU | Basic, 2 GB | Standard S2, 50 GB |
| Service Bus tier | Standard | Standard |
| Container CPU | 0.5 cores | 1 core |
| Container Memory | 1 Gi | 2 Gi |
| Min replicas | 0 (scale to zero) | 1 (always on) |
| Max replicas | 2 | 5 |

---

## Security design

- **Zero hardcoded secrets** — SQL admin password arrives via a Key Vault reference in the parameters file, never as plain text.
- **Managed Identity** — the Container App uses a System-Assigned Managed Identity; no connection strings in environment variables.
- **Key Vault Secrets User role** — `main.bicep` grants the Container App identity the built-in `Key Vault Secrets User` role (RBAC) so it can read the SQL connection string at runtime.
- **TLS 1.2 minimum** — enforced on the SQL Server.
- **HTTPS only** — Container App ingress has `allowInsecure: false`.

---

## Parameter reference

| Parameter | Description |
|---|---|
| `environment` | `dev` or `prod` — drives all SKU and scaling decisions |
| `location` | Azure region (default: `eastasia`) |
| `sqlAdminLogin` | SQL Server administrator username |
| `sqlAdminPassword` | SQL admin password — Key Vault reference only, never plain text |
| `sqlSku` | `Basic` (dev) or `Standard` (prod) |
| `minReplicas` | Minimum Container App replicas (`0` dev, `1` prod) |
| `keyVaultName` | Name of the pre-existing Key Vault |
| `containerImage` | Container image to deploy (default: dotnet sample app) |
| `existingContainerAppEnvId` | Resource ID of an existing CAE to reuse (empty = create new) |

---

# Day 24 — AZD + Azure Deployment Stacks

Deploy the full stack driven by the Azure Developer CLI (`azd`), with Azure Deployment Stacks wrapping all resources for clean teardown and drift detection.

---

## What Deployment Stacks give you over plain deployments

> Azure Deployment Stacks track all provisioned resources as one managed group — enabling `denyDelete` protection against accidental portal deletion and atomic teardown via a single `az stack group delete --action-on-unmanage deleteAll`, neither of which is possible with a plain `az deployment group create`.

---

## AZD configuration (`azure.yaml`)

```yaml
name: marblecraft-oms
metadata:
  template: marblecraft-oms@0.0.1-beta

hooks:
  preprovision:
    windows:
      shell: pwsh
      run: ./scripts/pre-provision.ps1
      continueOnError: false
      interactive: true
    posix:
      shell: sh
      run: ./scripts/pre-provision.sh
      continueOnError: false
      interactive: true

  postprovision:
    windows:
      shell: pwsh
      run: ./scripts/post-provision.ps1
      continueOnError: false
      interactive: true
    posix:
      shell: sh
      run: ./scripts/post-provision.sh
      continueOnError: false
      interactive: true

infra:
  provider: bicep
  path: infra
  module: main
```

---

## Deploy commands

```bash
# Dev
azd env new dev
azd env set AZURE_SUBSCRIPTION_ID <id>
azd env set AZURE_LOCATION eastasia
azd env set AZURE_RESOURCE_GROUP marblecraft-dev-rg
azd env set SQL_ADMIN_LOGIN marblecraft-dev-admin
azd env set MIN_REPLICAS 0
azd env set SQL_SKU Basic
azd env set KEY_VAULT_NAME mc-dev-kv-32fa
azd env set KEY_VAULT_RESOURCE_ID /subscriptions/<id>/resourceGroups/marblecraft-dev-rg/providers/Microsoft.KeyVault/vaults/mc-dev-kv-32fa
azd up --environment dev

# Prod (promotes same Bicep to prod resource group)
azd env new prod
azd env set AZURE_SUBSCRIPTION_ID <id>
azd env set AZURE_LOCATION eastasia
azd env set AZURE_RESOURCE_GROUP marblecraft-prod-rg
azd env set SQL_ADMIN_LOGIN marblecraft-prod-admin
azd env set MIN_REPLICAS 1
azd env set SQL_SKU Standard
azd env set KEY_VAULT_NAME mc-prod-kv-32fa
azd env set KEY_VAULT_RESOURCE_ID /subscriptions/<id>/resourceGroups/marblecraft-prod-rg/providers/Microsoft.KeyVault/vaults/mc-prod-kv-32fa
azd env set CONTAINER_APP_ENV_ID /subscriptions/<id>/resourceGroups/marblecraft-dev-rg/providers/Microsoft.App/managedEnvironments/marblecraft-dev-cae
azd up --environment prod
```

---

## Dev deploy output

```
==> Pre-provision: environment 'dev'
    Subscription : 32fa43c7-320e-4315-bbf7-e4c4593a899d
    Location     : eastasia
    Resource Grp : marblecraft-dev-rg
    Checking Key Vault 'mc-dev-kv-32fa' exists...
==> Pre-provision checks passed.

  (✓) Done: Service Bus Namespace: marblecraft-dev-sbus (682ms)
  (✓) Done: Container Apps Environment: marblecraft-dev-cae (3.084s)
  (✓) Done: Azure SQL Server: marblecraft-dev-sqlserver (8.031s)
  (✓) Done: Container App: marblecraft-dev-api (22.671s)

==> Post-provision: creating Deployment Stack 'marblecraft-dev-stack'
    Resource Group : marblecraft-dev-rg
    Template       : ./infra/main.bicep
    Stack params   : ...marblecraft-stack-params-dev.json (Key Vault reference, no plain-text secret)

    Running: az stack group create ...

==> Stack created. Verifying resources tracked:
{
  "Resources": [
    ".../Microsoft.App/containerApps/marblecraft-dev-api",
    ".../Microsoft.App/managedEnvironments/marblecraft-dev-cae",
    ".../Microsoft.KeyVault/vaults/mc-dev-kv-32fa/.../roleAssignments/...",
    ".../Microsoft.ServiceBus/namespaces/marblecraft-dev-sbus",
    ".../marblecraft-dev-sbus/topics/low-stock-events",
    ".../marblecraft-dev-sbus/topics/low-stock-events/subscriptions/low-stock-handler",
    ".../marblecraft-dev-sbus/topics/order-status-changed",
    ".../marblecraft-dev-sbus/topics/order-status-changed/subscriptions/order-status-handler",
    ".../Microsoft.Sql/servers/marblecraft-dev-sqlserver",
    ".../marblecraft-dev-sqlserver/databases/marblecraft-dev-db"
  ],
  "Stack": "marblecraft-dev-stack",
  "State": "succeeded"
}

==> Deployment Stack 'marblecraft-dev-stack' is active.
    Resources     : 10 tracked as one managed group
    Deny settings : denyDelete - portal deletion blocked

SUCCESS: Your application was provisioned and deployed to Azure in 2 minutes 53 seconds.
```

---

## Prod deploy output

```
==> Pre-provision: environment 'prod'
    Subscription : 32fa43c7-320e-4315-bbf7-e4c4593a899d
    Location     : eastasia
    Resource Grp : marblecraft-prod-rg
    Checking Key Vault 'mc-prod-kv-32fa' exists...
==> Pre-provision checks passed.

  (✓) Done: Service Bus Namespace: marblecraft-prod-sbus (18.849s)
  (✓) Done: Container App: marblecraft-prod-api (22.591s)
  (✓) Done: Azure SQL Server: marblecraft-prod-sqlserver (1m9.889s)

==> Post-provision: creating Deployment Stack 'marblecraft-prod-stack'
    Resource Group : marblecraft-prod-rg
    Template       : ./infra/main.bicep
    Stack params   : ...marblecraft-stack-params-prod.json (Key Vault reference, no plain-text secret)

    Running: az stack group create ...

==> Stack created. Verifying resources tracked:
{
  "Resources": [
    ".../Microsoft.App/containerApps/marblecraft-prod-api",
    ".../Microsoft.KeyVault/vaults/mc-prod-kv-32fa/.../roleAssignments/...",
    ".../Microsoft.ServiceBus/namespaces/marblecraft-prod-sbus",
    ".../marblecraft-prod-sbus/topics/low-stock-events",
    ".../marblecraft-prod-sbus/topics/low-stock-events/subscriptions/low-stock-handler",
    ".../marblecraft-prod-sbus/topics/order-status-changed",
    ".../marblecraft-prod-sbus/topics/order-status-changed/subscriptions/order-status-handler",
    ".../Microsoft.Sql/servers/marblecraft-prod-sqlserver",
    ".../marblecraft-prod-sqlserver/databases/marblecraft-prod-db"
  ],
  "Stack": "marblecraft-prod-stack",
  "State": "succeeded"
}

==> Deployment Stack 'marblecraft-prod-stack' is active.
    Resources     : 10 tracked as one managed group
    Deny settings : denyDelete - portal deletion blocked

SUCCESS: Your application was provisioned and deployed to Azure in 2 minutes 4 seconds.
```

---

## Stack verification

```bash
az stack group show \
  --name marblecraft-dev-stack \
  --resource-group marblecraft-dev-rg \
  --query "{Stack:name, State:provisioningState, Resources:length(resources), DenyMode:denySettings.mode}"

az stack group show \
  --name marblecraft-prod-stack \
  --resource-group marblecraft-prod-rg \
  --query "{Stack:name, State:provisioningState, Resources:length(resources), DenyMode:denySettings.mode}"
```

Output:
```
Stack                  State      Resources  DenyMode
---------------------  ---------  ---------  ----------
marblecraft-dev-stack  succeeded  10         denyDelete
marblecraft-prod-stack succeeded  9          denyDelete
```

---

## Screenshots

**`azd up` — dev environment**
![azd up dev output](../screenshots/azd-up-dev.png)

**`azd up` — prod environment**
![azd up prod output](../screenshots/azd-up-prod.png)

**Azure Portal — dev Deployment Stack**
![Portal dev stack](../screenshots/portal-dev-stack.png)

**Azure Portal — prod Deployment Stack**
![Portal prod stack](../screenshots/portal-prod-stack.png)
