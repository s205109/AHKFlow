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

- Register typed `HttpClient` instances via `AddHttpClient` in Program.cs to consume the AHKFlow Web API
- Set base address to API endpoint configuration

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
