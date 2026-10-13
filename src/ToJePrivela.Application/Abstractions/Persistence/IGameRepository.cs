using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>A game is always loaded whole, with its players, so its rules never run on half an aggregate.</summary>
public interface IGameRepository : IAccountScopedRepository<Game>
{
    /// <summary>Every game of the account the player took part in, finished or not.</summary>
    Task<IReadOnlyList<Game>> GetByPlayerAsync(int accountId, int playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The account's started, finished and not cancelled games the player took part in, read only (never saved).
    /// </summary>
    Task<IReadOnlyList<Game>> GetFinishedByPlayerAsync(int accountId, int playerId, CancellationToken cancellationToken = default);
}
