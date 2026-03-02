// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AHKFlow.Infrastructure.Services;
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

    // Configure Serilog from appsettings with two-stage initialization
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

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

    // Add controllers and API documentation
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    WebApplication app = builder.Build();

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
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    if (allowedOrigins.Length > 0)
    {
        app.UseCors(corsPolicyName);
    }

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
