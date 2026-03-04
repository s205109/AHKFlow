namespace AHKFlow.API.Models
{
    public class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Checks { get; set; } = [];
    }
}
