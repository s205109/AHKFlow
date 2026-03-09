# Configuration Management Strategy

This document explains how AHKFlow manages configuration for development and production environments, following Microsoft best practices for Blazor WebAssembly and ASP.NET Core applications.

## Overview

Different application layers require different configuration strategies:

| Layer | Strategy | Reason |
|-------|----------|--------|
| **Frontend (Blazor WASM)** | Production values in `appsettings.json` | Client-side, public, no secrets |
| **Backend (API)** | Use `.example` template + Azure App Service Configuration | Contains secrets (ConnectionStrings) |

---

## Frontend Configuration (Blazor WebAssembly)

### Strategy: Production Values in Base Config

**File:** `src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.json`

**Status:** ✅ Committed to repository (contains production values)

**Configuration pattern:**
- `appsettings.json` → Production values (deployed to Azure)
- `appsettings.Development.json` → Local development override (`localhost:7600`)

### Why This Works

Per [Microsoft documentation](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/configuration?view=aspnetcore-10.0):

> **"Provide _public_ authentication configuration in an app settings file."**
>
> **"Configuration and settings files in the web root (`wwwroot` folder) are visible to users on the client, and users can tamper with the data. Don't store app secrets, credentials, or any other sensitive data in any web root file."**

**Key Points:**
- ✅ Blazor WASM runs entirely in the browser (client-side)
- ✅ All files are downloaded and visible in browser DevTools
- ✅ OAuth Client ID is **public by design** (OAuth 2.0 specification)
- ✅ API Base Address is **public** (visible in Network tab)
- ❌ Cannot contain secrets (browser code is always inspectable)

### Configuration Files

```
src/Frontend/AHKFlow.UI.Blazor/wwwroot/
├── appsettings.json                     ✅ Committed (production values)
└── appsettings.Development.json         ❌ Ignored (local dev override with localhost)
```

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

### Deployment

**Workflow:** `.github/workflows/ahkflow-deploy-frontend.yml`

```yaml
- uses: actions/checkout@v4  # ✅ Includes appsettings.json with production values

- name: Build and Deploy to Azure Static Web Apps
  uses: Azure/static-web-apps-deploy@v1
  # ✅ Deploys with production config automatically
```

**Result:** 
- Production: Uses `https://ahkflow-api-dev.azurewebsites.net`
- Local development: Uses `localhost:7600` (from `appsettings.Development.json`)

---

## Backend Configuration (ASP.NET Core API)

### Strategy: Azure App Service Configuration + Key Vault

**Template File:** `src/Backend/AHKFlow.API/appsettings.Production.json.example`

**Actual File:** `src/Backend/AHKFlow.API/appsettings.Production.json` → ❌ **IGNORED by git**

### Why This Must Be Secret

The backend configuration contains:
- ❌ SQL Connection Strings (with passwords)
- ❌ Database credentials
- ❌ Service-to-service secrets

These **must never be committed** to git.

### How Secrets Are Managed

**1. Local Development:**
- Copy `.example` file to create your local `appsettings.Production.json`
- Use fake/test values locally
- File is ignored by git (in `.gitignore`)

**2. Production (Azure):**
- Secrets stored in **Azure App Service Configuration**
- Connection strings reference **Azure Key Vault** using Managed Identity
- CI/CD workflow sets all values automatically

### Deployment

**Workflow:** `.github/workflows/ahkflow-deploy-api.yml`

**Step 1: Login to Azure**
```yaml
- name: Login to Azure
  uses: azure/login@v2
  with:
    creds: ${{ secrets.AHKFLOW_AZURE_CREDENTIALS }}
```

**Step 2: Configure Production Environment**
```yaml
- name: Configure Production Environment (Secure)
  run: |
    # Set App Service environment variables (overrides appsettings.Production.json)
    az webapp config appsettings set \
      --name ahkflow-api-dev \
      --resource-group rg-ahkflow-dev \
      --settings \
        ASPNETCORE_ENVIRONMENT="Production" \
        AzureAd__ClientId="${{ secrets.AZURE_AD_CLIENT_ID }}" \
        AzureAd__TenantId="${{ secrets.AZURE_AD_TENANT_ID }}" \
        Cors__AllowedOrigins__0="https://white-island-04c4b8103.1.azurestaticapps.net"
    
    # Configure connection string with Key Vault reference
    CONNECTION_STRING="Server=...;Password=@Microsoft.KeyVault(SecretUri=...);"
    az webapp config connection-string set \
      --name ahkflow-api-dev \
      --settings DefaultConnection="$CONNECTION_STRING"
```

**Step 3: Deploy**
```yaml
- name: Deploy to Azure App Service
  uses: azure/webapps-deploy@v3
  # ✅ App Service Configuration overrides appsettings.Production.json
```

### Configuration Hierarchy

Azure App Service loads configuration in this order (later overrides earlier):

1. `appsettings.json` (base config)
2. `appsettings.Production.json` (if exists - ignored in our case)
3. **Azure App Service Configuration** ← ✅ **Our secrets live here**
4. Environment variables

**Result:** Secrets never touch git, managed securely in Azure.

---

## Summary: Best Practice Implementation

### ✅ Frontend (Blazor WASM)

| Aspect | Implementation |
|--------|---------------|
| **File** | `appsettings.Production.json` |
| **Storage** | ✅ Committed to git |
| **Contains** | Public config only (Client ID, API URL) |
| **Why** | Client-side code is always visible to users |
| **Microsoft Docs** | "Provide _public_ authentication configuration in an app settings file" |
| **Deployment** | Static Web Apps includes file automatically |

### ✅ Backend (API)

| Aspect | Implementation |
|--------|---------------|
| **File** | `appsettings.Production.json.example` (template) |
| **Storage** | ❌ NOT committed (in `.gitignore`) |
| **Contains** | Secrets (ConnectionStrings, credentials) |
| **Why** | Server-side secrets must never be public |
| **Production** | Azure App Service Configuration + Key Vault |
| **Deployment** | CI/CD sets environment variables in Azure |

---

## Configuration Files Reference

### ✅ Committed to Git (Safe)

```
src/Frontend/AHKFlow.UI.Blazor/wwwroot/
├── appsettings.json                     ✅ Committed (production values)
├── appsettings.Development.json         ❌ Ignored (local dev override)
└── appsettings.Production.json.example  ✅ Example (deprecated - not needed)

src/Backend/AHKFlow.API/
├── appsettings.json                     ✅ Base config
├── appsettings.Development.json         ❌ Ignored (local only)
├── appsettings.Production.json          ❌ IGNORED (has secrets)
└── appsettings.Production.json.example  ✅ Template (safe)
```

### `.gitignore` Rules

```gitignore
# Local development only
**/appsettings.Development.json
**/appsettings.Local.json

# Backend production secrets - use Azure App Service Configuration
src/Backend/**/appsettings.Production.json

# Frontend: appsettings.json has production values (public, safe to commit)
# Frontend: appsettings.Development.json overrides for local development
```

---

## GitHub Secrets Required

The CI/CD workflows require these secrets:

| Secret | Used By | Purpose |
|--------|---------|---------|
| `AHKFLOW_AZURE_CREDENTIALS` | Backend + Frontend | Service Principal for Azure login |
| `AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN` | Frontend | Static Web Apps deployment |
| `AHKFLOW_SQL_MIGRATION_CONNECTION_STRING` | Backend | EF Core migrations |
| `AZURE_AD_CLIENT_ID` | Backend | Azure AD authentication config |
| `AZURE_AD_TENANT_ID` | Backend | Azure AD authentication config |

See: [GitHub Secrets Setup Guide](../docs/GITHUB_SECRETS_SETUP.md)

---

## Local Development Setup

### Frontend

```powershell
# No setup needed!
# appsettings.Development.json is ignored but present locally
# Uses localhost:7600 for local API
```

### Backend

```powershell
# Copy the example file
Copy-Item src/Backend/AHKFlow.API/appsettings.Production.json.example `
          src/Backend/AHKFlow.API/appsettings.Production.json

# Edit with your local values (or leave as-is for local development)
# This file is ignored by git
```

---

## Production Deployment Flow

### Frontend Deployment

1. Developer pushes code to `main` (or feature branch)
2. GitHub Actions checks out code → **includes `appsettings.Production.json`** ✅
3. Static Web Apps deploys Blazor WASM
4. Browser downloads `appsettings.Production.json` → uses production API URL ✅

### Backend Deployment

1. Developer pushes code to `main` (or feature branch)
2. GitHub Actions builds and tests
3. Runs EF Core migrations using `AHKFLOW_SQL_MIGRATION_CONNECTION_STRING`
4. **Configures Azure App Service:**
   - Sets `AzureAd__ClientId` from GitHub Secret
   - Sets `AzureAd__TenantId` from GitHub Secret
   - Sets `Cors__AllowedOrigins__0` to Static Web App URL
   - Sets `DefaultConnection` with Key Vault reference
5. Deploys API to App Service
6. App Service loads config: base → **Azure Configuration (overrides all)** ✅

---

## Security Model

### Frontend (Public by Design)

```
Browser
  ↓ HTTPS
Static Web App (Azure)
  ↓ Downloads
appsettings.Production.json ← ✅ Public (visible in browser)
```

**Can contain:** Client ID, API URLs, public endpoints  
**Cannot contain:** Passwords, API keys, secrets

### Backend (Secrets in Azure)

```
GitHub Actions
  ↓ Authenticated
Azure App Service
  ↓ Managed Identity
Azure Key Vault
  ↓ Secure Reference
SQL Database
```

**Secrets stored:**
- ✅ Azure App Service Configuration (environment variables)
- ✅ Azure Key Vault (SQL passwords)
- ❌ Never in git

---

## Migration Path

### If You Previously Committed Secrets

If `appsettings.Production.json` with secrets was previously committed:

```powershell
# Remove from history (use BFG or git-filter-repo)
# Then update .gitignore as documented

# For simple cases (if just pushed recently)
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch src/Backend/AHKFlow.API/appsettings.Production.json" \
  --prune-empty --tag-name-filter cat -- --all

# Force push (⚠️  coordinate with team)
git push origin --force --all
```

**Better:** Rotate all secrets after removal.

---

## Verification Checklist

Before pushing to production, verify:

### Frontend
- [ ] `appsettings.Production.json` exists in git
- [ ] `BaseAddress` points to production API URL
- [ ] No secrets present (Client ID is public)

### Backend
- [ ] `appsettings.Production.json` is in `.gitignore`
- [ ] `appsettings.Production.json.example` exists (template)
- [ ] GitHub Secrets configured (see [QUICK_REFERENCE_SECRETS.md](QUICK_REFERENCE_SECRETS.md))
- [ ] Key Vault access granted to App Service Managed Identity

### CI/CD
- [ ] All required GitHub Secrets set
- [ ] Workflows include production configuration steps
- [ ] Test deployment succeeds on feature branch

---

## Related Documentation

- [GitHub Secrets Setup](../docs/GITHUB_SECRETS_SETUP.md) - Complete secrets configuration guide
- [Quick Reference: Secrets](QUICK_REFERENCE_SECRETS.md) - Fast commands for common tasks
- [Production Config Setup](../docs/PRODUCTION_CONFIG_SETUP.md) - Detailed production setup
- [Azure CLI Setup](../docs/AZURE_CLI_SETUP.md) - Azure resource provisioning
- [Microsoft: Blazor Configuration](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/configuration?view=aspnetcore-10.0) - Official documentation

---

## Questions?

- **"Should I commit this config file?"**
  - Frontend: ✅ Yes (public)
  - Backend: ❌ No (secrets)

- **"Where do production secrets go?"**
  - Azure App Service Configuration (set by CI/CD workflow)

- **"How does the API get the SQL password?"**
  - Key Vault reference with Managed Identity: `@Microsoft.KeyVault(SecretUri=...)`

- **"Why is Client ID public?"**
  - OAuth 2.0 spec: public clients can't have secrets (browser apps are public clients)

---

**Last Updated:** Based on Microsoft Blazor documentation for ASP.NET Core 10.0
