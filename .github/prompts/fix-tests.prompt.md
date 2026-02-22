---
description: Analyze and fix failing tests
---

# Fix Failing Tests

Follow this systematic approach to analyze and fix failing tests in AHKFlow:

## 1. Identify Failing Tests

Run tests to see failures:

```bash
dotnet test
```

For specific test project:

```bash
dotnet test tests/AHKFlow.API.Tests
dotnet test tests/AHKFlow.Application.Tests
```

For verbose output:

```bash
dotnet test --logger "console;verbosity=detailed"
```

## 2. Analyze Each Failing Test

For each failing test, examine:

### Test Expectations

- What is the test trying to verify?
- What is the expected behavior?
- What are the arrange/act/assert sections doing?

### Actual Behavior

- What error message or assertion failure occurred?
- Is it a null reference, wrong value, or exception?
- Look at the stack trace for clues

### Common Failure Patterns

**Null Reference Exceptions:**

- Check if mocks are properly configured with `.Returns(...)`
- Verify all dependencies are injected
- Ensure test data is properly initialized

**Assertion Failures:**

- Compare expected vs actual values
- Check if data types match
- Verify collection counts and ordering

**Async Issues:**

- Ensure `await` is used on async methods
- Check if `CancellationToken.None` is passed where required
- Verify async setup in mocks: `.Returns(Task.FromResult(...))`

**Validation Failures:**

- Check if DTOs meet validation rules
- Verify required fields are populated
- Ensure data constraints are satisfied

**Database/EF Core Issues:**

- Verify DbContext is properly configured
- Check if migrations are applied
- Ensure test data is seeded correctly
- Verify entity relationships are configured

## 3. Fix the Root Cause

### For Test Code Issues

- Correct mock setup
- Fix test data initialization
- Update assertions to match actual expected behavior
- Add missing `await` keywords

### For Implementation Code Issues

- Fix the implementation to match the test's expectations
- Ensure proper error handling
- Verify async operations are correct
- Check validation logic

### For Both

- Ensure test accurately represents requirements
- Verify implementation follows Clean Architecture patterns

## 4. Verify the Fix

After fixing:

```bash
# Run the specific test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run all tests in the test class
dotnet test --filter "FullyQualifiedName~TestClassName"

# Run all tests
dotnet test
```

## 5. Check for Regressions

Ensure your fix didn't break other tests:

```bash
dotnet test
```

If other tests broke:

- Review what changed
- Identify shared dependencies
- Consider if the original test was incorrect

## 6. Follow TDD Principles

If fixing implementation:

1. Keep the test failing initially
2. Implement minimal code to make it pass
3. Refactor while keeping tests green
4. Verify all tests still pass

## Common Test Patterns in AHKFlow

### Unit Test (Service Layer)

```csharp
[Fact]
public async Task MethodName_ShouldExpectedBehavior_WhenCondition()
{
    // Arrange
    var mockRepo = Substitute.For<IRepository>();
    mockRepo.GetByIdAsync(1, Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(new Entity { Id = 1 }));
    var service = new Service(mockRepo);

    // Act
    var result = await service.MethodAsync(1, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(1);
}
```

### Integration Test (API)

```csharp
[Fact]
public async Task GetById_ShouldReturn200_WhenEntityExists()
{
    // Arrange
    var client = _factory.CreateClient();
    var entity = await SeedTestData();

    // Act
    var response = await client.GetAsync($"/api/entities/{entity.Id}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var dto = await response.Content.ReadFromJsonAsync<EntityDto>();
    dto.Should().NotBeNull();
    dto!.Id.Should().Be(entity.Id);
}
```

### Validator Test

```csharp
[Fact]
public async Task Validate_ShouldFail_WhenNameIsEmpty()
{
    // Arrange
    var validator = new CreateEntityDtoValidator();
    var dto = new CreateEntityDto { Name = "" };

    // Act
    var result = await validator.ValidateAsync(dto);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(dto.Name));
}
```

## Troubleshooting Checklist

- [ ] Are all dependencies properly mocked with NSubstitute?
- [ ] Are async methods awaited?
- [ ] Is `CancellationToken` passed to async methods?
- [ ] Are test data and mocks initialized correctly?
- [ ] Do assertions use FluentAssertions syntax?
- [ ] Are HTTP status codes correct (200, 201, 204, 400, 404)?
- [ ] Is the test following the AAA pattern (Arrange, Act, Assert)?
- [ ] Does the implementation follow Clean Architecture?
- [ ] Are DTOs properly validated with FluentValidation?
- [ ] Is Mapster mapping configured correctly?
- [ ] Are database entities properly configured in EF Core?

## Reporting Results

After fixing tests, report:

1. Number of tests fixed
2. Root cause of failures
3. Changes made to fix them
4. Confirmation that all tests now pass
