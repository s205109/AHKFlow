# Application Insights Architecture

## Pattern: Single Resource for Frontend + Backend

**Best Practice:** Use **one Application Insights resource** for the entire AHKFlow application (both Blazor WASM frontend and ASP.NET Core backend).

```
┌─────────────────────────────────────────────┐
│   Application Insights: ahkflow-insights-dev │
└─────────────────────────────────────────────┘
           ▲                    ▲
           │                    │
   ┌───────┴────────┐   ┌──────┴──────────┐
   │ Blazor WASM    │   │ ASP.NET Core API│
   │ (Serilog sink) │───│ (Serilog sink)  │
   └────────────────┘   └─────────────────┘
```

## Why Single Resource?

| Benefit | Description |
|---------|-------------|
| **End-to-end tracing** | Correlate frontend errors → API calls → database queries in one view |
| **Application Map** | Unified topology: `Browser → AHKFlow-Frontend → AHKFlow-API → SQL` |
| **Cost efficiency** | Single resource, unified billing |
| **Simplified config** | One connection string shared across services |
| **Better debugging** | See frontend errors and backend failures together |

## Implementation

### 1. Create Resource (via CLI)

See `docs/AZURE_CLI_SETUP.md` Section 6 (Application Insights)

### 2. Differentiate Services with Cloud Role Name

**Frontend (`Program.cs`):**

```csharp
telemetryConfig.TelemetryInitializers.Add(new CloudRoleNameInitializer("AHKFlow-Frontend"));
```

**Backend (`Program.cs`):**

```csharp
telemetryConfig.TelemetryInitializers.Add(new CloudRoleNameInitializer("AHKFlow-API"));
```

### 3. Query Both Services

```kql
traces
| where cloud_RoleName in ("AHKFlow-Frontend", "AHKFlow-API")
| summarize count() by cloud_RoleName, severityLevel
| order by severityLevel desc
```

## Configuration

**Connection String:** Safe to commit (write-only ingestion endpoint, no secrets)

**Frontend:** `wwwroot/appsettings.json`

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;..."
  }
}
```

**Backend:** `appsettings.Production.json`

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;..."
  }
}
```

## Deployment

**✅ Recommended:** Set Application Insights connection string via deployment workflow.

### Backend (API) Deployment

Update `.github/workflows/ahkflow-deploy-api.yml`:

```yaml
- name: Configure App Service Settings
  run: |
    az webapp config appsettings set \
      --name ${{ env.APP_SERVICE_NAME }} \
      --resource-group ${{ env.RESOURCE_GROUP }} \
      --settings \
        ApplicationInsights__ConnectionString="${{ secrets.APP_INSIGHTS_CONNECTION_STRING }}"
```

### Frontend (Static Web Apps) Deployment

**For Blazor WASM:** Connection string must be in `appsettings.json` (client-side, public).

**Steps:**
1. Run CLI script (Section 6) to get connection string
2. Update `src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.json` directly
3. Commit to repository (safe - write-only telemetry endpoint)

**Alternative:** Use build-time substitution in workflow:

```yaml
- name: Update App Insights Connection String
  run: |
    $json = Get-Content src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.json | ConvertFrom-Json
    $json.ApplicationInsights.ConnectionString = "${{ secrets.APP_INSIGHTS_CONNECTION_STRING }}"
    $json | ConvertTo-Json -Depth 10 | Set-Content src/Frontend/AHKFlow.UI.Blazor/wwwroot/appsettings.json
  shell: pwsh
```

### Required GitHub Secret

Add to **Settings > Secrets and variables > Actions**:

```
APP_INSIGHTS_CONNECTION_STRING = InstrumentationKey=xxx;IngestionEndpoint=https://...
```

---

- **Standalone resource** (not linked to App Service lifecycle)
- Created via Azure CLI (see `docs/AZURE_CLI_SETUP.md` Section 6)
- Linked to both frontend and backend via connection string
- Uses workspace-based Application Insights (modern approach)

## Cost

**Free tier:** 5 GB ingestion/month included

See: [Azure Monitor pricing](https://azure.microsoft.com/pricing/details/monitor/)

---

*Reusable pattern for Clean Architecture projects*
