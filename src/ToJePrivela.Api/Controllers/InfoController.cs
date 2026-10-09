using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;

namespace ToJePrivela.Api.Controllers;

/// <summary>
/// Name and version of the running API, readable before signing in. Probes use <c>/api/health</c>.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/info")]
[Produces("application/json")]
public sealed class InfoController : ControllerBase
{
    private readonly ApiInfo _info;

    public InfoController(ApiInfo info)
    {
        _info = info;
    }

    [HttpGet]
    [ProducesResponseType<ApiInfo>(StatusCodes.Status200OK)]
    public ActionResult<ApiInfo> GetInfo() => _info;
}
