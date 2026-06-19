// ─────────────────────────────────────────────────────────────────────────────
// api.bicep — Container App Environment + Container App
//
// The Container App uses a System-Assigned Managed Identity so it can pull
// the SQL connection string directly from Key Vault — no secrets in code or
// environment variable plain text.
// ─────────────────────────────────────────────────────────────────────────────

@description('Azure region for all resources')
param location string

@description('Deployment environment tag (dev | prod)')
param environment string

@description('Container App Environment name')
param containerAppEnvName string

@description('Container App name')
param containerAppName string

@description('Container image (registry/image:tag)')
param containerImage string

@description('CPU cores allocated to each replica  (e.g. "0.5" or "1")')
param cpuCore string

@description('Memory allocated to each replica  (e.g. "1Gi" or "2Gi")')
param memorySize string

@description('Minimum number of replicas (0 = scale to zero)')
@minValue(0)
param minReplicas int

@description('Maximum number of replicas')
@minValue(1)
param maxReplicas int

@description('Name of the Key Vault that holds application secrets')
param keyVaultName string

@description('Key Vault secret name that holds the SQL connection string')
param sqlConnectionStringSecretName string

@description('Resource ID of an existing Container App Environment to reuse. Empty = create new.')
param existingContainerAppEnvId string = ''

// ─── Container App Environment ────────────────────────────────────────────────

var createNewEnv = empty(existingContainerAppEnvId)

resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-05-01' = if (createNewEnv) {
  name: containerAppEnvName
  location: location
  tags: {
    environment: environment
  }
  properties: {
    zoneRedundant: false
  }
}

var resolvedEnvId = createNewEnv ? containerAppEnv.id : existingContainerAppEnvId

// ─── Container App ────────────────────────────────────────────────────────────

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: containerAppName
  location: location
  tags: {
    environment: environment
    'azd-service-name': 'api'
    'azd-env-name': environment
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: resolvedEnvId
    configuration: {
      // Key Vault reference — the system identity is granted access in main.bicep
      secrets: [
        {
          name: 'sql-connection-string'
          keyVaultUrl: 'https://${keyVaultName}${az.environment().suffixes.keyvaultDns}/secrets/${sqlConnectionStringSecretName}'
          identity: 'system'
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
    }
    template: {
      containers: [
        {
          name: 'api'
          image: containerImage
          resources: {
            cpu: any(cpuCore)
            memory: memorySize
          }
          env: [
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

// ─── Outputs ──────────────────────────────────────────────────────────────────

@description('Public FQDN of the Container App')
output containerAppFqdn string = containerApp.properties.configuration.ingress.fqdn

@description('Principal ID of the system-assigned managed identity (needed for Key Vault RBAC)')
output principalId string = containerApp.identity.principalId
