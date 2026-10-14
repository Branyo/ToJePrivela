using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Statistics;
using ToJePrivela.Application.Statistics.Dtos;

namespace ToJePrivela.Api.Controllers;

[ApiController]
[Route("api/players/{id:int}/statistics")]
[Produces("application/json")]
public sealed class PlayerStatisticsController : ControllerBase
{
    private readonly IPlayerStatisticsService _statistics;

    public PlayerStatisticsController(IPlayerStatisticsService statistics)
    {
        _statistics = statistics;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerStatisticsDto>> GetStatistics(int id, CancellationToken cancellationToken) =>
        (await _statistics.GetAsync(id, cancellationToken)).ToActionResult();
}
