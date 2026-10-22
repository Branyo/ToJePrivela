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
    /// Read only, like <see cref="GetCountedByPlayerAsync"/>: the history never saves what it lists. A page continues
    /// after a position, not after a count, and is read as at most two ranges of the (AccountId, Started, Id) index,
    /// each walked newest first and stopped at the page's end: the games with a start after the position, then, should
    /// the page not be full, those without one (newest first puts them last, SQLite sorting NULL lowest).
    /// </summary>
    public async Task<IReadOnlyList<Game>> GetRecentAsync(
        int accountId,
        GameHistoryStatus status,
        GamePosition? after,
        int take,
        CancellationToken cancellationToken = default)
    {
        var games = status switch
        {
            GameHistoryStatus.All => Owned(accountId),
            GameHistoryStatus.Running => Owned(accountId).Where(Game.RunningGame),
            GameHistoryStatus.Finished => Owned(accountId).Where(Game.FinishedGame),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown game history status."),
        };
        games = games.AsNoTracking();

        var page = new List<Game>(take);

        if (after is null || after.Started is not null)
        {
            var started = games.Where(g => g.Started != null);

            if (after is { Started: DateTime start, Id: var id })
            {
                // "Started <= start" bounds the index range; the rest only drops the games at that very start whose id is
                // not below the position's (the position itself and those listed before it).
                started = started.Where(g => g.Started <= start && (g.Started < start || g.Id < id));
            }

            page.AddRange(await started
                .OrderByDescending(g => g.Started)
                .ThenByDescending(g => g.Id)
                .Take(take)
                .ToListAsync(cancellationToken));
        }

        if (page.Count < take)
        {
            var withoutStart = games.Where(g => g.Started == null);

            if (after is { Started: null, Id: var id })
            {
                withoutStart = withoutStart.Where(g => g.Id < id);
            }

            page.AddRange(await withoutStart
                .OrderByDescending(g => g.Id)
                .Take(take - page.Count)
                .ToListAsync(cancellationToken));
        }

        return page;
    }

    public async Task<GamePosition?> GetPositionAsync(int accountId, int gameId, CancellationToken cancellationToken = default) =>
        await base.Owned(accountId)
            .Where(g => g.Id == gameId)
            .Select(g => new GamePosition(g.Started, g.Id))
            .FirstOrDefaultAsync(cancellationToken);

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
