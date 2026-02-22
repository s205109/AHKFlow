````instructions
---
description: Private configuration values for AHKFlow Azure deployment
applyTo:
  - "**/.github/workflows/*.yml"
  - "**/build/_build.csproj"
  - "**/docs/**/*.md"
  - "**/scripts/**/*.ps1"
  - "**/scripts/**/*.sh"
---

# Private Instructions

This file is intentionally **committed** and must remain safe to share.

- Do **not** paste secrets into this repository.
- Keep real values in GitHub Secrets, Azure Key Vault, environment variables, or a local (gitignored) file.

If you want a local copy, create something like private.local.md (gitignored) and put real values there.

## Azure Configuration

### Resource Group
- **Name**: `<YOUR_RESOURCE_GROUP>` (placeholder)
- Always use this resource group name for all Azure resources in this project
- Do not create new resource groups

### Subscription
- **Subscription ID**: `<YOUR_SUBSCRIPTION_ID>` (placeholder)
- Always use this subscription ID for Azure CLI commands and deployments
- Do not prompt for subscription selection

## Usage Examples

### Azure CLI Commands
```bash
# Deploy resources
az deployment group create \
  --resource-group <YOUR_RESOURCE_GROUP> \
  --subscription <YOUR_SUBSCRIPTION_ID> \
  --template-file azuredeploy.json

# List resources
az resource list \
  --resource-group <YOUR_RESOURCE_GROUP> \
  --subscription <YOUR_SUBSCRIPTION_ID>
```

### GitHub Actions Workflow
```yaml
- name: Azure Login
  uses: azure/login@v1
  with:
    creds: ${{ secrets.AHKFLOW_AZURE_CREDENTIALS }}

- name: Deploy to Azure
  run: |
    az deployment group create \
      --resource-group <YOUR_RESOURCE_GROUP> \
      --subscription <YOUR_SUBSCRIPTION_ID> \
      --template-file infrastructure/main.bicep
```

### PowerShell Scripts
```powershell
# Set variables
$resourceGroup = "<YOUR_RESOURCE_GROUP>"
$subscriptionId = "<YOUR_SUBSCRIPTION_ID>"

# Set subscription context
az account set --subscription $subscriptionId

# Deploy resources
az deployment group create `
  --resource-group $resourceGroup `
  --subscription $subscriptionId `
  --template-file azuredeploy.json
```

## Setup Instructions

1. Keep this file as-is (placeholders only).
2. Store real values in GitHub Secrets / Key Vault / environment variables.
3. If you need local documentation, create a local copy (e.g., private.local.md) and ensure it is ignored by git.

## Important Notes
- These values are specific to your Azure deployment environment
- GitHub Actions should use secrets for sensitive values (e.g., `AHKFLOW_AZURE_CREDENTIALS`)
- Never hardcode subscription IDs or resource group names in public code
- Use environment variables or configuration files for deployment scripts

````
