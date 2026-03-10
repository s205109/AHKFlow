# Azure CLI Setup Guide

Create Azure resources for AHKFlow: Static Web App, App Service, SQL Database, and Microsoft Entra ID app registration.

## Prerequisites

- Azure CLI installed and logged in (`az login`)
- An active Azure subscription

---

## Variables

Set these once at the start of your session:

```bash
# Environment: dev or prod
ENVIRONMENT="dev"

# Resource configuration
RESOURCE_GROUP="rg-ahkflow-${ENVIRONMENT}"
LOCATION="westeurope"
SWA_NAME="ahkflow-swa-${ENVIRONMENT}"
APP_SERVICE_NAME="ahkflow-api-${ENVIRONMENT}"
SQL_SERVER_NAME="ahkflow-sql-${ENVIRONMENT}"
SQL_DATABASE_NAME="ahkflow-db"
SQL_ADMIN_USER="ahkflowadmin"
KEY_VAULT_NAME="ahkflow-kv-${ENVIRONMENT}"
APP_REG_NAME="AHKFlow-${ENVIRONMENT^}"  # Capitalizes first letter (Dev or Prod)

# Echo values
echo "ENVIRONMENT: $ENVIRONMENT"
echo "RESOURCE_GROUP: $RESOURCE_GROUP"
echo "LOCATION: $LOCATION"
echo "SWA_NAME: $SWA_NAME"
echo "APP_SERVICE_NAME: $APP_SERVICE_NAME"
echo "SQL_SERVER_NAME: $SQL_SERVER_NAME"
echo "SQL_DATABASE_NAME: $SQL_DATABASE_NAME"
echo "KEY_VAULT_NAME: $KEY_VAULT_NAME"
echo "APP_REG_NAME: $APP_REG_NAME"
```

---

## Setup Commands

Run each step in order. All commands are idempotent.

**💡 Tip:** Each section validates required variables at the start and displays them before execution.

### 1. Resource Group

```bash
# Step 1.1: Display and validate required variables
echo "=== Section 1: Resource Group ==="
echo "Required variables:"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  LOCATION: ${LOCATION:-NOT SET}"
echo ""

if [ -z "$RESOURCE_GROUP" ] || [ -z "$LOCATION" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first to set all required variables"
else
  # Step 1.2: Create or verify resource group
  if az group show --name $RESOURCE_GROUP &>/dev/null; then
    echo "✓ Resource group exists"
  else
    az group create --name $RESOURCE_GROUP --location $LOCATION
    echo "✓ Created resource group"
  fi
fi
```

### 2. Static Web App

```bash
# Step 2.1: Display and validate required variables
echo "=== Section 2: Static Web App ==="
echo "Required variables:"
echo "  SWA_NAME: ${SWA_NAME:-NOT SET}"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  LOCATION: ${LOCATION:-NOT SET}"
echo ""

if [ -z "$SWA_NAME" ] || [ -z "$RESOURCE_GROUP" ] || [ -z "$LOCATION" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first to set all required variables"
else
  # Step 2.2: Create or verify Static Web App
  if az staticwebapp show --name $SWA_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    echo "✓ Static Web App exists"
  else
    az staticwebapp create --name $SWA_NAME --resource-group $RESOURCE_GROUP --location $LOCATION --sku Free
    echo "✓ Created Static Web App"
  fi

  # Step 2.3: Get hostname
  SWA_HOSTNAME=$(az staticwebapp show --name $SWA_NAME --resource-group $RESOURCE_GROUP --query "defaultHostname" -o tsv)
  echo "SWA URL: https://$SWA_HOSTNAME"
fi
```

### 3. App Registration

```bash
# Step 3.1: Display and validate required variables
echo "=== Section 3: App Registration ==="
echo "Required variables:"
echo "  APP_REG_NAME: ${APP_REG_NAME:-NOT SET}"
echo ""

if [ -z "$APP_REG_NAME" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first to set all required variables"
else
  # Step 3.2: Get SWA_HOSTNAME if not already set
  if [ -z "$SWA_HOSTNAME" ]; then
    SWA_HOSTNAME=$(az staticwebapp show --name $SWA_NAME --resource-group $RESOURCE_GROUP --query "defaultHostname" -o tsv 2>/dev/null)
  fi

  # Step 3.3: Check if app registration exists
  CLIENT_ID=$(az ad app list --display-name "$APP_REG_NAME" --query "[0].appId" -o tsv)

  if [ -z "$CLIENT_ID" ]; then
    # Step 3.4: Create basic app registration first (no redirect URIs here)
    az ad app create \
      --display-name "$APP_REG_NAME" \
      --sign-in-audience AzureADMyOrg \
      --enable-id-token-issuance true \
      --enable-access-token-issuance true

    CLIENT_ID=$(az ad app list --display-name "$APP_REG_NAME" --query "[0].appId" -o tsv)
    APP_OBJECT_ID=$(az ad app show --id $CLIENT_ID --query id -o tsv)

    # Step 3.5: Update to SPA and set redirect URIs (use az rest for compatibility across CLI versions)
    az rest --method PATCH \
      --uri "https://graph.microsoft.com/v1.0/applications/$APP_OBJECT_ID" \
      --headers "Content-Type=application/json" \
      --body "{\"spa\":{\"redirectUris\":[\"https://$SWA_HOSTNAME/authentication/login-callback\",\"https://localhost:7228/authentication/login-callback\",\"https://localhost:5001/authentication/login-callback\"]},\"web\":{\"redirectUris\":[]}}"

    echo "✓ Created app registration (SPA)"
  else
    echo "✓ App registration exists"
  fi

  # Step 3.6: Get tenant ID
  TENANT_ID=$(az account show --query "tenantId" -o tsv)
  echo "CLIENT_ID: $CLIENT_ID"
  echo "TENANT_ID: $TENANT_ID"
fi
```

### 4. API Scope

```bash
# Step 4.1: Display and validate required variables
echo "=== Section 4: API Scope ==="

# Get CLIENT_ID if not already set
if [ -z "$CLIENT_ID" ]; then
  echo "Required variables:"
  echo "  APP_REG_NAME: ${APP_REG_NAME:-NOT SET}"
  echo ""

  if [ -z "$APP_REG_NAME" ]; then
    echo "❌ Error: Required variables not set"
    echo "Please run the 'Variables' section first to set all required variables"
  else
    CLIENT_ID=$(az ad app list --display-name "$APP_REG_NAME" --query "[0].appId" -o tsv)
    if [ -z "$CLIENT_ID" ]; then
      echo "❌ Error: App registration not found. Run Section 3 first."
    fi
  fi
fi

if [ -n "$CLIENT_ID" ]; then
  echo "Using CLIENT_ID: $CLIENT_ID"
  echo ""

  # Step 4.2: Check if API scope exists
  SCOPE_ID=$(az ad app show --id $CLIENT_ID --query "api.oauth2PermissionScopes[0].id" -o tsv)

  if [ -z "$SCOPE_ID" ]; then
    # Step 4.3: Generate new scope ID
    SCOPE_ID=$(uuidgen 2>/dev/null || powershell -Command "[guid]::NewGuid().ToString()")

    # Step 4.4: Create API scope
    az rest --method PATCH \
      --uri "https://graph.microsoft.com/v1.0/applications/$(az ad app show --id $CLIENT_ID --query id -o tsv)" \
      --headers "Content-Type=application/json" \
      --body "{
        \"identifierUris\": [\"api://$CLIENT_ID\"],
        \"api\": {
          \"oauth2PermissionScopes\": [{
            \"id\": \"$SCOPE_ID\",
            \"adminConsentDescription\": \"Access AHKFlow API on behalf of user\",
            \"adminConsentDisplayName\": \"Access AHKFlow API\",
            \"isEnabled\": true,
            \"type\": \"User\",
            \"userConsentDescription\": \"Access AHKFlow API on your behalf\",
            \"userConsentDisplayName\": \"Access AHKFlow API\",
            \"value\": \"access_as_user\"
          }]
        }
      }"
    echo "✓ Created API scope"
  else
    echo "✓ API scope exists"
  fi
fi
```

### 5. Azure Key Vault

```bash
# Step 5.1: Display and validate required variables
echo "=== Section 5: Azure Key Vault ==="
echo "Required variables:"
echo "  KEY_VAULT_NAME: ${KEY_VAULT_NAME:-NOT SET}"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  LOCATION: ${LOCATION:-NOT SET}"
echo ""

if [ -z "$KEY_VAULT_NAME" ] || [ -z "$RESOURCE_GROUP" ] || [ -z "$LOCATION" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first to set all required variables"
  echo "Or set them manually:"
  echo "  ENVIRONMENT=\"dev\""
  echo "  KEY_VAULT_NAME=\"ahkflow-kv-\${ENVIRONMENT}\""
  echo "  RESOURCE_GROUP=\"rg-ahkflow-\${ENVIRONMENT}\""
  echo "  LOCATION=\"westeurope\""
else
  # Step 5.2: Check Microsoft.KeyVault provider registration
  PROVIDER_STATE=$(az provider show --namespace Microsoft.KeyVault --query registrationState -o tsv 2>/dev/null)

  if [ "$PROVIDER_STATE" != "Registered" ]; then
    echo "Registering Microsoft.KeyVault resource provider..."
    az provider register --namespace Microsoft.KeyVault >/dev/null

    echo "Waiting for Microsoft.KeyVault registration to complete..."
    for _ in {1..12}; do
      PROVIDER_STATE=$(az provider show --namespace Microsoft.KeyVault --query registrationState -o tsv 2>/dev/null)
      if [ "$PROVIDER_STATE" = "Registered" ]; then
        echo "✓ Microsoft.KeyVault provider registered"
        break
      fi

      sleep 10
    done

    if [ "$PROVIDER_STATE" != "Registered" ]; then
      echo "❌ Microsoft.KeyVault provider registration did not complete in time"
      echo "Run this command and wait until it returns 'Registered':"
      echo "  az provider show --namespace Microsoft.KeyVault --query registrationState -o tsv"
    fi
  fi

  # Step 5.3: Check if Key Vault already exists
  if az keyvault show --name $KEY_VAULT_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    echo "✓ Key Vault exists"

    # Step 5.3a: Verify you have Key Vault permissions
    echo "Verifying Key Vault permissions..."
    USER_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null)
    KEY_VAULT_SCOPE=$(az keyvault show --name $KEY_VAULT_NAME --resource-group $RESOURCE_GROUP --query id -o tsv 2>/dev/null)

    KEY_VAULT_ROLE=$(az role assignment list \
      --assignee "$USER_OBJECT_ID" \
      --scope "$KEY_VAULT_SCOPE" \
      --query "[?roleDefinitionName=='Key Vault Secrets Officer' || roleDefinitionName=='Key Vault Administrator'].roleDefinitionName | [0]" \
      -o tsv 2>/dev/null)

    if [ -z "$KEY_VAULT_ROLE" ]; then
      echo "⚠️  No Key Vault role found. You may not be able to set secrets."
      echo "Assign one of these roles:"
      echo "  - Key Vault Secrets Officer"
      echo "  - Key Vault Administrator"
      echo ""
      echo "To assign via Azure Portal:"
      echo "  1. Go to: https://portal.azure.com/#@/resource$KEY_VAULT_SCOPE"
      echo "  2. Click 'Access control (IAM)'"
      echo "  3. Click 'Add role assignment'"
      echo "  4. Select 'Key Vault Secrets Officer'"
      echo "  5. Assign to yourself"
      echo ""
      echo "Or run this command:"
      echo "  USER_ID=\$(az ad signed-in-user show --query id -o tsv)"
      echo "  $USER_ID"
      #echo "  az role assignment create --role \"Key Vault Secrets Officer\" --assignee \"\$USER_ID\" --scope \"$KEY_VAULT_SCOPE\""
      echo "  az role assignment create --role \"Key Vault Secrets Officer\" --assignee \"\$USER_ID\" --scope \"${KEY_VAULT_SCOPE#\\}\""
    else
      echo "✓ Found Key Vault role: $KEY_VAULT_ROLE"
    fi
  else
    # Step 5.4: Create Key Vault with RBAC authorization
    echo "Creating Key Vault..."
    if az keyvault create \
      --name $KEY_VAULT_NAME \
      --resource-group $RESOURCE_GROUP \
      --location $LOCATION \
      --sku standard \
      --enable-rbac-authorization true; then
      echo "✓ Created Key Vault (RBAC-enabled)"

      # Step 5.5: Get current user and Key Vault scope
      echo ""
      echo "=== Step 5.5: Assigning Key Vault Permissions ==="
      USER_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null)
      KEY_VAULT_SCOPE=$(az keyvault show --name $KEY_VAULT_NAME --resource-group $RESOURCE_GROUP --query id -o tsv 2>/dev/null)

      if [ -z "$USER_OBJECT_ID" ] || [ -z "$KEY_VAULT_SCOPE" ]; then
        echo "❌ Failed to get user or Key Vault scope information"
        echo "Please run: az account show"
        echo "If that fails, re-authenticate: az logout && az login"
      else
        echo "User Object ID: $USER_OBJECT_ID"
        echo "Key Vault Scope: $KEY_VAULT_SCOPE"
        echo ""

        # Step 5.6: Attempt to assign Key Vault Secrets Officer role
        echo "Attempting to assign 'Key Vault Secrets Officer' role..."
        if az role assignment create \
          --role "Key Vault Secrets Officer" \
          --assignee "$USER_OBJECT_ID" \
          --scope "$KEY_VAULT_SCOPE" \
          2>/dev/null; then
          echo "✓ Successfully assigned role"
        else
          echo "⚠️  Failed to assign role automatically"
          echo ""
          echo "This usually means you need 'Owner' or 'User Access Administrator' permissions."
          echo ""
          echo "📋 Manual Steps:"
          echo ""
          echo "Option 1 - Azure Portal:"
          echo "  1. Open: https://portal.azure.com/#@/resource$KEY_VAULT_SCOPE"
          echo "  2. Click 'Access control (IAM)' in the left menu"
          echo "  3. Click '+ Add' → 'Add role assignment'"
          echo "  4. Select role: 'Key Vault Secrets Officer'"
          echo "  5. Click 'Next'"
          echo "  6. Click '+ Select members'"
          echo "  7. Search for your email and select yourself"
          echo "  8. Click 'Review + assign'"
          echo ""
          echo "Option 2 - Azure CLI (if you have permissions):"
          echo "  USER_ID=\$(az ad signed-in-user show --query id -o tsv)"
          echo "  az role assignment create --role \"Key Vault Secrets Officer\" --assignee \"\$USER_ID\" --scope \"${KEY_VAULT_SCOPE#/}\""
          echo ""
          echo -n "After assigning the role, press Enter to continue..."
          read
        fi

        # Step 5.7: Verify the role assignment worked
        echo ""
        echo "Verifying role assignment..."
        for i in {1..6}; do
          KEY_VAULT_ROLE=$(az role assignment list \
            --assignee "$USER_OBJECT_ID" \
            --scope "$KEY_VAULT_SCOPE" \
            --query "[?roleDefinitionName=='Key Vault Secrets Officer' || roleDefinitionName=='Key Vault Administrator'].roleDefinitionName | [0]" \
            -o tsv 2>/dev/null)

          if [ -n "$KEY_VAULT_ROLE" ]; then
            echo "✓ Verified role: $KEY_VAULT_ROLE"
            break
          fi

          if [ $i -lt 6 ]; then
            echo "Waiting for role to propagate (attempt $i/6)..."
            sleep 10
          else
            echo "⚠️  Role not detected after 60 seconds"
            echo "The role may still be propagating. Continue anyway? (y/n)"
            read CONTINUE
            if [ "$CONTINUE" != "y" ] && [ "$CONTINUE" != "Y" ]; then
              echo "Please assign the role manually and run this section again"
              exit 1
            fi
          fi
        done
      fi

      # Step 5.8: Wait for Key Vault DNS to propagate
      echo ""
      echo "Waiting for Key Vault DNS to propagate (20 seconds)..."
      sleep 20
      echo "✓ Key Vault is ready"
    else
      echo "❌ Failed to create Key Vault"
      echo ""
      echo "Common fixes:"
      echo "1. Check Microsoft.KeyVault provider registration state:"
      echo "   az provider show --namespace Microsoft.KeyVault --query registrationState -o tsv"
      echo ""
      echo "2. Re-authenticate if MFA is required:"
      echo "   az logout && az login"
      echo ""
      echo "3. Check if the Key Vault name is available (must be globally unique):"
      echo "   az keyvault list --query \"[].name\" -o tsv"
    fi
  fi
fi
```

### 6. Application Insights (Optional)

**Recommended:** Single Application Insights resource for both frontend and backend. See `.github/docs/ARCHITECTURE_APPLICATION_INSIGHTS.md` for details.

**Why Log Analytics Workspace?** Modern Application Insights (workspace-based) requires a Log Analytics workspace for better retention (90 days vs 30 days classic), unified query experience, and better cost management. Classic mode is being deprecated.

```bash
# Step 6.1: Display and validate required variables
echo "=== Section 6: Application Insights (Optional) ==="
echo "Required variables:"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  LOCATION: ${LOCATION:-NOT SET}"
echo "  ENVIRONMENT: ${ENVIRONMENT:-NOT SET}"
echo ""

if [ -z "$RESOURCE_GROUP" ] || [ -z "$LOCATION" ] || [ -z "$ENVIRONMENT" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first to set all required variables"
else
  APP_INSIGHTS_NAME="ahkflow-insights-${ENVIRONMENT}"
  LOG_ANALYTICS_WORKSPACE_NAME="ahkflow-logs-${ENVIRONMENT}"

  echo "App Insights Name: $APP_INSIGHTS_NAME"
  echo "Log Analytics Workspace: $LOG_ANALYTICS_WORKSPACE_NAME"
  echo ""

  # Step 6.2: Create Log Analytics Workspace (required for workspace-based App Insights)
  if az monitor log-analytics workspace show --resource-group $RESOURCE_GROUP --workspace-name $LOG_ANALYTICS_WORKSPACE_NAME &>/dev/null; then
    echo "✓ Log Analytics Workspace exists"
  else
    echo "Creating Log Analytics Workspace..."
    az monitor log-analytics workspace create \
      --resource-group $RESOURCE_GROUP \
      --workspace-name $LOG_ANALYTICS_WORKSPACE_NAME \
      --location $LOCATION \
      >/dev/null

    echo "✓ Created Log Analytics Workspace"
  fi

  # Step 6.3: Get workspace resource ID
  WORKSPACE_ID=$(az monitor log-analytics workspace show \
    --resource-group $RESOURCE_GROUP \
    --workspace-name $LOG_ANALYTICS_WORKSPACE_NAME \
    --query id -o tsv)

  # Step 6.4: Create Application Insights (workspace-based)
  if az monitor app-insights component show --app $APP_INSIGHTS_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    echo "✓ Application Insights exists"
  else
    echo "Creating Application Insights..."

    # Fix for Git Bash path translation issue: Prevent /subscriptions/... from being converted to C:/Program Files/Git/subscriptions/...
    MSYS_NO_PATHCONV=1 az monitor app-insights component create \
      --app $APP_INSIGHTS_NAME \
      --location $LOCATION \
      --resource-group $RESOURCE_GROUP \
      --application-type web \
      --workspace "$WORKSPACE_ID" \
      2>&1 | tee /tmp/appinsights-create.log

    # Check if creation was successful
    if az monitor app-insights component show --app $APP_INSIGHTS_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
      echo "✓ Created Application Insights (workspace-based)"
    else
      echo "❌ Failed to create Application Insights"
      echo ""
      echo "Common fixes:"
      echo "1. If using Git Bash on Windows, the script already uses MSYS_NO_PATHCONV=1"
      echo "2. If still failing, try running in PowerShell or CMD instead of Git Bash"
      echo "3. Check if workspace-based App Insights is available in your region:"
      echo "   az feature show --namespace microsoft.insights --name AIWorkspacePreview"
      echo ""
      exit 1
    fi
  fi

  # Step 6.5: Get connection string
  APP_INSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show \
    --app $APP_INSIGHTS_NAME \
    --resource-group $RESOURCE_GROUP \
    --query connectionString -o tsv)

  echo ""
  echo "✅ Application Insights Connection String:"
  echo "$APP_INSIGHTS_CONNECTION_STRING"
  echo ""
  echo "📋 Next Steps:"
  echo "1. Update frontend: src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.json"
  echo "2. Update backend: src/Backend/AHKFlow.API/appsettings.Production.json"
  echo ""
  echo "Add this configuration:"
  echo '  "ApplicationInsights": {'
  echo '    "ConnectionString": "<paste-connection-string-above>"'
  echo '  }'
  echo ""
  echo "See .github/docs/ARCHITECTURE_APPLICATION_INSIGHTS.md for implementation details"
fi
```

### 7. Azure SQL Server

**⚠️ Important:** Run Section 5 (Key Vault) first before running this section.

```bash
# Step 7.1: Display and validate required variables
echo "=== Section 7: Azure SQL Server ==="
echo "Required variables:"
echo "  SQL_SERVER_NAME: ${SQL_SERVER_NAME:-NOT SET}"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  LOCATION: ${LOCATION:-NOT SET}"
echo "  SQL_ADMIN_USER: ${SQL_ADMIN_USER:-NOT SET}"
echo "  KEY_VAULT_NAME: ${KEY_VAULT_NAME:-NOT SET}"
echo ""

if [ -z "$SQL_SERVER_NAME" ] || [ -z "$RESOURCE_GROUP" ] || [ -z "$LOCATION" ] || [ -z "$SQL_ADMIN_USER" ] || [ -z "$KEY_VAULT_NAME" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first to set all required variables"
else
  # Step 7.2: Check if SQL Server exists
  if az sql server show --name $SQL_SERVER_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    echo "✓ SQL Server exists"

    # Retrieve password from Key Vault if it exists
    SQL_ADMIN_PASSWORD=$(az keyvault secret show --vault-name $KEY_VAULT_NAME --name sql-admin-password --query value -o tsv 2>/dev/null)

    if [ -z "$SQL_ADMIN_PASSWORD" ]; then
      echo "⚠️  SQL Server exists but password not found in Key Vault."
      echo "If you need to configure connection strings, add the password manually to Key Vault:"
      echo "  az keyvault secret set --vault-name $KEY_VAULT_NAME --name sql-admin-password --value '<your-password>'"
    else
      echo "✓ Retrieved SQL password from Key Vault"
    fi
  else
    # Step 7.3: Generate secure password
    SQL_ADMIN_PASSWORD=$(openssl rand -base64 32 2>/dev/null || powershell -Command "Add-Type -AssemblyName System.Web; [System.Web.Security.Membership]::GeneratePassword(32,10)")

    # Step 7.4: Create SQL Server
    echo "Creating SQL Server..."
    if az sql server create \
      --name $SQL_SERVER_NAME \
      --resource-group $RESOURCE_GROUP \
      --location $LOCATION \
      --admin-user $SQL_ADMIN_USER \
      --admin-password "$SQL_ADMIN_PASSWORD"; then

      echo "✓ Created SQL Server"

      # Step 7.5: Store password in Key Vault
      echo "Storing password in Key Vault..."
      if az keyvault secret set \
        --vault-name $KEY_VAULT_NAME \
        --name sql-admin-password \
        --value "$SQL_ADMIN_PASSWORD" \
        >/dev/null; then
        echo "✓ Stored password in Key Vault: $KEY_VAULT_NAME"
      else
        echo "⚠️  Failed to store password in Key Vault (DNS may not be ready yet)"
        echo "Wait 1-2 minutes and retry storing the password:"
        echo "  az keyvault secret set --vault-name $KEY_VAULT_NAME --name sql-admin-password --value \"$SQL_ADMIN_PASSWORD\""
      fi

      echo ""
      echo "SQL Admin User: $SQL_ADMIN_USER"
      echo "SQL Admin Password: $SQL_ADMIN_PASSWORD"
      echo ""
      echo "⚠️  Password displayed above. You can retrieve it later with:"
      echo "  az keyvault secret show --vault-name $KEY_VAULT_NAME --name sql-admin-password --query value -o tsv"
      echo ""
    else
      echo "❌ Failed to create SQL Server"
      echo ""
      echo "Common fixes:"
      echo "1. Re-authenticate if MFA is required:"
      echo "   az logout && az login"
      echo ""
      echo "2. Check if the SQL Server name is available (must be globally unique):"
      echo "   az sql server list --query \"[].name\" -o tsv"
    fi
  fi

  # Step 7.6: Configure firewall rules (only if SQL Server exists)
  if az sql server show --name $SQL_SERVER_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    az sql server firewall-rule create \
      --resource-group $RESOURCE_GROUP \
      --server $SQL_SERVER_NAME \
      --name AllowAzureServices \
      --start-ip-address 0.0.0.0 \
      --end-ip-address 0.0.0.0 &>/dev/null && echo "✓ Created AllowAzureServices firewall rule" || echo "✓ Firewall rule exists"

    # Allow your current IP (for local development)
    MY_IP=$(curl -s https://api.ipify.org)
    az sql server firewall-rule create \
      --resource-group $RESOURCE_GROUP \
      --server $SQL_SERVER_NAME \
      --name AllowMyIP \
      --start-ip-address $MY_IP \
      --end-ip-address $MY_IP &>/dev/null && echo "✓ Created AllowMyIP firewall rule ($MY_IP)" || echo "✓ Your IP firewall rule exists"

    SQL_SERVER_FQDN=$(az sql server show --name $SQL_SERVER_NAME --resource-group $RESOURCE_GROUP --query "fullyQualifiedDomainName" -o tsv)
    echo "SQL Server FQDN: $SQL_SERVER_FQDN"
  else
    echo "⚠️  SQL Server not found. Skipping firewall rules."
  fi
fi
```

### 8. Azure SQL Database

```bash
# Step 8.1: Display and validate required variables
echo "=== Section 8: Azure SQL Database ==="
echo "Required variables:"
echo "  SQL_DATABASE_NAME: ${SQL_DATABASE_NAME:-NOT SET}"
echo "  SQL_SERVER_NAME: ${SQL_SERVER_NAME:-NOT SET}"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo ""

if [ -z "$SQL_DATABASE_NAME" ] || [ -z "$SQL_SERVER_NAME" ] || [ -z "$RESOURCE_GROUP" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first to set all required variables"
else
  # Step 8.2: Create or verify SQL Database
  if az sql db show --name $SQL_DATABASE_NAME --server $SQL_SERVER_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    echo "✓ SQL Database exists"
  else
    # Use Azure SQL Database FREE tier (32 GB, 1 per subscription)
    az sql db create \
      --resource-group $RESOURCE_GROUP \
      --server $SQL_SERVER_NAME \
      --name $SQL_DATABASE_NAME \
      --edition GeneralPurpose \
      --compute-model Serverless \
      --family Gen5 \
      --capacity 1 \
      --use-free-limit \
      --free-limit-exhaustion-behavior AutoPause \
      --backup-storage-redundancy Local

    echo "✓ Created SQL Database (FREE tier - 32 GB)"
    echo "⚠️  Note: Free tier limited to 1 database per subscription"
  fi
fi
```

### 9. Azure App Service Plan

```bash
# Step 9.1: Display and validate required variables
echo "=== Section 9: Azure App Service Plan ==="
echo "Required variables:"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  LOCATION: ${LOCATION:-NOT SET}"
echo "  ENVIRONMENT: ${ENVIRONMENT:-NOT SET}"
echo ""

if [ -z "$RESOURCE_GROUP" ] || [ -z "$LOCATION" ] || [ -z "$ENVIRONMENT" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first to set all required variables"
else
  APP_SERVICE_PLAN_NAME="ahkflow-plan-${ENVIRONMENT}"
  echo "App Service Plan Name: $APP_SERVICE_PLAN_NAME"
  echo ""

  # Step 9.2: Create or verify App Service Plan
  if az appservice plan show --name $APP_SERVICE_PLAN_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    echo "✓ App Service Plan exists"
  else
    az appservice plan create \
      --name $APP_SERVICE_PLAN_NAME \
      --resource-group $RESOURCE_GROUP \
      --location $LOCATION \
      --sku F1

    echo "✓ Created App Service Plan"
  fi
fi
```

### 10. Azure App Service (API Backend)

```bash
# Step 10.1: Display and validate required variables
echo "=== Section 10: Azure App Service (API Backend) ==="
echo "Required variables:"
echo "  APP_SERVICE_NAME: ${APP_SERVICE_NAME:-NOT SET}"
echo "  RESOURCE_GROUP: ${RESOURCE_GROUP:-NOT SET}"
echo "  ENVIRONMENT: ${ENVIRONMENT:-NOT SET}"
echo ""

if [ -z "$APP_SERVICE_NAME" ] || [ -z "$RESOURCE_GROUP" ] || [ -z "$ENVIRONMENT" ]; then
  echo "❌ Error: Required variables not set"
  echo "Please run the 'Variables' section first to set all required variables"
else
  # Ensure plan name is set (in case running this section independently)
  APP_SERVICE_PLAN_NAME="ahkflow-plan-${ENVIRONMENT}"

  # Step 10.2: Create or verify App Service
  if az webapp show --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    echo "✓ App Service exists"
  else
    # Verify the App Service Plan exists first
    if ! az appservice plan show --name $APP_SERVICE_PLAN_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
      echo "❌ Error: App Service Plan '$APP_SERVICE_PLAN_NAME' not found."
      echo "⚠️  Please run Section 9 first to create the App Service Plan."
      echo ""
      echo "Skipping App Service creation..."
    else
      # F1 (Free tier) uses Windows, so use 'dotnet:10' runtime format
      az webapp create \
        --name $APP_SERVICE_NAME \
        --resource-group $RESOURCE_GROUP \
        --plan $APP_SERVICE_PLAN_NAME \
        --runtime "dotnet:10"

      echo "✓ Created App Service"

      # Wait for App Service to be ready
      echo "Waiting for App Service to be ready..."
      sleep 10
    fi
  fi

  # Step 10.2a: Enable Managed Identity (if not already enabled)
  if az webapp show --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    echo ""
    echo "=== Step 10.2a: Configuring Managed Identity ==="

    PRINCIPAL_ID=$(az webapp identity show \
      --name $APP_SERVICE_NAME \
      --resource-group $RESOURCE_GROUP \
      --query principalId -o tsv 2>/dev/null)

    if [ -z "$PRINCIPAL_ID" ] || [ "$PRINCIPAL_ID" = "None" ]; then
      echo "Enabling Managed Identity..."
      az webapp identity assign \
        --name $APP_SERVICE_NAME \
        --resource-group $RESOURCE_GROUP \
        >/dev/null

      PRINCIPAL_ID=$(az webapp identity show \
        --name $APP_SERVICE_NAME \
        --resource-group $RESOURCE_GROUP \
        --query principalId -o tsv)

      echo "✓ Managed Identity enabled: $PRINCIPAL_ID"

      # Wait for Managed Identity to propagate to Azure AD
      echo "Waiting for Managed Identity to propagate to Azure AD (30 seconds)..."
      sleep 30
    else
      echo "✓ Managed Identity already enabled: $PRINCIPAL_ID"
    fi

    # Step 10.2b: Grant Key Vault access to Managed Identity
    echo ""
    echo "=== Step 10.2b: Granting Key Vault Access ==="
    KEY_VAULT_NAME="ahkflow-kv-${ENVIRONMENT}"

    KV_SCOPE=$(az keyvault show \
      --name $KEY_VAULT_NAME \
      --resource-group $RESOURCE_GROUP \
      --query id -o tsv 2>/dev/null)

    if [ -n "$KV_SCOPE" ]; then
      # Check if role assignment already exists
      EXISTING_ROLE=$(az role assignment list \
        --assignee "$PRINCIPAL_ID" \
        --scope "$KV_SCOPE" \
        --query "[?roleDefinitionName=='Key Vault Secrets User'].roleDefinitionName | [0]" \
        -o tsv 2>/dev/null)

      if [ -z "$EXISTING_ROLE" ]; then
        echo "Assigning 'Key Vault Secrets User' role to Managed Identity..."

        # Use --assignee-object-id with --assignee-principal-type for newly created identities
        if az role assignment create \
          --role "Key Vault Secrets User" \
          --assignee-object-id "$PRINCIPAL_ID" \
          --assignee-principal-type ServicePrincipal \
          --scope "$KV_SCOPE" \
          2>/dev/null; then
          echo "✓ Granted Key Vault access to App Service"
        else
          echo "⚠️  Failed to grant Key Vault access automatically"
          echo "This may be due to insufficient permissions or propagation delay."
          echo ""
          echo "To grant access manually, wait 1-2 minutes and run:"
          echo "  az role assignment create --role \"Key Vault Secrets User\" --assignee-object-id \"$PRINCIPAL_ID\" --assignee-principal-type ServicePrincipal --scope \"${KV_SCOPE#/}\""
        fi
      else
        echo "✓ Key Vault access already granted"
      fi
    else
      echo "⚠️  Key Vault not found. Skipping Key Vault access setup."
      echo "Run Section 5 first to create the Key Vault, then manually grant access:"
      echo "  az role assignment create --role \"Key Vault Secrets User\" --assignee-object-id \"$PRINCIPAL_ID\" --assignee-principal-type ServicePrincipal --scope \"\$(az keyvault show --name ahkflow-kv-${ENVIRONMENT} --resource-group $RESOURCE_GROUP --query id -o tsv)\""
    fi
  fi

  # Step 10.3: Configure connection string (only if App Service exists)
  if az webapp show --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
    # Retrieve password from Key Vault if not already in session
    if [ -z "$SQL_ADMIN_PASSWORD" ]; then
      KEY_VAULT_NAME="ahkflow-kv-${ENVIRONMENT}"
      SQL_ADMIN_PASSWORD=$(az keyvault secret show --vault-name $KEY_VAULT_NAME --name sql-admin-password --query value -o tsv 2>/dev/null)
    fi

    # Configure connection string
    if [ -z "$SQL_ADMIN_PASSWORD" ]; then
      echo "⚠️  SQL password not found in session or Key Vault."
      echo "To configure connection string manually:"
      echo "  1. Retrieve password: az keyvault secret show --vault-name ahkflow-kv-${ENVIRONMENT} --name sql-admin-password --query value -o tsv"
      echo "  2. Configure in Azure Portal: App Service → Configuration → Connection strings"
    else
      # Get SQL Server FQDN if not already set
      if [ -z "$SQL_SERVER_FQDN" ]; then
        SQL_SERVER_NAME="ahkflow-sql-${ENVIRONMENT}"
        SQL_SERVER_FQDN=$(az sql server show --name $SQL_SERVER_NAME --resource-group $RESOURCE_GROUP --query "fullyQualifiedDomainName" -o tsv 2>/dev/null)
      fi

      # Get SQL admin user if not already set
      if [ -z "$SQL_ADMIN_USER" ]; then
        SQL_ADMIN_USER="ahkflowadmin"
      fi

      # Get SQL database name if not already set
      if [ -z "$SQL_DATABASE_NAME" ]; then
        SQL_DATABASE_NAME="ahkflow-db"
      fi

      SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Initial Catalog=${SQL_DATABASE_NAME};User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

      az webapp config connection-string set \
        --name $APP_SERVICE_NAME \
        --resource-group $RESOURCE_GROUP \
        --connection-string-type SQLAzure \
        --settings DefaultConnection="$SQL_CONNECTION_STRING"

      echo "✓ Configured connection string (password from Key Vault)"
    fi

    APP_SERVICE_URL=$(az webapp show --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP --query "defaultHostName" -o tsv)
    echo "API URL: https://$APP_SERVICE_URL"
  else
    echo "⚠️  App Service not found. Skipping connection string configuration."
  fi
fi
```

---

## Retrieve Values

If you already ran setup and need the values again:

```bash
# Set the environment you want to retrieve: dev or prod
ENVIRONMENT="dev"

RESOURCE_GROUP="rg-ahkflow-${ENVIRONMENT}"
SWA_NAME="ahkflow-swa-${ENVIRONMENT}"
APP_SERVICE_NAME="ahkflow-api-${ENVIRONMENT}"
SQL_SERVER_NAME="ahkflow-sql-${ENVIRONMENT}"
APP_REG_NAME="AHKFlow-${ENVIRONMENT^}"

# App Registration
CLIENT_ID=$(az ad app list --display-name "$APP_REG_NAME" --query "[0].appId" -o tsv)
TENANT_ID=$(az account show --query "tenantId" -o tsv)

# Static Web App
SWA_HOSTNAME=$(az staticwebapp show --name $SWA_NAME --resource-group $RESOURCE_GROUP --query "defaultHostname" -o tsv)

# App Service
APP_SERVICE_URL=$(az webapp show --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP --query "defaultHostName" -o tsv)

# SQL Server
SQL_SERVER_FQDN=$(az sql server show --name $SQL_SERVER_NAME --resource-group $RESOURCE_GROUP --query "fullyQualifiedDomainName" -o tsv)

# Application Insights (optional)
APP_INSIGHTS_NAME="ahkflow-insights-${ENVIRONMENT}"
APP_INSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show --app $APP_INSIGHTS_NAME --resource-group $RESOURCE_GROUP --query "connectionString" -o tsv 2>/dev/null)

echo "CLIENT_ID: $CLIENT_ID"
echo "TENANT_ID: $TENANT_ID"
echo "SWA URL: https://$SWA_HOSTNAME"
echo "API URL: https://$APP_SERVICE_URL"
echo "SQL Server: $SQL_SERVER_FQDN"

if [ -n "$APP_INSIGHTS_CONNECTION_STRING" ]; then
  echo "App Insights Connection String: $APP_INSIGHTS_CONNECTION_STRING"
fi
```

---

## Configure Local Development

Copy the example files and add your values:

```bash
cp src/Backend/AHKFlow.API/appsettings.Development.json.example \
   src/Backend/AHKFlow.API/appsettings.Development.json

cp src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.Development.json.example \
   src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.Development.json
```

Update each file with `CLIENT_ID` and `TENANT_ID`. These files are gitignored.

---

## Cleanup

**⚠️ Note:** The cleanup script has been improved to handle missing App Registrations gracefully. If the App Registration is not found (already deleted), the script will ask for confirmation before continuing to delete other resources.

**Recommended:** Use the dedicated cleanup script located at `docs/scripts/cleanup-azure.sh`:

```bash
# 1. Edit docs/scripts/cleanup-azure.sh and set ENVIRONMENT on line 6
# 2. Run the script
bash docs/scripts/cleanup-azure.sh
```

**Alternative:** Copy and paste the manual commands below.

First, edit line 3 below to set ENVIRONMENT="dev" or ENVIRONMENT="prod", then copy and paste this entire block into your terminal:

```bash
cleanup_ahkflow() {
  # Set the environment to delete: dev or prod
  ENVIRONMENT="dev"

  # Safety check: ensure ENVIRONMENT is set
  if [ -z "$ENVIRONMENT" ]; then
    echo "Error: ENVIRONMENT not set. Edit the function above and set ENVIRONMENT to 'dev' or 'prod'"
    return 1
  fi

  # Safety check: confirm if deleting production resources
  if [ "$ENVIRONMENT" = "prod" ]; then
    echo "WARNING: Deleting PRODUCTION resources"
    echo -n "Continue? (yes/no): "
    read CONFIRM
    if [ "$CONFIRM" != "yes" ]; then
      echo "Cancelled"
      return 0
    fi
  fi

  # Derived names
  RESOURCE_GROUP="rg-ahkflow-${ENVIRONMENT}"
  SWA_NAME="ahkflow-swa-${ENVIRONMENT}"
  APP_SERVICE_NAME="ahkflow-api-${ENVIRONMENT}"
  APP_SERVICE_PLAN_NAME="ahkflow-plan-${ENVIRONMENT}"
  SQL_SERVER_NAME="ahkflow-sql-${ENVIRONMENT}"
  SQL_DATABASE_NAME="ahkflow-db"
  KEY_VAULT_NAME="ahkflow-kv-${ENVIRONMENT}"
  if [ "$ENVIRONMENT" = "dev" ]; then
    APP_REG_NAME="AHKFlow-Dev"
  elif [ "$ENVIRONMENT" = "prod" ]; then
    APP_REG_NAME="AHKFlow-Prod"
  else
    echo "Error: ENVIRONMENT must be 'dev' or 'prod'"
    return 1
  fi

  # Get CLIENT_ID
  CLIENT_ID=$(az ad app list --display-name "$APP_REG_NAME" --query "[0].appId" -o tsv)

  if [ -z "$CLIENT_ID" ]; then
    echo "⚠ App registration not found: $APP_REG_NAME"
    echo "It may have been deleted already."
    echo -n "Continue with cleanup of other resources? (y/n): "
    read CONTINUE
    if [ "$CONTINUE" != "y" ] && [ "$CONTINUE" != "Y" ]; then
      echo "Cancelled"
      return 0
    fi
  fi

  echo "Deleting resources for: $ENVIRONMENT"
  echo "  Resource Group: $RESOURCE_GROUP"
  echo "  Static Web App: $SWA_NAME"
  echo "  App Service: $APP_SERVICE_NAME"
  echo "  SQL Server: $SQL_SERVER_NAME"
  if [ -n "$CLIENT_ID" ]; then
    echo "  App Registration: $APP_REG_NAME"
  fi
  echo ""

  # Delete resources
  az staticwebapp delete --name $SWA_NAME --resource-group $RESOURCE_GROUP --yes 2>/dev/null || echo "Static Web App not found"
  az webapp delete --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP 2>/dev/null || echo "App Service not found"
  az sql server delete --name $SQL_SERVER_NAME --resource-group $RESOURCE_GROUP --yes 2>/dev/null || echo "SQL Server not found"
  
  # Only delete app registration if it was found
  if [ -n "$CLIENT_ID" ]; then
    az ad app delete --id $CLIENT_ID && echo "Deleted app registration"
  else
    echo "Skipped app registration (not found)"
  fi
  
  az group delete --name $RESOURCE_GROUP --yes && echo "Deleted resource group"

  echo "Cleanup complete"
}

# Now run it
cleanup_ahkflow
```

---

## Summary

This setup creates all Azure resources needed for AHKFlow:

1. **Resource Group** - Container for all resources (Free)
2. **Static Web App** - Hosts Blazor WASM frontend (Free tier)
3. **App Registration** - Microsoft Entra ID authentication (Free)
4. **API Scope** - OAuth2 permission for API access (Free)
5. **Azure Key Vault** - Stores secrets securely (Free tier - 10,000 operations/month)
6. **Application Insights** - Unified monitoring for frontend + backend (**Optional**, Free tier - 5 GB/month)
7. **Azure SQL Server** - Database server with firewall rules (Free)
8. **Azure SQL Database** - ahkflow-db database (**Free tier - 32 GB**)
9. **App Service Plan** - Hosting plan for API (**F1 Free tier**)
10. **App Service** - Hosts .NET 10 API backend (Free tier)

### Cost Breakdown

**Total Monthly Cost: €0** (all free tiers!)

| Resource | Tier | Cost |
|----------|------|------|
| Static Web App | Free | €0 |
| App Service Plan | F1 | €0 |
| App Service | F1 | €0 |
| SQL Server | - | €0 |
| SQL Database | Free (32 GB) | €0 |
| App Registration | - | €0 |
| Application Insights | Free (5 GB) | €0 |

### Free Tier Limitations

**App Service F1:**
- 60 min CPU time/day
- Sleeps after 20 min idle (no Always On)
- 1 GB RAM, 1 GB storage

**SQL Database Free:**
- 32 GB storage
- 100,000 vCore seconds/month
- Auto-pauses when idle
- 1 free database per subscription

**Application Insights Free:**
- 5 GB data ingestion/month
- 90 days retention
- Additional ingestion: ~€2/GB

### Key Concepts

- **API Scope** (Section 4) = Authentication permission (OAuth2) for frontend to call API
- **App Service** (Section 10) = Actual hosting infrastructure where your .NET API code runs
- **Application Insights** (Section 6) = Optional unified monitoring for frontend + backend (see `.github/docs/ARCHITECTURE_APPLICATION_INSIGHTS.md`)

Both API Scope and App Service are needed for a complete working deployment!
