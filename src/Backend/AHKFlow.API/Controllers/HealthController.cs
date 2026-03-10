using AHKFlow.API.Models;
using AHKFlow.Infrastructure.Data;
using AHKFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AHKFlow.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IVersionService _versionService;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly AHKFlowDbContext _dbContext;

        public HealthController(
            ILogger<HealthController> logger,
            IVersionService versionService,
            IHostEnvironment hostEnvironment,
            AHKFlowDbContext dbContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        [HttpGet]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HealthResponse>> GetHealthAsync(CancellationToken cancellationToken)
        {
            string version = await _versionService.GetVersionAsync(cancellationToken);
            string environment = _hostEnvironment.EnvironmentName;

            // Build API URL from request context
            string scheme = HttpContext.Request.Scheme;
            string host = HttpContext.Request.Host.Host;
            int? port = HttpContext.Request.Host.Port;
            string apiUrl = port.HasValue && port > 0
                ? $"{scheme}://{host}:{port}"
                : $"{scheme}://{host}";

            var checks = new Dictionary<string, string>
            {
                ["api"] = "Healthy"
            };

            // Check database connectivity by performing a query
            string? databaseError = null;
            try
            {
                int testMessageCount = await _dbContext.TestMessages.CountAsync(cancellationToken);
                checks["database"] = "Healthy";
                checks["database_records"] = testMessageCount.ToString();

                var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
                checks["migrations_applied"] = appliedMigrations.Count().ToString();
                checks["migrations_pending"] = pendingMigrations.Count().ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                checks["database"] = "Unhealthy";
                databaseError = ex.Message;
            }

            string overallStatus = checks.Values.All(v => v == "Healthy" || int.TryParse(v, out _)) ? "Healthy" : "Degraded";

#pragma warning disable CA1873 // Avoid potentially expensive logging
            _logger.LogInformation("Health check completed with status: {Status}", overallStatus);
#pragma warning restore CA1873 // Avoid potentially expensive logging

            var response = new HealthResponse
            {
                Status = overallStatus,
                Version = version,
                Environment = environment,
                ApiUrl = apiUrl,
                Timestamp = DateTime.UtcNow,
                Checks = checks
            };

            // Return 503 Service Unavailable if database is unhealthy (critical dependency)
            if (checks["database"] == "Unhealthy")
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    response.Status,
                    response.Version,
                    response.Environment,
                    response.ApiUrl,
                    response.Timestamp,
                    response.Checks,
                    Error = "Database is unavailable. API cannot function without database connectivity.",
                    Details = databaseError
                });
            }

            return Ok(response);
        }
    }
}
