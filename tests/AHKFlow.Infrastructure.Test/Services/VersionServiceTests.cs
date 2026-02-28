using AHKFlow.Infrastructure.Services;
using FluentAssertions;

namespace AHKFlow.Infrastructure.Tests.Services
{
    public class VersionServiceTests
    {
        [Fact]
        public async Task GetVersionAsync_ShouldReturnVersion()
        {
            // Arrange
            var service = new VersionService();

            // Act
            string version = await service.GetVersionAsync();

            // Assert
            version.Should().NotBeNullOrEmpty();
            version.Should().NotBe("0.0.0-dev");
        }
    }
}
