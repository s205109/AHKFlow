using AHKFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace AHKFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IVersionService _versionService;

        public HealthController(ILogger<HealthController> logger, IVersionService versionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
        }

        [HttpGet]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<HealthResponse>> GetHealthAsync(CancellationToken cancellationToken)
        {
            try
            {
                string version = await _versionService.GetVersionAsync(cancellationToken);
                string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

                _logger.LogInformation("Health check successful");

                return Ok(new HealthResponse
                {
                    Status = "Healthy",
                    Version = version,
                    Environment = environment,
                    Timestamp = DateTime.UtcNow,
                    Checks = new Dictionary<string, string>
                    {
                        ["api"] = "Healthy"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");

                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Service Unavailable",
                    Detail = "The service is currently unavailable.",
                    Instance = HttpContext.Request.Path
                });
            }
        }

        public class HealthResponse
        {
            public string Status { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string Environment { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public Dictionary<string, string> Checks { get; set; } = new();
        }
    }
}
