using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Administration.Dtos;
using ToJePrivela.Application.Administration.Mapping;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Players.Mapping;
using ToJePrivela.Application.Statistics;
using ToJePrivela.Application.Statistics.Dtos;

namespace ToJePrivela.Application.Administration;

/// <summary>
/// Reads another login's data through the same account-scoped reads the login itself uses (<see cref="GameReads"/>,
/// <see cref="PlayerStatisticsReads"/>), for the account the admin names instead of the signed-in one.
/// </summary>
public sealed class AccountAdministrationService : IAccountAdministrationService
{
    private readonly IAccountRepository _accounts;
    private readonly IPlayerRepository _players;
    private readonly IGameRepository _games;
    private readonly ICurrentAccount _currentAccount;

    public AccountAdministrationService(
        IAccountRepository accounts,
        IPlayerRepository players,
        IGameRepository games,
        ICurrentAccount currentAccount)
    {
        _accounts = accounts;
        _players = players;
        _games = games;
        _currentAccount = currentAccount;
    }

    public async Task<Result<IReadOnlyList<AccountOverviewDto>>> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        Result.Success(AccountOverviewMapper.ToDtos(await _accounts.GetLoginsAsync(cancellationToken)));

    public Task<Result<IReadOnlyList<PlayerDto>>> GetPlayersAsync(int accountId, CancellationToken cancellationToken = default) =>
        OfLoginAsync(
            accountId,
            async () => Result.Success(PlayerMapper.ToDtos(await _players.GetAllAsync(accountId, cancellationToken))),
            cancellationToken);

    public Task<Result<PlayerStatisticsDto>> GetPlayerStatisticsAsync(
        int accountId,
        int playerId,
        CancellationToken cancellationToken = default) =>
        OfLoginAsync(
            accountId,
            () => PlayerStatisticsReads.GetAsync(_players, _games, accountId, playerId, cancellationToken),
            cancellationToken);

    public Task<Result<GameHistoryDto>> GetGameHistoryAsync(
        int accountId,
        GameHistoryFilter filter,
        CancellationToken cancellationToken = default) =>
        OfLoginAsync(accountId, () => GameReads.GetHistoryAsync(_games, accountId, filter, cancellationToken), cancellationToken);

    public Task<Result<GameDetailsDto>> GetGameDetailsAsync(int accountId, int gameId, CancellationToken cancellationToken = default) =>
        OfLoginAsync(accountId, () => GameReads.GetDetailsAsync(_games, accountId, gameId, cancellationToken), cancellationToken);

    public async Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (accountId == _currentAccount.Id)
        {
            return Result.Failure(AdministrationErrors.CannotDeleteOwnAccount);
        }

        return await _accounts.DeleteLoginAsync(accountId, cancellationToken)
            ? Result.Success()
            : Result.Failure(AdministrationErrors.AccountNotFound(accountId));
    }

    /// <summary>
    /// Reads the login's data once the login is known to exist, so a login deleted meanwhile (or never there) is told
    /// apart from one without players or games.
    /// </summary>
    private async Task<Result<T>> OfLoginAsync<T>(int accountId, Func<Task<Result<T>>> read, CancellationToken cancellationToken) =>
        await _accounts.LoginExistsAsync(accountId, cancellationToken)
            ? await read()
            : Result.Failure<T>(AdministrationErrors.AccountNotFound(accountId));
}
