namespace AHKFlow.UI.Blazor.Services;

public interface IAhkFlowApiHttpClient
{
    Task<string?> GetVersionAsync(CancellationToken cancellationToken);
}
