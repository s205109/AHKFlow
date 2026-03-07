# CI/CD Production Configuration Guide

This guide explains how to configure production environment securely via GitHub Actions workflows.

## Overview

Instead of manual PowerShell scripts, use GitHub Actions workflows to:
- ✅ Enable Managed Identity on App Service
- ✅ Grant Key Vault access to Managed Identity  
- ✅ Configure environment variables (Azure AD settings)
- ✅ Set connection string with Key Vault reference
- ✅ Deploy and verify the application

## Workflows

### 1. Standalone Configuration Workflow

**File**: `.github/workflows/ahkflow-configure-production.yml`

**Purpose**: Configure production environment settings (run once or when settings change)

**Trigger**: Manual (`workflow_dispatch`)

**Usage**:
1. Go to GitHub Actions tab
2. Select "Configure Production Environment"
3. Click "Run workflow"
4. Select environment (dev/prod)
5. Click "Run workflow"

**What it does**:
- Retrieves Azure AD Client ID and Tenant ID
- Enables Managed Identity on App Service
- Grants Key Vault `get` and `list` permissions to Managed Identity
- Sets App Service environment variables:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `AzureAd__ClientId`
  - `AzureAd__TenantId`
  - `AzureAd__Instance`
  - `AzureAd__CallbackPath`
  - `AzureAd__Scopes`
- Configures connection string with Key Vault reference format:
  ```
  Server=tcp:...;Password=@Microsoft.KeyVault(SecretUri=...);...
  ```
- Configures CORS for Static Web App origin
- Restarts App Service
- Tests health endpoint

### 2. Integrated Deployment Workflow (Recommended)

**File**: `.github/workflows/ahkflow-deploy-api.yml`

**Purpose**: Build, test, migrate, configure, and deploy API in one workflow

**Trigger**: 
- Push to `main` branch (Backend changes)
- Manual (`workflow_dispatch`)

**Jobs**:
1. **Build** - Compile and test .NET application
2. **Migrate** - Run EF Core database migrations
3. **Deploy** - Configure production settings + deploy code

**What the Deploy job does**:
```yaml
steps:
  - Download build artifact
  - Azure Login
  - Configure Production Environment (Secure)  # ← Integrated!
    - Enable Managed Identity
    - Grant Key Vault access
    - Set environment variables
    - Configure connection string with Key Vault reference
  - Deploy to Azure App Service
  - Verify API Health
```

## Security Benefits

### No Secrets in Code ✅
- `appsettings.Production.json` contains **no secrets**
- Placeholder values like `YOUR_PRODUCTION_CLIENT_ID` are replaced at runtime

### Managed Identity ✅
- App Service authenticates to Key Vault without passwords
- No service principal credentials needed in App Service

### Key Vault References ✅
Connection string format:
```
Password=@Microsoft.KeyVault(SecretUri=https://ahkflow-kv-dev.vault.azure.net/secrets/sql-admin-password/...)
```
- Password is **never** stored in App Service configuration
- Fetched at runtime from Key Vault
- Automatically rotates when secret is updated

### Environment Variables ✅
Non-secret configuration stored as App Service settings:
- `AzureAd__ClientId` - public identifier
- `AzureAd__TenantId` - public identifier
- `AzureAd__Instance` - public URL

---

## How to Use

### Option 1: Run Standalone Configuration (One-Time Setup)

```bash
# Trigger the workflow via GitHub CLI
gh workflow run ahkflow-configure-production.yml -f environment=dev

# Or via GitHub Web UI:
# 1. Go to Actions tab
# 2. Select "Configure Production Environment"
# 3. Click "Run workflow"
```

### Option 2: Automatic Configuration on Every Deploy (Recommended)

The configuration step is already integrated into `ahkflow-deploy-api.yml`.

Every time you deploy:
1. Code is built and tested
2. Database migrations run
3. **Production settings are configured** ← Happens automatically
4. Code is deployed
5. Health check verifies success

**To deploy:**
```bash
# Commit your code changes
git add .
git commit -m "Update API"
git push origin main

# Deployment workflow runs automatically
```

---

## Configuration Details

### App Service Environment Variables

Set via `az webapp config appsettings set`:

| Variable | Value | Description |
|----------|-------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Loads `appsettings.Production.json` |
| `AzureAd__ClientId` | Retrieved from Azure AD | App Registration Client ID |
| `AzureAd__TenantId` | Retrieved from Azure AD | Azure AD Tenant ID |
| `AzureAd__Instance` | `https://login.microsoftonline.com/` | Azure AD authority |
| `AzureAd__CallbackPath` | `/signin-oidc` | OIDC callback path |
| `AzureAd__Scopes` | `access_as_user` | API scope |

### Connection String with Key Vault Reference

Set via `az webapp config connection-string set`:

**Format:**
```
Server=tcp:{SQL_SERVER_FQDN},1433;
Initial Catalog={DATABASE_NAME};
User ID={ADMIN_USER};
Password=@Microsoft.KeyVault(SecretUri={KEY_VAULT_SECRET_URI});
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

**Example:**
```
Server=tcp:ahkflow-sql-dev.database.windows.net,1433;
Initial Catalog=ahkflow-db;
User ID=ahkflowadmin;
Password=@Microsoft.KeyVault(SecretUri=https://ahkflow-kv-dev.vault.azure.net/secrets/sql-admin-password/abc123);
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

At runtime, App Service:
1. Sees `@Microsoft.KeyVault(...)`
2. Uses Managed Identity to authenticate to Key Vault
3. Fetches the secret value
4. Replaces the reference with the actual password

### Managed Identity Permissions

Key Vault access policy grants:
- **Secret Permissions**: `get`, `list`
- **Object ID**: Managed Identity Principal ID from App Service

Command:
```bash
az keyvault set-policy \
  --name ahkflow-kv-dev \
  --object-id <PRINCIPAL_ID> \
  --secret-permissions get list
```

---

## Verification

### Check App Service Configuration

```bash
# View environment variables
az webapp config appsettings list \
  --name ahkflow-api-dev \
  --resource-group rg-ahkflow-dev

# View connection strings
az webapp config connection-string list \
  --name ahkflow-api-dev \
  --resource-group rg-ahkflow-dev
```

### Test Health Endpoint

```bash
curl https://ahkflow-api-dev.azurewebsites.net/api/v1/health
```

Expected response:
```json
{"status":"Healthy","timestamp":"2026-03-07T..."}
```

### View Logs

```bash
# Stream logs
az webapp log tail \
  --name ahkflow-api-dev \
  --resource-group rg-ahkflow-dev

# Or via Azure Portal:
# App Service → Monitoring → Log stream
```

---

## Troubleshooting

### 500.30 Error

**Cause**: Missing `appsettings.Production.json` or invalid configuration

**Fix**: 
1. Ensure `appsettings.Production.json` exists in `src/Backend/AHKFlow.API/`
2. Run configuration workflow
3. Check logs for detailed error

### Key Vault Access Denied

**Cause**: Managed Identity doesn't have Key Vault permissions

**Fix**:
```bash
# Re-run configuration workflow, or manually grant access:
PRINCIPAL_ID=$(az webapp identity show \
  --name ahkflow-api-dev \
  --resource-group rg-ahkflow-dev \
  --query principalId -o tsv)

az keyvault set-policy \
  --name ahkflow-kv-dev \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Connection String Error

**Cause**: Invalid Key Vault reference format or secret doesn't exist

**Fix**:
1. Verify secret exists:
```bash
az keyvault secret show \
  --vault-name ahkflow-kv-dev \
  --name sql-admin-password
```

2. Re-run configuration workflow to reset connection string

---

## Comparison: Manual vs CI/CD

| Aspect | Manual PowerShell | CI/CD Workflow |
|--------|------------------|----------------|
| **Repeatability** | ❌ Manual steps | ✅ Automated |
| **Consistency** | ❌ Prone to errors | ✅ Same every time |
| **Auditability** | ❌ No record | ✅ Git history |
| **Team Sharing** | ❌ Local scripts | ✅ Centralized |
| **Rollback** | ❌ Manual | ✅ Re-run previous workflow |
| **Documentation** | ❌ Must be updated | ✅ Self-documenting |

---

## Next Steps

1. ✅ **Configuration is now integrated** into `ahkflow-deploy-api.yml`
2. ✅ Push your changes to trigger deployment
3. ✅ Verify health endpoint after deployment
4. ✅ (Optional) Run standalone workflow for immediate configuration

**All production settings are now managed via CI/CD!** 🎉

---

## Related Files

- `.github/workflows/ahkflow-configure-production.yml` - Standalone configuration
- `.github/workflows/ahkflow-deploy-api.yml` - Integrated deployment
- `src/Backend/AHKFlow.API/appsettings.Production.json` - Production config template
- `docs/AZURE_CLI_SETUP.md` - Azure resource provisioning
- `docs/GITHUB_SECRETS_SETUP.md` - GitHub secrets configuration
