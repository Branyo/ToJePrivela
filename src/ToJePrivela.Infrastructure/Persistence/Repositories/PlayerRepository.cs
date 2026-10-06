using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class PlayerRepository : AccountScopedRepository<Player>, IPlayerRepository
{
    public PlayerRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public async Task<Player?> GetByNameAsync(int accountId, string name, CancellationToken cancellationToken = default)
    {
        var key = NameKeys.Of(name);
        return await Owned(accountId).FirstOrDefaultAsync(p => p.NameKey == key, cancellationToken);
    }

    public async Task<int> CountAsync(int accountId, CancellationToken cancellationToken = default) =>
        await Set.CountAsync(p => p.AccountId == accountId, cancellationToken);

    public async Task<IReadOnlyList<string>> GetAvatarsAsync(int accountId, CancellationToken cancellationToken = default) =>
        await Owned(accountId).Select(p => p.Avatar).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> GetExistingIdsAsync(
        int accountId,
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var requested = ids.Distinct().ToList();

        return await Owned(accountId)
            .Where(p => requested.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
    }
}
