using System.Text.Json;
using AHKFlow.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AHKFlow.API.Tests.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_ShouldReturnProblemDetails_WhenUnhandledExceptionOccurs()
        {
            // Arrange
            ILogger<GlobalExceptionMiddleware> logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();
            IHostEnvironment environment = CreateEnvironment(Environments.Production);
            static Task next(HttpContext _) => throw new InvalidOperationException("boom");

            var middleware = new GlobalExceptionMiddleware(next, logger, environment);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/v1/version";
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            context.Response.ContentType.Should().Be("application/problem+json");

            context.Response.Body.Position = 0;
            using JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
            document.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status500InternalServerError);
            document.RootElement.GetProperty("title").GetString().Should().Be("Internal Server Error");
            document.RootElement.GetProperty("instance").GetString().Should().Be("/api/v1/version");
        }

        [Fact]
        public async Task InvokeAsync_ShouldReturnExceptionDetailInDetail_WhenEnvironmentIsDevelopment()
        {
            // Arrange
            ILogger<GlobalExceptionMiddleware> logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();
            IHostEnvironment environment = CreateEnvironment(Environments.Development);
            string exceptionMessage = "boom";
            Task next(HttpContext _) => throw new InvalidOperationException(exceptionMessage);

            var middleware = new GlobalExceptionMiddleware(next, logger, environment);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/v1/version";
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            context.Response.ContentType.Should().Be("application/problem+json");

            context.Response.Body.Position = 0;
            using JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
            document.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status500InternalServerError);
            document.RootElement.GetProperty("title").GetString().Should().Be("Internal Server Error");
            document.RootElement.GetProperty("detail").GetString().Should().Be(exceptionMessage);
            document.RootElement.GetProperty("instance").GetString().Should().Be("/api/v1/version");
        }

        [Fact]
        public async Task InvokeAsync_ShouldContinuePipeline_WhenNoExceptionOccurs()
        {
            // Arrange
            ILogger<GlobalExceptionMiddleware> logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();
            IHostEnvironment environment = CreateEnvironment(Environments.Production);
            bool nextCalled = false;

            Task next(HttpContext _) 
            {
                nextCalled = true;
                return Task.CompletedTask;
            }
;
            var middleware = new GlobalExceptionMiddleware(next, logger, environment);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        private static IHostEnvironment CreateEnvironment(string environmentName)
        {
            IHostEnvironment environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);
            environment.ApplicationName.Returns("AHKFlow.API.Tests");
            environment.ContentRootPath.Returns("C:\\");
            environment.ContentRootFileProvider.Returns(Substitute.For<IFileProvider>());
            return environment;
        }
    }
}
