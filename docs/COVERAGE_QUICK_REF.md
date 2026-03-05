# Code Coverage Quick Reference

## 🚀 Quick Start

```bash
# Run coverage and view report
.\run-coverage.ps1          # Windows
./run-coverage.sh           # Linux/macOS
```

## 📊 Manual Commands

```bash
# 1. Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# 2. Generate HTML report
reportgenerator \
  "-reports:./coverage/**/coverage.cobertura.xml" \
  "-targetdir:./coverage/report" \
  "-reporttypes:Html"

# 3. View report
start ./coverage/report/index.html    # Windows
open ./coverage/report/index.html     # macOS
xdg-open ./coverage/report/index.html # Linux
```

## 🎯 Coverage Targets

| Layer | Target | Priority |
|-------|--------|----------|
| Domain | 100% | ⭐⭐⭐ Critical |
| Application | 90%+ | ⭐⭐⭐ Critical |
| Infrastructure | 80%+ | ⭐⭐ High |
| API | 80%+ | ⭐⭐ High |
| UI (Blazor) | 60%+ | ⭐ Medium |

## 📁 Coverage Files Location

```
./coverage/report/
├── index.html           # Main coverage report
├── summary.html         # Quick summary
├── Cobertura.xml        # CI/CD format
└── Summary.json         # Programmatic access
```

## 🧪 Writing Tests

```csharp
public class ServiceTests
{
    [Fact]
    public async Task Method_ShouldBehavior_WhenCondition()
    {
        // Arrange - Setup mocks & data
        var mock = Substitute.For<IRepository>();
        var service = new Service(mock);

        // Act - Execute method
        var result = await service.MethodAsync();

        // Assert - Verify expectations
        result.Should().NotBeNull();
    }
}
```

## 🚫 Exclude from Coverage

```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class StartupCode { }
```

## 🔧 Troubleshooting

```bash
# Install ReportGenerator
dotnet tool install --global dotnet-reportgenerator-globaltool

# Add to PATH (if needed)
export PATH="$PATH:$HOME/.dotnet/tools"  # Linux/macOS
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"  # Windows

# Clean coverage data
rm -rf ./coverage ./TestResults  # Bash
Remove-Item -Recurse ./coverage, ./TestResults  # PowerShell
```

## 📚 Learn More

- [Full Coverage Guide](CODE_COVERAGE.md)
- [Testing Guidelines](../.github/agents/testing.agent.md)
- [Implementation Summary](COVERAGE_IMPLEMENTATION.md)

---

**Current Coverage:** 14.2% line, 7.0% branch, 35.1% method
**Target:** 80%+ across all metrics
