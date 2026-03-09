# Quick Reference: GitHub Secrets & Azure Configuration

Quick commands for setting up GitHub Secrets and Azure configuration required for CI/CD.

## GitHub Repository

**Settings URL:** https://github.com/s205109/AHKFlow/settings/secrets/actions

---

## Required GitHub Secrets

| Secret Name | PowerShell Command to Get Value |
|-------------|--------------------------------|
| `AZURE_AD_CLIENT_ID` | `az ad app list --display-name "AHKFlow-Dev" --query "[0].appId" -o tsv` |
| `AZURE_AD_TENANT_ID` | `az account show --query "tenantId" -o tsv` |
| `AHKFLOW_AZURE_CREDENTIALS` | See [Full Setup Guide](../docs/GITHUB_SECRETS_SETUP.md#2-service-principal-ahkflow_azure_credentials) |
| `AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN` | `az staticwebapp secrets list --name ahkflow-swa-dev --resource-group rg-ahkflow-dev --query "properties.apiKey" -o tsv` |
| `AHKFLOW_SQL_MIGRATION_CONNECTION_STRING` | From App Service Configuration or Key Vault |

---

## One-Time Azure Configuration

### Grant Key Vault Access to App Service (PowerShell)

```powershell
# Get the Managed Identity Principal ID
$PRINCIPAL_ID = az webapp identity show `
  --name ahkflow-api-dev `
  --resource-group rg-ahkflow-dev `
  --query principalId `
  --output tsv

# Get Subscription ID
$SUBSCRIPTION_ID = az account show --query id --output tsv

# Grant Key Vault access
az role assignment create `
  --role "Key Vault Secrets User" `
  --assignee-object-id $PRINCIPAL_ID `
  --assignee-principal-type ServicePrincipal `
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-ahkflow-dev/providers/Microsoft.KeyVault/vaults/ahkflow-kv-dev"

Write-Host "✓ Key Vault access granted"
```

### Alternative: Azure Portal

1. Go to `ahkflow-api-dev` → **Identity** → Copy **Object (principal) ID**
2. Go to `ahkflow-kv-dev` → **Access control (IAM)** → **Add role assignment**
3. Select **Key Vault Secrets User** → Select `ahkflow-api-dev` → **Assign**

---

## Quick Setup All Secrets (PowerShell)

```powershell
# Set GitHub repo
$GITHUB_REPO = "s205109/AHKFlow"

# 1. Azure AD Client ID
$CLIENT_ID = az ad app list --display-name "AHKFlow-Dev" --query "[0].appId" -o tsv
$CLIENT_ID | gh secret set AZURE_AD_CLIENT_ID --repo $GITHUB_REPO
Write-Host "✓ AZURE_AD_CLIENT_ID"

# 2. Azure AD Tenant ID
$TENANT_ID = az account show --query "tenantId" -o tsv
$TENANT_ID | gh secret set AZURE_AD_TENANT_ID --repo $GITHUB_REPO
Write-Host "✓ AZURE_AD_TENANT_ID"

# 3. Static Web Apps Token
$SWA_TOKEN = az staticwebapp secrets list `
  --name ahkflow-swa-dev `
  --resource-group rg-ahkflow-dev `
  --query "properties.apiKey" -o tsv
$SWA_TOKEN | gh secret set AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN --repo $GITHUB_REPO
Write-Host "✓ AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN"

# 4. Service Principal (if not already set)
# See full guide: docs/GITHUB_SECRETS_SETUP.md#2-service-principal

# 5. SQL Connection String (if not already set)
# See full guide: docs/GITHUB_SECRETS_SETUP.md#4-sql-connection-string

# Verify
Write-Host "`n=== Verify Secrets ==="
gh secret list --repo $GITHUB_REPO
```

---

## Common Issues

### PowerShell Syntax Error

❌ **Wrong (Bash):**
```bash
PRINCIPAL_ID=$(az webapp identity show ...)
```

✅ **Correct (PowerShell):**
```powershell
$PRINCIPAL_ID = az webapp identity show ...
```

### Service Principal Permission Error

If you see `AuthorizationFailed` when creating role assignments:

1. The Service Principal doesn't have permission to assign roles
2. Run the **One-Time Azure Configuration** section above **locally** (you have higher permissions)
3. The workflow will then work without needing to assign roles

---

## Full Documentation

For complete setup instructions, see:
- [GitHub Secrets Setup Guide](../docs/GITHUB_SECRETS_SETUP.md)
- [Azure CLI Setup Guide](../docs/AZURE_CLI_SETUP.md)
- [Production Configuration Setup](../docs/PRODUCTION_CONFIG_SETUP.md)
