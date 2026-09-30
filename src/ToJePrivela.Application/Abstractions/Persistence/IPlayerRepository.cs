using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IPlayerRepository : IRepository<Player>
{
    /// <param name="name">Already normalized with <see cref="Player.NormalizeName"/>; compared case-insensitively.</param>
    Task<Player?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>The avatar of every player, one entry per player.</summary>
    Task<IReadOnlyList<string>> GetAvatarsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
