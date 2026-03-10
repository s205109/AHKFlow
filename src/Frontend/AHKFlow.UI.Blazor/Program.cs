using AHKFlow.UI.Blazor;
using AHKFlow.UI.Blazor.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Serilog;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure Serilog with Application Insights
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration);

// Add Application Insights sink if connection string is configured
string? appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    var telemetryConfig = new TelemetryConfiguration
    {
        ConnectionString = appInsightsConnectionString
    };

    loggerConfig.WriteTo.ApplicationInsights(
        telemetryConfig,
        TelemetryConverter.Traces);
}

Log.Logger = loggerConfig.CreateLogger();

try
{
    Log.Information("AHKFlow Blazor UI starting up");

    // TEST: Log test error for Application Insights verification (remove after testing)
    Log.Error("TEST ERROR [Program.cs]: Application Insights integration test - this error should appear in Azure Portal");

    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");

    // Use Serilog for logging
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger);
    
    string apiBaseUrl = await ApiBaseUrlResolver.ResolveAsync(
        builder.HostEnvironment.BaseAddress,
        builder.Configuration["ApiHttpClient:BaseAddress"],
        builder.Configuration.GetSection("ApiHttpClient:BaseAddressCandidates").Get<string[]>());

    Log.Information("Selected API base address: {ApiBaseUrl}", apiBaseUrl);

    // Register typed HttpClient for API calls with resilience
    builder.Services.AddHttpClient<IAhkFlowApiHttpClient, AhkFlowApiHttpClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddStandardResilienceHandler();

    builder.Services.AddMudServices();

    builder.Services.AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        options.ProviderOptions.LoginMode = "redirect";
    });

    Log.Information("AHKFlow Blazor UI configured successfully");

    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AHKFlow Blazor UI failed to start");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
