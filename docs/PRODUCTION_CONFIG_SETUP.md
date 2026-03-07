# Production Configuration Setup

## Overview

Production configuration files (`appsettings.Production.json`) are **NOT committed to git** for security reasons. Instead, we use:

1. **Local example templates** (`.example` files) - Checked into git
2. **Azure App Service Configuration** - Actual production values stored securely in Azure

## Setup Instructions

### Backend API (`src/Backend/AHKFlow.API`)

1. Copy the example file:
   ```sh
   cp src/Backend/AHKFlow.API/appsettings.Production.json.example \
      src/Backend/AHKFlow.API/appsettings.Production.json
   ```

2. Update with your production values:
   - `AzureAd:ClientId` - From Azure AD App Registration
   - `AzureAd:TenantId` - Your Azure AD Tenant ID
   - `Cors:AllowedOrigins` - Your Static Web App URL

3. **DO NOT commit this file** - It's in `.gitignore`

### Frontend Blazor (`src/Frontend/AHKFlow.UI.Blazor/wwwroot`)

1. Copy the example file:
   ```sh
   cp src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.Production.json.example \
      src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.Production.json
   ```

2. Update with your production values:
   - `AzureAd:ClientId` - From Azure AD App Registration
   - `AzureAd:Authority` - Your Azure AD Authority URL
   - `ApiHttpClient:BaseAddress` - Your deployed API URL

3. **DO NOT commit this file** - It's in `.gitignore`

## Azure App Service Configuration (Recommended for API)

Instead of using `appsettings.Production.json`, configure production settings directly in Azure:

### Azure Portal
1. Go to your App Service → **Configuration** → **Application settings**
2. Add these settings:
   - `AzureAd__ClientId`
   - `AzureAd__TenantId`
   - `ConnectionStrings__DefaultConnection` (use Connection strings tab)

### Azure CLI
```sh
# Set App Settings
az webapp config appsettings set \
  --name ahkflow-api-dev \
  --resource-group rg-ahkflow-dev \
  --settings \
    AzureAd__ClientId="YOUR_CLIENT_ID" \
    AzureAd__TenantId="YOUR_TENANT_ID"

# Set Connection String
az webapp config connection-string set \
  --name ahkflow-api-dev \
  --resource-group rg-ahkflow-dev \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="YOUR_CONNECTION_STRING"
```

## Security Notes

- ✅ `.gitignore` prevents committing production secrets
- ✅ Example files are safe to commit (no real values)
- ✅ Azure App Service Configuration overrides `appsettings.Production.json`
- ⚠️ Always verify secrets are not in git history: `git log --all --full-history -- "*appsettings.Production.json"`

## Current Production URLs

**Dev Environment:**
- API: `https://ahkflow-api-dev.azurewebsites.net`
- Frontend: `https://white-island-04c4b8103.1.azurestaticapps.net`
- Resource Group: `rg-ahkflow-dev`

## Related Documentation

- [Azure CLI Setup](AZURE_CLI_SETUP.md)
- [GitHub Secrets Setup](GITHUB_SECRETS_SETUP.md)
