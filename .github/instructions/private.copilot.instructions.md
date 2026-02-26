````instructions
---
description: Private Copilot defaults for AHKFlow Azure operations
applyTo:
  - "**/.github/workflows/*.yml"
  - "**/docs/**/*.md"
  - "**/scripts/**/*.ps1"
  - "**/scripts/**/*.sh"
---

# Private Copilot Instructions

Use these Azure defaults unless explicitly overridden:

- **Subscription Name**: `<YOUR_SUBSCRIPTION_NAME>`
- **Subscription ID**: `<YOUR_SUBSCRIPTION_ID>`
- **Tenant ID**: `<YOUR_TENANT_ID>`
- **Resource Group**: `<YOUR_RESOURCE_GROUP>`
- **Location**: `<YOUR_AZURE_REGION>`
- **Static Web App Name**: `<YOUR_STATIC_WEB_APP_NAME>`
- **Entra App Registration Name**: `<YOUR_ENTRA_APP_REGISTRATION_NAME>`
- **Application (Client) ID**: `<YOUR_APPLICATION_CLIENT_ID>`

Naming note:

- Use `ENTRA_APP_REGISTRATION_NAME` (clearer than `APP_REG_NAME`) for the Microsoft Entra app registration display name.

First commit guidance:

1. Commit this file with placeholders only.
2. After first commit, add this file to `.gitignore` if you want local-only real values.
3. Replace placeholders locally with your own values.
4. Keep secrets out of this file; use GitHub Secrets/Key Vault/environment variables.

Constraints:

- Do not prompt for subscription selection when running Azure commands.
- Do not create new resource groups unless explicitly requested.
- Never commit secrets; use GitHub Secrets/Key Vault/environment variables.

````
