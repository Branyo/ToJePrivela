using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Players;
using ToJePrivela.Application.Players.Dtos;

namespace ToJePrivela.Api.Controllers;

[ApiController]
[Route("api/players")]
[Produces("application/json")]
public sealed class PlayersController : ControllerBase
{
    private readonly IPlayerService _players;

    public PlayersController(IPlayerService players)
    {
        _players = players;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlayerDto>>> GetPlayers(CancellationToken cancellationToken) =>
        (await _players.GetAllAsync(cancellationToken)).ToActionResult();

    [HttpGet("{id:int}", Name = nameof(GetPlayer))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerDto>> GetPlayer(int id, CancellationToken cancellationToken) =>
        (await _players.GetByIdAsync(id, cancellationToken)).ToActionResult();

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerDto>> CreatePlayer(
        [FromBody] CreatePlayerRequest request,
        CancellationToken cancellationToken) =>
        (await _players.CreateAsync(request, cancellationToken))
            .ToCreatedResult(nameof(GetPlayer), player => new { id = player.Id });

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdatePlayer(
        int id,
        [FromBody] UpdatePlayerRequest request,
        CancellationToken cancellationToken) =>
        (await _players.UpdateAsync(id, request, cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeletePlayer(int id, CancellationToken cancellationToken) =>
        (await _players.DeleteAsync(id, cancellationToken)).ToActionResult();
}
