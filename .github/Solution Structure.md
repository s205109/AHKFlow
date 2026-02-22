# AHKFlow – Visual Studio Solution Structure

## 1. Overview

This document outlines the physical organization of the AHKFlow solution in Visual Studio, following Clean Architecture principles with clear separation between frontend, backend, and supporting infrastructure.

---

## 2. Solution Structure

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

---

## 3. Frontend Projects

### 3.1 AHKFlow.UI.Blazor

**Type:** Blazor WebAssembly (Standalone)

**Responsibilities:**

- User interface and interaction
- Profile selection and management UI
- Hotstring/hotkey CRUD screens
- Script download functionality
- PWA configuration

**Key Dependencies:**

- MudBlazor (UI component library)
- AHKFlow.Application (for shared DTOs and contracts)
- HttpClient for API consumption
- MSAL for authentication

**Structure:**

``` plaintext
AHKFlow.UI.Blazor/
├── 📁 Pages/
│   ├── 📁 Hotstrings/
│   ├── 📁 Hotkeys/
│   ├── 📁 Profiles/
│   └── 📁 Download/
├── 📁 Components/
├── 📁 Services/
│   ├── 📁 ApiClient/
│   └── 📁 AuthenticationService/
├── 📁 wwwroot/
│   ├── 📄 manifest.json
│   └── 📄 service-worker.js
└── 📄 Program.cs
```

**Notes:**

- Consumes DTOs from `AHKFlow.Application`
- No direct database or business logic access
- All data operations via API calls

---

## 4. Backend Projects

### 4.1 AHKFlow.API

**Type:** ASP.NET Core Web API

**Responsibilities:**

- HTTP endpoint exposure
- Request routing
- Authentication/authorization enforcement
- Problem Details (RFC 9457) error handling
- OpenAPI/Swagger documentation

**Key Dependencies:**

- AHKFlow.Application (commands, queries, DTOs)
- Serilog (structured logging)
- FluentValidation.AspNetCore
- MSAL/Azure AD integration

**Structure:**

``` plaintext
AHKFlow.API/
├── 📁 Controllers/
│   ├── 📄 HotstringsController.cs
│   ├── 📄 HotkeysController.cs
│   ├── 📄 ProfilesController.cs
│   └── 📄 ScriptGenerationController.cs
├── 📁 Middleware/
│   ├── 📄 ExceptionHandlingMiddleware.cs
│   └── 📄 RequestLoggingMiddleware.cs
├── 📁 Extensions/
│   └── 📄 ServiceCollectionExtensions.cs
├── 📄 appsettings.json
├── 📄 appsettings.Development.json
└── 📄 Program.cs
```

**Design Principles:**

- **Thin controllers** – delegate to Application layer
- **No business logic** in controllers
- Works **only with DTOs** from Application layer
- No direct EF Core or database access

---

### 4.2 AHKFlow.Application

**Type:** Class Library

**Responsibilities:**

- **DTOs (Data Transfer Objects)** – API contracts
- Commands and Queries (CQRS-style)
- Application services
- Validation logic (FluentValidation)
- Mapping configuration (Mapster)
- Use case orchestration

**Key Dependencies:**

- AHKFlow.Domain
- FluentValidation
- Mapster

**Structure:**

``` plaintext
AHKFlow.Application/
├── 📁 DTOs/
│   ├── 📁 Hotstrings/
│   │   ├── 📄 HotstringDto.cs
│   │   ├── 📄 CreateHotstringDto.cs
│   │   ├── 📄 UpdateHotstringDto.cs
│   │   └── 📄 HotstringQueryDto.cs
│   ├── 📁 Hotkeys/
│   │   ├── 📄 HotkeyDto.cs
│   │   ├── 📄 CreateHotkeyDto.cs
│   │   └── 📄 UpdateHotkeyDto.cs
│   ├── 📁 Profiles/
│   │   ├── 📄 ProfileDto.cs
│   │   ├── 📄 CreateProfileDto.cs
│   │   └── 📄 UpdateProfileDto.cs
│   └── 📁 Scripts/
│       └── 📄 GeneratedScriptDto.cs
├── 📁 Commands/
│   ├── 📁 Hotstrings/
│   │   ├── 📄 CreateHotstringCommand.cs
│   │   ├── 📄 UpdateHotstringCommand.cs
│   │   └── 📄 DeleteHotstringCommand.cs
│   ├── 📁 Hotkeys/
│   └── 📁 Profiles/
├── 📁 Queries/
│   ├── 📁 Hotstrings/
│   │   ├── 📄 GetHotstringByIdQuery.cs
│   │   ├── 📄 GetHotstringsByProfileQuery.cs
│   │   └── 📄 SearchHotstringsQuery.cs
│   ├── 📁 Hotkeys/
│   └── 📁 Profiles/
├── 📁 Validators/
│   ├── 📄 CreateHotstringDtoValidator.cs
│   ├── 📄 UpdateHotstringDtoValidator.cs
│   └── 📄 CreateHotkeyDtoValidator.cs
├── 📁 Mappings/
│   ├── 📄 HotstringMappingProfile.cs
│   ├── 📄 HotkeyMappingProfile.cs
│   └── 📄 ProfileMappingProfile.cs
├── 📁 Services/
│   ├── 📄 IScriptGenerationService.cs
│   └── 📄 ScriptGenerationService.cs
└── 📁 Interfaces/
    ├── 📄 IHotstringRepository.cs
    ├── 📄 IHotkeyRepository.cs
    └── 📄 IProfileRepository.cs
```

**Critical Notes:**

- **DTOs are the API contract** – shared between API, UI, and CLI
- DTOs are **versioned independently** from domain models
- **No Infrastructure dependencies** (no EF Core references)
- Depends on Domain for business rules
- Validation rules live here (FluentValidation)

---

### 4.3 AHKFlow.Domain

**Type:** Class Library

**Responsibilities:**

- Core business entities
- Value objects
- Business rules and invariants
- Domain interfaces
- Domain events (if applicable)

**Key Dependencies:**

- **None** – pure business logic

**Structure:**

``` plaintext
AHKFlow.Domain/
├── 📁 Entities/
│   ├── 📄 Hotstring.cs
│   ├── 📄 Hotkey.cs
│   ├── 📄 Profile.cs
│   └── 📄 HeaderTemplate.cs
├── 📁 ValueObjects/
│   ├── 📄 Trigger.cs
│   ├── 📄 Replacement.cs
│   └── 📄 KeyCombination.cs
├── 📁 Enums/
│   ├── 📄 HotstringOptions.cs
│   └── 📄 ModifierKey.cs
└── 📁 Interfaces/
    └── (Domain-specific interfaces)
```

**Design Principles:**

- **No technical dependencies** (no EF Core, no ASP.NET)
- Pure C# business logic
- Framework-agnostic
- Testable without infrastructure

---

### 4.4 AHKFlow.Infrastructure

**Type:** Class Library

**Responsibilities:**

- EF Core DbContext
- Entity type configurations
- Repository implementations
- Database migrations
- External service integrations

**Key Dependencies:**

- AHKFlow.Domain
- AHKFlow.Application (implements interfaces)
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools

**Structure:**

``` plaintext
AHKFlow.Infrastructure/
├── 📁 Data/
│   ├── 📄 AHKFlowDbContext.cs
│   └── 📁 Migrations/
├── 📁 Configurations/
│   ├── 📄 HotstringConfiguration.cs
│   ├── 📄 HotkeyConfiguration.cs
│   └── 📄 ProfileConfiguration.cs
├── 📁 Repositories/
│   ├── 📄 HotstringRepository.cs
│   ├── 📄 HotkeyRepository.cs
│   └── 📄 ProfileRepository.cs
└── 📁 Services/
    └── (Infrastructure-specific service implementations)
```

**Design Principles:**

- Implements interfaces defined in Application/Domain
- Contains **all EF Core and database logic**
- No business rules (only data access)

---

## 5. Tools Projects

### 5.1 AHKFlow.CLI

**Type:** .NET Console Application

**Responsibilities:**

- Command-line interface for power users
- API consumption (same as UI)
- Scriptable output (JSON support)
- Profile and hotstring management
- Script download

**Key Dependencies:**

- AHKFlow.Application (shared DTOs)
- System.CommandLine (or Spectre.Console)
- HttpClient for API calls
- MSAL for authentication

**Structure:**

``` plaintext
AHKFlow.CLI/
├── 📁 Commands/
│   ├── 📁 HotstringCommands/
│   │   ├── 📄 NewCommand.cs
│   │   ├── 📄 ListCommand.cs
│   │   └── 📄 DeleteCommand.cs
│   ├── 📁 HotkeyCommands/
│   ├── 📁 ProfileCommands/
│   └── 📄 DownloadCommand.cs
├── 📁 Services/
│   └── 📁 ApiClient/
└── 📄 Program.cs
```

**Example Commands:**

```bash
ahkflow new "you're welcome" --profile work
ahkflow list --profile work --grep "typo" --ignore-case
ahkflow download ahk --profile work
```

**Notes:**

- Uses **same DTOs** as UI and API
- Same validation and contracts
- Ensures consistency across all interfaces

---

## 6. Test Projects

### 6.1 Testing Strategy

- **Unit Tests:** Domain and Application layers (fast, isolated)
- **Integration Tests:** API and Infrastructure (SQL Server provider via Testcontainers or LocalDB)
- **Component Tests:** Blazor UI (bUnit)

### 6.2 Test Project Structure

``` plaintext
tests/
├── 📁 AHKFlow.Domain.Tests/
│   ├── 📁 Entities/
│   └── 📁 ValueObjects/
├── 📁 AHKFlow.Application.Tests/
│   ├── 📁 Commands/
│   ├── 📁 Queries/
│   └── 📁 Validators/
├── 📁 AHKFlow.Infrastructure.Tests/
│   ├── 📁 Repositories/
│   └── (Integration tests with Testcontainers)
├── 📁 AHKFlow.API.Tests/
│   ├── 📁 Controllers/
│   └── 📁 Integration/
└── 📁 AHKFlow.UI.Blazor.Tests/
    └── 📁 Pages/
```

**Testing Tools:**

- xUnit
- FluentAssertions
- NSubstitute (mocking)
- Testcontainers (integration tests)
- bUnit (Blazor component testing)

---

## 7. Dependency Flow

``` plaintext
┌─────────────────────────────────────┐
│   Frontend (Blazor UI)              │
│   Tools (CLI)                       │
└──────────────┬──────────────────────┘
               │ (consumes DTOs)
               ▼
┌──────────────────────────────────────┐
│   API (Controllers)                  │
└──────────────┬───────────────────────┘
               │
               ▼
┌──────────────────────────────────────┐
│   Application (DTOs, Commands, etc.) │ ◄── Shared by UI, CLI, API
└──────────────┬───────────────────────┘
               │
               ▼
┌──────────────────────────────────────┐
│   Domain (Business Logic)            │
└──────────────▲───────────────────────┘
               │
               │
┌──────────────┴───────────────────────┐
│   Infrastructure (EF Core, SQL Server)│
└──────────────────────────────────────┘
```

**Rules:**

- API → Application → Domain
- Infrastructure → Domain
- **Never reverse this flow**
- Frontend and CLI consume Application DTOs via API

---

## 8. DTO Architecture (Key Principle)

### 8.1 Why DTOs Live in Application Layer

From the [DTO documentation](docs/DTOs.md):

> **The API contract should not be your database schema.**

DTOs create a protective boundary between:

- External clients (UI, CLI)
- Application logic
- Domain logic
- Infrastructure (database)

### 8.2 DTO Benefits

1. **Separation of Concerns**
   - Decouples database structure from API contract
   - Internal changes don't break external consumers

2. **Security & Controlled Exposure**
   - Only intended fields are exposed
   - Prevents overposting attacks
   - No accidental sensitive data leakage

3. **Performance Optimization**
   - Select only required fields
   - Avoid loading full entity graphs
   - Reduce serialization size

4. **API Stability & Versioning**
   - Version DTOs independently (e.g., `HotstringDtoV1`, `HotstringDtoV2`)
   - Domain models remain stable

5. **Validation Boundary**
   - FluentValidation applied to DTOs
   - Keeps domain logic focused on business rules

### 8.3 DTO Sharing Across Projects

The Application layer project (`AHKFlow.Application`) is referenced by:

- **AHKFlow.API** – Controllers receive/return DTOs
- **AHKFlow.UI.Blazor** – Blazor components bind to DTOs
- **AHKFlow.CLI** – Commands serialize/deserialize DTOs

This ensures:

- **Single source of truth** for contracts
- **Consistent validation** across all interfaces
- **Unified versioning strategy**

---

## 10. Configuration & Cross-Cutting

### 10.1 Shared Configuration

- **EditorConfig** – enforced coding standards
- **Directory.Build.props** – shared project properties
- **MinVer** – automated versioning
- **Serilog configuration** – structured logging

### 10.2 NuGet Packages (Common)

- FluentValidation
- Mapster
- Serilog
- xUnit, FluentAssertions, NSubstitute

---

## 11. Build & Deployment Structure

``` plaintext
build/
├── Build.cs (NUKE build script)
└── _build.csproj
```

**NUKE Build Targets:**

- Clean
- Restore
- Compile
- Test
- Pack
- Publish
- Deploy

---

## 12. Key Takeaways

✅ **Clear separation:** Frontend vs Backend vs Tools
✅ **DTOs in Application layer** – shared contract across all interfaces
✅ **Domain is pure** – no technical dependencies
✅ **Infrastructure is isolated** – replaceable and testable
✅ **API is thin** – delegates to Application layer
✅ **Single source of truth** – DTOs ensure consistency between UI, CLI, and API
✅ **Dependency flow is always inward** – outer layers depend on inner layers
