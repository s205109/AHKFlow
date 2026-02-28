using AHKFlow.UI.Blazor;
using AHKFlow.UI.Blazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Get API base URL from configuration
var apiBaseUrl = builder.Configuration["ApiHttpClient:BaseAddress"] ?? "https://localhost:7600";

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
});

await builder.Build().RunAsync();
