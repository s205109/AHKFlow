---
description: Testing patterns and conventions for AHKFlow
applyTo: "**/tests/**/*.cs"
---

# Testing Instructions

## Framework and Libraries

- **Test Framework**: xUnit
- **Assertions**: FluentAssertions (`.Should()` syntax)
- **Mocking**: NSubstitute (`Substitute.For<T>()`)
- **Integration Tests**: WebApplicationFactory, Testcontainers (optional)

## Test Naming Convention

```csharp
public async Task MethodName_ShouldExpectedBehavior_WhenCondition()
```

Examples:

- `GetByIdAsync_ShouldReturnEntity_WhenIdExists()`
- `CreateAsync_ShouldThrowException_WhenDtoIsNull()`
- `Validate_ShouldReturnErrors_WhenNameIsEmpty()`

## Test Structure (AAA Pattern)

```csharp
[Fact]
public async Task MethodName_ShouldExpectedBehavior_WhenCondition()
{
    // Arrange - Set up mocks, test data, and dependencies
    var mockRepository = Substitute.For<IRepository>();
    mockRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(testEntity));

    // Act - Execute the method under test
    var result = await sut.MethodAsync(1, CancellationToken.None);

    // Assert - Verify expected outcomes using FluentAssertions
    result.Should().NotBeNull();
    result.Id.Should().Be(1);
}
```

## NSubstitute Patterns

### Basic Mock Setup

```csharp
var mock = Substitute.For<IRepository>();
mock.GetByIdAsync(1).Returns(entity);
```

### With CancellationToken

```csharp
mock.GetByIdAsync(1, Arg.Any<CancellationToken>())
    .Returns(Task.FromResult(entity));
```

### Verify Method Calls

```csharp
await mock.Received(1).AddAsync(Arg.Any<Entity>(), Arg.Any<CancellationToken>());
```

### Throw Exceptions

```csharp
mock.GetByIdAsync(99).Returns(Task.FromException<Entity>(new NotFoundException()));
```

## FluentAssertions Patterns

### Object Assertions

```csharp
result.Should().NotBeNull();
result.Should().BeEquivalentTo(expected);
result.Id.Should().Be(1);
result.Name.Should().Be("Expected Name");
```

### Collection Assertions

```csharp
results.Should().NotBeEmpty();
results.Should().HaveCount(3);
results.Should().Contain(x => x.Id == 1);
results.Should().OnlyContain(x => x.IsActive);
```

### Exception Assertions

```csharp
var act = async () => await sut.MethodAsync();
await act.Should().ThrowAsync<NotFoundException>();
```

### Validation Result Assertions

```csharp
result.IsValid.Should().BeFalse();
result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
result.Errors.Should().Contain(e => e.ErrorMessage.Contains("required"));
```

## Test Categories by Layer

### Unit Tests (Application Layer)

- **Location**: `tests/AHKFlow.Application.Tests/`
- **Purpose**: Test business logic in services and validators
- **Dependencies**: Mock all external dependencies (repositories, other services)
- **Database**: None (use mocks)

```csharp
public class HotstringServiceTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnHotstring_WhenIdExists()
    {
        // Arrange
        var mockRepo = Substitute.For<IHotstringRepository>();
        var expected = new Hotstring { Id = 1, Trigger = "btw" };
        mockRepo.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));
        var service = new HotstringService(mockRepo);

        // Act
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }
}
```

### Repository Tests (Infrastructure Layer)

- **Location**: tests/AHKFlow.Infrastructure.Tests/
- **Purpose**: Test data access logic
- **Dependencies**: Real DbContext using the SQL Server provider
- **Database**: Testcontainers (SQL Server) recommended (avoids provider mismatches)

```csharp
public class HotstringRepositoryTests : IDisposable
{
    private readonly AHKFlowDbContext _context;
    private readonly HotstringRepository _repository;

    public HotstringRepositoryTests()
    {
        // Prefer a real SQL Server provider (Testcontainers or LocalDB)
        // so tests catch provider-specific behaviors and type mappings.
        var connectionString = "<TEST_DB_CONNECTION_STRING>";

        var options = new DbContextOptionsBuilder<AHKFlowDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .Options;
        _context = new AHKFlowDbContext(options);
        _repository = new HotstringRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddHotstring_WhenValidEntity()
    {
        // Arrange
        var hotstring = new Hotstring { Trigger = "test", Replacement = "result" };

        // Act
        var result = await _repository.AddAsync(hotstring, CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        result.Id.Should().BeGreaterThan(0);
        _context.Hotstrings.Should().ContainSingle();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

### Controller Tests (API Layer)

- **Location**: tests/AHKFlow.API.Tests/Controllers/
- **Purpose**: Test controller logic (routing, validation, status codes)
- **Dependencies**: Mock repository, mapper, validators
- **Focus**: API contract, NOT business logic

```csharp
public class HotstringsControllerTests
{
    [Fact]
    public async Task GetById_ShouldReturn200_WhenEntityExists()
    {
        // Arrange
        var mockRepo = Substitute.For<IHotstringRepository>();
        var mockMapper = Substitute.For<IMapper>();
        var entity = new Hotstring { Id = 1 };
        var dto = new HotstringDto(1, "btw", "by the way");

        mockRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);
        mockMapper.Map<HotstringDto>(entity).Returns(dto);

        var controller = new HotstringsController(mockRepo, mockMapper, ...);

        // Act
        var result = await controller.GetById(1, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(dto);
    }
}
```

### Integration Tests (API Layer)

- **Location**: `tests/AHKFlow.API.Tests/Integration/`
- **Purpose**: Test full HTTP request/response cycle
- **Dependencies**: WebApplicationFactory, test database
- **Database**: Prefer SQL Server provider via Testcontainers (or LocalDB)

```csharp
public class HotstringsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HotstringsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_ShouldReturn200_WhenHotstringExists()
    {
        // Arrange
        var hotstring = await SeedTestHotstring();

        // Act
        var response = await _client.GetAsync($"/api/hotstrings/{hotstring.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<HotstringDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(hotstring.Id);
    }
}
```

### Validator Tests (Application Layer)

- **Location**: `tests/AHKFlow.Application.Tests/Validators/`
- **Purpose**: Test FluentValidation rules
- **Test both**: Valid inputs (IsValid = true) and Invalid inputs (IsValid = false)

```csharp
public class CreateHotstringDtoValidatorTests
{
    private readonly CreateHotstringDtoValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenDtoIsValid()
    {
        // Arrange
        var dto = new CreateHotstringDto("btw", "by the way");

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "valid replacement")]
    [InlineData(null, "valid replacement")]
    [InlineData("trigger", "")]
    public async Task Validate_ShouldFail_WhenFieldsAreInvalid(
        string trigger,
        string replacement)
    {
        // Arrange
        var dto = new CreateHotstringDto(trigger, replacement);

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
```

## Coverage Goals

- Unit tests: 100% of service layer business logic
- Validator tests: All validation rules (valid + invalid scenarios)
- Repository tests: All CRUD operations
- Controller tests: All endpoints (happy path + error cases)
- Integration tests: Critical end-to-end flows

## Best Practices

- ✅ Keep tests fast (use unit tests for pure logic; use SQL Server provider tests where DB behavior matters)
- ✅ Keep tests isolated (no shared state between tests)
- ✅ Use descriptive test names
- ✅ Test one behavior per test
- ✅ Test both happy path and error scenarios
- ✅ Use `[Theory]` and `[InlineData]` for multiple similar scenarios
- ✅ Always use `CancellationToken.None` or `Arg.Any<CancellationToken>()`
- ✅ Clean up resources in `Dispose()` if implementing `IDisposable`
- ✅ Mock external dependencies (don't test framework code)
- ✅ Use FluentAssertions for readable assertions
