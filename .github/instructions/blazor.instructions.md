---
description: 'Blazor WebAssembly PWA patterns for AHKFlow'
applyTo: '**/*.razor, **/*.razor.cs, **/*.razor.css'
---

## Project-Specific Conventions

- Blazor WebAssembly PWA application (not Blazor Server)
- Prefix private fields with underscore (`_fieldName`)
- Add inline comments only for non-obvious logic
- Prefer inline functions for smaller components but separate complex logic into code-behind or service classes

## MudBlazor UI Framework

- Use MudBlazor components for all UI elements (forms, dialogs, tables, buttons, inputs, etc.)
- Leverage MudBlazor's theming system for consistent styling
- Use MudBlazor's form validation integration with FluentValidation
- Provide user feedback using MudBlazor's `ISnackbar` for errors and confirmations

## HttpClient and API Integration

**Pattern:** Single typed HttpClient for all backend API operations.

### Structure

```
Services/
├── IProjectApiHttpClient.cs      # Interface for all API methods
└── ProjectApiHttpClient.cs       # Implementation with HttpClient
```

### Registration (Program.cs)

```csharp
var apiBaseUrl = builder.Configuration["ApiHttpClient:BaseAddress"] ?? "https://localhost:5000";

builder.Services.AddHttpClient<IProjectApiHttpClient, ProjectApiHttpClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### Configuration (appsettings.Development.json)

```json
{
  "ApiHttpClient": {
    "BaseAddress": "https://localhost:5000"
  }
}
```

### Usage in Components

```razor
@inject IProjectApiHttpClient ApiClient

@code {
    protected override async Task OnInitializedAsync()
    {
        var data = await ApiClient.GetDataAsync(_cts.Token);
    }
}
```

**Details:** See [frontend.instructions.md](frontend.instructions.md#httpclient-pattern)

## Validation

- Use FluentValidation for all form validation logic (DTOs in Application layer)
- Integrate FluentValidation with MudBlazor form components

## Authentication

- Use Microsoft Authentication Library (MSAL) with Azure AD for authentication
- Implement authentication in components using `AuthenticationStateProvider`
- Use `[Authorize]` attribute for protected pages/components

## Progressive Web App (PWA)

- Ensure service worker registration in index.html
- Use PWA manifest for app metadata
- Implement offline fallback pages and caching strategies as needed
