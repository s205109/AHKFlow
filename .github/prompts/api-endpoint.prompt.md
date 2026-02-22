---
description: Create a new RESTful API endpoint with tests
---

# Create API Endpoint

Follow AHKFlow's API patterns to create a new RESTful endpoint:

## 1. Define or Verify DTOs (Application Layer)

If DTOs don't exist, create them in `src/Backend/AHKFlow.Application/DTOs/`:

```csharp
// Read DTO
public record EntityDto(int Id, string Name, DateTime CreatedAt);

// Create DTO
public record CreateEntityDto(string Name);

// Update DTO
public record UpdateEntityDto(string Name);
```

## 2. Create or Update Controller (API Layer)

### Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EntitiesController : ControllerBase
{
    private readonly IEntityRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateEntityDto> _createValidator;
    private readonly IValidator<UpdateEntityDto> _updateValidator;
    private readonly ILogger<EntitiesController> _logger;

    public EntitiesController(
        IEntityRepository repository,
        IMapper mapper,
        IValidator<CreateEntityDto> createValidator,
        IValidator<UpdateEntityDto> updateValidator,
        ILogger<EntitiesController> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }
}
```

### Endpoint Patterns

#### GET All

```csharp
[HttpGet]
[ProducesResponseType(typeof(IEnumerable<EntityDto>), StatusCodes.Status200OK)]
public async Task<ActionResult<IEnumerable<EntityDto>>> GetAll(CancellationToken cancellationToken)
{
    var entities = await _repository.GetAllAsync(cancellationToken);
    var dtos = _mapper.Map<IEnumerable<EntityDto>>(entities);
    return Ok(dtos);
}
```

#### GET by ID

```csharp
[HttpGet("{id}")]
[ProducesResponseType(typeof(EntityDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<EntityDto>> GetById(int id, CancellationToken cancellationToken)
{
    var entity = await _repository.GetByIdAsync(id, cancellationToken);

    if (entity == null)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource not found",
            detail: $"Entity with ID {id} was not found.");
    }

    var dto = _mapper.Map<EntityDto>(entity);
    return Ok(dto);
}
```

#### POST (Create)

```csharp
[HttpPost]
[ProducesResponseType(typeof(EntityDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<EntityDto>> Create(
    [FromBody] CreateEntityDto createDto,
    CancellationToken cancellationToken)
{
    // Validate
    var validationResult = await _createValidator.ValidateAsync(createDto, cancellationToken);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return ValidationProblem(errors);
    }

    // Map and save
    var entity = _mapper.Map<Entity>(createDto);
    var createdEntity = await _repository.AddAsync(entity, cancellationToken);

    _logger.LogInformation("Created entity with ID {EntityId}", createdEntity.Id);

    var dto = _mapper.Map<EntityDto>(createdEntity);
    return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
}
```

#### PUT (Update)

```csharp
[HttpPut("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Update(
    int id,
    [FromBody] UpdateEntityDto updateDto,
    CancellationToken cancellationToken)
{
    // Validate
    var validationResult = await _updateValidator.ValidateAsync(updateDto, cancellationToken);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return ValidationProblem(errors);
    }

    // Check if exists
    var existingEntity = await _repository.GetByIdAsync(id, cancellationToken);
    if (existingEntity == null)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource not found",
            detail: $"Entity with ID {id} was not found.");
    }

    // Map and update
    _mapper.Map(updateDto, existingEntity);
    await _repository.UpdateAsync(existingEntity, cancellationToken);

    _logger.LogInformation("Updated entity with ID {EntityId}", id);

    return NoContent();
}
```

#### DELETE

```csharp
[HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
{
    var entity = await _repository.GetByIdAsync(id, cancellationToken);

    if (entity == null)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource not found",
            detail: $"Entity with ID {id} was not found.");
    }

    await _repository.DeleteAsync(id, cancellationToken);

    _logger.LogInformation("Deleted entity with ID {EntityId}", id);

    return NoContent();
}
```

## 3. Create Integration Tests (API.Tests)

### Test Setup

```csharp
public class EntitiesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EntitiesControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
}
```

### Test Happy Paths

```csharp
[Fact]
public async Task GetAll_ShouldReturn200WithEntities()
{
    // Arrange - seed test data if needed

    // Act
    var response = await _client.GetAsync("/api/entities");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var entities = await response.Content.ReadFromJsonAsync<IEnumerable<EntityDto>>();
    entities.Should().NotBeNull();
}

[Fact]
public async Task Create_ShouldReturn201_WhenDtoIsValid()
{
    // Arrange
    var createDto = new CreateEntityDto("Test Entity");

    // Act
    var response = await _client.PostAsJsonAsync("/api/entities", createDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    response.Headers.Location.Should().NotBeNull();

    var dto = await response.Content.ReadFromJsonAsync<EntityDto>();
    dto.Should().NotBeNull();
    dto!.Name.Should().Be("Test Entity");
}
```

### Test Error Scenarios

```csharp
[Fact]
public async Task GetById_ShouldReturn404_WhenEntityNotFound()
{
    // Act
    var response = await _client.GetAsync("/api/entities/99999");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}

[Fact]
public async Task Create_ShouldReturn400_WhenDtoIsInvalid()
{
    // Arrange
    var invalidDto = new CreateEntityDto(""); // Empty name

    // Act
    var response = await _client.PostAsJsonAsync("/api/entities", invalidDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

## 4. Verify Swagger Documentation

Run the application and check Swagger UI:

```bash
cd src/Backend/AHKFlow.API
dotnet run
```

Navigate to: <https://localhost:5001/swagger>

Verify:

- Endpoint appears in Swagger UI
- Request/response models are documented
- Status codes are listed
- Try it out functionality works

## Questions to Ask User

Before generating code, ask:

1. What is the endpoint route? (e.g., `/api/entities`, `/api/hotstrings/{id}/enable`)
2. What HTTP verb? (GET, POST, PUT, DELETE, PATCH)
3. What are the request parameters? (route params, query params, body)
4. What should the response look like? (DTO type, status codes)
5. What validation rules apply?
6. Are there any authorization requirements beyond the default `[Authorize]`?

## Constraints

- ✅ Use `[ApiController]` and `[Authorize]` attributes
- ✅ Accept DTOs as parameters, return DTOs as responses
- ✅ Validate using FluentValidation before processing
- ✅ Return Problem Details (RFC 9457) for errors
- ✅ Use appropriate HTTP status codes (200, 201, 204, 400, 404)
- ✅ Add `[ProducesResponseType]` attributes for OpenAPI documentation
- ✅ Use `CancellationToken` for all async operations
- ✅ Log important operations with Serilog
- ✅ Write integration tests for all endpoints (happy path + errors)
- ✅ Follow Allman brace style (braces on new line)
