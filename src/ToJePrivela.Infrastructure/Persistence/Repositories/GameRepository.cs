using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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
        await ByPlayer(Owned(accountId), playerId).ToListAsync(cancellationToken);

    /// <summary>
    /// The scores are all statistics read, so the seats come without their <see cref="GamePlayer.Player"/>: loading
    /// one per seat of every game would grow with the account's history for nothing.
    /// </summary>
    public async Task<IReadOnlyList<Game>> GetCountedByPlayerAsync(int accountId, int playerId, CancellationToken cancellationToken = default) =>
        await ByPlayer(WithSeats(accountId), playerId)
            .AsNoTracking()
            .Where(PlayerStatistics.CountedGame)
            .ToListAsync(cancellationToken);

    /// <summary>Always whole: with the players, so the game's rules never run on half an aggregate.</summary>
    protected override IQueryable<Game> Owned(int accountId) => WithSeats(accountId).ThenInclude(gp => gp.Player);

    /// <summary>
    /// The account's games (scoped by the base <see cref="AccountScopedRepository{TEntity}.Owned"/>, like every other
    /// read) with their seats; every game query starts here.
    /// </summary>
    private IIncludableQueryable<Game, IEnumerable<GamePlayer>> WithSeats(int accountId) =>
        base.Owned(accountId).Include(g => g.GamePlayers);

    private static IQueryable<Game> ByPlayer(IQueryable<Game> games, int playerId) =>
        games.Where(g => g.GamePlayers.Any(gp => gp.PlayerId == playerId));
}
