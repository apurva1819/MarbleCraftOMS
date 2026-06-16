# ─────────────────────────────────────────────────────────────────────────────
# pre-provision.ps1 — MarbleCraft OMS pre-provision hook
# Validates that all required azd env vars are set and the Azure CLI session
# is authenticated before the Bicep template is deployed.
# ─────────────────────────────────────────────────────────────────────────────
$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "==> Pre-provision: environment '$($env:AZURE_ENV_NAME)'"
Write-Host "    Subscription : $($env:AZURE_SUBSCRIPTION_ID)"
Write-Host "    Location     : $($env:AZURE_LOCATION)"
Write-Host "    Resource Grp : $($env:AZURE_RESOURCE_GROUP)"

# ─── Required env vars ───────────────────────────────────────────────────────
$required = @(
    'AZURE_ENV_NAME'
    'AZURE_SUBSCRIPTION_ID'
    'AZURE_LOCATION'
    'AZURE_RESOURCE_GROUP'
    'SQL_ADMIN_LOGIN'
    'MIN_REPLICAS'
    'SQL_SKU'
    'KEY_VAULT_NAME'
    'KEY_VAULT_RESOURCE_ID'
    # SQL_ADMIN_PASSWORD is not required here — it is fetched at deploy time
    # via the Key Vault reference embedded in the parameters file.
)

$missing = @()
foreach ($var in $required) {
    if (-not [System.Environment]::GetEnvironmentVariable($var)) {
        $missing += $var
    }
}

if ($missing.Count -gt 0) {
    Write-Error @"
Missing required env vars:
  $($missing -join "`n  ")

Run the following before azd up:
  azd env set AZURE_RESOURCE_GROUP   marblecraft-<env>-rg
  azd env set SQL_ADMIN_LOGIN        marblecraft-<env>-admin
  azd env set MIN_REPLICAS           0            # (dev) or 1 (prod)
  azd env set SQL_SKU                Basic        # (dev) or Standard (prod)
  azd env set KEY_VAULT_NAME         marblecraft-<env>-kv
  azd env set KEY_VAULT_RESOURCE_ID  /subscriptions/<sub>/resourceGroups/marblecraft-<env>-rg/providers/Microsoft.KeyVault/vaults/marblecraft-<env>-kv
"@
    exit 1
}

# ─── Azure CLI login check ────────────────────────────────────────────────────
try {
    $null = az account show --output none 2>&1
    if ($LASTEXITCODE -ne 0) { throw }
} catch {
    Write-Error "Not logged in to Azure CLI.  Run 'az login' first."
    exit 1
}

# ─── Key Vault existence check ────────────────────────────────────────────────
Write-Host "    Checking Key Vault '$($env:KEY_VAULT_NAME)' exists..."
$kvExists = az keyvault show --name $env:KEY_VAULT_NAME --output none 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error @"
Key Vault '$($env:KEY_VAULT_NAME)' not found.
The Key Vault must exist and hold secrets 'sql-admin-password' and 'sql-connection-string'
before infrastructure can be deployed.

Create it:
  az keyvault create --name $($env:KEY_VAULT_NAME) --resource-group $($env:AZURE_RESOURCE_GROUP) --location $($env:AZURE_LOCATION)
  az keyvault secret set --vault-name $($env:KEY_VAULT_NAME) --name sql-admin-password --value '<strong-password>'
  az keyvault secret set --vault-name $($env:KEY_VAULT_NAME) --name sql-connection-string --value 'Server=...;Initial Catalog=...;'
"@
    exit 1
}

Write-Host "==> Pre-provision checks passed."
Write-Host ""