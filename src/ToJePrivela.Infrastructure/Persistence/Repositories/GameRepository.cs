using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class GameRepository : Repository<Game>, IGameRepository
{
    public GameRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public async Task<Game?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        await Set
            .Include(g => g.GamePlayers)
            .ThenInclude(gp => gp.Player)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Game>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
        await Set
            .Include(g => g.GamePlayers)
            .ThenInclude(gp => gp.Player)
            .ToListAsync(cancellationToken);
}
