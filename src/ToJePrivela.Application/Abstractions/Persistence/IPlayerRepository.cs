using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IPlayerRepository : IAccountScopedRepository<Player>
{
    /// <param name="name">Compared by <see cref="Player.NameKey"/>: case-insensitively, accented letters included.</param>
    Task<Player?> GetByNameAsync(int accountId, string name, CancellationToken cancellationToken = default);

    Task<int> CountAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>The avatar of every player of the account, one entry per player.</summary>
    Task<IReadOnlyList<string>> GetAvatarsAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>The given ids that name players of the account.</summary>
    Task<IReadOnlyList<int>> GetExistingIdsAsync(int accountId, IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
