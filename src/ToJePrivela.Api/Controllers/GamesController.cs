using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Games;
using ToJePrivela.Application.Games.Dtos;

namespace ToJePrivela.Api.Controllers;

[ApiController]
[Route("api/games")]
[Produces("application/json")]
public sealed class GamesController : ControllerBase
{
    private readonly IGameService _games;

    public GamesController(IGameService games)
    {
        _games = games;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GameDto>>> GetGames(CancellationToken cancellationToken) =>
        (await _games.GetAllAsync(cancellationToken)).ToActionResult();

    [HttpGet("{id:int}", Name = nameof(GetGame))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDto>> GetGame(int id, CancellationToken cancellationToken) =>
        (await _games.GetByIdAsync(id, cancellationToken)).ToActionResult();

    [HttpGet("{id:int}/details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDetailsDto>> GetGameDetails(int id, CancellationToken cancellationToken) =>
        (await _games.GetDetailsAsync(id, cancellationToken)).ToActionResult();

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameDto>> CreateGame(
        [FromBody] CreateGameRequest request,
        CancellationToken cancellationToken) =>
        (await _games.CreateAsync(request, cancellationToken))
            .ToCreatedResult(nameof(GetGame), game => new { id = game.Id });

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateGame(
        int id,
        [FromBody] UpdateGameRequest request,
        CancellationToken cancellationToken) =>
        (await _games.UpdateAsync(id, request, cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteGame(int id, CancellationToken cancellationToken) =>
        (await _games.DeleteAsync(id, cancellationToken)).ToActionResult();
}
