---
description: Shared GitHub Copilot instructions for AHKFlow - AutoHotkey script management application
---

# AHKFlow - Repository Instructions

This is **AHKFlow**, a .NET application for managing AutoHotkey hotstrings and hotkeys on Windows.

## Project Context

**Tech Stack:** Blazor WebAssembly PWA + ASP.NET Core Web API (.NET 10), SQL Server (LocalDB/Docker Compose) + Azure SQL, MudBlazor, Docker
**Architecture:** Clean Architecture (4 layers: API, Application, Domain, Infrastructure)
**Focus:** TDD with xUnit, FluentValidation, NSubstitute, Mapster, Polly
**Goal:** Centrally manage AutoHotkey definitions and generate `.ahk` scripts per profile

For more details, see:

- [AHKFlow – Product Vision & Architecture Overview.md](AHKFlow – Product Vision & Architecture Overview.md)
- [Solution Structure.md](Solution Structure.md)

### Validation Steps Before PR

1. Ensure all tests pass: `dotnet test`
2. Ensure build succeeds: `dotnet build`
3. Verify no compiler warnings
4. Check GitHub Actions will pass (workflows in .github/workflows/)

## Solution Structure

``` plaintext
Solution 'AHKFlow'
├── 📁 build
│   └── _build.csproj                             # NUKE build project
├── 📁 docs
│   └── (documentation files)
├── 📁 Solution Items
│   ├── 📁 .github
│   │   ├── 📁 agents
│   │   ├── 📁 instructions
│   │   ├── 📁 prompts
│   │   ├── AHKFlow – Product Vision & Architecture Overview.md
│   │   ├── copilot-instructions.md
│   │   └── Solution Structure.md
│   ├── .dockerignore
│   ├── .editorconfig
│   ├── .gitignore
│   └── README.md
├── 📁 src
│   ├── 📁 Frontend
│   │   └── AHKFlow.UI.Blazor                     # Blazor WebAssembly PWA
│   │
│   ├── 📁 Backend
│   │   ├── AHKFlow.API                           # ASP.NET Core Web API
│   │   ├── AHKFlow.Application                   # Application layer (DTOs, Commands, Queries)
│   │   ├── AHKFlow.Domain                        # Core business logic and entities
│   │   └── AHKFlow.Infrastructure                # EF Core, SQL Server, External services
│   │
│   └── 📁 Tools
│       └── AHKFlow.CLI                           # Command-line interface
└── 📁 tests
    ├── AHKFlow.API.Tests                         # API integration tests
    ├── AHKFlow.Application.Tests                 # Application layer unit tests
    ├── AHKFlow.Domain.Tests                      # Domain unit tests
    ├── AHKFlow.Infrastructure.Tests              # Infrastructure integration tests
    └── AHKFlow.UI.Blazor.Tests                   # Blazor component tests
```

## General Coding Rules

Follow these rules for all code generation:

- Use Allman brace style (opening braces on new lines) - enforced by `.editorconfig`
- Always use `async`/`await` for asynchronous operations with `CancellationToken` support
- Prefer null-coalescing operators (`??`) over verbose null checks
- Add inline comments only for non-obvious logic
- Write tests FIRST before implementation (TDD approach)
- Use FluentAssertions for all test assertions
- Mock external dependencies using NSubstitute
- Support both unit tests (isolated) and integration tests (with test database)
- Use Serilog for structured logging throughout
- Use FluentValidation for all validation logic
- Use Mapster for DTO-to-entity mapping
- Use `.AddStandardResilienceHandler()` for all HttpClient registrations (Frontend, Backend, CLI)
- Use `EnableRetryOnFailure()` for EF Core DbContext when using SQL Server/Azure SQL providers
- Use Polly directly via `AddResiliencePipeline()` for custom resilience scenarios

## Naming Conventions

- Use descriptive names that clearly indicate purpose
- DTOs should end with `Dto` (e.g., `HotstringDto`, `CreateHotstringDto`)
- Commands should end with `Command` (e.g., `CreateHotstringCommand`)
- Queries should end with `Query` (e.g., `GetHotstringsByProfileQuery`)
- Controllers should be plural (e.g., `HotstringsController`, `ProfilesController`)

## Architecture Patterns

- **Clean Architecture**: 4 layers - API, Application, Domain, Infrastructure
- **DTO pattern**: DTOs live in Application layer - shared across API, UI, and CLI
- **Repository pattern**: Interfaces in Application; implementations in Infrastructure (e.g., `IHotstringRepository`, `HotstringRepository`)
- **CQRS-style**: Separate commands and queries for complex operations
- **Service layer**: Business logic in Application services (e.g., `IScriptGenerationService`)
- **Dependency injection**: Use constructor injection throughout
- **API-first design**: RESTful endpoints (`GET /api/hotstrings`, `POST /api/hotstrings`, etc.)
- **Authentication**: Microsoft Authentication Library (MSAL) with Azure AD
- **Validation boundary**: FluentValidation on DTOs in Application layer
- **Mapping**: Mapster for DTO ↔ Domain entity conversion

## HttpClient Usage

### Backend (ASP.NET Core Web API)

Use `IHttpClientFactory` with typed clients registered in Program.cs for external API calls. Always add `.AddStandardResilienceHandler()` for automatic retry, circuit breaker, and timeout.

### Frontend (Blazor WebAssembly)

Register typed clients via `AddHttpClient` in Program.cs to consume the AHKFlow Web API. Always chain `.AddStandardResilienceHandler()` for automatic resilience.

### CLI (Console Application)

Use `HttpClient` with typed clients registered in dependency injection to consume the AHKFlow Web API. Always add `.AddStandardResilienceHandler()` for resilience. Use MSAL for authentication token acquisition.

Example CLI commands:

```bash
ahkflow new "you're welcome" --profile work
ahkflow list --profile work --grep "typo" --ignore-case
ahkflow download ahk --profile work
```

## Test-Driven Development (TDD)

Always write tests BEFORE implementation:

1. Write failing test using xUnit and FluentAssertions
2. Implement minimal code to make test pass
3. Refactor if needed
4. Repeat for next feature

Example test structure:

```csharp
public class HotstringServiceTests
{
    [Fact]
    public async Task GetHotstringByIdAsync_ShouldReturnHotstring_WhenIdExists()
    {
        // Arrange
        var repository = Substitute.For<IHotstringRepository>();
        var expectedHotstring = new Hotstring { Id = 1, Trigger = "btw", Replacement = "by the way" };
        repository.GetByIdAsync(1).Returns(expectedHotstring);
        var service = new HotstringService(repository);

        // Act
        var result = await service.GetHotstringByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedHotstring);
    }
}
```

## Resilience Strategy

### HttpClient Resilience (Frontend, Backend, CLI)

- Always chain `.AddStandardResilienceHandler()` when registering HttpClient instances
- Provides automatic retry (exponential backoff), circuit breaker, timeout, and rate limiting
- Log resilience events using Serilog

### Database Resilience (EF Core)

- **SQL Server / Azure SQL providers**: Configure `EnableRetryOnFailure()` in DbContext options
  - Recommended settings: `maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30)`
  - Handles transient SQL failures, connection drops, and timeouts automatically
  - Applies to LocalDB, Docker Compose SQL Server, and Azure SQL

### Frontend HttpClient Configuration

```csharp
builder.Services.AddHttpClient<MyApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
    options.Retry.UseJitter = true;
    // Circuit breaker sampling must be at least 2x attempt timeout
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
});
```

### Custom Resilience Patterns

- For non-HTTP critical operations, use Polly directly via `AddResiliencePipeline()`
- Always log resilience events for observability

## CI/CD and Deployment

### Branching Strategy: GitHub Flow

This repository uses the GitHub Flow branching model. Work from short-lived branches and merge into `main` via pull requests.

Workflow

1. Feature development
   - Create a short-lived feature branch from `main`:

     ```bash
     git checkout -b feature/short-description main
     ```

   - Commit and push regularly.
   - Open a pull request (PR) into `main`.
   - After review and CI checks pass, merge to deploy.

2. Bug fixes
   - Create a fix branch from `main`:

     ```bash
     git checkout -b fix/short-description main
     ```

   - Open a PR into `main`, get it reviewed, and merge.

3. Urgent fixes
   - Create a hotfix branch from `main`:

     ```bash
     git checkout -b hotfix/issueid-short-description main
     ```

   - Open a PR into `main`, merge after review, and deploy immediately.

Naming conventions

- `feature/short-description`
- `fix/short-description`
- `hotfix/issueid-short-description`

General guidelines

- Keep branches short-lived and focused.
- Use PRs for all merges to maintain code quality and transparency.
- Deploy frequently to keep `main` stable and up to date.
- Ensure all tests pass before merging to `main`.
- Delete branches after merging to keep the repository clean.

Branch protection

- Protect `main`: do not allow direct commits.
- Require PR review before merging.
- Require status checks (build + tests) to pass before merging.
- Require branches to be up to date before merging.
- Dismiss stale approvals when new commits are pushed.

### GitHub Actions Workflows

1. deploy-ahkflow-api.yml - Backend API deployment
   - Triggered on push to main (Backend path changes)
   - Jobs: build → test → migrate database → deploy to Azure App Service
   - Runs EF Core migrations automatically
   - Deploys to Azure App Service (Linux)

2. deploy-ahkflow-azure-static-web-app.yml - Frontend deployment
   - Triggered on push to main (Frontend path changes)
   - Builds and deploys Blazor WASM to Azure Static Web Apps

3. ahkflow-migrate-database.yml - Manual database migration
   - Manually triggered workflow for running EF Core migrations

### Azure Deployment (One-Click)

- Infrastructure provisioning via Azure CLI scripts
- Required resources:
  - Azure App Service (Linux, .NET 10 runtime)
  - Azure SQL Database (optional; recommended for production hosting)
  - Azure Static Web Apps
  - Application Insights (optional, for monitoring)

### Deployment Secrets (GitHub)

- `AHKFLOW_AZURE_CREDENTIALS` - Service principal JSON for Azure login
- `AHKFLOW_AZURE_STATIC_WEB_APPS_API_TOKEN` - Static Web Apps deployment token
- `AHKFLOW_SQL_MIGRATION_CONNECTION_STRING` - Azure SQL connection string for migrations

## Out of Scope

Do NOT suggest or implement the following (these are handled separately or are intentionally excluded):

- Authentication/authorization implementation details (see backlog item 011) - currently using Azure AD / MSAL placeholders
- CLI implementation (planned for future)
- Hotstring CRUD features (planned for future)
- Hotkey CRUD features (planned for future)
- Profile management features (planned for future)
- Script generation features (planned for future)
- Hotkey management via CLI (planned for future)
- Hotkey blacklisting (planned for future)
- Custom AHK script management (planned for future)
- Runtime execution of AutoHotkey scripts

## Implementation Roadmap

For the recommended order of development work, see:

- **Implementation Roadmap**: [IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md)

## Related Instructions

For language/framework-specific patterns, refer to:

- Backend patterns: [instructions/backend.instructions.md](instructions/backend.instructions.md)
- Frontend patterns: [instructions/frontend.instructions.md](instructions/frontend.instructions.md)
