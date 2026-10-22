using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>A game is always loaded whole, with its players, so its rules never run on half an aggregate.</summary>
public interface IGameRepository : IAccountScopedRepository<Game>
{
    /// <summary>Every game of the account the player took part in, finished or not.</summary>
    Task<IReadOnlyList<Game>> GetByPlayerAsync(int accountId, int playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Up to <paramref name="take"/> of the account's games with the <paramref name="status"/>, newest first (by start,
    /// a game without one last, then by id), read only: the first ones, or those after <paramref name="after"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined <paramref name="status"/>.</exception>
    Task<IReadOnlyList<Game>> GetRecentAsync(
        int accountId,
        GameHistoryStatus status,
        GamePosition? after,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Where the account's game stands in the history's order; null when it has no such game.</summary>
    Task<GamePosition?> GetPositionAsync(int accountId, int gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The account's counted games the player took part in (<c>PlayerStatistics.CountedGame</c>), with every seat's
    /// scores but without the seated <see cref="GamePlayer.Player"/> (statistics never read it), and read only: never
    /// saved.
    /// </summary>
    Task<IReadOnlyList<Game>> GetCountedByPlayerAsync(int accountId, int playerId, CancellationToken cancellationToken = default);
}
