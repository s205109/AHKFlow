# Typed HttpClient Pattern for Blazor WASM

## Pattern Summary

**Single typed HttpClient** for all backend API operations in Blazor WebAssembly apps.

## Structure

```
Services/
├── IProjectApiHttpClient.cs      # Interface for all API methods
└── ProjectApiHttpClient.cs       # Implementation with HttpClient
```

## Naming Convention

- Interface: `I{ProjectName}ApiHttpClient`
- Implementation: `{ProjectName}ApiHttpClient`
- Config key: `"ApiHttpClient"` (consistent)

**Example for AHKFlow:**
- `IAhkFlowApiHttpClient`
- `AhkFlowApiHttpClient`

## Implementation Template

### Interface

```csharp
public interface IProjectApiHttpClient
{
    Task<TResponse?> GetAsync(string endpoint, CancellationToken cancellationToken);
    Task<TResponse?> PostAsync<TRequest>(string endpoint, TRequest data, CancellationToken cancellationToken);
}
```

### Implementation

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;

public class ProjectApiHttpClient : IProjectApiHttpClient
{
    private readonly HttpClient _httpClient;

    public ProjectApiHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
    }

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<TResponse>(endpoint, cancellationToken);
    }
}
```

### Registration (Program.cs)

```csharp
var apiBaseUrl = builder.Configuration["ApiHttpClient:BaseAddress"] ?? "https://localhost:5000";

builder.Services.AddHttpClient<IProjectApiHttpClient, ProjectApiHttpClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler();
```

**Required Package:** `Microsoft.Extensions.Http.Resilience`

Provides automatic:
- Retry (exponential backoff)
- Circuit breaker
- Timeout handling
- Rate limiting

### Configuration (appsettings.Development.json)

```json
{
  "ApiHttpClient": {
    "BaseAddress": "https://localhost:5000"
  }
}
```

## Benefits

- ✅ Single source for all API calls
- ✅ Testable (mock interface)
- ✅ Type-safe compile-time checks
- ✅ Configuration-driven base URL
- ✅ Easy to extend (add methods)
- ✅ Consistent naming across projects
- ✅ Built-in resilience (retry, circuit breaker, timeout)

## Usage in Components

```razor
@inject IProjectApiHttpClient ApiClient

@code {
    protected override async Task OnInitializedAsync()
    {
        var data = await ApiClient.GetDataAsync(_cts.Token);
    }
}
```

## Key Principles

- One interface for all operations
- Naming: `I{ProjectName}ApiHttpClient` pattern
- Config key: `"ApiHttpClient"` (reusable)
- Headers: `Accept: application/json`, `Cache-Control: no-cache`
- `CancellationToken` on all methods
- Return `[]` not null for collections
- Let exceptions bubble to components

---

*Reusable pattern template for Blazor WASM + ASP.NET Core API projects*
