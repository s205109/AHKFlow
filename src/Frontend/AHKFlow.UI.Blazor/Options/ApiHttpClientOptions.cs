using System.ComponentModel.DataAnnotations;

namespace AHKFlow.UI.Blazor.Options;

public class ApiHttpClientOptions
{
    [Required]
    public Uri BaseAddress { get; set; } = null!;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
