# Deploy React + Azure Functions to Azure

This guide prepares two independent deployments: a .NET 10 isolated Function App on **Flex Consumption** and a React static website on **Azure Storage**. Azure Functions invokes the official C# Messages SDK; no Node agent subprocess is deployed. React calls the HTTPS Function App directly for JSON and SSE.

The Bicep template is compile-checked locally. No subscription resources have been created or published, and cloud authentication/streaming has not been tested. Provisioning and publication below are explicit user commands and create billed resources.

## 1. Prerequisites and identity registrations

Install Azure CLI with Bicep support; local `npm ci` supplies Core Tools for code publication. Sign in to the intended subscription:

```powershell
az login
az account set --subscription 'YOUR-SUBSCRIPTION-ID'
az bicep install
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.Storage
az provider register --namespace Microsoft.KeyVault
```

In **Microsoft Entra ID → App registrations**, create two single-tenant registrations:

1. **Smallest Agent API:** note its Application (client) ID. In Expose an API, set Application ID URI to `api://API-CLIENT-ID`; add delegated scope `access_as_user`. Set the API manifest's `api.requestedAccessTokenVersion` to **2**. No client secret is needed for bearer-token validation.
2. **Smallest Agent React:** note its Application (client) ID. Add the API's delegated `access_as_user` permission and grant consent as appropriate for your tenant. Its SPA redirect URI will be the frontend origin printed after provisioning. In the API registration's authorized client applications, add this SPA and its delegated scope.

The Bicep template configures Function App **Authentication / Easy Auth** to require Entra tokens for the tenant, API audience and allowed SPA client. HTTP triggers use AuthorizationLevel.Anonymous because Easy Auth validates the bearer token at the Azure platform boundary; no shared Function key or Claude key is delivered to the browser. Local Core Tools does not emulate this cloud authentication boundary. Use Enterprise Applications assignment requirements/groups to restrict who in your tenant can use the paid API.

## 2. Review and create infrastructure

From this application root, choose a lowercase alphanumeric prefix, your tenant/app IDs, and a region supporting Flex Consumption with .NET 10:

```powershell
$resourceGroup = 'smallest-agent-13-rg'
$region = 'westeurope'
$apiClientId = 'YOUR-API-APPLICATION-ID'
$spaClientId = 'YOUR-SPA-APPLICATION-ID'
$tenantId = 'YOUR-TENANT-ID'

az functionapp list-flexconsumption-locations --output table
az group create --name $resourceGroup --location $region --output none
az bicep build --file infra/main.bicep --outfile .tools/main.json

az deployment group what-if --resource-group $resourceGroup `
  --template-file infra/main.bicep `
  --parameters location=$region prefix=smallagent13 `
    apiClientId=$apiClientId spaClientId=$spaClientId tenantId=$tenantId

# Run after reviewing the proposed resources:
az deployment group create --resource-group $resourceGroup --name smallest-agent-13 `
  --template-file infra/main.bicep `
  --parameters location=$region prefix=smallagent13 `
    apiClientId=$apiClientId spaClientId=$spaClientId tenantId=$tenantId `
  --output none
```

The template creates a Flex plan, Function App, private runtime/deployment storage, separate frontend storage and a Key Vault. The app's managed identity receives Blob Data Contributor and Key Vault Secrets User. Shared storage keys are disabled. The Function App uses 2048 MiB, HTTP concurrency four and a maximum of 40 instances (Flex's minimum allowed maximum); these are limits, not a spend cap. There are no always-ready instances. Configure Azure budgets/alerts for your subscription and adjust scaling for real workloads.

Read the non-secret deployment outputs:

```powershell
$outputs = az deployment group show --resource-group $resourceGroup `
  --name smallest-agent-13 --query properties.outputs --output json | ConvertFrom-Json
$functionName = $outputs.functionAppName.value
$functionOrigin = $outputs.functionOrigin.value
$webStorage = $outputs.frontendStorageName.value
$frontendOrigin = $outputs.frontendOrigin.value
$vaultName = $outputs.keyVaultName.value
```

If a region does not advertise .NET 10, choose a supported Flex region; do not silently change this project to in-process Functions or a different .NET version. Region availability and subscription quotas must be checked in your subscription.

## 3. Set the server key in Key Vault

In the new Key Vault, grant the operator the **Key Vault Secrets Officer** role if necessary, then use the Azure portal's Secrets screen to create a secret named **anthropic-api-key** with your own Claude key. Do not paste it into the Bicep template, frontend configuration, repository or terminal output.

The Function App setting already references this secret without a version. Its managed identity can read it. Allow RBAC propagation, then restart the Function App after initially setting the secret. An unresolved reference is treated as missing configuration by the application. The optional CLAUDE_MODEL and AGENT_MAX_TOKENS settings are server settings; set a model available to your Claude account.

## 4. Publish the Functions backend

```powershell
npm run publish:backend
Push-Location artifacts/functions
try {
    & ..\..\node_modules\.bin\func.cmd azure functionapp publish $functionName --dotnet-isolated --no-build
} finally {
    Pop-Location
}
```

Ensure artifacts/functions contains **host.json**, worker.config.json and compiled files at its root, with no .env or local.settings.json. Publish the package root, not its parent directory. Core Tools must use an identity with deployment access; identity-based deployment storage may require appropriate Blob Data permissions for the publisher as well. Use the Functions Azure portal deployment diagnostics for cloud indexing, permissions or runtime availability errors.

## 5. Publish the React frontend

Enable static website hosting on its separate storage account. The publishing identity needs **Storage Blob Data Contributor** and permission to configure the account:

```powershell
az storage blob service-properties update --account-name $webStorage --auth-mode login `
  --static-website --index-document index.html --404-document index.html --output none

if (!(Test-Path frontend/.env)) { Copy-Item frontend/.env.example frontend/.env }
notepad frontend/.env
```

Set these **public** values, using the output origins without a trailing slash:

```dotenv
VITE_API_BASE_URL=https://YOUR-FUNCTION.azurewebsites.net
VITE_ENTRA_CLIENT_ID=YOUR-SPA-APPLICATION-ID
VITE_ENTRA_TENANT_ID=YOUR-TENANT-ID
VITE_ENTRA_SCOPE=api://YOUR-API-APPLICATION-ID/access_as_user
```

Add `$frontendOrigin` as a **Single-page application** redirect URI to the SPA registration. The MSAL popup uses that exact origin. No SPA client secret is required. Rebuild after configuration changes:

```powershell
npm run generate:lesson
npm run build --workspace frontend
az storage blob upload-batch --account-name $webStorage --auth-mode login `
  --destination '$web' --source frontend/dist --overwrite --output none

Write-Output $frontendOrigin
```

Open that origin and click **Sign in to Azure**, then run a prompt. The bearer token is held in MSAL's in-memory cache; page reload requires sign-in again. The Function App CORS and application allowlist use the exact storage website origin. If adding a custom domain, update both allowlists and the SPA redirect URI, rebuild the frontend if its public settings change, and use HTTPS.

Hash-based lesson navigation works on the static website without a backend router. Static files are public; model credentials are not. Use a CDN/cache policy appropriate to your release process if replacing long-lived deployments.

## 6. Verify the hosted boundary

- Anonymous Function App requests should receive **401** from Azure authentication. No anonymous health response is expected with the template's settings.
- Signed-in React should report Backend connected. Key presence alone does not prove provider authentication; a live prompt uses Claude API credits.
- Check Simple and Stream separately. Stream should progressively display text, then a final result and done. max_tokens is still an incomplete result.
- Test Cancel, a missing/revoked key and invalid model access. Public responses should be safe; private provider details should not appear in browser output.
- Cloud front ends, cold starts and platform limits can affect streaming; local Core Tools success does not prove deployed streaming behavior. Avoid the Azure Static Web Apps managed API proxy for this configuration. Longer tasks need an async job design; heartbeats do not remove HTTP lifetime limits.

No cleanup or deletion is run automatically. All resources are in the selected resource group for user-managed lifecycle and cost control.

Primary references: [isolated worker and .NET support](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide), [Flex Consumption infrastructure](https://learn.microsoft.com/en-us/azure/azure-functions/functions-infrastructure-as-code), [Functions HTTP limits](https://learn.microsoft.com/en-us/azure/azure-functions/functions-scale), [Anthropic C# SDK](https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp).
