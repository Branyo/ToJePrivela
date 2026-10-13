using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>A game is always loaded whole, with its players, so its rules never run on half an aggregate.</summary>
public interface IGameRepository : IAccountScopedRepository<Game>
{
    /// <summary>Every game of the account the player took part in, finished or not.</summary>
    Task<IReadOnlyList<Game>> GetByPlayerAsync(int accountId, int playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The account's counted games the player took part in (<c>PlayerStatistics.CountedGame</c>), read only (never
    /// saved) and with their seats but not the players behind them.
    /// </summary>
    Task<IReadOnlyList<Game>> GetCountedByPlayerAsync(int accountId, int playerId, CancellationToken cancellationToken = default);
}
