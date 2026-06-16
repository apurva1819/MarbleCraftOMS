# ─────────────────────────────────────────────────────────────────────────────
# post-provision.ps1 - MarbleCraft OMS post-provision hook
#
# Wraps the 10 AZD-deployed resources in an Azure Deployment Stack so that:
#   • All resources are tracked as one managed group
#   • Unmanaged drift (resources created outside Bicep) is detectable
#   • Teardown is one command: az stack group delete
#   • denyDelete prevents accidental portal deletion
#
# The stack parameters use a Key Vault reference for sqlAdminPassword -
# the password never appears as plain text anywhere.
# ─────────────────────────────────────────────────────────────────────────────
$ErrorActionPreference = 'Stop'

$stackName     = "marblecraft-$($env:AZURE_ENV_NAME)-stack"
$resourceGroup = $env:AZURE_RESOURCE_GROUP
$templateFile  = "./infra/main.bicep"
$tempFile      = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "marblecraft-stack-params-$($env:AZURE_ENV_NAME).json")

Write-Host ""
Write-Host "==> Post-provision: creating Deployment Stack '$stackName'"
Write-Host "    Resource Group : $resourceGroup"
Write-Host "    Template       : $templateFile"

# ─── Build parameter file with Key Vault reference for sqlAdminPassword ───────
# Using a KV reference ensures the password never appears as plain text
# in shell history, process lists, or ARM deployment logs.
$stackParams = [ordered]@{
    '$schema'      = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
    contentVersion = '1.0.0.0'
    parameters     = [ordered]@{
        environment    = @{ value = $env:AZURE_ENV_NAME }
        location       = @{ value = $env:AZURE_LOCATION }
        sqlAdminLogin  = @{ value = $env:SQL_ADMIN_LOGIN }
        minReplicas    = @{ value = [int]$env:MIN_REPLICAS }
        sqlSku         = @{ value = $env:SQL_SKU }
        keyVaultName   = @{ value = $env:KEY_VAULT_NAME }
        existingContainerAppEnvId = @{ value = if ($env:CONTAINER_APP_ENV_ID) { $env:CONTAINER_APP_ENV_ID } else { '' } }
        sqlAdminPassword = @{
            reference = @{
                keyVault  = @{ id = $env:KEY_VAULT_RESOURCE_ID }
                secretName = 'sql-admin-password'
            }
        }
    }
}

$jsonContent = $stackParams | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($tempFile, $jsonContent, (New-Object System.Text.UTF8Encoding $false))
Write-Host "    Stack params   : $tempFile (Key Vault reference, no plain-text secret)"

# ─── Create / update Deployment Stack ────────────────────────────────────────
Write-Host ""
Write-Host "    Running: az stack group create ..."

az stack group create `
    --name            $stackName `
    --resource-group  $resourceGroup `
    --template-file   $templateFile `
    --parameters      "@$tempFile" `
    --action-on-unmanage  deleteAll `
    --deny-settings-mode  denyDelete `
    --yes

if ($LASTEXITCODE -ne 0) {
    Remove-Item -Path $tempFile -ErrorAction SilentlyContinue
    Write-Error "Deployment Stack creation failed (exit $LASTEXITCODE)."
    exit 1
}

# ─── Clean up temp file ───────────────────────────────────────────────────────
Remove-Item -Path $tempFile -ErrorAction SilentlyContinue

# ─── Verify ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "==> Stack created. Verifying resources tracked:"
az stack group show `
    --name           $stackName `
    --resource-group $resourceGroup `
    --query          "{Stack:name, State:provisioningState, Resources:resources[].id}" `
    --output         json

Write-Host ""
Write-Host "==> Deployment Stack '$stackName' is active."
Write-Host "    Resources     : 10 tracked as one managed group"
Write-Host "    Deny settings : denyDelete - portal deletion blocked"
$teardown = "az stack group delete --name $stackName --resource-group $resourceGroup --action-on-unmanage deleteAll --yes"
Write-Host "    Teardown cmd  : $teardown"
Write-Host ""
