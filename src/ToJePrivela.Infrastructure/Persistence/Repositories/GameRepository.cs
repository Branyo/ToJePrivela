using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class GameRepository : Repository<Game>, IGameRepository
{
    public GameRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public override async Task<Game?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await WithPlayers().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public override async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await WithPlayers().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Game>> GetByPlayerAsync(int playerId, CancellationToken cancellationToken = default) =>
        await WithPlayers()
            .Where(g => g.GamePlayers.Any(gp => gp.PlayerId == playerId))
            .ToListAsync(cancellationToken);

    private IQueryable<Game> WithPlayers() =>
        Set.Include(g => g.GamePlayers).ThenInclude(gp => gp.Player);
}
