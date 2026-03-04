using AHKFlow.API.Controllers;
using AHKFlow.API.Models;
using AHKFlow.Infrastructure.Data;
using AHKFlow.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AHKFlow.API.Tests.Controllers
{
    public class HealthControllerTests : IDisposable
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IVersionService _versionService;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly AHKFlowDbContext _dbContext;
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            _logger = Substitute.For<ILogger<HealthController>>();
            _versionService = Substitute.For<IVersionService>();
            _hostEnvironment = Substitute.For<IHostEnvironment>();
            _hostEnvironment.EnvironmentName.Returns("Development");

            // Use in-memory database for testing
            var options = new DbContextOptionsBuilder<AHKFlowDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new AHKFlowDbContext(options);

            _controller = new HealthController(_logger, _versionService, _hostEnvironment, _dbContext)
            {
                // Setup HttpContext for controller
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task GetHealthAsync_ShouldReturnHealthy_WhenServiceIsRunning()
        {
            // Arrange
            string expectedVersion = "1.0.0";
            _versionService.GetVersionAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedVersion));

            // Act
            ActionResult<HealthResponse> result = await _controller.GetHealthAsync(CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value as HealthResponse;

            response.Should().NotBeNull();
            response!.Status.Should().Be("Healthy");
            response.Version.Should().Be(expectedVersion);
            response.Environment.Should().Be("Development");
            response.Checks.Should().ContainKey("api");
            response.Checks["api"].Should().Be("Healthy");
            response.Checks.Should().ContainKey("database");
            response.Checks["database"].Should().Be("Healthy");
        }

        [Fact]
        public Task GetHealthAsync_ShouldPropagateException_WhenVersionServiceThrows()
        {
            // Arrange
            _versionService.GetVersionAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException<string>(new Exception("Service unavailable")));

            // Act
            Func<Task> act = async () => await _controller.GetHealthAsync(CancellationToken.None);

            // Assert - exception propagates to GlobalExceptionMiddleware
            return act.Should().ThrowAsync<Exception>().WithMessage("Service unavailable");
        }
    }
}
