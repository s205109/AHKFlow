using AHKFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace AHKFlow.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class VersionController : ControllerBase
    {
        private readonly IVersionService _versionService;

        public VersionController(IVersionService versionService)
        {
            _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
        }

        [HttpGet]
        [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<VersionResponse>> GetVersionAsync(CancellationToken cancellationToken)
        {
            string version = await _versionService.GetVersionAsync(cancellationToken);

            return Ok(new VersionResponse(version));
        }

        public record VersionResponse(string Version);
    }
}
