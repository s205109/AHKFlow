using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AHKFlow.UI.Blazor.Services;

public class AhkFlowApiHttpClient : IAhkFlowApiHttpClient
{
    private readonly HttpClient _httpClient;

    public AhkFlowApiHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };
    }

    public async Task<string?> GetVersionAsync(CancellationToken cancellationToken)
    {
        VersionResponse? response = await _httpClient.GetFromJsonAsync<VersionResponse>("api/v1/version", cancellationToken);
        return response?.Version;
    }

    private sealed class VersionResponse
    {
        public string Version { get; set; } = string.Empty;
    }
}
