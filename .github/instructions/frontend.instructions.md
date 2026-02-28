````instructions
---
description: Frontend patterns for AHKFlow.UI.Blazor
applyTo:
  - "**/src/Frontend/**/*.razor"
  - "**/src/Frontend/**/*.cs"
  - "**/tests/AHKFlow.UI.Blazor.Tests/**/*.cs"
---

# Frontend Instructions

## Project Location
- **Frontend**: src/Frontend/AHKFlow.UI.Blazor/
- **Tests**: tests/AHKFlow.UI.Blazor.Tests/

## Component Structure
- Pages in Pages/ folder organized by feature area (Hotstrings, Hotkeys, Profiles, Download)
- Reusable components in Components/ folder
- Use `@inject` for dependency injection
- MudBlazor components for UI (e.g., MudTable, MudDialog, MudButton)

## HttpClient Pattern

**Single typed HttpClient for all backend API operations.**

### Implementation

**1. Interface** (`Services/IProjectApiHttpClient.cs`):

```csharp
public interface IProjectApiHttpClient
{
    Task<DataDto?> GetDataAsync(CancellationToken cancellationToken);
    // Add methods as features develop
}
```

**2. Implementation** (`Services/ProjectApiHttpClient.cs`):

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

    public async Task<DataDto?> GetDataAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<DataDto>("api/v1/data", cancellationToken);
    }
}
```

**3. Registration** (`Program.cs`):

```csharp
var apiBaseUrl = builder.Configuration["ApiHttpClient:BaseAddress"] ?? "https://localhost:5000";

builder.Services.AddHttpClient<IProjectApiHttpClient, ProjectApiHttpClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler();
```

**4. Configuration** (`appsettings.Development.json`):

```json
{
  "ApiHttpClient": {
    "BaseAddress": "https://localhost:5000"
  }
}
```

### Adding Endpoints

Add method to interface → implement → use in components. No registration changes needed.

### Principles

- One interface for all API operations
- Naming: `I{ProjectName}ApiHttpClient`, `{ProjectName}ApiHttpClient`
- Config key: `"ApiHttpClient"` (consistent across projects)
- Headers: Accept JSON, no-cache
- CancellationToken on all methods
- Return `[]` not null for collections
- Exceptions bubble to components

## Component Patterns

### Page Components
- Use `@page` directive for routing
- Inject API clients, `IDialogService`, `ISnackbar` for notifications
- Use `OnInitializedAsync` for data loading
- Set `<PageTitle>` for browser tab title
- Wrap content in `<MudContainer MaxWidth="MaxWidth.Large">`
- Use `MudTable` for list views with `Items`, `Hover`, `Loading` properties
- Handle CRUD operations via dialogs (`IDialogService.ShowAsync<T>`)
- Refresh data after dialog closes successfully
- Use `_loading` field to show/hide loading indicators

### Dialog Components
- Accept `[CascadingParameter] MudDialogInstance MudDialog`
- Use `[Parameter]` for data passed from parent
- Wrap form in `<MudDialog>` with `<DialogContent>` and `<DialogActions>`
- Use `MudForm` with `Model` and `Validation` properties
- Bind fields with `@bind-Value` and `For` lambda for validation
- Use `Variant.Outlined` for consistent styling
- Call `MudDialog.Close(DialogResult.Ok(...))` on success, `MudDialog.Cancel()` on cancel
- Validate form before submission (`await _form.Validate(); if (_form.IsValid) ...`)

## State Management
- Use private fields with `_` prefix
- Load data in `OnInitializedAsync()`
- Use `StateHasChanged()` when needed after async operations
- For complex state, consider component parameters and EventCallbacks

## MudBlazor Patterns

### Common Components
- `MudTable` for data tables
- `MudDialog` for modals
- `MudForm` with validation
- `MudSnackbar` for notifications
- `MudSelect`, `MudTextField`, `MudButton` for forms

### Validation
```csharp
// Use FluentValidation validators from Application layer
private CreateHotstringDtoValidator _validator = new();

// In MudForm
<MudForm Model="@_model" Validation="@_validator.ValidateValue">
```

## Testing

### Component Tests (bUnit)
- Inherit from `TestContext`
- Mock API clients using NSubstitute
- Register mocked dependencies via `Services.AddSingleton(...)`
- Add MudBlazor services with `Services.AddMudServices()`
- Render component with `RenderComponent<T>()`
- Wait for async operations with `cut.WaitForState(...)`
- Assert markup with `cut.Find(...)` and `cut.Markup.Should().Contain(...)`

### API Client Tests
- Use `MockHttpMessageHandler` to mock HTTP responses
- Configure mock with `mockHandler.When("pattern").Respond(...)`
- Create `HttpClient` with mock handler and base address
- Test return values, error handling, and correct API calls

## Authentication
- Use `AddMsalAuthentication` in Program.cs with Azure AD configuration
- Bind configuration from appsettings.json "AzureAd" section
- Add default access token scopes for API access
- Use `<AuthorizeView>` component to show/hide content based on authentication
- Inject `AuthenticationStateProvider` for programmatic access to user state

## Progressive Web App (PWA)
- Service worker configured in wwwroot/service-worker.js
- Manifest in wwwroot/manifest.json
- Icons in `wwwroot/icon-*` files
- Offline support via service worker caching

## DI Registration (Program.cs)
- Add MudBlazor: `builder.Services.AddMudServices()`
- Register typed HttpClient with resilience: `builder.Services.AddHttpClient<IProjectApiHttpClient, ProjectApiHttpClient>(...).AddStandardResilienceHandler()`
- Add MSAL authentication: `builder.Services.AddMsalAuthentication(...)` with Azure AD config

````
