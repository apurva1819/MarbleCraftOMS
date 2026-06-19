// ─────────────────────────────────────────────────────────────────────────────
// main.bicep — MarbleCraft OMS  •  Distributor Order & Stock Allocation
//
// Deployed via Azure Developer CLI (azd) + Azure Deployment Stacks.
// azd invokes 'az stack group create' (post-provision hook) which wraps
// ALL 10 resources in a single managed stack with:
//   --action-on-unmanage deleteAll   → teardown deletes every resource cleanly
//   --deny-settings-mode denyDelete  → portal deletion blocked for all members
//
// One-command deploy (driven entirely by azd):
//   azd up --environment dev
//   azd up --environment prod
//
// Manual stack deploy (bypassing azd, for emergencies):
//   az stack group create \
//     --name marblecraft-dev-stack \
//     --resource-group marblecraft-dev-rg \
//     --template-file main.bicep \
//     --parameters dev.parameters.json \
//     --action-on-unmanage deleteAll \
//     --deny-settings-mode denyDelete \
//     --yes
//
// The sqlAdminPassword parameter MUST arrive via a Key Vault reference in the
// parameters file — see dev.parameters.json for the reference format.
// ─────────────────────────────────────────────────────────────────────────────

targetScope = 'resourceGroup'

// ─── Parameters ───────────────────────────────────────────────────────────────

@description('Deployment environment — controls all SKU and scaling decisions')
@allowed(['dev', 'prod'])
param environment string

@description('Azure region for all resources')
param location string = 'eastasia'

@description('SQL Server administrator login')
param sqlAdminLogin string

@description('SQL administrator password — supplied via Key Vault reference, never plain text')
@secure()
param sqlAdminPassword string

@description('Minimum Container App replicas (0 = scale-to-zero in dev)')
@minValue(0)
param minReplicas int

@description('SQL Database SKU (Basic → dev 2 GB  |  Standard → prod S2 50 GB)')
@allowed(['Basic', 'Standard'])
param sqlSku string

@description('Name of the pre-existing Key Vault that holds application secrets')
param keyVaultName string

@description('Container image to run in the API Container App')
param containerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Resource ID of an existing Container App Environment to reuse. Empty = create new.')
param existingContainerAppEnvId string = ''

@description('Name of the Azure Container Registry used by azd to push and pull images')
param acrName string

// ─── Variables ────────────────────────────────────────────────────────────────

var prefix = 'marblecraft-${environment}'

// Map the caller-supplied sqlSku token to the full database SKU shape
var sqlSkuMap = {
  Basic: {
    skuName: 'Basic'
    skuTier: 'Basic'
    maxSizeBytes: 2147483648   // 2 GB
  }
  Standard: {
    skuName: 'S2'
    skuTier: 'Standard'
    maxSizeBytes: 53687091200  // 50 GB
  }
}

// ─── Reference existing Key Vault (for RBAC assignment below) ─────────────────

resource existingKeyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource existingAcr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

// ─── Module: Virtual Network + Private DNS ───────────────────────────────────

module network './modules/network.bicep' = {
  name: '${prefix}-network-deploy'
  params: {
    location: location
    environment: environment
    vnetName: '${prefix}-vnet'
  }
}

// ─── Module: SQL Server + Database ────────────────────────────────────────────

module sql './modules/sql.bicep' = {
  name: '${prefix}-sql-deploy'
  params: {
    location: location
    environment: environment
    sqlServerName: '${prefix}-sqlserver'
    sqlDatabaseName: '${prefix}-db'
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    sqlSkuName: sqlSkuMap[sqlSku].skuName
    sqlSkuTier: sqlSkuMap[sqlSku].skuTier
    sqlMaxSizeBytes: sqlSkuMap[sqlSku].maxSizeBytes
    privateEndpointSubnetId: network.outputs.privateEndpointSubnetId
    privateDnsZoneId: network.outputs.sqlPrivateDnsZoneId
  }
}

// ─── Module: Service Bus Namespace + Topics ───────────────────────────────────
// Both environments use Standard tier — Basic does not support Topics.

module servicebus './modules/servicebus.bicep' = {
  name: '${prefix}-sb-deploy'
  params: {
    location: location
    environment: environment
    namespaceName: '${prefix}-sbus'
    skuName: 'Standard'
  }
}

// ─── Module: Container App Environment + Container App ────────────────────────

module api './modules/api.bicep' = {
  name: '${prefix}-api-deploy'
  params: {
    location: location
    environment: environment
    containerAppEnvName: '${prefix}-cae'
    containerAppName: '${prefix}-api'
    containerImage: containerImage
    cpuCore: environment == 'dev' ? '0.5' : '1'
    memorySize: environment == 'dev' ? '1Gi' : '2Gi'
    minReplicas: minReplicas
    maxReplicas: environment == 'dev' ? 2 : 5
    keyVaultName: keyVaultName
    sqlConnectionStringSecretName: 'sql-connection-string'
    existingContainerAppEnvId: existingContainerAppEnvId
  }
}

// ─── Grant Container App identity Key Vault Secrets User role ─────────────────
// Role definition ID: 4633458b-17de-408a-b874-0445c86b69e6 = Key Vault Secrets User

resource kvSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: existingKeyVault
  // Name must be computable at deploy start — use stable identifiers, not runtime outputs
  name: guid(existingKeyVault.id, '${prefix}-api', 'kv-secrets-user')
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6'
    )
    principalId: api.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// ─── Grant Container App identity AcrPull role ───────────────────────────────
// Role definition ID: 7f951dda-4ed3-4680-a7ca-43fe172d538d = AcrPull

resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: existingAcr
  name: guid(existingAcr.id, '${prefix}-api', 'acr-pull')
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d'
    )
    principalId: api.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// ─── Outputs ──────────────────────────────────────────────────────────────────

@description('SQL Server FQDN')
output sqlServerFqdn string = sql.outputs.sqlServerFqdn

@description('Service Bus endpoint')
output serviceBusEndpoint string = servicebus.outputs.serviceBusEndpoint

@description('Container App public FQDN')
output containerAppFqdn string = api.outputs.containerAppFqdn

@description('Container App system-assigned identity principal ID')
output containerAppPrincipalId string = api.outputs.principalId

@description('Virtual Network resource ID')
output vnetId string = network.outputs.vnetId

@description('Private endpoint subnet resource ID')
output privateEndpointSubnetId string = network.outputs.privateEndpointSubnetId
