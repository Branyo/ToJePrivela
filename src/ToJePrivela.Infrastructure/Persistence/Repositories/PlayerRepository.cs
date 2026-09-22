using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class PlayerRepository : Repository<Player>, IPlayerRepository
{
    public PlayerRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    /// <summary>Case-insensitive through the NOCASE collation on Player.Name.</summary>
    public async Task<Player?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        return await Set.FirstOrDefaultAsync(p => p.Name == trimmed, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var requested = ids.Distinct().ToList();

        return await Set
            .Where(p => requested.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
    }
}
