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
- **Frontend**: `src/Frontend/AHKFlow.UI.Blazor/`
- **Tests**: `tests/AHKFlow.UI.Blazor.Tests/`

## Component Structure
- Pages in `Pages/` folder organized by feature area (Hotstrings, Hotkeys, Profiles, Download)
- Reusable components in `Components/` folder
- Use `@inject` for dependency injection
- MudBlazor components for UI (e.g., MudTable, MudDialog, MudButton)

## HttpClient Pattern

### Registration
- Register typed clients in `Program.cs` with `AddHttpClient<TInterface, TImplementation>()`

### API Client Pattern
- Define interface with async methods
- Implement with `HttpClient` injected via constructor
- Use `GetFromJsonAsync`, `PostAsJsonAsync`, `PutAsJsonAsync`, `DeleteAsync`
- Call `EnsureSuccessStatusCode()` after mutations
- Return empty collections instead of null for lists

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
- Use `AddMsalAuthentication` in `Program.cs` with Azure AD configuration
- Bind configuration from `appsettings.json` "AzureAd" section
- Add default access token scopes for API access
- Use `<AuthorizeView>` component to show/hide content based on authentication
- Inject `AuthenticationStateProvider` for programmatic access to user state

## Progressive Web App (PWA)
- Service worker configured in `wwwroot/service-worker.js`
- Manifest in `wwwroot/manifest.json`
- Icons in `wwwroot/icon-*` files
- Offline support via service worker caching

## DI Registration (Program.cs)
- Add MudBlazor: `builder.Services.AddMudServices()`
- Register typed HttpClients: `builder.Services.AddHttpClient<IClient, Client>(client => client.BaseAddress = ...)`
- Set base address to `builder.HostEnvironment.BaseAddress` for API calls
- Add MSAL authentication: `builder.Services.AddMsalAuthentication(...)` with Azure AD config
- Register one HttpClient per API area: Hotstrings, Hotkeys, Profiles, Scripts

````
