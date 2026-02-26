---
description: 'ASP.NET Core Web API patterns for AHKFlow'
applyTo: '**/src/Backend/AHKFlow.API/**/*.cs'
---

## Project-Specific Conventions

- Controller-based Web API (not Minimal APIs)
- Controllers should be thin - delegate to Application layer (services/repositories)
- Use plural controller names (e.g., `HotstringsController`, `ProfilesController`)

## Controller Design

- Use `[ApiController]` and `[Authorize]` attributes on controllers
- Use attribute routing: `[Route("api/[controller]")]`
- Inject dependencies via constructor: repositories, validators, mapper, logger
- Return appropriate status codes:
  - `200 OK` for successful GET/PUT
  - `201 Created` for successful POST
  - `204 No Content` for successful DELETE
  - `400 Bad Request` for validation errors
  - `404 Not Found` for missing resources
- Use `ProducesResponseType` attributes for OpenAPI documentation

Example controller structure:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HotstringsController : ControllerBase
{
    private readonly IHotstringRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateHotstringDto> _createValidator;
    private readonly ILogger<HotstringsController> _logger;

    public HotstringsController(
        IHotstringRepository repository,
        IMapper mapper,
        IValidator<CreateHotstringDto> createValidator,
        ILogger<HotstringsController> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _createValidator = createValidator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<HotstringDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HotstringDto>>> GetAll(CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

## Validation and Error Responses

- Use FluentValidation validators injected into controllers
- Validate DTOs before processing
- Return ProblemDetails (RFC 9457) for standardized error responses
- Use `ValidationProblem()` for validation errors
- Use `Problem()` for other error scenarios

Enable ProblemDetails in `Program.cs`

```csharp
builder.Services.AddProblemDetails();
```

Validation error example:

```csharp
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
```

Not found error example:

```csharp
var entity = await _repository.GetByIdAsync(id, cancellationToken);
if (entity == null)
{
    return Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Resource not found",
        detail: $"Hotstring with ID {id} was not found.");
}
```

## Authentication

- Use MSAL with Azure AD (Microsoft Entra ID)
- Apply `[Authorize]` attribute at controller or action level
- Use role-based or policy-based authorization where needed

## Database Configuration

**Development**: LocalDB (recommended) or Docker Compose (SQL Server)

**Production**: Azure SQL Database

- Use `EnableRetryOnFailure()` for SQL Server/Azure SQL providers
- Migrations may auto-apply in Development environment (if enabled)

## Health Checks and Deployment

- Implement health check endpoints for Azure App Service
- Use health checks to verify database connectivity
- Configure for Azure App Service (Linux, .NET 10 runtime)
- Use GitHub Actions for CI/CD with automatic EF Core migrations
