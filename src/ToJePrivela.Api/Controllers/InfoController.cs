using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Info;
using ToJePrivela.Application.Info.Dtos;

namespace ToJePrivela.Api.Controllers;

/// <summary>Name and version of the running API, for clients; readable before signing in. Probes use <c>/api/health</c>.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/info")]
[Produces("application/json")]
public sealed class InfoController : ControllerBase
{
    private readonly IApiInfoService _info;

    public InfoController(IApiInfoService info)
    {
        _info = info;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ApiInfoDto> GetInfo() => _info.Get().ToActionResult();
}
