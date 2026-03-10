# GitHub Secrets Setup

Retrieve Azure credentials and configure GitHub secrets for CI/CD workflows.

## Prerequisites

- Azure CLI installed and logged in (`az login`)
- GitHub CLI installed and authenticated (`gh auth login`)
- Active Azure subscription with deployed resources
- Repository: `s205109/AHKFlow`

**💡 Windows Users:** **Use PowerShell instead of Git Bash** to avoid path conversion issues. Each section provides both Bash and PowerShell examples.

---

## Required GitHub Secrets for CI/CD

The CI/CD workflows require these secrets to be configured in GitHub:

### Core Secrets

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `AHKFLOW_AZURE_CREDENTIALS` | Service Principal JSON for Azure login | See [Section 2](#2-service-principal-ahkflow_azure_credentials) |
| `AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN` | Static Web Apps deployment token | See [Section 3](#3-static-web-apps-token-ahkflow_azure_static_web_apps_api_token) |
| `AHKFLOW_SQL_MIGRATION_CONNECTION_STRING` | SQL connection string for migrations | See [Section 4](#4-sql-connection-string-ahkflow_sql_migration_connection_string) |
| `APP_INSIGHTS_CONNECTION_STRING` | Application Insights connection string for backend telemetry | See [Section 5](#5-application-insights-connection-string-app_insights_connection_string) |
| `AZURE_AD_CLIENT_ID` | Azure AD App Registration Client ID | See [Section 6](#6-azure-ad-configuration-azure_ad_client_id--azure_ad_tenant_id) |
| `AZURE_AD_TENANT_ID` | Azure AD Tenant ID | See [Section 6](#6-azure-ad-configuration-azure_ad_client_id--azure_ad_tenant_id) |

### Optional Secrets

| Secret Name | Description | When Needed |
|-------------|-------------|-------------|
| `KEY_VAULT_SECRET_URI` | Key Vault secret URI for SQL password | For Key Vault integration (recommended for production) |

### Where to Add Secrets

Go to: **https://github.com/s205109/AHKFlow/settings/secrets/actions**

Click **"New repository secret"** and add each secret name and value.

---

## Variables

Set these once at the start:

```bash
# Environment: dev or prod
ENVIRONMENT="dev"

# Azure resources
RESOURCE_GROUP="rg-ahkflow-${ENVIRONMENT}"
SWA_NAME="ahkflow-swa-${ENVIRONMENT}"
APP_SERVICE_NAME="ahkflow-appservice-api-${ENVIRONMENT}"
SQL_SERVER_NAME="ahkflow-sql-${ENVIRONMENT}"
SQL_DATABASE_NAME="ahkflow-db"

# GitHub
GITHUB_REPO="s205109/AHKFlow"

echo "ENVIRONMENT: $ENVIRONMENT"
echo "RESOURCE_GROUP: $RESOURCE_GROUP"
echo "GITHUB_REPO: $GITHUB_REPO"
```

---

## Retrieve and Set Secrets

### 1. Verify GitHub CLI Authentication

```bash
# Step 1.1: Display and validate required variables
echo "=== Step 1: Verify GitHub CLI Authentication ==="
echo "Required variables:"
echo "  GITHUB_REPO: ${GITHUB_REPO:-NOT SET}"
echo ""

if [ -z "$GITHUB_REPO" ]; then
  echo "❌ Error: GITHUB_REPO not set"
  echo "Please run the 'Variables' section first"
else
  # Step 1.2: Check GitHub CLI authentication
  if gh auth status &>/dev/null; then
    echo "✓ GitHub CLI authenticated"
  else
    echo "❌ GitHub CLI not authenticated"
    echo "Run: gh auth login"
    exit 1
  fi
fi
```

### 2. Service Principal (`AHKFLOW_AZURE_CREDENTIALS`)

Create or retrieve service principal with contributor access:

#### Option A: Bash (Git Bash / WSL / Linux / macOS)

```bash
# Step 2.1: Display and validate required variables
echo "=== Step 2: Service Principal ==="
echo "Required variables:"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  GITHUB_REPO: ${GITHUB_REPO:-NOT SET}"
echo "  ENVIRONMENT: ${ENVIRONMENT:-NOT SET}"
echo ""

if [ -z "$RESOURCE_GROUP" ] || [ -z "$GITHUB_REPO" ] || [ -z "$ENVIRONMENT" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first"
  exit 1
fi

# IMPORTANT: Export to prevent Git Bash path conversion on Windows
export MSYS_NO_PATHCONV=1

# Step 2.2: Get subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

if [ -z "$SUBSCRIPTION_ID" ]; then
  echo "❌ Error: Could not retrieve subscription ID"
  echo "Ensure you're logged in: az login"
  exit 1
fi

echo "Subscription ID: $SUBSCRIPTION_ID"
echo ""

# Step 2.3: Create service principal (or retrieve if exists)
SP_NAME="ahkflow-github-actions-${ENVIRONMENT}"

# Check if exists
SP_APP_ID=$(az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv)

if [ -z "$SP_APP_ID" ]; then
  echo "Creating service principal..."

  SP_CREDENTIALS=$(az ad sp create-for-rbac \
    --name "$SP_NAME" \
    --role contributor \
    --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
    --sdk-auth)

  echo "✓ Created service principal"
else
  echo "⚠ Service principal exists. Creating new credentials..."

  # Reset credentials
  SP_CREDENTIALS=$(az ad sp create-for-rbac \
    --name "$SP_NAME" \
    --role contributor \
    --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
    --sdk-auth)

  echo "✓ Reset service principal credentials"
fi

# Step 2.4: Set GitHub secret
echo "$SP_CREDENTIALS" | gh secret set AHKFLOW_AZURE_CREDENTIALS --repo $GITHUB_REPO
echo "✓ Set AHKFLOW_AZURE_CREDENTIALS"
```

#### Option B: PowerShell (Recommended for Windows)

```powershell
# Step 2.1: Display and validate required variables
Write-Host "=== Step 2: Service Principal ==="
Write-Host "Required variables:"
Write-Host "  RESOURCE_GROUP: $RESOURCE_GROUP"
Write-Host "  GITHUB_REPO: $GITHUB_REPO"
Write-Host "  ENVIRONMENT: $ENVIRONMENT"
Write-Host ""

if ([string]::IsNullOrEmpty($RESOURCE_GROUP) -or [string]::IsNullOrEmpty($GITHUB_REPO) -or [string]::IsNullOrEmpty($ENVIRONMENT)) {
    Write-Host "❌ Error: Required variables not set" -ForegroundColor Red
    Write-Host "Please run the 'Variables' section first"
    exit 1
}

# Step 2.2: Get subscription ID
$SUBSCRIPTION_ID = az account show --query id -o tsv

if ([string]::IsNullOrEmpty($SUBSCRIPTION_ID)) {
    Write-Host "❌ Error: Could not retrieve subscription ID" -ForegroundColor Red
    Write-Host "Ensure you're logged in: az login"
    exit 1
}

Write-Host "Subscription ID: $SUBSCRIPTION_ID"
Write-Host ""

# Step 2.3: Create service principal (or retrieve if exists)
$SP_NAME = "ahkflow-github-actions-$ENVIRONMENT"

# Check if exists
$SP_APP_ID = az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv

if ([string]::IsNullOrEmpty($SP_APP_ID)) {
    Write-Host "Creating service principal..."

    $SP_CREDENTIALS = az ad sp create-for-rbac `
        --name "$SP_NAME" `
        --role contributor `
        --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP" `
        --sdk-auth

    Write-Host "✓ Created service principal"
} else {
    Write-Host "⚠ Service principal exists. Creating new credentials..."

    # Reset credentials
    $SP_CREDENTIALS = az ad sp create-for-rbac `
        --name "$SP_NAME" `
        --role contributor `
        --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP" `
        --sdk-auth

    Write-Host "✓ Reset service principal credentials"
}

# Step 2.4: Set GitHub secret
$SP_CREDENTIALS | gh secret set AHKFLOW_AZURE_CREDENTIALS --repo $GITHUB_REPO
Write-Host "✓ Set AHKFLOW_AZURE_CREDENTIALS"
```

### 3. Static Web Apps Token (`AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN`)

```bash
# Step 3.1: Display and validate required variables
echo "=== Step 3: Static Web Apps Token ==="
echo "Required variables:"
echo "  SWA_NAME: ${SWA_NAME:-NOT SET}"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  GITHUB_REPO: ${GITHUB_REPO:-NOT SET}"
echo ""

if [ -z "$SWA_NAME" ] || [ -z "$RESOURCE_GROUP" ] || [ -z "$GITHUB_REPO" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first"
  exit 1
fi

# Step 3.2: Retrieve deployment token
SWA_TOKEN=$(az staticwebapp secrets list \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.apiKey" -o tsv)

if [ -z "$SWA_TOKEN" ]; then
  echo "❌ Error: Static Web App not found or no token available"
  echo "Verify the Static Web App exists:"
  echo "  az staticwebapp list --resource-group $RESOURCE_GROUP --query \"[].name\" -o tsv"
  exit 1
fi

# Step 3.3: Set GitHub secret
echo "$SWA_TOKEN" | gh secret set AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN --repo $GITHUB_REPO
echo "✓ Set AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN"
```

### 4. SQL Connection String (`AHKFLOW_SQL_MIGRATION_CONNECTION_STRING`)

#### Option A: Bash

```bash
# Step 4.1: Display and validate required variables
echo "=== Step 4: SQL Connection String ==="
echo "Required variables:"
echo "  APP_SERVICE_NAME: ${APP_SERVICE_NAME:-NOT SET}"
echo "  SQL_SERVER_NAME: ${SQL_SERVER_NAME:-NOT SET}"
echo "  SQL_DATABASE_NAME: ${SQL_DATABASE_NAME:-NOT SET}"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  ENVIRONMENT: ${ENVIRONMENT:-NOT SET}"
echo "  GITHUB_REPO: ${GITHUB_REPO:-NOT SET}"
echo ""

if [ -z "$APP_SERVICE_NAME" ] || [ -z "$SQL_SERVER_NAME" ] || [ -z "$SQL_DATABASE_NAME" ] || [ -z "$RESOURCE_GROUP" ] || [ -z "$ENVIRONMENT" ] || [ -z "$GITHUB_REPO" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first"
  exit 1
fi

# Step 4.2: Retrieve SQL admin credentials from App Service configuration
SQL_CONNECTION_STRING=$(az webapp config connection-string list \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "[?name=='DefaultConnection'].value" -o tsv)

if [ -z "$SQL_CONNECTION_STRING" ]; then
  echo "⚠ Connection string not found in App Service config"
  echo "Attempting to retrieve from Key Vault..."

  # Step 4.3: Try to retrieve SQL password from Key Vault
  KEY_VAULT_NAME="ahkflow-kv-${ENVIRONMENT}"
  SQL_ADMIN_PASSWORD=$(az keyvault secret show \
    --vault-name $KEY_VAULT_NAME \
    --name sql-admin-password \
    --query value -o tsv 2>/dev/null)

  if [ -n "$SQL_ADMIN_PASSWORD" ]; then
    # Step 4.4: Get SQL Server details
    SQL_SERVER_FQDN=$(az sql server show \
      --name $SQL_SERVER_NAME \
      --resource-group $RESOURCE_GROUP \
      --query fullyQualifiedDomainName -o tsv 2>/dev/null)

    SQL_ADMIN_USER=$(az sql server show \
      --name $SQL_SERVER_NAME \
      --resource-group $RESOURCE_GROUP \
      --query administratorLogin -o tsv 2>/dev/null)

    if [ -n "$SQL_SERVER_FQDN" ] && [ -n "$SQL_ADMIN_USER" ]; then
      # Step 4.5: Construct connection string
      SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Initial Catalog=${SQL_DATABASE_NAME};User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

      echo "✓ Retrieved SQL password from Key Vault: $KEY_VAULT_NAME"
      echo "✓ Constructed connection string"

      # Step 4.6: Set GitHub secret
      echo "$SQL_CONNECTION_STRING" | gh secret set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING --repo $GITHUB_REPO
      echo "✓ Set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING"
    else
      echo "❌ Error: SQL Server details not found"
      exit 1
    fi
  else
    echo "⚠ SQL password not found in Key Vault: $KEY_VAULT_NAME"
    echo ""
    echo "⚠ MANUAL ACTION REQUIRED:"
    echo "1. Retrieve password from Key Vault manually:"
    echo "   az keyvault secret show --vault-name $KEY_VAULT_NAME --name sql-admin-password --query value -o tsv"
    echo ""
    echo "2. Or construct the connection string manually and set the secret:"
    echo "   gh secret set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING --repo $GITHUB_REPO"
    echo ""
    exit 1
  fi
else
  # Step 4.7: Set GitHub secret only if connection string is not empty
  echo "$SQL_CONNECTION_STRING" | gh secret set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING --repo $GITHUB_REPO
  echo "✓ Set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING (from App Service config)"
fi
```

### 5. Application Insights Connection String (`APP_INSIGHTS_CONNECTION_STRING`)

**Purpose:** Backend API telemetry and monitoring via Serilog → Application Insights.

#### Option A: Bash

```bash
# Step 5.1: Display and validate required variables
echo "=== Step 5: Application Insights Connection String ==="
echo "Required variables:"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  ENVIRONMENT: ${ENVIRONMENT:-NOT SET}"
echo "  GITHUB_REPO: ${GITHUB_REPO:-NOT SET}"
echo ""

if [ -z "$RESOURCE_GROUP" ] || [ -z "$ENVIRONMENT" ] || [ -z "$GITHUB_REPO" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first"
  exit 1
fi

# Step 5.2: Retrieve Application Insights connection string
APP_INSIGHTS_NAME="ahkflow-insights-${ENVIRONMENT}"

APP_INSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv 2>/dev/null)

if [ -z "$APP_INSIGHTS_CONNECTION_STRING" ]; then
  echo "⚠ Application Insights not found: $APP_INSIGHTS_NAME"
  echo ""
  echo "Create Application Insights first using docs/AZURE_CLI_SETUP.md Section 6"
  echo "Or skip this secret if you're not using Application Insights"
  echo ""
  exit 1
fi

echo "✓ Retrieved Application Insights connection string"

# Step 5.3: Set GitHub secret
echo "$APP_INSIGHTS_CONNECTION_STRING" | gh secret set APP_INSIGHTS_CONNECTION_STRING --repo $GITHUB_REPO
echo "✓ Set APP_INSIGHTS_CONNECTION_STRING"
```

#### Option B: PowerShell (Recommended for Windows)

```powershell
# Step 5.1: Display and validate required variables
Write-Host "=== Step 5: Application Insights Connection String ==="
Write-Host "Required variables:"
Write-Host "  RESOURCE_GROUP: $RESOURCE_GROUP"
Write-Host "  ENVIRONMENT: $ENVIRONMENT"
Write-Host "  GITHUB_REPO: $GITHUB_REPO"
Write-Host ""

if ([string]::IsNullOrEmpty($RESOURCE_GROUP) -or 
    [string]::IsNullOrEmpty($ENVIRONMENT) -or 
    [string]::IsNullOrEmpty($GITHUB_REPO)) {
    Write-Host "❌ Error: Required variables not set" -ForegroundColor Red
    Write-Host "Please run the 'Variables' section first"
    exit 1
}

# Step 5.2: Retrieve Application Insights connection string
$APP_INSIGHTS_NAME = "ahkflow-insights-$ENVIRONMENT"

$APP_INSIGHTS_CONNECTION_STRING = az monitor app-insights component show `
    --app $APP_INSIGHTS_NAME `
    --resource-group $RESOURCE_GROUP `
    --query connectionString -o tsv 2>$null

if ([string]::IsNullOrEmpty($APP_INSIGHTS_CONNECTION_STRING)) {
    Write-Host "⚠ Application Insights not found: $APP_INSIGHTS_NAME" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Create Application Insights first using docs/AZURE_CLI_SETUP.md Section 6"
    Write-Host "Or skip this secret if you're not using Application Insights"
    Write-Host ""
    exit 1
}

Write-Host "✓ Retrieved Application Insights connection string"

# Step 5.3: Set GitHub secret
$APP_INSIGHTS_CONNECTION_STRING | gh secret set APP_INSIGHTS_CONNECTION_STRING --repo $GITHUB_REPO
Write-Host "✓ Set APP_INSIGHTS_CONNECTION_STRING"
```

### 6. Azure AD Configuration (`AZURE_AD_CLIENT_ID` & `AZURE_AD_TENANT_ID`)

#### Option A: Bash

```bash
# Step 6.1: Display and validate required variables
echo "=== Step 6: Azure AD Configuration ==="
# Step 4.1: Display and validate required variables
Write-Host "=== Step 4: SQL Connection String ==="
Write-Host "Required variables:"
Write-Host "  APP_SERVICE_NAME: $APP_SERVICE_NAME"
Write-Host "  SQL_SERVER_NAME: $SQL_SERVER_NAME"
Write-Host "  SQL_DATABASE_NAME: $SQL_DATABASE_NAME"
Write-Host "  RESOURCE_GROUP: $RESOURCE_GROUP"
Write-Host "  ENVIRONMENT: $ENVIRONMENT"
Write-Host "  GITHUB_REPO: $GITHUB_REPO"
Write-Host ""

if ([string]::IsNullOrEmpty($APP_SERVICE_NAME) -or 
    [string]::IsNullOrEmpty($SQL_SERVER_NAME) -or 
    [string]::IsNullOrEmpty($SQL_DATABASE_NAME) -or 
    [string]::IsNullOrEmpty($RESOURCE_GROUP) -or 
    [string]::IsNullOrEmpty($ENVIRONMENT) -or 
    [string]::IsNullOrEmpty($GITHUB_REPO)) {
    Write-Host "❌ Error: Required variables not set" -ForegroundColor Red
    Write-Host "Please run the 'Variables' section first"
    exit 1
}

# Step 4.2: Retrieve SQL admin credentials from App Service configuration
$SQL_CONNECTION_STRING = az webapp config connection-string list `
    --name $APP_SERVICE_NAME `
    --resource-group $RESOURCE_GROUP `
    --query "[?name=='DefaultConnection'].value" -o tsv

if ([string]::IsNullOrEmpty($SQL_CONNECTION_STRING)) {
    Write-Host "⚠ Connection string not found in App Service config" -ForegroundColor Yellow
    Write-Host "Attempting to retrieve from Key Vault..."

    # Step 4.3: Try to retrieve SQL password from Key Vault
    $KEY_VAULT_NAME = "ahkflow-kv-$ENVIRONMENT"
    $SQL_ADMIN_PASSWORD = az keyvault secret show `
        --vault-name $KEY_VAULT_NAME `
        --name sql-admin-password `
        --query value -o tsv 2>$null

    if (-not [string]::IsNullOrEmpty($SQL_ADMIN_PASSWORD)) {
        # Step 4.4: Get SQL Server details
        $SQL_SERVER_FQDN = az sql server show `
            --name $SQL_SERVER_NAME `
            --resource-group $RESOURCE_GROUP `
            --query fullyQualifiedDomainName -o tsv 2>$null

        $SQL_ADMIN_USER = az sql server show `
            --name $SQL_SERVER_NAME `
            --resource-group $RESOURCE_GROUP `
            --query administratorLogin -o tsv 2>$null

        if (-not [string]::IsNullOrEmpty($SQL_SERVER_FQDN) -and -not [string]::IsNullOrEmpty($SQL_ADMIN_USER)) {
            # Step 4.5: Construct connection string
            $SQL_CONNECTION_STRING = "Server=tcp:$SQL_SERVER_FQDN,1433;Initial Catalog=$SQL_DATABASE_NAME;User ID=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

            Write-Host "✓ Retrieved SQL password from Key Vault: $KEY_VAULT_NAME"
            Write-Host "✓ Constructed connection string"

            # Step 4.6: Set GitHub secret
            $SQL_CONNECTION_STRING | gh secret set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING --repo $GITHUB_REPO
            Write-Host "✓ Set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING"
        } else {
            Write-Host "❌ Error: SQL Server details not found" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "⚠ SQL password not found in Key Vault: $KEY_VAULT_NAME" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "⚠ MANUAL ACTION REQUIRED:" -ForegroundColor Yellow
        Write-Host "1. Retrieve password from Key Vault manually:"
        Write-Host "   az keyvault secret show --vault-name $KEY_VAULT_NAME --name sql-admin-password --query value -o tsv"
        Write-Host ""
        Write-Host "2. Or construct the connection string manually and set the secret:"
        Write-Host "   gh secret set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING --repo $GITHUB_REPO"
        Write-Host ""
        exit 1
    }
} else {
    # Step 4.7: Set GitHub secret only if connection string is not empty
    $SQL_CONNECTION_STRING | gh secret set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING --repo $GITHUB_REPO
    Write-Host "✓ Set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING (from App Service config)"
}
```

### 5. Azure AD Configuration (`AZURE_AD_CLIENT_ID` & `AZURE_AD_TENANT_ID`)

#### Option A: Bash

```bash
# Step 5.1: Display and validate required variables
echo "=== Step 5: Azure AD Configuration ==="
echo "Required variables:"
echo "  GITHUB_REPO: ${GITHUB_REPO:-NOT SET}"
echo ""

if [ -z "$GITHUB_REPO" ]; then
  echo "❌ Error: GITHUB_REPO not set"
  echo "Please run the 'Variables' section first"
  exit 1
fi

# Step 5.2: Get Client ID
echo "Retrieving Azure AD Client ID..."
CLIENT_ID=$(az ad app list --display-name "AHKFlow-Dev" --query "[0].appId" -o tsv)

if [ -z "$CLIENT_ID" ]; then
  echo "❌ Error: Could not find AHKFlow-Dev app registration"
  echo "Verify the app registration exists: az ad app list --query \"[].displayName\" -o tsv"
  exit 1
fi

echo "Client ID: $CLIENT_ID"

# Step 5.3: Set GitHub secret for Client ID
echo "$CLIENT_ID" | gh secret set AZURE_AD_CLIENT_ID --repo $GITHUB_REPO
echo "✓ Set AZURE_AD_CLIENT_ID"

# Step 5.4: Get Tenant ID
echo "Retrieving Tenant ID..."
TENANT_ID=$(az account show --query "tenantId" -o tsv)

if [ -z "$TENANT_ID" ]; then
  echo "❌ Error: Could not retrieve Tenant ID"
  exit 1
fi

echo "Tenant ID: $TENANT_ID"

# Step 5.5: Set GitHub secret for Tenant ID
echo "$TENANT_ID" | gh secret set AZURE_AD_TENANT_ID --repo $GITHUB_REPO
echo "✓ Set AZURE_AD_TENANT_ID"
```

#### Option B: PowerShell (Recommended for Windows)

```powershell
# Step 5.1: Display and validate required variables
Write-Host "=== Step 5: Azure AD Configuration ==="
Write-Host "Required variables:"
Write-Host "  GITHUB_REPO: $GITHUB_REPO"
Write-Host ""

if ([string]::IsNullOrEmpty($GITHUB_REPO)) {
    Write-Host "❌ Error: GITHUB_REPO not set" -ForegroundColor Red
    Write-Host "Please run the 'Variables' section first"
    exit 1
}

# Step 5.2: Get Client ID
Write-Host "Retrieving Azure AD Client ID..."
$CLIENT_ID = az ad app list --display-name "AHKFlow-Dev" --query "[0].appId" -o tsv

if ([string]::IsNullOrEmpty($CLIENT_ID)) {
    Write-Host "❌ Error: Could not find AHKFlow-Dev app registration" -ForegroundColor Red
    Write-Host "Verify the app registration exists: az ad app list --query `"[].displayName`" -o tsv"
    exit 1
}

Write-Host "Client ID: $CLIENT_ID"

# Step 5.3: Set GitHub secret for Client ID
$CLIENT_ID | gh secret set AZURE_AD_CLIENT_ID --repo $GITHUB_REPO
Write-Host "✓ Set AZURE_AD_CLIENT_ID"

# Step 5.4: Get Tenant ID
Write-Host "Retrieving Tenant ID..."
$TENANT_ID = az account show --query "tenantId" -o tsv

if ([string]::IsNullOrEmpty($TENANT_ID)) {
    Write-Host "❌ Error: Could not retrieve Tenant ID" -ForegroundColor Red
    exit 1
}

Write-Host "Tenant ID: $TENANT_ID"

# Step 5.5: Set GitHub secret for Tenant ID
$TENANT_ID | gh secret set AZURE_AD_TENANT_ID --repo $GITHUB_REPO
Write-Host "✓ Set AZURE_AD_TENANT_ID"
```

---

## Verify Secrets

```bash
gh secret list --repo $GITHUB_REPO
# Expected output:
# AHKFLOW_AZURE_CREDENTIALS
# AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN
# AHKFLOW_SQL_MIGRATION_CONNECTION_STRING
# APP_INSIGHTS_CONNECTION_STRING
# AZURE_AD_CLIENT_ID
# AZURE_AD_TENANT_ID
```

---

## Manual Secret Entry

If automatic retrieval fails, set secrets manually:

### Service Principal

```bash
# Create service principal and copy output
az ad sp create-for-rbac \
  --name "ahkflow-github-actions-${ENVIRONMENT}" \
  --role contributor \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth

# Copy the JSON output and paste when prompted
gh secret set AHKFLOW_AZURE_CREDENTIALS --repo $GITHUB_REPO
```

### Static Web Apps Token

```bash
# Get token
az staticwebapp secrets list \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.apiKey" -o tsv

# Copy output and paste when prompted
gh secret set AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN --repo $GITHUB_REPO
```

### SQL Connection String

```bash
# Get from App Service or construct manually
# Format: Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<db>;User ID=<user>;Password=<pass>;Encrypt=True;

# Paste when prompted
gh secret set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING --repo $GITHUB_REPO
```

### Application Insights Connection String

```bash
# Get connection string
az monitor app-insights component show \
  --app ahkflow-insights-dev \
  --resource-group rg-ahkflow-dev \
  --query connectionString -o tsv

# Copy output and set secret
gh secret set APP_INSIGHTS_CONNECTION_STRING --repo $GITHUB_REPO
```

### Azure AD Configuration

```bash
# Get Client ID
az ad app list --display-name "AHKFlow-Dev" --query "[0].appId" -o tsv

# Copy output and set secret
gh secret set AZURE_AD_CLIENT_ID --repo $GITHUB_REPO
```

```bash
# Get Tenant ID
az account show --query "tenantId" -o tsv

# Copy output and set secret
gh secret set AZURE_AD_TENANT_ID --repo $GITHUB_REPO
```

---

## Update Secrets

To rotate or update secrets, re-run the retrieval commands. The `gh secret set` command overwrites existing values.

### Rotate Service Principal Credentials

#### Bash Version

```bash
# IMPORTANT: Export to prevent Git Bash path conversion on Windows
export MSYS_NO_PATHCONV=1

SP_NAME="ahkflow-github-actions-${ENVIRONMENT}"

# Reset credentials
SP_CREDENTIALS=$(az ad sp create-for-rbac \
  --name "$SP_NAME" \
  --role contributor \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth)

echo "$SP_CREDENTIALS" | gh secret set AHKFLOW_AZURE_CREDENTIALS --repo $GITHUB_REPO
echo "✓ Rotated AHKFLOW_AZURE_CREDENTIALS"
```

#### PowerShell Version

```powershell
$SP_NAME = "ahkflow-github-actions-$ENVIRONMENT"
$SUBSCRIPTION_ID = az account show --query id -o tsv

# Reset credentials
$SP_CREDENTIALS = az ad sp create-for-rbac `
    --name "$SP_NAME" `
    --role contributor `
    --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP" `
    --sdk-auth

$SP_CREDENTIALS | gh secret set AHKFLOW_AZURE_CREDENTIALS --repo $GITHUB_REPO
Write-Host "✓ Rotated AHKFLOW_AZURE_CREDENTIALS"
```

---

## Troubleshooting

### "gh: command not found"

Install GitHub CLI:
- **Windows:** `winget install --id GitHub.cli`
- **macOS:** `brew install gh`
- **Linux:** See [GitHub CLI installation](https://github.com/cli/cli#installation)

### "gh auth login required"

```bash
gh auth login
# Follow prompts to authenticate
```

### Service Principal Already Exists

If you get an error about the service principal already existing, retrieve the existing one:

```bash
SP_NAME="ahkflow-github-actions-${ENVIRONMENT}"
SP_APP_ID=$(az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv)

# Reset credentials
az ad sp credential reset --id $SP_APP_ID --create-cert
```

### Git Bash Path Conversion Error (Windows)

If you see an error like `MissingSubscription` or paths prefixed with `C:/Program Files/Git/`:

**Problem:** Git Bash on Windows converts Unix-style paths (e.g., `/subscriptions/...`) to Windows paths.

**Solution 1 (Recommended):** Use PowerShell instead (see PowerShell examples in each section above).

**Solution 2:** Export `MSYS_NO_PATHCONV=1` at the start of your script:
```bash
# Add this at the very beginning of your script
export MSYS_NO_PATHCONV=1

# Then run your commands normally
az ad sp create-for-rbac \
  --name "$SP_NAME" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

**Solution 3:** Use WSL (Windows Subsystem for Linux):
```bash
# Run in WSL terminal
az ad sp create-for-rbac \
  --name "$SP_NAME" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

**Solution 4:** Use double slash prefix:
```bash
az ad sp create-for-rbac \
  --name "$SP_NAME" \
  --role contributor \
  --scopes //subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

### Static Web App Not Found

Verify the Static Web App exists:

```bash
az staticwebapp list --resource-group $RESOURCE_GROUP --query "[].name" -o tsv
```

### SQL Connection String Not in App Service

Retrieve from Azure Portal or Key Vault, or construct manually:

```bash
Server=tcp:<server-name>.database.windows.net,1433;Initial Catalog=<db-name>;User ID=<admin-user>;Password=<password>;Encrypt=True;
```

---

## Manual Azure Configuration (One-Time Setup)

### Grant Key Vault Access to App Service Managed Identity

The CI/CD workflow requires the App Service Managed Identity to have access to Key Vault secrets. This must be done once manually.

#### PowerShell (Windows - Recommended)

```powershell
# Step 1: Get the Managed Identity Principal ID
$PRINCIPAL_ID = az webapp identity show `
  --name ahkflow-api-dev `
  --resource-group rg-ahkflow-dev `
  --query principalId `
  --output tsv

Write-Host "Principal ID: $PRINCIPAL_ID"

# Step 2: Get Subscription ID
$SUBSCRIPTION_ID = az account show --query id --output tsv

# Step 3: Grant Key Vault access
az role assignment create `
  --role "Key Vault Secrets User" `
  --assignee-object-id $PRINCIPAL_ID `
  --assignee-principal-type ServicePrincipal `
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-ahkflow-dev/providers/Microsoft.KeyVault/vaults/ahkflow-kv-dev"

Write-Host "✓ Granted Key Vault access to App Service Managed Identity"
```

#### Bash (Linux / macOS / WSL)

```bash
# Step 1: Get the Managed Identity Principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name ahkflow-api-dev \
  --resource-group rg-ahkflow-dev \
  --query principalId \
  --output tsv)

echo "Principal ID: $PRINCIPAL_ID"

# Step 2: Get Subscription ID
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

# Step 3: Grant Key Vault access
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee-object-id $PRINCIPAL_ID \
  --assignee-principal-type ServicePrincipal \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-ahkflow-dev/providers/Microsoft.KeyVault/vaults/ahkflow-kv-dev"

echo "✓ Granted Key Vault access to App Service Managed Identity"
```

#### Azure Portal Method (Alternative)

If CLI commands fail, use Azure Portal:

1. **Get Principal ID:**
   - Go to: https://portal.azure.com
   - Search for `ahkflow-api-dev`
   - Click **Identity** → **System assigned**
   - Copy the **Object (principal) ID**

2. **Grant Key Vault Access:**
   - Go to Key Vault: `ahkflow-kv-dev`
   - Click **Access control (IAM)**
   - Click **+ Add** → **Add role assignment**
   - Select role: **Key Vault Secrets User**
   - Click **Next** → **+ Select members**
   - Search for: `ahkflow-api-dev`
   - Select it → Click **Review + assign**

---

## Complete Setup (All Secrets)

Run all commands in sequence:

### Bash Version (Git Bash / WSL / Linux / macOS)

```bash
# IMPORTANT: Export to prevent Git Bash path conversion on Windows
export MSYS_NO_PATHCONV=1

# Set variables
ENVIRONMENT="dev"
RESOURCE_GROUP="rg-ahkflow-${ENVIRONMENT}"
SWA_NAME="ahkflow-swa-${ENVIRONMENT}"
APP_SERVICE_NAME="ahkflow-api-${ENVIRONMENT}"
GITHUB_REPO="s205109/AHKFlow"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# 1. Service Principal
SP_NAME="ahkflow-github-actions-${ENVIRONMENT}"

SP_CREDENTIALS=$(az ad sp create-for-rbac \
  --name "$SP_NAME" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth 2>/dev/null || \
  az ad sp create-for-rbac \
    --name "$SP_NAME" \
    --role contributor \
    --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
    --sdk-auth)
echo "$SP_CREDENTIALS" | gh secret set AHKFLOW_AZURE_CREDENTIALS --repo $GITHUB_REPO
echo "✓ AHKFLOW_AZURE_CREDENTIALS"

# 2. Static Web Apps Token
SWA_TOKEN=$(az staticwebapp secrets list \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.apiKey" -o tsv)
echo "$SWA_TOKEN" | gh secret set AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN --repo $GITHUB_REPO
echo "✓ AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN"

# 3. SQL Connection String
SQL_SERVER_NAME="ahkflow-sql-${ENVIRONMENT}"
SQL_DATABASE_NAME="ahkflow-db"
SQL_CONNECTION_STRING=$(az webapp config connection-string list \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "[?name=='DefaultConnection'].value" -o tsv)

if [ -z "$SQL_CONNECTION_STRING" ]; then
  echo "⚠ Connection string not found in App Service config"
  echo "Attempting to retrieve from Key Vault..."

  # Try to retrieve SQL password from Key Vault
  KEY_VAULT_NAME="ahkflow-kv-${ENVIRONMENT}"
  SQL_ADMIN_PASSWORD=$(az keyvault secret show \
    --vault-name $KEY_VAULT_NAME \
    --name sql-admin-password \
    --query value -o tsv 2>/dev/null)

  if [ -n "$SQL_ADMIN_PASSWORD" ]; then
    # Get SQL Server details
    SQL_SERVER_FQDN=$(az sql server show \
      --name $SQL_SERVER_NAME \
      --resource-group $RESOURCE_GROUP \
      --query fullyQualifiedDomainName -o tsv 2>/dev/null)

    SQL_ADMIN_USER=$(az sql server show \
      --name $SQL_SERVER_NAME \
      --resource-group $RESOURCE_GROUP \
      --query administratorLogin -o tsv 2>/dev/null)

    if [ -n "$SQL_SERVER_FQDN" ] && [ -n "$SQL_ADMIN_USER" ]; then
      # Construct connection string
      SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Initial Catalog=${SQL_DATABASE_NAME};User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

      echo "✓ Retrieved SQL password from Key Vault: $KEY_VAULT_NAME"
      echo "✓ Constructed connection string"
    else
      echo "❌ Error: SQL Server details not found"
      exit 1
    fi
  else
    echo "⚠ SQL password not found in Key Vault: $KEY_VAULT_NAME"
    echo "Skipping SQL connection string secret"
  fi
fi

if [ -n "$SQL_CONNECTION_STRING" ]; then
  echo "$SQL_CONNECTION_STRING" | gh secret set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING --repo $GITHUB_REPO
  echo "✓ AHKFLOW_SQL_MIGRATION_CONNECTION_STRING"
else
  echo "⚠ Skipped AHKFLOW_SQL_MIGRATION_CONNECTION_STRING (not available)"
fi

# 4. Application Insights Connection String
APP_INSIGHTS_NAME="ahkflow-insights-${ENVIRONMENT}"
APP_INSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv 2>/dev/null)

if [ -n "$APP_INSIGHTS_CONNECTION_STRING" ]; then
  echo "$APP_INSIGHTS_CONNECTION_STRING" | gh secret set APP_INSIGHTS_CONNECTION_STRING --repo $GITHUB_REPO
  echo "✓ APP_INSIGHTS_CONNECTION_STRING"
else
  echo "⚠ Skipped APP_INSIGHTS_CONNECTION_STRING (Application Insights not found)"
fi

# 5. Azure AD Configuration
CLIENT_ID=$(az ad app list --display-name "AHKFlow-Dev" --query "[0].appId" -o tsv)
echo "$CLIENT_ID" | gh secret set AZURE_AD_CLIENT_ID --repo $GITHUB_REPO
echo "✓ AZURE_AD_CLIENT_ID"

TENANT_ID=$(az account show --query "tenantId" -o tsv)
echo "$TENANT_ID" | gh secret set AZURE_AD_TENANT_ID --repo $GITHUB_REPO
echo "✓ AZURE_AD_TENANT_ID"

# Verify
echo ""
echo "Secrets set. Verify:"
gh secret list --repo $GITHUB_REPO
```

### PowerShell Version (Recommended for Windows)

```powershell
# Set variables
$ENVIRONMENT = "dev"
$RESOURCE_GROUP = "rg-ahkflow-$ENVIRONMENT"
$SWA_NAME = "ahkflow-swa-$ENVIRONMENT"
$APP_SERVICE_NAME = "ahkflow-api-$ENVIRONMENT"
$GITHUB_REPO = "s205109/AHKFlow"
$SUBSCRIPTION_ID = az account show --query id -o tsv

# 1. Service Principal
$SP_NAME = "ahkflow-github-actions-$ENVIRONMENT"

try {
    $SP_CREDENTIALS = az ad sp create-for-rbac `
        --name "$SP_NAME" `
        --role contributor `
        --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP" `
        --sdk-auth 2>$null
} catch {
    $SP_CREDENTIALS = az ad sp create-for-rbac `
        --name "$SP_NAME" `
        --role contributor `
        --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP" `
        --sdk-auth
}

$SP_CREDENTIALS | gh secret set AHKFLOW_AZURE_CREDENTIALS --repo $GITHUB_REPO
Write-Host "✓ AHKFLOW_AZURE_CREDENTIALS"

# 2. Static Web Apps Token
$SWA_TOKEN = az staticwebapp secrets list `
    --name $SWA_NAME `
    --resource-group $RESOURCE_GROUP `
    --query "properties.apiKey" -o tsv

$SWA_TOKEN | gh secret set AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN --repo $GITHUB_REPO
Write-Host "✓ AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN"

# 3. SQL Connection String
$SQL_SERVER_NAME = "ahkflow-sql-$ENVIRONMENT"
$SQL_DATABASE_NAME = "ahkflow-db"
$SQL_CONNECTION_STRING = az webapp config connection-string list `
    --name $APP_SERVICE_NAME `
    --resource-group $RESOURCE_GROUP `
    --query "[?name=='DefaultConnection'].value" -o tsv

if ([string]::IsNullOrEmpty($SQL_CONNECTION_STRING)) {
    Write-Host "⚠ Connection string not found in App Service config" -ForegroundColor Yellow
    Write-Host "Attempting to retrieve from Key Vault..."

    # Try to retrieve SQL password from Key Vault
    $KEY_VAULT_NAME = "ahkflow-kv-$ENVIRONMENT"
    $SQL_ADMIN_PASSWORD = az keyvault secret show `
        --vault-name $KEY_VAULT_NAME `
        --name sql-admin-password `
        --query value -o tsv 2>$null

    if (-not [string]::IsNullOrEmpty($SQL_ADMIN_PASSWORD)) {
        # Get SQL Server details
        $SQL_SERVER_FQDN = az sql server show `
            --name $SQL_SERVER_NAME `
            --resource-group $RESOURCE_GROUP `
            --query fullyQualifiedDomainName -o tsv 2>$null

        $SQL_ADMIN_USER = az sql server show `
            --name $SQL_SERVER_NAME `
            --resource-group $RESOURCE_GROUP `
            --query administratorLogin -o tsv 2>$null

        if (-not [string]::IsNullOrEmpty($SQL_SERVER_FQDN) -and -not [string]::IsNullOrEmpty($SQL_ADMIN_USER)) {
            # Construct connection string
            $SQL_CONNECTION_STRING = "Server=tcp:$SQL_SERVER_FQDN,1433;Initial Catalog=$SQL_DATABASE_NAME;User ID=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

            Write-Host "✓ Retrieved SQL password from Key Vault: $KEY_VAULT_NAME"
            Write-Host "✓ Constructed connection string"
        } else {
            Write-Host "❌ Error: SQL Server details not found" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "⚠ SQL password not found in Key Vault: $KEY_VAULT_NAME" -ForegroundColor Yellow
        Write-Host "Skipping SQL connection string secret"
    }
}

if (-not [string]::IsNullOrEmpty($SQL_CONNECTION_STRING)) {
    $SQL_CONNECTION_STRING | gh secret set AHKFLOW_SQL_MIGRATION_CONNECTION_STRING --repo $GITHUB_REPO
    Write-Host "✓ AHKFLOW_SQL_MIGRATION_CONNECTION_STRING"
} else {
    Write-Host "⚠ Skipped AHKFLOW_SQL_MIGRATION_CONNECTION_STRING (not available)" -ForegroundColor Yellow
}

# 4. Application Insights Connection String
$APP_INSIGHTS_NAME = "ahkflow-insights-$ENVIRONMENT"
$APP_INSIGHTS_CONNECTION_STRING = az monitor app-insights component show `
    --app $APP_INSIGHTS_NAME `
    --resource-group $RESOURCE_GROUP `
    --query connectionString -o tsv 2>$null

if (-not [string]::IsNullOrEmpty($APP_INSIGHTS_CONNECTION_STRING)) {
    $APP_INSIGHTS_CONNECTION_STRING | gh secret set APP_INSIGHTS_CONNECTION_STRING --repo $GITHUB_REPO
    Write-Host "✓ APP_INSIGHTS_CONNECTION_STRING"
} else {
    Write-Host "⚠ Skipped APP_INSIGHTS_CONNECTION_STRING (Application Insights not found)" -ForegroundColor Yellow
}

# 5. Azure AD Configuration
$CLIENT_ID = az ad app list --display-name "AHKFlow-Dev" --query "[0].appId" -o tsv
$CLIENT_ID | gh secret set AZURE_AD_CLIENT_ID --repo $GITHUB_REPO
Write-Host "✓ AZURE_AD_CLIENT_ID"

$TENANT_ID = az account show --query "tenantId" -o tsv
$TENANT_ID | gh secret set AZURE_AD_TENANT_ID --repo $GITHUB_REPO
Write-Host "✓ AZURE_AD_TENANT_ID"

# Verify
Write-Host ""
Write-Host "Secrets set. Verify:"
gh secret list --repo $GITHUB_REPO
```

---

## Related Documentation

- [CI/CD Pipeline](CICD_PIPELINE.md) - GitHub Actions workflows
- [Azure CLI Setup](AZURE_CLI_SETUP.md) - Azure resource provisioning
