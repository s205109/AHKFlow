---
description: Scaffold a new domain entity with full Clean Architecture layers
---

# Create New Entity

Create a new entity following AHKFlow's Clean Architecture pattern:

## 1. Domain Layer (src/Backend/AHKFlow.Domain/Entities/{EntityName}.cs)

- Create the domain entity class
- Add properties with appropriate types
- Include validation logic in constructor if needed
- Follow entity patterns from existing entities (Hotstring, Profile, Hotkey)

## 2. Application Layer (src/Backend/AHKFlow.Application/)

### DTOs

- `{EntityName}Dto` - Read model
- `Create{EntityName}Dto` - Create input model
- `Update{EntityName}Dto` - Update input model
- Use `record` types for DTOs
- Place in `DTOs/{EntityName}/` folder

### Repository Interface

- `I{EntityName}Repository` in Interfaces/ folder
- Standard methods: `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- All methods should accept `CancellationToken` parameter

### FluentValidation Validators

- `Create{EntityName}DtoValidator`
- `Update{EntityName}DtoValidator`
- Place in Validators/ folder
- Include validation rules for required fields, lengths, and business rules

## 3. Infrastructure Layer (src/Backend/AHKFlow.Infrastructure/)

### Entity Configuration

- `{EntityName}Configuration : IEntityTypeConfiguration<{EntityName}>`
- Configure table name, primary key, relationships, and column constraints
- Place in Data/Configurations/ folder

### Repository Implementation

- `{EntityName}Repository : I{EntityName}Repository`
- Inject `AHKFlowDbContext` via constructor
- Implement all interface methods using EF Core
- Place in Repositories/ folder

### Update DbContext

- Add `DbSet<{EntityName}> {EntityName}s` property to `AHKFlowDbContext`
- Apply configuration in `OnModelCreating` using `modelBuilder.ApplyConfiguration(new {EntityName}Configuration())`

### Create Migration

```bash
cd src/Backend/AHKFlow.Infrastructure
dotnet ef migrations add Add{EntityName} --project ../../src/Backend/AHKFlow.Infrastructure/AHKFlow.Infrastructure.csproj --startup-project ../../src/Backend/AHKFlow.API/AHKFlow.API.csproj
```

## 4. API Layer (src/Backend/AHKFlow.API/Controllers/)

### Controller

- `{EntityName}sController : ControllerBase` (plural controller name)
- Decorate with `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]`
- Inject: `I{EntityName}Repository`, `IMapper`, validators, `ILogger<{EntityName}sController>`

### CRUD Endpoints

- `GET /api/{entitynames}` - Get all (returns `ActionResult<IEnumerable<{EntityName}Dto>>`)
- `GET /api/{entitynames}/{id}` - Get by ID (returns `ActionResult<{EntityName}Dto>`)
- `POST /api/{entitynames}` - Create (accepts `Create{EntityName}Dto`, returns `CreatedAtAction`)
- `PUT /api/{entitynames}/{id}` - Update (accepts `Update{EntityName}Dto`, returns `NoContent`)
- `DELETE /api/{entitynames}/{id}` - Delete (returns `NoContent`)

### Add OpenAPI Documentation

- Use `[ProducesResponseType]` attributes for each endpoint
- Document status codes: 200, 201, 204, 400, 404

## 5. Tests

### Unit Tests (tests/AHKFlow.Application.Tests/)

#### Validator Tests

- Test valid DTOs return `IsValid = true`
- Test invalid DTOs return `IsValid = false` with correct error messages
- Use `[Theory]` and `[InlineData]` for multiple scenarios

### Integration Tests (tests/AHKFlow.Infrastructure.Tests/)

#### Repository Tests

- Prefer a real SQL Server provider (Testcontainers or LocalDB) to avoid provider-specific mismatches
- Test all CRUD operations
- Verify ordering, filtering, and includes
- Clean up in `Dispose()`

### API Integration Tests (tests/AHKFlow.API.Tests/)

#### Controller Tests

- Use `WebApplicationFactory<Program>`
- Test all endpoints with valid and invalid inputs
- Verify status codes and response bodies
- Test authentication requirements

## 6. Register Services in Program.cs

### API Project (src/Backend/AHKFlow.API/Program.cs)

```csharp
// Register repository
builder.Services.AddScoped<I{EntityName}Repository, {EntityName}Repository>();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<Create{EntityName}DtoValidator>();
```

## Questions to Ask User

Before generating code, ask:

1. What is the entity name? (e.g., "Category", "Tag", "Setting")
2. What properties should the entity have? (name, type, constraints)
3. Are there any relationships to other entities?
4. Are there any specific validation rules?
5. Should this entity support soft delete?

## Constraints

- ✅ Follow Allman brace style (braces on new line)
- ✅ Use `async`/`await` with `CancellationToken` support
- ✅ Write tests FIRST (TDD approach)
- ✅ Use FluentValidation for all validation
- ✅ Use Mapster for mapping between DTOs and entities
- ✅ Return Problem Details (RFC 9457) for errors
- ✅ Log important operations using Serilog
