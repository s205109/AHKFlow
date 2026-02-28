namespace AHKFlow.UI.Blazor.Services;

public interface IAhkFlowApiClient
{
    Task<string?> GetVersionAsync(CancellationToken cancellationToken);
}
