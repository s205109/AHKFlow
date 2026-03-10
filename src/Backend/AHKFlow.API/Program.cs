using AHKFlow.API;
using AHKFlow.API.Middleware;
using AHKFlow.Infrastructure.Data;
using AHKFlow.Infrastructure.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// Two-stage initialization: https://github.com/serilog/serilog-aspnetcore#two-stage-initialization
// Bootstrap logger captures startup errors before configuration is loaded
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting AHKFlow API...");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Configure Serilog with Application Insights
    builder.Services.AddSerilog((services, configuration) =>
    {
        var loggerConfig = configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services);

        // Add Application Insights sink if connection string is configured
        string? appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            var telemetryConfig = new TelemetryConfiguration
            {
                ConnectionString = appInsightsConnectionString
            };

            // Set cloud role name for backend
            telemetryConfig.TelemetryInitializers.Add(new CloudRoleNameInitializer("AHKFlow-API"));

            loggerConfig.WriteTo.ApplicationInsights(
                telemetryConfig,
                TelemetryConverter.Traces);

            Log.Information("Application Insights configured for backend API");
        }
    });

    // TEST: Log test error for Application Insights verification (remove after testing)
    Log.Error("TEST ERROR [Backend Program.cs]: Application Insights integration test - Backend API");

    // Start SQL Server in Docker if requested (for "https + Docker SQL" launch profile)
    if (builder.Environment.IsDevelopment() &&
        string.Equals(Environment.GetEnvironmentVariable("AHKFLOW_START_DOCKER_SQL"), "true", StringComparison.OrdinalIgnoreCase))
    {
        DevDockerSqlServer.EnsureStarted(builder.Environment.ContentRootPath);
    }

    // Add CORS - allowed origins are configured in appsettings (Cors:AllowedOrigins)
    const string corsPolicyName = "AllowConfiguredOrigins";
    string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (allowedOrigins.Length > 0)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });
    }

    // Register services
    builder.Services.AddSingleton<IVersionService, VersionService>();

    // Configure database
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<AHKFlowDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        };
    });

    // Add controllers and API documentation
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var validationProblemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Detail = "See the errors field for details.",
                    Instance = context.HttpContext.Request.Path,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "One or more validation errors occurred."
                };

                return new UnprocessableEntityObjectResult(validationProblemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    WebApplication app = builder.Build();

    // Apply migrations automatically in Development
    if (app.Environment.IsDevelopment())
    {
        using IServiceScope scope = app.Services.CreateScope();
        AHKFlowDbContext dbContext = scope.ServiceProvider.GetRequiredService<AHKFlowDbContext>();
        ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying database migrations.");
            throw;
        }
    }

    // Add Serilog HTTP request logging with useful context (status code, path, duration)
    // Excludes sensitive data by default - only logs request path, method, status code, and elapsed time
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    // Configure middleware pipeline
    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        // Enable Swagger in production (Azure)
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "AHKFlow API v1");
            options.RoutePrefix = "swagger"; // Swagger UI at /swagger
        });

        // Only use HTTPS redirection in production
        app.UseHttpsRedirection();
    }

    if (allowedOrigins.Length > 0)
    {
        app.UseCors(corsPolicyName);
    }

    // Redirect root to Swagger in all environments
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

    app.MapControllers();

    Log.Information("AHKFlow API started successfully");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "AHKFlow API terminated unexpectedly");
}
finally
{
    Log.Information("AHKFlow API shutting down...");
    Log.CloseAndFlush();
}
