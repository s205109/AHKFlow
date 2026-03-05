# Code Coverage Guide

Run tests with code coverage and view HTML reports.

## Quick Start

```powershell
# Windows/Linux/macOS
.\run-coverage.ps1

# Linux/macOS (Bash)
./run-coverage.sh
```

This will:
1. Clean previous results
2. Run all tests with coverage
3. Generate HTML report
4. Open report in your browser

**Report location:** `./coverage/report/index.html`

## Options

```powershell
.\run-coverage.ps1 -SkipReport  # Skip HTML report generation
.\run-coverage.ps1 -NoOpen      # Don't open browser
```

## Manual Commands

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Install ReportGenerator (one-time)
dotnet tool install --global dotnet-reportgenerator-globaltool

# Generate report
reportgenerator \
  "-reports:./coverage/**/coverage.cobertura.xml" \
  "-targetdir:./coverage/report" \
  "-reporttypes:Html"
```

## Coverage Targets

| Layer | Target |
|-------|--------|
| Domain | 100% |
| Application | 90%+ |
| Infrastructure | 80%+ |
| API | 80%+ |

Current thresholds in test projects: **80%** for line, branch, and method coverage.

## Common Messages (Can Ignore)

### FluentAssertions License Notice
```
Warning: The component "Fluent Assertions" is governed by...
```
**What it means:** Licensing information for commercial use. No action needed for non-commercial projects.

### ReportGenerator GitHub Fetch Errors
```
Error during reading file 'https://raw.githubusercontent.com/...': 404 (Not Found).
```
**What it means:** ReportGenerator tries to link source files from GitHub but your branch hasn't been pushed yet. The report still generates correctly. These messages disappear after pushing to GitHub.

## Improving Coverage

1. Run `.\run-coverage.ps1` to see current coverage
2. Open `./coverage/report/index.html`
3. Find red (uncovered) lines
4. Write tests for uncovered code

Example test:
```csharp
public class ServiceTests
{
    [Fact]
    public async Task Method_ShouldBehavior_WhenCondition()
    {
        // Arrange
        var mock = Substitute.For<IRepository>();
        var service = new Service(mock);

        // Act
        var result = await service.MethodAsync();

        // Assert
        result.Should().NotBeNull();
    }
}
```

## Exclude from Coverage

```csharp
[ExcludeFromCodeCoverage]
public class Program { }
```

## CI/CD Integration

See `.github/workflows/test-coverage.yml` for GitHub Actions integration.

## Troubleshooting

**ReportGenerator not found:**
```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
```

**No coverage results:**
- Ensure tests are passing
- Check that `coverlet.collector` is installed in test projects

## Resources

- [Testing Guidelines](.github/agents/testing.agent.md)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
