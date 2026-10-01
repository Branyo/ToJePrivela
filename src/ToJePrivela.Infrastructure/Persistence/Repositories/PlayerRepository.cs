using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class PlayerRepository : Repository<Player>, IPlayerRepository
{
    public PlayerRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public async Task<Player?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var key = NameKeys.Of(name);
        return await Set.FirstOrDefaultAsync(p => p.NameKey == key, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAvatarsAsync(CancellationToken cancellationToken = default) =>
        await Set.Select(p => p.Avatar).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var requested = ids.Distinct().ToList();

        return await Set
            .Where(p => requested.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
    }
}
