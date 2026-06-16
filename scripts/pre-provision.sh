#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# pre-provision.sh — MarbleCraft OMS pre-provision hook (POSIX)
# Validates that all required azd env vars are set and the Azure CLI session
# is authenticated before the Bicep template is deployed.
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

echo ""
echo "==> Pre-provision: environment '${AZURE_ENV_NAME}'"
echo "    Subscription : ${AZURE_SUBSCRIPTION_ID}"
echo "    Location     : ${AZURE_LOCATION}"
echo "    Resource Grp : ${AZURE_RESOURCE_GROUP}"

# ─── Required env vars ────────────────────────────────────────────────────────
# SQL_ADMIN_PASSWORD not listed — fetched at deploy time via Key Vault reference in parameters file
required_vars=(
  AZURE_ENV_NAME
  AZURE_SUBSCRIPTION_ID
  AZURE_LOCATION
  AZURE_RESOURCE_GROUP
  SQL_ADMIN_LOGIN
  MIN_REPLICAS
  SQL_SKU
  KEY_VAULT_NAME
  KEY_VAULT_RESOURCE_ID
)

missing=()
for var in "${required_vars[@]}"; do
  [[ -z "${!var:-}" ]] && missing+=("$var")
done

if [[ ${#missing[@]} -gt 0 ]]; then
  echo "Error: Missing required env vars:"
  for v in "${missing[@]}"; do echo "  $v"; done
  echo ""
  echo "Run the following before azd up:"
  echo "  azd env set AZURE_RESOURCE_GROUP   marblecraft-<env>-rg"
  echo "  azd env set SQL_ADMIN_LOGIN        marblecraft-<env>-admin"
  echo "  azd env set MIN_REPLICAS           0            # (dev) or 1 (prod)"
  echo "  azd env set SQL_SKU                Basic        # (dev) or Standard (prod)"
  echo "  azd env set KEY_VAULT_NAME         marblecraft-<env>-kv"
  echo "  azd env set KEY_VAULT_RESOURCE_ID  /subscriptions/<sub>/resourceGroups/marblecraft-<env>-rg/providers/Microsoft.KeyVault/vaults/marblecraft-<env>-kv"
  exit 1
fi

# ─── Azure CLI login check ────────────────────────────────────────────────────
if ! az account show --output none 2>/dev/null; then
  echo "Error: Not logged in to Azure CLI.  Run 'az login' first."
  exit 1
fi

# ─── Key Vault existence check ────────────────────────────────────────────────
echo "    Checking Key Vault '${KEY_VAULT_NAME}' exists..."
if ! az keyvault show --name "${KEY_VAULT_NAME}" --output none 2>/dev/null; then
  echo "Error: Key Vault '${KEY_VAULT_NAME}' not found."
  echo "The Key Vault must exist and hold secrets 'sql-admin-password' and 'sql-connection-string'"
  echo "before infrastructure can be deployed."
  echo ""
  echo "Create it:"
  echo "  az keyvault create --name ${KEY_VAULT_NAME} --resource-group ${AZURE_RESOURCE_GROUP} --location ${AZURE_LOCATION}"
  echo "  az keyvault secret set --vault-name ${KEY_VAULT_NAME} --name sql-admin-password --value '<strong-password>'"
  echo "  az keyvault secret set --vault-name ${KEY_VAULT_NAME} --name sql-connection-string --value 'Server=...;'"
  exit 1
fi

echo "==> Pre-provision checks passed."
echo ""