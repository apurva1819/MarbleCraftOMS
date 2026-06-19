// ─────────────────────────────────────────────────────────────────────────────
// network.bicep — Virtual Network, Private DNS Zone, VNet Link
// Supports private endpoint for Azure SQL Server
// ─────────────────────────────────────────────────────────────────────────────

@description('Azure region for all resources')
param location string

@description('Deployment environment tag (dev | prod)')
param environment string

@description('Virtual Network resource name')
param vnetName string

var prefix = 'marblecraft-${environment}'

// ─── Virtual Network ──────────────────────────────────────────────────────────

resource vnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: vnetName
  location: location
  tags: { environment: environment }
  properties: {
    addressSpace: {
      addressPrefixes: ['10.0.0.0/16']
    }
    subnets: [
      {
        name: 'private-endpoints'
        properties: {
          addressPrefix: '10.0.1.0/24'
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
    ]
  }
}

// ─── Private DNS Zone ─────────────────────────────────────────────────────────

resource sqlPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink${az.environment().suffixes.sqlServerHostname}'
  location: 'global'
  tags: { environment: environment }
}

// ─── VNet Link ────────────────────────────────────────────────────────────────

resource vnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: sqlPrivateDnsZone
  name: '${prefix}-vnet-link'
  location: 'global'
  properties: {
    virtualNetwork: { id: vnet.id }
    registrationEnabled: false
  }
}

// ─── Outputs ──────────────────────────────────────────────────────────────────

@description('Resource ID of the private-endpoints subnet')
output privateEndpointSubnetId string = '${vnet.id}/subnets/private-endpoints'

@description('Resource ID of the SQL Private DNS Zone')
output sqlPrivateDnsZoneId string = sqlPrivateDnsZone.id

@description('Resource ID of the Virtual Network')
output vnetId string = vnet.id
