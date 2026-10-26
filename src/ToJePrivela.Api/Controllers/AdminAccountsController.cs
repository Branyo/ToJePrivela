using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Administration;
using ToJePrivela.Application.Administration.Dtos;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Statistics.Dtos;

namespace ToJePrivela.Api.Controllers;

/// <summary>
/// Every login for admins: the list, and read only, each login's players, statistics and games, answered as the login's
/// own endpoints answer them. Deleting a login (with everything it owns) is the only change. 404
/// <c>Account.NotFound</c> for a login that does not exist.
/// </summary>
[ApiController]
[Route("api/admin/accounts")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[Produces("application/json")]
public sealed class AdminAccountsController : ControllerBase
{
    private readonly IAccountAdministrationService _administration;

    public AdminAccountsController(IAccountAdministrationService administration)
    {
        _administration = administration;
    }

    /// <summary>Every login, the most recently signed in first; those never signed in last.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountOverviewDto>>> GetAccounts(CancellationToken cancellationToken) =>
        (await _administration.GetAccountsAsync(cancellationToken)).ToActionResult();

    [HttpGet("{accountId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountOverviewDto>> GetAccount(int accountId, CancellationToken cancellationToken) =>
        (await _administration.GetAccountAsync(accountId, cancellationToken)).ToActionResult();

    /// <summary>
    /// Deletes the login with all its players and games; 409 <c>Account.CannotDeleteOwn</c> for the admin's own and
    /// <c>Account.CannotDeleteReserved</c> for the reserved first login.
    /// </summary>
    [HttpDelete("{accountId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteAccount(int accountId, CancellationToken cancellationToken) =>
        (await _administration.DeleteAccountAsync(accountId, cancellationToken)).ToActionResult();

    [HttpGet("{accountId:int}/players")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PlayerDto>>> GetPlayers(int accountId, CancellationToken cancellationToken) =>
        (await _administration.GetPlayersAsync(accountId, cancellationToken)).ToActionResult();

    [HttpGet("{accountId:int}/players/{playerId:int}/statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerStatisticsDto>> GetPlayerStatistics(
        int accountId,
        int playerId,
        CancellationToken cancellationToken) =>
        (await _administration.GetPlayerStatisticsAsync(accountId, playerId, cancellationToken)).ToActionResult();

    /// <summary>The login's games, paged like <c>GET /api/games/history</c>.</summary>
    [HttpGet("{accountId:int}/games/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameHistoryDto>> GetGameHistory(
        int accountId,
        [FromQuery] GameHistoryFilter filter,
        CancellationToken cancellationToken) =>
        (await _administration.GetGameHistoryAsync(accountId, filter, cancellationToken)).ToActionResult();

    [HttpGet("{accountId:int}/games/{gameId:int}/details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDetailsDto>> GetGameDetails(int accountId, int gameId, CancellationToken cancellationToken) =>
        (await _administration.GetGameDetailsAsync(accountId, gameId, cancellationToken)).ToActionResult();
}
