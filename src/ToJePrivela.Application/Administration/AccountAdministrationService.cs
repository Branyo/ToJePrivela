using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Administration.Dtos;
using ToJePrivela.Application.Administration.Mapping;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Players;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Statistics;
using ToJePrivela.Application.Statistics.Dtos;

namespace ToJePrivela.Application.Administration;

/// <summary>
/// Reads another login's data through the same account-scoped reads the login itself uses (<see cref="PlayerReads"/>,
/// <see cref="GameReads"/>, <see cref="PlayerStatisticsReads"/>), for the account the admin names instead of the
/// signed-in one.
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
        Result.Success(AccountOverviewMapper.ToDtos(await _accounts.GetLoginsAsync(cancellationToken), _currentAccount.Id));

    public async Task<Result<AccountOverviewDto>> GetAccountAsync(int accountId, CancellationToken cancellationToken = default) =>
        await _accounts.GetLoginAsync(accountId, cancellationToken) is { } login
            ? Result.Success(AccountOverviewMapper.ToDto(login, _currentAccount.Id))
            : Result.Failure<AccountOverviewDto>(AdministrationErrors.AccountNotFound(accountId));

    public Task<Result<IReadOnlyList<PlayerDto>>> GetPlayersAsync(int accountId, CancellationToken cancellationToken = default) =>
        OfLoginAsync(
            accountId,
            () => PlayerReads.GetAllAsync(_players, accountId, cancellationToken),
            players => players.Count == 0,
            cancellationToken);

    public Task<Result<PlayerStatisticsDto>> GetPlayerStatisticsAsync(
        int accountId,
        int playerId,
        CancellationToken cancellationToken = default) =>
        OfLoginAsync(
            accountId,
            () => PlayerStatisticsReads.GetAsync(_players, _games, accountId, playerId, cancellationToken),
            _ => false,
            cancellationToken);

    /// <remarks>
    /// <see cref="GameReads.GetHistoryAsync"/> validates the filter before reading anything; an invalid one is a
    /// validation failure, which <see cref="OfLoginAsync{T}"/> passes on without asking for the login.
    /// </remarks>
    public Task<Result<GameHistoryDto>> GetGameHistoryAsync(
        int accountId,
        GameHistoryFilter filter,
        CancellationToken cancellationToken = default) =>
        OfLoginAsync(
            accountId,
            () => GameReads.GetHistoryAsync(_games, accountId, filter, cancellationToken),
            history => history.Games.Count == 0,
            cancellationToken);

    public Task<Result<GameDetailsDto>> GetGameDetailsAsync(int accountId, int gameId, CancellationToken cancellationToken = default) =>
        OfLoginAsync(
            accountId,
            () => GameReads.GetDetailsAsync(_games, accountId, gameId, cancellationToken),
            _ => false,
            cancellationToken);

    public async Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (AccountDeletion.Refusal(accountId, _currentAccount.Id) is { } refusal)
        {
            return Result.Failure(refusal);
        }

        return await _accounts.DeleteLoginAsync(accountId, cancellationToken)
            ? Result.Success()
            : Result.Failure(AdministrationErrors.AccountNotFound(accountId));
    }

    /// <summary>
    /// Reads the login's data, and only when the read finds nothing (<paramref name="isEmpty"/>, or a 404 of what was
    /// asked for) asks whether the login exists at all, so a login deleted meanwhile (or never there) is told apart from
    /// one without players or games without a second query on every read. What a read found belongs to a login that
    /// existed when it was read, whatever happens to it afterwards.
    /// </summary>
    private async Task<Result<T>> OfLoginAsync<T>(
        int accountId,
        Func<Task<Result<T>>> read,
        Func<T, bool> isEmpty,
        CancellationToken cancellationToken)
    {
        var result = await read();

        var foundNothing = result.IsSuccess ? isEmpty(result.Value) : result.Error.Type == ErrorType.NotFound;

        return foundNothing && !await _accounts.LoginExistsAsync(accountId, cancellationToken)
            ? Result.Failure<T>(AdministrationErrors.AccountNotFound(accountId))
            : result;
    }
}
