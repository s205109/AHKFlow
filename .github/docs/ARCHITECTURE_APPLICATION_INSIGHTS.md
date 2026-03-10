# Application Insights Architecture

## Pattern: Backend-Only Telemetry

**Best Practice:** Use Application Insights **only for backend API**, not for Blazor WASM frontend.

```
┌─────────────────────────────────────────────┐
│   Application Insights: ahkflow-insights-dev │
└─────────────────────────────────────────────┘
                         ▲
                         │
                 ┌───────┴──────────┐
                 │ ASP.NET Core API │
                 │ (Serilog sink)   │
                 └──────────────────┘
```

## Why Backend-Only?

| Aspect | Frontend (Blazor WASM) | Backend (API) |
|--------|------------------------|---------------|
| **Log Visibility** | ✅ Browser DevTools (always visible) | ❌ Server logs (hidden from users) |
| **Debugging** | ✅ Browser Network tab + Sources | ❌ Requires remote logging |
| **Startup Impact** | ❌ Blocks WASM initialization | ✅ Async background telemetry |
| **Bundle Size** | ❌ Adds ~500KB to WASM bundle | ✅ Minimal server impact |
| **User Privacy** | ⚠️ Tracks client behavior | ✅ Server telemetry only |
| **Value** | ❌ Browser console already sufficient | ✅ Critical for production monitoring |

**Recommendation:** Keep frontend logging to browser console only.

## Implementation

### 1. Create Resource (via CLI)

See `docs/AZURE_CLI_SETUP.md` Section 6 (Application Insights)

### 2. Configure Backend Only

**Backend (`Program.cs`):**

```csharp
telemetryConfig.TelemetryInitializers.Add(new CloudRoleNameInitializer("AHKFlow-API"));
```

**Frontend:** Use browser console only (Serilog.Sinks.BrowserConsole)

### 3. Query Backend Telemetry

```kql
traces
| where cloud_RoleName == "AHKFlow-API"
| summarize count() by severityLevel
| order by severityLevel desc
```

## Configuration

**Connection String:** Safe to commit (write-only ingestion endpoint, no secrets)

**Backend Only:** `appsettings.Production.json`

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;..."
  }
}
```

**Frontend:** No Application Insights configuration needed. Use browser DevTools console.

## Deployment

**✅ Backend-only deployment** via GitHub Actions.

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

### Frontend Deployment

**No Application Insights configuration needed.** Frontend uses browser console only.

### Required GitHub Secret

Add to **Settings > Secrets and variables > Actions**:

```
APP_INSIGHTS_CONNECTION_STRING = InstrumentationKey=xxx;IngestionEndpoint=https://...
```

---

- **Standalone resource** (not linked to App Service lifecycle)
- Created via Azure CLI (see `docs/AZURE_CLI_SETUP.md` Section 6)
- Linked to backend API only
- Uses workspace-based Application Insights (modern approach)

## Cost

**Free tier:** 5 GB ingestion/month included

See: [Azure Monitor pricing](https://azure.microsoft.com/pricing/details/monitor/)

---

*Reusable pattern for Clean Architecture projects*
