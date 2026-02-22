````instructions
---
description: Backend patterns for AHKFlow
applyTo:
  - "**/src/Backend/**/*.cs"
  - "**/tests/AHKFlow.*.Tests/**/*.cs"
---

# Backend Instructions

## Architecture
- **Clean Architecture**: 4 layers (API → Application → Domain ← Infrastructure)
- **DTO pattern**: DTOs in Application layer - shared across API, UI, and CLI
- **Repository pattern**: Interfaces in Application, implementations in Infrastructure
- **CQRS-style**: Commands and Queries for complex operations
- **Service layer**: Application services for business logic (e.g., `IScriptGenerationService`)
- **Constructor injection**: Use DI throughout

## Naming
- Interfaces: `IHotstringRepository`, `IScriptGenerationService`
- Implementations: `HotstringRepository`, `ScriptGenerationService`
- DTOs: `HotstringDto`, `CreateHotstringDto`, `UpdateHotstringDto`
- Commands: `CreateHotstringCommand`, `UpdateHotstringCommand`
- Queries: `GetHotstringByIdQuery`, `GetHotstringsByProfileQuery`
- Controllers: `HotstringsController`, `ProfilesController` (plural)
- Domain entities: `Hotstring`, `Hotkey`, `Profile`, `HeaderTemplate`

## Key Patterns

### DTOs (Application Layer)
- Use `record` types for immutability
- Separate read DTOs from create/update DTOs
- Example: `HotstringDto`, `CreateHotstringDto`, `UpdateHotstringDto`

### Repository Pattern
- **Interfaces** in Application layer
- **Implementations** in Infrastructure layer
- Use async methods with `CancellationToken`
- Standard methods: `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`

### Service Layer (Application)
- Define interface + implementation
- Inject repositories and logger via constructor
- Orchestrate business logic
- Throw custom exceptions (e.g., `NotFoundException`)

### Validation (FluentValidation)
- One validator per DTO
- Use `RuleFor` with method chaining
- Provide clear error messages

### Mapping (Mapster)
- Auto-maps by convention
- Configure custom mappings in `MappingConfig.Configure()`
- Use `IMapper` in controllers for testability

### Controllers (API Layer)
- Keep controllers thin - delegate to Application layer
- Use `[ApiController]` and `[Authorize]` attributes
- Inject repositories, validators, mapper, and logger
- Return appropriate HTTP status codes (200, 201, 204, 400, 404)
- Use `ProducesResponseType` for OpenAPI documentation
- Validate input using FluentValidation validators
- Map between domain entities and DTOs
- Log important operations

## Testing Approach

### Unit Tests (Application Layer)
- Use xUnit + FluentAssertions
- Mock dependencies with NSubstitute
- Test service layer business logic
- Test validators with valid and invalid scenarios
- Follow AAA pattern (Arrange, Act, Assert)

### Controller Tests
- Mock repository, mapper, validator, logger
- Test HTTP status codes (200, 201, 204, 400, 404)
- Verify response types
- Don't test business logic (delegate to services)

### Integration Tests (API Layer)
- Use `WebApplicationFactory<Program>`
- Test full HTTP request/response cycle
- Verify API contract with DTOs
- Use a real test database (Testcontainers for SQL Server recommended)

## Database Configuration

**Development**: LocalDB (recommended) or Docker Compose (SQL Server)

**Production**: Azure SQL Database

- Use `EnableRetryOnFailure()` for SQL Server/Azure SQL providers
- Migrations may auto-apply in Development (if enabled)

## DI Registration

Register in `Program.cs`:
- DbContext with SQL Server provider (LocalDB/Docker Compose)
- Repositories (scoped)
- Services (scoped)
- FluentValidation validators
- Mapster with `AddMapster()` and `MappingConfig.Configure()`
- Serilog via `UseSerilog()`
- Authentication with Azure AD via `AddMicrosoftIdentityWebApi()`

````
