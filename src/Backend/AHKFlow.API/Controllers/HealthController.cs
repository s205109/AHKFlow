using AHKFlow.API.Models;
using AHKFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace AHKFlow.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IVersionService _versionService;
        private readonly IHostEnvironment _hostEnvironment;

        public HealthController(ILogger<HealthController> logger, IVersionService versionService, IHostEnvironment hostEnvironment)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        }

        [HttpGet]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HealthResponse>> GetHealthAsync(CancellationToken cancellationToken)
        {
            string version = await _versionService.GetVersionAsync(cancellationToken);
            string environment = _hostEnvironment.EnvironmentName;

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
    }
}
