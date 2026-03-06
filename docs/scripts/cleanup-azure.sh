#!/bin/bash

# AHKFlow Cleanup Script
# Deletes Azure resources for specified environment (dev or prod)

cleanup_ahkflow() {
  # Set the environment to delete: dev or prod
  ENVIRONMENT="dev"

  # Safety check: ensure ENVIRONMENT is set
  if [ -z "$ENVIRONMENT" ]; then
    echo "Error: ENVIRONMENT not set. Edit line 6 above and set ENVIRONMENT to 'dev' or 'prod'"
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

# Run cleanup
cleanup_ahkflow
