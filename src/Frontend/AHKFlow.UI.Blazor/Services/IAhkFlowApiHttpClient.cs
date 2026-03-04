using AHKFlow.UI.Blazor.DTOs;

namespace AHKFlow.UI.Blazor.Services;

public interface IAhkFlowApiHttpClient
{
    Task<string?> GetVersionAsync(CancellationToken cancellationToken);
    Task<HealthResponse?> GetHealthAsync(CancellationToken cancellationToken);
}
