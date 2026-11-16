targetScope = 'resourceGroup'

@description('Use a region that offers Flex Consumption with .NET 10.')
param location string = resourceGroup().location
@minLength(3)
@maxLength(18)
param prefix string = 'smallagent13'
@description('Entra API application client ID. The SPA requests its access_as_user scope.')
param apiClientId string
@description('Public Entra SPA application ID allowed to call this API.')
param spaClientId string
param tenantId string = tenant().tenantId
param claudeModel string = 'claude-sonnet-5-5'

var suffix = uniqueString(resourceGroup().id)
var storageName = take(toLower('${prefix}${suffix}'), 24)
var webStorageName = take(toLower('web${prefix}${suffix}'), 24)
var functionName = '${prefix}-${suffix}'
var vaultName = take('${prefix}-${suffix}', 24)
var blobContributor = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var secretsUser = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
var frontendOrigin = substring(webStorage.properties.primaryEndpoints.web, 0, max(0, length(webStorage.properties.primaryEndpoints.web) - 1))

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' = {
  name: storageName
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: { minimumTlsVersion: 'TLS1_2', supportsHttpsTrafficOnly: true, allowBlobPublicAccess: false, allowSharedKeyAccess: false }
}
resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2025-01-01' = { parent: storage, name: 'default' }
resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-01-01' = {
  parent: blobs
  name: 'function-releases'
  properties: { publicAccess: 'None' }
}
resource webStorage 'Microsoft.Storage/storageAccounts@2025-01-01' = {
  name: webStorageName
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: { minimumTlsVersion: 'TLS1_2', supportsHttpsTrafficOnly: true, allowBlobPublicAccess: false, allowSharedKeyAccess: false }
}
resource vault 'Microsoft.KeyVault/vaults@2025-05-01' = {
  name: vaultName
  location: location
  properties: { tenantId: tenantId, sku: { family: 'A', name: 'standard' }, enableRbacAuthorization: true, enableSoftDelete: true, softDeleteRetentionInDays: 7, publicNetworkAccess: 'Enabled' }
}
resource plan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: '${prefix}-flex'
  location: location
  kind: 'functionapp'
  sku: { tier: 'FlexConsumption', name: 'FC1' }
  properties: { reserved: true }
}
resource functionApp 'Microsoft.Web/sites@2024-11-01' = {
  name: functionName
  location: location
  kind: 'functionapp,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    functionAppConfig: {
      runtime: { name: 'dotnet-isolated', version: '10.0' }
      deployment: { storage: { type: 'blobContainer', value: '${storage.properties.primaryEndpoints.blob}${deploymentContainer.name}', authentication: { type: 'SystemAssignedIdentity' } } }
      scaleAndConcurrency: { maximumInstanceCount: 40, instanceMemoryMB: 2048, triggers: { http: { perInstanceConcurrency: 4 } } }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'AzureWebJobsStorage__accountName', value: storage.name }
        { name: 'ANTHROPIC_API_KEY', value: '@Microsoft.KeyVault(SecretUri=${vault.properties.vaultUri}secrets/anthropic-api-key)' }
        { name: 'CLAUDE_MODEL', value: claudeModel }
        { name: 'AGENT_MAX_TOKENS', value: '1024' }
        { name: 'ALLOWED_ORIGINS', value: frontendOrigin }
      ]
      cors: { allowedOrigins: [frontendOrigin], supportCredentials: false }
    }
  }
}
resource storageAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, functionApp.id, blobContributor)
  scope: storage
  properties: { principalId: functionApp.identity.principalId, principalType: 'ServicePrincipal', roleDefinitionId: blobContributor }
}
resource keyAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, functionApp.id, secretsUser)
  scope: vault
  properties: { principalId: functionApp.identity.principalId, principalType: 'ServicePrincipal', roleDefinitionId: secretsUser }
}
resource authentication 'Microsoft.Web/sites/config@2024-11-01' = {
  parent: functionApp
  name: 'authsettingsV2'
  properties: {
    platform: { enabled: true, runtimeVersion: '~1' }
    globalValidation: { requireAuthentication: true, unauthenticatedClientAction: 'Return401' }
    httpSettings: { requireHttps: true }
    identityProviders: { azureActiveDirectory: { enabled: true, registration: { clientId: apiClientId, openIdIssuer: '${environment().authentication.loginEndpoint}${tenantId}/v2.0' }, validation: { allowedAudiences: [apiClientId, 'api://${apiClientId}'], defaultAuthorizationPolicy: { allowedApplications: [spaClientId] } } } }
  }
}
output functionAppName string = functionApp.name
output functionOrigin string = 'https://${functionApp.properties.defaultHostName}'
output frontendStorageName string = webStorage.name
output frontendOrigin string = frontendOrigin
output keyVaultName string = vault.name
