# Production Configuration Setup

## Overview

AHKFlow follows **Microsoft best practices** for configuration management, with different strategies for frontend and backend:

| Layer | Strategy | File Status | Reason |
|-------|----------|-------------|--------|
| **Frontend (Blazor WASM)** | Commit to git | ✅ Tracked | Client-side, public, no secrets possible |
| **Backend (API)** | Azure App Service Config | ❌ Ignored | Contains secrets (ConnectionStrings) |

**Key Principle:** Blazor WebAssembly runs in the browser - all configuration is visible to users. Per [Microsoft docs](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/configuration?view=aspnetcore-10.0): _"Don't store app secrets, credentials, or any other sensitive data in any web root file."_

---

## Frontend Configuration (Blazor WASM)

### ✅ Production Values in Base Config File

**File:** `src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.json`

**Status:** ✅ **COMMITTED TO GIT** (contains production values)

### Why This is Safe

1. ✅ Blazor WASM runs in browser (client-side code)
2. ✅ All files are visible in browser DevTools
3. ✅ OAuth Client ID is **public by design** (OAuth 2.0 spec)
4. ✅ API URLs are **public** (visible in Network tab)
5. ❌ Cannot contain secrets (browser code is always inspectable)

### Configuration Pattern

**Simple two-file approach:**

```
wwwroot/
├── appsettings.json              ✅ Production values (committed)
└── appsettings.Development.json  ❌ Local override (ignored by git)
```

**How it works:**
1. `appsettings.json` has production values → deployed to Azure
2. Local development: `appsettings.Development.json` overrides with `localhost:7600`
3. No environment-specific deployment logic needed

### What's in Frontend Config

```json
{
  "AzureAd": {
    "ClientId": "18680edd-0d55-4280-a3a6-d0df5acd6c03",    // ✅ PUBLIC
    "Authority": "https://login.microsoftonline.com/...", // ✅ PUBLIC
    "ValidateAuthority": true                             // ✅ PUBLIC
  },
  "ApiHttpClient": {
    "BaseAddress": "https://ahkflow-api-dev.azurewebsites.net"  // ✅ PUBLIC
  },
  "Serilog": { ... }  // ✅ PUBLIC (logging config)
}
```

### Deployment Process

The frontend workflow is **simple** - no special steps needed:

```yaml
# .github/workflows/ahkflow-deploy-frontend.yml
- uses: actions/checkout@v4  # ✅ Includes appsettings.json with production values
- name: Build and Deploy to Azure Static Web Apps
  # ✅ Deploys and uses production API automatically
```

**No production-specific file needed!** The base config has production values. ✅

---

## Backend Configuration (ASP.NET Core API)

### ❌ DO NOT Commit `appsettings.Production.json`

**Template:** `src/Backend/AHKFlow.API/appsettings.Production.json.example` (✅ safe to commit)

**Actual File:** `src/Backend/AHKFlow.API/appsettings.Production.json` (❌ ignored by git)

### Why This Must Be Secret

Backend configuration contains:
- ❌ SQL Connection Strings (with passwords)
- ❌ Database credentials
- ❌ API keys or service-to-service secrets

These **must never be committed** to git.

---

## Setup Instructions

### Backend API (`src/Backend/AHKFlow.API`)

**For local testing only:**

1. Copy the example file:
   ```sh
   cp src/Backend/AHKFlow.API/appsettings.Production.json.example \
      src/Backend/AHKFlow.API/appsettings.Production.json
   ```

2. Update with test/dev values (not real production secrets)

3. **DO NOT commit this file** - It's in `.gitignore`

**For production:** Use Azure App Service Configuration (see below)

### Frontend Blazor (`src/Frontend/AHKFlow.UI.Blazor/wwwroot`)

**No setup needed!** ✅

The file `appsettings.json` is already committed with production values:
- `ClientId`: `18680edd-0d55-4280-a3a6-d0df5acd6c03`
- `Authority`: `https://login.microsoftonline.com/e97883e4-340d-4299-b85b-4e4ded6846cf`
- `ApiHttpClient.BaseAddress`: `https://ahkflow-api-dev.azurewebsites.net`

**These values are safe to commit** because they're public (client-side, no secrets).

**Local development:** `appsettings.Development.json` (ignored by git) overrides with `localhost:7600`.

---

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

- ✅ `.gitignore` prevents committing **backend** production secrets
- ✅ Frontend config is public (client-side) - **safe to commit**
- ✅ Example files (`.example`) are safe to commit (no real values)
- ✅ Azure App Service Configuration overrides backend `appsettings.Production.json`
- ✅ OAuth Client ID is public by design (OAuth 2.0 specification)
- ⚠️ Verify backend secrets are not in git history: `git log --all --full-history -- "src/Backend/**/appsettings.Production.json"`

## Best Practice Summary

### ✅ Frontend (Blazor WASM)

| Aspect | Implementation |
|--------|---------------|
| **File** | `appsettings.json` (base file with production values) |
| **Git Status** | ✅ Committed |
| **Contains** | Public config only (Client ID, API URL) |
| **Local Dev** | Overridden by `appsettings.Development.json` (ignored) |
| **Why Safe** | Runs in browser - always visible to users |
| **Authority** | [Microsoft Blazor Configuration Docs](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/configuration?view=aspnetcore-10.0) |

### ✅ Backend (API)

| Aspect | Implementation |
|--------|---------------|
| **Template** | `appsettings.Production.json.example` |
| **Git Status** | ❌ Ignored by `.gitignore` |
| **Contains** | Secrets (ConnectionStrings, credentials) |
| **Why Secret** | Server-side secrets must never be public |
| **Production** | Azure App Service Configuration + Key Vault |

## Current Production URLs

**Dev Environment:**
- API: `https://ahkflow-api-dev.azurewebsites.net`
- Frontend: `https://white-island-04c4b8103.1.azurestaticapps.net`
- Resource Group: `rg-ahkflow-dev`

## Related Documentation

- [Azure CLI Setup](AZURE_CLI_SETUP.md)
- [GitHub Secrets Setup](GITHUB_SECRETS_SETUP.md)
