using AHKFlow.API.Controllers;
using AHKFlow.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AHKFlow.API.Tests.Controllers
{
    public class HealthControllerTests
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IVersionService _versionService;
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            _logger = Substitute.For<ILogger<HealthController>>();
            _versionService = Substitute.For<IVersionService>();
            _controller = new HealthController(_logger, _versionService)
            {
                // Setup HttpContext for controller
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetHealthAsync_ShouldReturnHealthy_WhenServiceIsRunning()
        {
            // Arrange
            string expectedVersion = "1.0.0";
            _versionService.GetVersionAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedVersion));

            // Act
            ActionResult<HealthController.HealthResponse> result = await _controller.GetHealthAsync(CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value as HealthController.HealthResponse;

            response.Should().NotBeNull();
            response!.Status.Should().Be("Healthy");
            response.Version.Should().Be(expectedVersion);
            response.Checks.Should().ContainKey("api");
            response.Checks["api"].Should().Be("Healthy");
        }

        [Fact]
        public async Task GetHealthAsync_ShouldPropagateException_WhenVersionServiceThrows()
        {
            // Arrange
            _versionService.GetVersionAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException<string>(new Exception("Service unavailable")));

            // Act
            var act = async () => await _controller.GetHealthAsync(CancellationToken.None);

            // Assert - exception propagates to GlobalExceptionMiddleware which returns 500
            await act.Should().ThrowAsync<Exception>().WithMessage("Service unavailable");
        }
    }
}
