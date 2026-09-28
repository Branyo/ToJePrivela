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

    /// <summary>Gives a player a bad card worth the question's bad points; finishes the game at the limit.</summary>
    [HttpPost("{id:int}/bad-cards")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameDetailsDto>> AwardBadCard(
        int id,
        [FromBody] AwardBadCardRequest request,
        CancellationToken cancellationToken) =>
        (await _games.AwardBadCardAsync(id, request, cancellationToken)).ToActionResult();

    /// <summary>Credits a player with a double that held: one bad point off at the end; never finishes the game.</summary>
    [HttpPost("{id:int}/doubles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameDetailsDto>> AwardDouble(
        int id,
        [FromBody] AwardDoubleRequest request,
        CancellationToken cancellationToken) =>
        (await _games.AwardDoubleAsync(id, request, cancellationToken)).ToActionResult();

    /// <summary>Takes back one double tapped by mistake; 409 when the player has none left.</summary>
    [HttpDelete("{id:int}/doubles/{playerId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameDetailsDto>> RemoveDouble(int id, int playerId, CancellationToken cancellationToken) =>
        (await _games.RemoveDoubleAsync(id, playerId, cancellationToken)).ToActionResult();

    [HttpPost("{id:int}/finish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameDetailsDto>> FinishGame(int id, CancellationToken cancellationToken) =>
        (await _games.FinishAsync(id, cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteGame(int id, CancellationToken cancellationToken) =>
        (await _games.DeleteAsync(id, cancellationToken)).ToActionResult();
}
