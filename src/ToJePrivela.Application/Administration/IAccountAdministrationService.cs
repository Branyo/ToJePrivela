using ToJePrivela.Application.Administration.Dtos;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Statistics.Dtos;

namespace ToJePrivela.Application.Administration;

/// <summary>
/// What an admin sees of every login: the logins themselves and, read only, each one's players, games and statistics,
/// read as the login itself reads them. Deleting a login is the only change. A login that does not exist (any more) is
/// 404 <see cref="AdministrationErrors.AccountNotFound"/>, before anything of it is read. The host lets only admins in.
/// </summary>
public interface IAccountAdministrationService
{
    /// <summary>Every login, the most recently signed in first (<c>IAccountRepository.GetLoginsAsync</c>).</summary>
    Task<Result<IReadOnlyList<AccountOverviewDto>>> GetAccountsAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PlayerDto>>> GetPlayersAsync(int accountId, CancellationToken cancellationToken = default);

    Task<Result<PlayerStatisticsDto>> GetPlayerStatisticsAsync(
        int accountId,
        int playerId,
        CancellationToken cancellationToken = default);

    Task<Result<GameHistoryDto>> GetGameHistoryAsync(
        int accountId,
        GameHistoryFilter filter,
        CancellationToken cancellationToken = default);

    Task<Result<GameDetailsDto>> GetGameDetailsAsync(int accountId, int gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the login with all its players and games; its sign-ins end with it. Never the admin's own login (409
    /// <see cref="AdministrationErrors.CannotDeleteOwnAccount"/>). An admin login configured in
    /// <c>Authentication:Admins</c> is created again, empty, on the next startup.
    /// </summary>
    Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default);
}
