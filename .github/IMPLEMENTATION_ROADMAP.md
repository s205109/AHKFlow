# AHKFlow - Implementation Roadmap (MVP)

This document outlines the recommended order of implementation for building the AHKFlow MVP (Minimum Viable Product).

The roadmap is organized by phases and aligns with the backlog in [BACKLOG.md](BACKLOG.md).

## Phase 1: Project Setup

Scaffold the complete solution structure with all projects:

- Create solution and projects (Backend API, Application, Domain, Infrastructure; Frontend Blazor; Test projects)
- Add repository .editorconfig for consistent code style and editor settings
- Add Copilot instructions in the .github/ folder (multiple markdown files with guidance and patterns)
- Add NUKE build project at build/_build.csproj to standardize local and CI builds
- Setup NuGet packages
- Configure project references
  - Reference: [003 - Scaffold initial solution structure](backlog/003-scaffold-initial-solution.md)

## Phase 2: Versioning

Setup MinVer for automatic semantic versioning:

- Reference: [008 - Add versioning (MinVer)](backlog/008-add-versioning-minver.md)

## Phase 3: Logging and Configuration

Add Serilog structured logging across the application:

- Configure Serilog in Program.cs
- Setup appsettings.json structure
  - Reference: [009 - Add structured logging (Serilog)](backlog/009-add-logging-serilog.md)

## Phase 4: GitHub Flow and Repository Setup

Setup GitHub project, branching strategy, and branch protection:

- Create GitHub project with backlog items
- Setup GitHub Flow branching
- Configure branch protection rules on main
- Setup PR review requirements
  - Reference: [002 - Setup GitHub Flow and project branching](backlog/002-github-flow-setup.md)

Note: This repository uses **GitHub Flow** (not GitFlow).

## Phase 5: Database Foundation

Setup Entity Framework Core with SQL Server provider end-to-end:

- Create `AHKFlowDbContext`
- Add a simple test entity (e.g., `TestMessage`) to validate the pipeline
- Create and apply migrations
- Configure development profiles:
  - LocalDB (recommended)
  - Docker Compose (SQL Server)
- Configure production profile: Azure SQL Database
- Use `EnableRetryOnFailure()` for SQL Server/Azure SQL providers
  - Reference: [004 - Setup database foundation (EF Core + SQL Server provider)](backlog/004-database-foundation-ef-core.md)

## Phase 6: Health Checks and Basic API

Create foundational API endpoints:

- Health check endpoint (includes database connectivity test)
- Swagger/OpenAPI configuration
- Global error handling with ProblemDetails (RFC 9457)
  - Reference: [005 - Setup API health checks and Problem Details (RFC 9457)](backlog/005-api-health-checks-problem-details.md)

## Phase 7: Unit and Integration Testing Infrastructure

Setup testing infrastructure:

- Configure xUnit + FluentAssertions
- Setup NSubstitute for mocking
- Write tests for health endpoints
- Setup integration test patterns
  - Reference: [006 - Setup testing infrastructure (xUnit + FluentAssertions + NSubstitute)](backlog/006-testing-infrastructure-setup.md)

## Phase 8: Docker and Deployment Infrastructure

Setup containerization and deployment:

- Create Dockerfile for API
- Create docker-compose.yml with SQL Server + API services for local development
  - Reference: [007 - Setup Docker for development (Dockerfile + Docker Compose)](backlog/007-docker-development-setup.md)

### GitHub Actions Workflows

Automate deployment using descriptive naming conventions:

- workflows/deploy-ahkflow-api.yml (auto-migrate database on deploy to Azure App Service)
- workflows/deploy-ahkflow-azure-static-web-app.yml (frontend deployment)
- workflows/ahkflow-database-migration.yml (manual migrations)
  - Reference: [010 - Create CI/CD pipeline for UI + API](backlog/010-create-ci-cd-pipeline.md)

### Azure Infrastructure

- Create deployment scripts for Azure resources
- Configure GitHub secrets
- Document deployment process

## Phase 9: Authentication and Authorization

Implement Azure AD (Microsoft Entra ID) authentication:

- Configure backend API with MSAL
- Setup frontend authentication
- Apply authorization to controllers
  - Reference: [011 - Add authentication and authorization](backlog/011-add-authentication-authorization.md)

## Phase 10: Feature Development - Hotstrings Epic

Implement hotstring management (create, read, update, delete):

- API CRUD endpoints with OpenAPI documentation
- FluentValidation validators
- ProblemDetails error responses
- Frontend UI with MudBlazor
- CLI support
- Search and filtering
- Unit and integration tests
- References:
  - [012 - Hotstrings API CRUD + OpenAPI](backlog/012-hotstrings-api-crud-openapi.md)
  - [013 - Hotstrings UI CRUD](backlog/013-hotstrings-ui-crud.md)
  - [014 - Hotstrings validation](backlog/014-hotstrings-validation-fluentvalidation.md)
  - [015 - Hotstrings errors](backlog/015-hotstrings-problem-details.md)
  - [016 - Hotstrings CLI support](backlog/016-hotstrings-cli-support.md)
  - [017 - Hotstrings search & filtering](backlog/017-hotstrings-search-filtering.md)
  - [018 - Hotstrings tests](backlog/018-hotstrings-tests.md)

## Phase 11: Feature Development - Hotkeys Epic

Implement hotkey management (create, read, update, delete):

- API CRUD endpoints
- Frontend UI with MudBlazor
- Validation and error handling
- References:
  - [019 - Hotkeys API CRUD](backlog/019-hotkeys-api-crud.md)
  - [020 - Hotkeys UI CRUD](backlog/020-hotkeys-ui-crud.md)

## Phase 12: Feature Development - Profiles Epic

Implement profile management:

- Profile CRUD endpoints
- Header templates per profile
- Frontend UI for profile management and templates
- References:
  - [021 - Profile management (CRUD + select default)](backlog/021-profile-management.md)
  - [022 - Header templates per profile](backlog/022-header-templates-per-profile.md)

## Phase 13: Feature Development - Script Generation & Download

Implement script generation and download functionality:

- Script generation service (combines hotstrings, hotkeys, and header templates)
- Download endpoint for generated .ahk scripts
- Frontend download UI
- CLI download command
- References:
  - [023 - Generate .ahk script per profile](backlog/023-generate-ahk-per-profile.md)
  - [024 - Download generated script endpoint](backlog/024-download-generated-script-endpoint.md)
  - [025 - CLI download command](backlog/025-cli-download-command.md)

## Development Approach

For each feature:

1. **Write tests first** (Test-Driven Development)
2. Implement minimal code to pass tests
3. Refactor and clean up
4. Create pull request
5. Merge to main after approval
6. Verify deployment

### Golden Rules

- Always write tests first
- Follow instruction files in `.github/instructions/`
- Use feature branches; never commit directly to main
- Keep controllers thin; delegate to services
- Use DTOs for all API contracts; never expose entities
- Log important operations
- Handle errors gracefully with ProblemDetails

## Reference Documents

- Copilot instructions: [copilot-instructions.md](copilot-instructions.md) (multiple markdown files with guidance and patterns)
- Backend Patterns: [instructions/backend.instructions.md](instructions/backend.instructions.md)
- Frontend Patterns: [instructions/blazor.instructions.md](instructions/blazor.instructions.md)
- API Patterns: [instructions/aspnet-rest-apis.instructions.md](instructions/aspnet-rest-apis.instructions.md)
- Backlog: [BACKLOG.md](BACKLOG.md)
- EditorConfig: .editorconfig
- NUKE build project: build/_build.csproj

## Getting Started

Begin with Phase 1 and work sequentially. Each phase builds on the previous one.

**First task:** Create the solution and add all projects with proper references.
