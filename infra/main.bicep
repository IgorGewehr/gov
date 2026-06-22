// =====================================================================
// Tensorroot.Gov — Infraestrutura Azure (Bicep)
// App Service (Linux/.NET 8) + Azure SQL + Blob Storage + Key Vault.
// Certificados A1 e segredos ficam no Key Vault (constituição §6).
// =====================================================================

@description('Nome base dos recursos.')
param nomeBase string = 'tensorroot-gov'

@description('Região do Azure.')
param localizacao string = resourceGroup().location

@description('Login do administrador do Azure SQL.')
param sqlAdminLogin string

@secure()
@description('Senha do administrador do Azure SQL.')
param sqlAdminSenha string

var sufixo = uniqueString(resourceGroup().id)
var appServicePlanNome = '${nomeBase}-plan'
var appServiceNome = '${nomeBase}-api-${sufixo}'
var sqlServerNome = '${nomeBase}-sql-${sufixo}'
var sqlDbNome = '${nomeBase}-db'
var storageNome = toLower(replace('${nomeBase}st${sufixo}', '-', ''))
var keyVaultNome = '${nomeBase}-kv-${sufixo}'
var keyVaultSecretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanNome
  location: localizacao
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceNome
  location: localizacao
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      alwaysOn: true
      appSettings: [
        {
          name: 'KeyVault__Uri'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'Database__Provider'
          value: 'SqlServer'
        }
      ]
    }
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerNome
  location: localizacao
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminSenha
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDbNome
  location: localizacao
  sku: {
    name: 'S0'
    tier: 'Standard'
  }
}

resource sqlAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageNome
  location: localizacao
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultNome
  location: localizacao
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
  }
}

// Identidade do App Service pode ler segredos do Key Vault (Key Vault Secrets User).
resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, app.id, keyVaultSecretsUserRoleId)
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleId
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output appServiceUrl string = 'https://${app.properties.defaultHostName}'
output keyVaultUri string = keyVault.properties.vaultUri
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output storageAccountNome string = storage.name
