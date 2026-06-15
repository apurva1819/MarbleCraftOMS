// ─────────────────────────────────────────────────────────────────────────────
// servicebus.bicep — Service Bus Namespace + Topics + Subscriptions
//
// NOTE: Azure Service Bus Basic tier does NOT support Topics or Subscriptions.
// Both environments use Standard tier so that the two application topics work.
// The caller is still free to pass 'Premium' for higher throughput.
// ─────────────────────────────────────────────────────────────────────────────

@description('Azure region for all resources')
param location string

@description('Deployment environment tag (dev | prod)')
param environment string

@description('Service Bus namespace name')
param namespaceName string

@description('Service Bus SKU — Standard or Premium required for Topics')
@allowed(['Standard', 'Premium'])
param skuName string

// ─── Topic definitions ────────────────────────────────────────────────────────

var topicDefs = [
  {
    topicName: 'low-stock-events'
    subscriptionName: 'low-stock-handler'
  }
  {
    topicName: 'order-status-changed'
    subscriptionName: 'order-status-handler'
  }
]

// ─── Namespace ────────────────────────────────────────────────────────────────

resource sbNamespace 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: namespaceName
  location: location
  tags: {
    environment: environment
  }
  sku: {
    name: skuName
    tier: skuName
  }
}

// ─── Topics ───────────────────────────────────────────────────────────────────

resource sbTopics 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = [for t in topicDefs: {
  parent: sbNamespace
  name: t.topicName
  properties: {
    enablePartitioning: false
    maxSizeInMegabytes: 1024
    defaultMessageTimeToLive: 'P14D'
  }
}]

// ─── Subscriptions (one per topic) ───────────────────────────────────────────

resource sbSubscriptions 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = [for (t, i) in topicDefs: {
  parent: sbTopics[i]
  name: t.subscriptionName
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
  }
}]

// ─── Outputs ──────────────────────────────────────────────────────────────────

@description('Resource ID of the Service Bus namespace')
output serviceBusNamespaceId string = sbNamespace.id

@description('Service Bus endpoint URL')
output serviceBusEndpoint string = sbNamespace.properties.serviceBusEndpoint
