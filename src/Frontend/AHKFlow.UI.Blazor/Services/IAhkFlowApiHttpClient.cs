namespace AHKFlow.UI.Blazor.Services;

public interface IAhkFlowApiHttpClient
{
    Task<string?> GetVersionAsync(CancellationToken cancellationToken);
    Task<HealthCheckResponse?> GetHealthAsync(CancellationToken cancellationToken);
}

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Checks { get; set; } = new();
}
