using AHKFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace AHKFlow.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<VersionController> _logger;
        private readonly IVersionService _versionService;
        private const string ErrorMessage = "An unexpected error occurred while retrieving the application version.";

        public VersionController(ILogger<VersionController> logger, IVersionService versionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
        }

        [HttpGet]
        [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<VersionResponse>> GetVersionAsync(CancellationToken cancellationToken)
        {
            try
            {
                string version = await _versionService.GetVersionAsync(cancellationToken);

                return Ok(new VersionResponse(version));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, ErrorMessage);
            }
        }

        public record VersionResponse(string Version);
    }
}
