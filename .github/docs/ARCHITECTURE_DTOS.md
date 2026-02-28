# DTO Pattern for Clean Architecture

## Pattern Summary

**DTOs (Data Transfer Objects)** protect API boundaries by decoupling external contracts from internal models.

## Why Use DTOs?

1. **Security** - Expose only intended fields, prevent overposting
2. **Stability** - Internal changes don't break API consumers
3. **Performance** - Select only required fields
4. **Versioning** - Support multiple API versions
5. **Validation** - Centralized input validation

## Layer Structure

```
Application/
├── DTOs/
│   ├── EntityDto.cs              # Read DTO
│   ├── CreateEntityDto.cs        # Create DTO
│   └── UpdateEntityDto.cs        # Update DTO
├── Commands/
└── Queries/

Domain/
└── Entities/
    └── Entity.cs                 # Domain entity

Infrastructure/
└── Data/
    ├── DbContext.cs              # EF Core context
    └── Configurations/           # EF configurations
```

## Implementation

### Read DTO

```csharp
namespace MyApp.Application.DTOs;

public record EntityDto(int Id, string Name, DateTime CreatedAt);
```

### Create DTO

```csharp
namespace MyApp.Application.DTOs;

public record CreateEntityDto(string Name);
```

### Update DTO

```csharp
namespace MyApp.Application.DTOs;

public record UpdateEntityDto(int Id, string Name);
```

### Domain Entity

```csharp
namespace MyApp.Domain.Entities;

public class Entity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## Mapping

Use **Mapster** for DTO ↔ Entity conversion:

```csharp
// In controller
var dto = entity.Adapt<EntityDto>();
var entity = createDto.Adapt<Entity>();
```

## Validation

Use **FluentValidation** on DTOs:

```csharp
public class CreateEntityDtoValidator : AbstractValidator<CreateEntityDto>
{
    public CreateEntityDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

## API Usage

Controllers work **only with DTOs**, never with domain entities:

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<EntityDto>> Get(int id)
{
    var entity = await _repository.GetByIdAsync(id);
    return Ok(entity.Adapt<EntityDto>());
}

[HttpPost]
public async Task<ActionResult<EntityDto>> Create(CreateEntityDto dto)
{
    var entity = dto.Adapt<Entity>();
    await _repository.AddAsync(entity);
    return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity.Adapt<EntityDto>());
}
```

## Dependency Direction

```
API → Application (DTOs) → Domain (Entities)
              ↑
       Infrastructure (EF Core)
```

## Key Principles

- DTOs live in **Application layer**
- Domain entities never exposed via API
- Controllers use DTOs only
- Validation on DTOs (FluentValidation)
- Mapping between layers (Mapster)
- One DTO per operation context (read, create, update)

---

*Reusable pattern template for Clean Architecture projects*
