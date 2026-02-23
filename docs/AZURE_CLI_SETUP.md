# Azure CLI Setup Guide

Create an Azure Static Web App and Microsoft Entra ID app registration for AHKFlow.

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
APP_REG_NAME="AHKFlow-${ENVIRONMENT^}"  # Capitalizes first letter (Dev or Prod)

# Echo values
echo "ENVIRONMENT: $ENVIRONMENT"
echo "RESOURCE_GROUP: $RESOURCE_GROUP"
echo "LOCATION: $LOCATION"
echo "SWA_NAME: $SWA_NAME"
echo "APP_REG_NAME: $APP_REG_NAME"
```

---

## Setup Commands

Run each step in order. All commands are idempotent.

### 1. Resource Group

```bash
if az group show --name $RESOURCE_GROUP &>/dev/null; then
  echo "✓ Resource group exists"
else
  az group create --name $RESOURCE_GROUP --location $LOCATION
  echo "✓ Created resource group"
fi
```

### 2. Static Web App

```bash
if az staticwebapp show --name $SWA_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
  echo "✓ Static Web App exists"
else
  az staticwebapp create --name $SWA_NAME --resource-group $RESOURCE_GROUP --location $LOCATION --sku Free
  echo "✓ Created Static Web App"
fi

SWA_HOSTNAME=$(az staticwebapp show --name $SWA_NAME --resource-group $RESOURCE_GROUP --query "defaultHostname" -o tsv)
echo "SWA URL: https://$SWA_HOSTNAME"
```

### 3. App Registration

```bash
CLIENT_ID=$(az ad app list --display-name "$APP_REG_NAME" --query "[0].appId" -o tsv)

if [ -z "$CLIENT_ID" ]; then
  # Create basic app registration first (no redirect URIs here)
  az ad app create \
    --display-name "$APP_REG_NAME" \
    --sign-in-audience AzureADMyOrg \
    --enable-id-token-issuance true \
    --enable-access-token-issuance true

  CLIENT_ID=$(az ad app list --display-name "$APP_REG_NAME" --query "[0].appId" -o tsv)
  APP_OBJECT_ID=$(az ad app show --id $CLIENT_ID --query id -o tsv)

  # Update to SPA and set redirect URIs (use az rest for compatibility across CLI versions)
  az rest --method PATCH \
    --uri "https://graph.microsoft.com/v1.0/applications/$APP_OBJECT_ID" \
    --headers "Content-Type=application/json" \
    --body "{\"spa\":{\"redirectUris\":[\"https://$SWA_HOSTNAME/authentication/login-callback\",\"https://localhost:7228/authentication/login-callback\",\"https://localhost:5001/authentication/login-callback\"]},\"web\":{\"redirectUris\":[]}}"

  echo "✓ Created app registration (SPA)"
else
  echo "✓ App registration exists"
fi

TENANT_ID=$(az account show --query "tenantId" -o tsv)
echo "CLIENT_ID: $CLIENT_ID"
echo "TENANT_ID: $TENANT_ID"
```

### 4. API Scope

```bash
SCOPE_ID=$(az ad app show --id $CLIENT_ID --query "api.oauth2PermissionScopes[0].id" -o tsv)

if [ -z "$SCOPE_ID" ]; then
  SCOPE_ID=$(uuidgen 2>/dev/null || powershell -Command "[guid]::NewGuid().ToString()")

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
```

---

## Retrieve Values

If you already ran setup and need the values again:

```bash
# Set the environment you want to retrieve: dev or prod
ENVIRONMENT= ""

APP_REG_NAME="AHKFlow-${ENVIRONMENT^}"
CLIENT_ID=$(az ad app list --display-name "$APP_REG_NAME" --query "[0].appId" -o tsv)
TENANT_ID=$(az account show --query "tenantId" -o tsv)

echo "CLIENT_ID: $CLIENT_ID"
echo "TENANT_ID: $TENANT_ID"
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

First, edit line 3 below to set ENVIRONMENT="dev" or ENVIRONMENT="prod", then copy and paste this entire block into your terminal:

```bash
cleanup_ahkflow() {
  # Set the environment to delete: dev or prod
  ENVIRONMENT=""

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
    echo "Error: App registration not found: $APP_REG_NAME"
    return 1
  fi

  echo "Deleting resources for: $ENVIRONMENT"
  echo "  Resource Group: $RESOURCE_GROUP"
  echo "  Static Web App: $SWA_NAME"
  echo "  App Registration: $APP_REG_NAME"
  echo ""

  # Delete resources
  az staticwebapp delete --name $SWA_NAME --resource-group $RESOURCE_GROUP --yes 2>/dev/null || echo "Static Web App not found"
  az ad app delete --id $CLIENT_ID && echo "Deleted app registration"
  az group delete --name $RESOURCE_GROUP --yes && echo "Deleted resource group"

  echo "Cleanup complete"
}

# Now run it
cleanup_ahkflow
```
