// JobApplyAI - core infrastructure
// Provisions: Cosmos DB (serverless, per-user partitioned containers), Azure AI Foundry
// (Azure OpenAI) resource + chat model deployment, storage account (Functions), Function App
// (isolated worker, .NET 8, Linux Consumption), Container Apps Environment + Container App
// for the Blazor Server web front end, Log Analytics + Application Insights, and a
// user-assigned managed identity shared by both compute hosts for keyless (Managed Identity)
// access to Cosmos DB and Azure OpenAI.
//
// Deploy with: az deployment group create -g <rg> -f main.bicep -p @main.parameters.json

targetScope = 'resourceGroup'

@description('Short name used as a prefix for all resources, e.g. "jobapplyai".')
param appName string = 'jobapplyai'

@description('Deployment environment suffix, e.g. dev, uat, prod.')
param environmentName string = 'dev'

@description('Primary Azure region for all resources.')
param location string = resourceGroup().location

@description('Azure AI Foundry / Azure OpenAI chat model to deploy, e.g. gpt-4o-mini.')
param chatModelName string = 'gpt-4o-mini'

@description('Model version for the chat deployment.')
param chatModelVersion string = '2024-07-18'

@description('Container image for the Blazor web app. Leave default placeholder until the first CI/CD push.')
param webContainerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Entra External ID (CIAM) authority, e.g. https://<tenant-name>.ciamlogin.com/<tenant-id>/v2.0. Leave placeholder until the CIAM tenant/app registration is created.')
param entraExternalIdAuthority string = 'https://<your-tenant-name>.ciamlogin.com/<your-tenant-id>/v2.0'

@description('Expected audience ("aud" claim) on access tokens presented to the Functions API - the API app registration Client ID or Application ID URI.')
param entraExternalIdApiAudience string = '<api-app-registration-client-id-or-api-uri>'

@description('Adzuna developer API App ID (free tier at https://developer.adzuna.com/). Leave blank to deploy without live job search results.')
@secure()
param adzunaAppId string = ''

@description('Adzuna developer API App Key, issued alongside the App ID.')
@secure()
param adzunaAppKey string = ''

@description('Jooble developer API key (free tier at https://jooble.org/api/about). Leave blank to deploy without live job search results.')
@secure()
param joobleApiKey string = ''

@description('Google AdSense Publisher ID (e.g. ca-pub-1234567890123456). Leave as the placeholder until an AdSense account is approved - ad slots quietly stay hidden until this is set.')
param googleAdSensePublisherId string = '<your-ca-pub-id>'

@description('Buy Me a Coffee page username (buymeacoffee.com/{username}). Leave as the placeholder to hide the tip button.')
param buyMeACoffeeUsername string = '<your-buymeacoffee-username>'

var resourceToken = '${appName}-${environmentName}'
var tags = {
  application: 'JobApplyAI'
  environment: environmentName
}

// ---------- Identity ----------

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${resourceToken}-identity'
  location: location
  tags: tags
}

// ---------- Observability ----------

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${resourceToken}-logs'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${resourceToken}-appi'
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ---------- Cosmos DB ----------

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: '${resourceToken}-cosmos'
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      { locationName: location, failoverPriority: 0, isZoneRedundant: false }
    ]
    capabilities: [
      { name: 'EnableServerless' }
    ]
    disableLocalAuth: true // Managed Identity / Entra RBAC only - no primary keys
  }
}

resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  parent: cosmos
  name: 'JobApplyAI'
  properties: {
    resource: { id: 'JobApplyAI' }
  }
}

var containers = [
  'UserProfiles'
  'Resumes'
  'JobPostings'
  'JobApplications'
  'CoverLetters'
  'InterviewSessions'
]

resource cosmosContainers 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = [for name in containers: {
  parent: cosmosDb
  name: name
  properties: {
    resource: {
      id: name
      partitionKey: {
        paths: ['/userId']
        kind: 'Hash'
      }
    }
  }
}]

// Grants the shared managed identity data-plane access to Cosmos DB (Cosmos DB Built-in Data Contributor).
resource cosmosDataContributorRoleDef 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-08-15' existing = {
  parent: cosmos
  name: '00000000-0000-0000-0000-000000000002'
}

resource cosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
  parent: cosmos
  name: guid(cosmos.id, identity.id, 'data-contributor')
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: cosmosDataContributorRoleDef.id
    scope: cosmos.id
  }
}

// ---------- Azure AI Foundry (Azure OpenAI) ----------

resource aiFoundry 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: '${resourceToken}-foundry'
  location: location
  tags: tags
  kind: 'AIServices'
  sku: { name: 'S0' }
  identity: { type: 'SystemAssigned' }
  properties: {
    customSubDomainName: '${resourceToken}-foundry'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true // Managed Identity only - no API keys
  }
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: aiFoundry
  name: chatModelName
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModelName
      version: chatModelVersion
    }
  }
}

// Grants the shared managed identity "Cognitive Services OpenAI User" on the Foundry resource.
resource openAiUserRoleDef 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
}

resource aiFoundryRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiFoundry.id, identity.id, 'openai-user')
  scope: aiFoundry
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: openAiUserRoleDef.id
    principalType: 'ServicePrincipal'
  }
}

// ---------- Storage (required by the Functions host) ----------

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: replace('${resourceToken}stor', '-', '')
  location: location
  tags: tags
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// ---------- Function App (isolated worker, .NET 8, Linux Consumption) ----------

resource functionsPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${resourceToken}-func-plan'
  location: location
  tags: tags
  sku: { name: 'Y1', tier: 'Dynamic' }
  kind: 'functionapp'
  properties: { reserved: true }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${resourceToken}-func'
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    serverFarmId: functionsPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        { name: 'AzureWebJobsStorage__accountName', value: storage.name }
        { name: 'AzureWebJobsStorage__credential', value: 'managedidentity' }
        { name: 'AzureWebJobsStorage__clientId', value: identity.properties.clientId }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'Cosmos__AccountEndpoint', value: cosmos.properties.documentEndpoint }
        { name: 'Cosmos__DatabaseName', value: 'JobApplyAI' }
        { name: 'AzureAiFoundry__Endpoint', value: 'https://${aiFoundry.name}.openai.azure.com/' }
        { name: 'AzureAiFoundry__ChatDeploymentName', value: chatModelName }
        { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId } // used by DefaultAzureCredential
        { name: 'EntraExternalId__Authority', value: entraExternalIdAuthority }
        { name: 'EntraExternalId__Audience', value: entraExternalIdApiAudience }
        { name: 'EntraExternalId__RequireAuthentication', value: 'true' }
        { name: 'Adzuna__AppId', value: adzunaAppId }
        { name: 'Adzuna__AppKey', value: adzunaAppKey }
        { name: 'Jooble__ApiKey', value: joobleApiKey }
      ]
    }
  }
}

// ---------- Container Apps Environment + Blazor Web App ----------

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${resourceToken}-env'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${resourceToken}-web'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webContainerImage
          env: [
            { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
            { name: 'Cosmos__AccountEndpoint', value: cosmos.properties.documentEndpoint }
            { name: 'Cosmos__DatabaseName', value: 'JobApplyAI' }
            { name: 'AzureAiFoundry__Endpoint', value: 'https://${aiFoundry.name}.openai.azure.com/' }
            { name: 'AzureAiFoundry__ChatDeploymentName', value: chatModelName }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
            { name: 'Adzuna__AppId', value: adzunaAppId }
            { name: 'Adzuna__AppKey', value: adzunaAppKey }
            { name: 'Jooble__ApiKey', value: joobleApiKey }
            { name: 'GoogleAdSense__PublisherId', value: googleAdSensePublisherId }
            { name: 'BuyMeACoffee__Username', value: buyMeACoffeeUsername }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/healthz', port: 8080 }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: { path: '/healthz', port: 8080 }
              initialDelaySeconds: 5
              periodSeconds: 15
            }
          ]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 3 }
    }
  }
}

output cosmosEndpoint string = cosmos.properties.documentEndpoint
output aiFoundryEndpoint string = 'https://${aiFoundry.name}.openai.azure.com/'
output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
output webAppFqdn string = webApp.properties.configuration.ingress.fqdn
output managedIdentityClientId string = identity.properties.clientId
