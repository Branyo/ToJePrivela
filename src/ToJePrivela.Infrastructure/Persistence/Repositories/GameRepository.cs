using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Domain.Statistics;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class GameRepository : AccountScopedRepository<Game>, IGameRepository
{
    public GameRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Game>> GetByPlayerAsync(int accountId, int playerId, CancellationToken cancellationToken = default) =>
        await Owned(accountId)
            .Where(g => g.GamePlayers.Any(gp => gp.PlayerId == playerId))
            .ToListAsync(cancellationToken);

    /// <summary>Only the seats: the statistics never read the players behind them.</summary>
    public async Task<IReadOnlyList<Game>> GetCountedByPlayerAsync(int accountId, int playerId, CancellationToken cancellationToken = default) =>
        await Set
            .AsNoTracking()
            .Where(g => g.AccountId == accountId)
            .Where(PlayerStatistics.CountedGame)
            .Where(g => g.GamePlayers.Any(gp => gp.PlayerId == playerId))
            .Include(g => g.GamePlayers)
            .ToListAsync(cancellationToken);

    /// <summary>Always whole: with the players, so the game's rules never run on half an aggregate.</summary>
    protected override IQueryable<Game> Owned(int accountId) =>
        Set.Where(g => g.AccountId == accountId).Include(g => g.GamePlayers).ThenInclude(gp => gp.Player);
}
