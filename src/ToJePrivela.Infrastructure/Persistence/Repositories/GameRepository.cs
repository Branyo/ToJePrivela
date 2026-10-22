using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Games.Dtos;
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
    /// after a position, not after a count, so it reads a range of the (AccountId, Started, Id) index.
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
            GameHistoryStatus.Running => Owned(accountId).Where(Game.RunningGame),
            GameHistoryStatus.Finished => Owned(accountId).Where(Game.FinishedGame),
            _ => Owned(accountId),
        };

        // Newest first puts a game without a start (SQLite sorts NULL lowest) after every game with one.
        if (after is { Started: DateTime started, Id: var id })
        {
            games = games.Where(g => g.Started < started || g.Started == null || (g.Started == started && g.Id < id));
        }
        else if (after is not null)
        {
            games = games.Where(g => g.Started == null && g.Id < after.Id);
        }

        return await games
            .AsNoTracking()
            .OrderByDescending(g => g.Started)
            .ThenByDescending(g => g.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

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
