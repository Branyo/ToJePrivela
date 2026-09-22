using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IPlayerRepository : IRepository<Player>
{
    Task<Player?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
