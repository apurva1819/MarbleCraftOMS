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
![azd up dev output](../screenshots/Day24_Deployement_stacks+azd/azd-up-dev.png)

**`azd up` — prod environment**
![azd up prod output](../screenshots/Day24_Deployement_stacks+azd/azd-up-prod.png)

**Stack verification output**
![Stack verification](../screenshots/Day24_Deployement_stacks+azd/azd-up-stack-verification.png)

**Azure Portal — dev Deployment Stack**
![Portal dev stack](../screenshots/Day24_Deployement_stacks+azd/portal-dev-stack.png)

**Azure Portal — prod Deployment Stack**
![Portal prod stack](../screenshots/Day24_Deployement_stacks+azd/portal-prod-stack.png)
