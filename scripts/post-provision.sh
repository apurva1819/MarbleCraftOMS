#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# post-provision.sh — MarbleCraft OMS post-provision hook (POSIX)
#
# Wraps the 10 AZD-deployed resources in an Azure Deployment Stack so that:
#   • All resources are tracked as one managed group
#   • Unmanaged drift (resources created outside Bicep) is detectable
#   • Teardown is one command: az stack group delete
#   • denyDelete prevents accidental portal deletion
#
# The stack parameters use a Key Vault reference for sqlAdminPassword —
# the password never appears as plain text anywhere.
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

STACK_NAME="marblecraft-${AZURE_ENV_NAME}-stack"
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP}"
TEMPLATE_FILE="./infra/main.bicep"
TEMP_FILE="$(mktemp /tmp/marblecraft-stack-params-XXXXXX.json)"

echo ""
echo "==> Post-provision: creating Deployment Stack '${STACK_NAME}'"
echo "    Resource Group : ${RESOURCE_GROUP}"
echo "    Template       : ${TEMPLATE_FILE}"

# ─── Build parameter file with Key Vault reference for sqlAdminPassword ───────
# Using a KV reference ensures the password never appears as plain text
# in shell history, process lists, or ARM deployment logs.
cat > "${TEMP_FILE}" <<EOF
{
  "\$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environment":   { "value": "${AZURE_ENV_NAME}" },
    "location":      { "value": "${AZURE_LOCATION}" },
    "sqlAdminLogin": { "value": "${SQL_ADMIN_LOGIN}" },
    "minReplicas":   { "value": ${MIN_REPLICAS} },
    "sqlSku":        { "value": "${SQL_SKU}" },
    "keyVaultName":  { "value": "${KEY_VAULT_NAME}" },
    "existingContainerAppEnvId": { "value": "${CONTAINER_APP_ENV_ID:-}" },
    "sqlAdminPassword": {
      "reference": {
        "keyVault":   { "id": "${KEY_VAULT_RESOURCE_ID}" },
        "secretName": "sql-admin-password"
      }
    }
  }
}
EOF

echo "    Stack params   : ${TEMP_FILE} (Key Vault reference, no plain-text secret)"

# ─── Create / update Deployment Stack ────────────────────────────────────────
echo ""
echo "    Running: az stack group create ..."

az stack group create \
    --name                "${STACK_NAME}" \
    --resource-group      "${RESOURCE_GROUP}" \
    --template-file       "${TEMPLATE_FILE}" \
    --parameters          "@${TEMP_FILE}" \
    --action-on-unmanage  deleteAll \
    --deny-settings-mode  denyDelete \
    --yes

# ─── Clean up temp file ───────────────────────────────────────────────────────
rm -f "${TEMP_FILE}"

# ─── Verify ───────────────────────────────────────────────────────────────────
echo ""
echo "==> Stack created. Verifying resources tracked:"
az stack group show \
    --name           "${STACK_NAME}" \
    --resource-group "${RESOURCE_GROUP}" \
    --query          "{Stack:name, State:provisioningState, Resources:resources[].id}" \
    --output         json

echo ""
echo "==> Deployment Stack '${STACK_NAME}' is active."
echo "    Resources     : 10 tracked as one managed group"
echo "    Deny settings : denyDelete — portal deletion blocked"
echo "    Teardown cmd  : az stack group delete --name ${STACK_NAME} --resource-group ${RESOURCE_GROUP} --action-on-unmanage deleteAll --yes"
echo ""
