// ─────────────────────────────────────────────────────────────────────────────
// acr-rbac.bicep — AcrPull role assignment for the Container App identity
//
// Deployed into the ACR's resource group (which may differ from the app RG).
// Called from main.bicep with scope: resourceGroup(acrResourceGroup).
// ─────────────────────────────────────────────────────────────────────────────

@description('Name of the existing Azure Container Registry')
param acrName string

@description('Principal ID of the Container App system-assigned identity')
param principalId string

@description('Deployment prefix used to produce a stable role assignment name')
param deploymentPrefix string

resource existingAcr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

// Role definition ID: 7f951dda-4ed3-4680-a7ca-43fe172d538d = AcrPull
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: existingAcr
  name: guid(existingAcr.id, deploymentPrefix, 'acr-pull')
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d'
    )
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
