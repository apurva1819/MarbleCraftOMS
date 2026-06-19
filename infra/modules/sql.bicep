// ─────────────────────────────────────────────────────────────────────────────
// sql.bicep — Azure SQL Server + Database
// All names and SKUs are injected by the caller; nothing is hardcoded here.
// ─────────────────────────────────────────────────────────────────────────────

@description('Azure region for all resources')
param location string

@description('Deployment environment tag (dev | prod)')
param environment string

@description('Logical SQL Server name')
param sqlServerName string

@description('SQL Database name')
param sqlDatabaseName string

@description('SQL Server administrator login')
param sqlAdminLogin string

@description('SQL Server administrator password — must arrive via Key Vault reference, never plain text')
@secure()
param sqlAdminPassword string

@description('SQL Database SKU name  (Basic | S2)')
@allowed(['Basic', 'S2'])
param sqlSkuName string

@description('SQL Database SKU tier  (Basic | Standard)')
@allowed(['Basic', 'Standard'])
param sqlSkuTier string

@description('Maximum database size in bytes  (2 GB dev / 50 GB prod)')
param sqlMaxSizeBytes int

@description('Resource ID of the private endpoint subnet — empty string = no private endpoint')
param privateEndpointSubnetId string = ''

@description('Resource ID of the private DNS zone for SQL — empty string = skip DNS zone group')
param privateDnsZoneId string = ''

// ─── SQL Server ───────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2022-11-01-preview' = {
  name: sqlServerName
  location: location
  tags: {
    environment: environment
  }
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    publicNetworkAccess: empty(privateEndpointSubnetId) ? 'Enabled' : 'Disabled'
    minimalTlsVersion: '1.2'
  }
}

// ─── SQL Database ─────────────────────────────────────────────────────────────

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-11-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: {
    environment: environment
  }
  sku: {
    name: sqlSkuName
    tier: sqlSkuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: sqlMaxSizeBytes
  }
}

// ─── Private Endpoint ────────────────────────────────────────────────────────

resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' =
  if (!empty(privateEndpointSubnetId)) {
    name: '${sqlServerName}-pe'
    location: location
    properties: {
      subnet: { id: privateEndpointSubnetId }
      privateLinkServiceConnections: [
        {
          name: '${sqlServerName}-plsc'
          properties: {
            privateLinkServiceId: sqlServer.id
            groupIds: ['sqlServer']
          }
        }
      ]
    }
  }

resource sqlPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' =
  if (!empty(privateEndpointSubnetId) && !empty(privateDnsZoneId)) {
    parent: sqlPrivateEndpoint
    name: 'default'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'privatelink-database-windows-net'
          properties: { privateDnsZoneId: privateDnsZoneId }
        }
      ]
    }
  }

// ─── Outputs ──────────────────────────────────────────────────────────────────

@description('Fully-qualified domain name of the SQL Server')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Resource ID of the SQL Database')
output sqlDatabaseId string = sqlDatabase.id

@description('Resource ID of the SQL Server')
output sqlServerId string = sqlServer.id
