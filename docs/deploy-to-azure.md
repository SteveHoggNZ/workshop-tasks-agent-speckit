# Deploying to Azure (optional stretch)

Deployment is not needed for any core lab. This page is for attendees who want to run the app on Azure App Service, optionally with the Foundry Agent Service module.

For the full walkthrough, see the tutorial: [Build an agentic web app in Azure App Service with Microsoft Agent Framework or Foundry Agent Service (.NET)](https://learn.microsoft.com/azure/app-service/tutorial-ai-agent-web-app-semantic-kernel-foundry-dotnet).

## What gets deployed

- `azure.yaml` — Azure Developer CLI (`azd`) configuration. The `web` service is the project at `src/CRUDTasksWithAgent`, hosted on Azure App Service (Linux).
- `infra/` — Bicep templates and parameters for the App Service, its managed identity, and the Microsoft Entra app registration used for authentication.

Prerequisites: the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd), the Azure CLI, and an Azure subscription where you can create Entra app registrations.

```bash
azd auth login
azd up
```

Remember that the Development-only demo seed data does not run on App Service, and the default app-agent model is the offline scripted one. To use a real model in Azure, set the `Llm__*` app settings described in [model-providers.md](model-providers.md).

## Security configuration note

The Bicep template enables App Service authentication (`authsettingsV2`) with Microsoft Entra ID. The Microsoft Graph Bicep extension creates the tenant-local app registration, service principal, and federated identity credential. App Service uses a user-assigned managed identity as its client assertion, so the authentication setup is fully declarative and does not use client secrets.

This protects the Blazor app and all API endpoints, including `/api/tasks` and `/openapi/v1.json`.

### Foundry OpenAPI tool

When a Foundry OpenAPI tool calls the protected task API, configure the parent Foundry resource identity's application ID in the AZD environment:

```bash
azd env set AZURE_AI_FOUNDRY_ACCOUNT_CLIENT_ID <application-id>
azd provision
```

The deployment prints the managed identity audience to use in the OpenAPI tool. Point the tool at the app's OpenAPI document (`https://<your-app>.azurewebsites.net/openapi/v1.json`).

### Enabling the Foundry page

The Foundry chat page (`/foundryagent`) stays disabled until you set `Features:Foundry` to `true` and provide `FoundryProjectEndpoint` and `FoundryAgentName` (as app settings, for example `Features__Foundry`).

## Cleaning up

```bash
azd down
```

When you run `azd down`, the template also deletes the tenant-level Entra application created for App Service authentication.
