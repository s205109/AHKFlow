# AHKFlow Product Backlog

This backlog is derived from **"AHKFlow – Product Vision & Architecture Overview"**.

## How this backlog is organized

- This file is the roadmap/index.
- Each backlog item lives in backlog/.

## Ordered backlog (per requested sequence)

### Backlog setup

- [001 - Create backlog in Azure (epics + initial items)](backlog/001-create-backlog-in-azure.md)
-- [002 - Setup GitHub Flow and project branching](backlog/002-github-flow-setup.md)

### Initial project / solution

-- [003 - Scaffold initial solution structure](backlog/003-scaffold-initial-solution.md)
-- [004 - Setup database foundation (EF Core + SQL Server provider)](backlog/004-database-foundation-ef-core.md)
-- [005 - Setup API health checks and Problem Details (RFC 9457)](backlog/005-api-health-checks-problem-details.md)
-- [006 - Setup testing infrastructure (xUnit + FluentAssertions + NSubstitute)](backlog/006-testing-infrastructure-setup.md)
-- [007 - Setup Docker for development (Dockerfile + Docker Compose)](backlog/007-docker-development-setup.md)

### Versioning

-- [008 - Add versioning (MinVer)](backlog/008-add-versioning-minver.md)

### Logging

-- [009 - Add structured logging (Serilog)](backlog/009-add-logging-serilog.md)

### CI/CD

-- [010 - Create CI/CD pipeline for UI + API](backlog/010-create-ci-cd-pipeline.md)

### Authentication and authorization

-- [011 - Add authentication and authorization](backlog/011-add-authentication-authorization.md)

### Epic: Hotstrings

-- [012 - Hotstrings API CRUD + OpenAPI](backlog/012-hotstrings-api-crud-openapi.md)
-- [013 - Hotstrings UI CRUD](backlog/013-hotstrings-ui-crud.md)
-- [014 - Hotstrings validation (FluentValidation)](backlog/014-hotstrings-validation-fluentvalidation.md)
-- [015 - Hotstrings errors via Problem Details (RFC 9457)](backlog/015-hotstrings-problem-details.md)
-- [016 - Hotstrings CLI support (create/list + JSON)](backlog/016-hotstrings-cli-support.md)
-- [017 - Hotstrings search & filtering (grep, ignore-case)](backlog/017-hotstrings-search-filtering.md)
-- [018 - Hotstrings unit + integration tests](backlog/018-hotstrings-tests.md)

### Epic: Hotkeys

-- [019 - Hotkeys API CRUD](backlog/019-hotkeys-api-crud.md)
-- [020 - Hotkeys UI CRUD](backlog/020-hotkeys-ui-crud.md)

### Epic: Profiles

-- [021 - Profile management (CRUD + select default)](backlog/021-profile-management.md)
-- [022 - Header templates per profile](backlog/022-header-templates-per-profile.md)

### Epic: Script generation & download

-- [023 - Generate .ahk script per profile](backlog/023-generate-ahk-per-profile.md)
-- [024 - Download generated script endpoint](backlog/024-download-generated-script-endpoint.md)
-- [025 - CLI download command](backlog/025-cli-download-command.md)

## Tags (suggested)

- **Type**: Feature / Tech / Security / Quality
- **Interfaces**: UI / API / CLI
