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

Template usage:

- This `.example` file is a template only; do not put real values here.
- Do not use this file directly for local development.
- For local development, copy this file to `private.copilot.instructions.md` in the same folder.
- Ensure `private.copilot.instructions.md` is listed in `.gitignore` and never committed.

Naming note:

- Use `ENTRA_APP_REGISTRATION_NAME` (clearer than `APP_REG_NAME`) for the Microsoft Entra app registration display name.

First commit guidance:

1. Commit only this `.example` file with placeholders.
2. Do not commit `private.copilot.instructions.md`; it is for local development only.
3. After cloning, developers copy this file to `private.copilot.instructions.md` and replace placeholders locally.
4. Keep secrets out of tracked files; use GitHub Secrets/Key Vault/environment variables.

Constraints:

- Do not prompt for subscription selection when running Azure commands.
- Do not create new resource groups unless explicitly requested.
- Never commit secrets; use GitHub Secrets/Key Vault/environment variables.

````
