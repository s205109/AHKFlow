using AHKFlow.UI.Blazor;
using AHKFlow.UI.Blazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Serilog;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure Serilog from appsettings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

try
{
    Log.Information("AHKFlow Blazor UI starting up");

    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");

    // Use Serilog for logging
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger);

    var apiBaseUrlResolver = new ApiBaseUrlResolver();
    string apiBaseUrl = await apiBaseUrlResolver.ResolveAsync(
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
