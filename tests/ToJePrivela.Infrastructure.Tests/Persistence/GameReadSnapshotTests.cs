using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>
/// A game is read in two queries (the games, then their seats). A change saved between them must not pair the game as
/// it was with its seats as they are now: the read keeps one snapshot, because EF keeps the first query's reader open
/// while the second runs. Should EF ever buffer the first query and close its reader, this fails. Needs a database file
/// in WAL mode, as the app's, so that another connection can write while the read is open.
/// </summary>
public sealed class GameReadSnapshotTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"tojeprivela-snapshot-{Guid.NewGuid():N}.db");

    public GameReadSnapshotTests()
    {
        using var context = CreateContext();
        context.Database.Migrate();
        Assert.Equal("wal", context.Database.SqlQueryRaw<string>("PRAGMA journal_mode").AsEnumerable().Single());
    }

    public static TheoryData<string> Reads => ["GetById", "GetAll", "GetByPlayer", "GetRecent"];

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task AChangeSavedBetweenTheQueriesOfOneRead_IsNotSeenByIt(string read)
    {
        int gameId;
        long version;

        await using (var seeding = CreateContext())
        {
            var seeded = new Game(TestAccountId, [1, 2], Start);
            seeding.Games.Add(seeded);
            await seeding.SaveChangesAsync();
            (gameId, version) = (seeded.Id, seeded.Version);
        }

        // Just before the read's second query, another request saves a double: the seat and the game's version change.
        var meanwhile = new WriteBeforeSecondQuery(async () =>
        {
            await using var writer = CreateContext();
            var changed = (await new GameRepository(writer).GetByIdAsync(TestAccountId, gameId))!;
            changed.AwardDouble(1);
            await new UnitOfWork(writer).SaveChangesAsync();
        });
        await using var context = CreateContext(meanwhile);
        var sut = new GameRepository(context);

        IReadOnlyList<Game> games = read switch
        {
            "GetById" => [(await sut.GetByIdAsync(TestAccountId, gameId))!],
            "GetAll" => await sut.GetAllAsync(TestAccountId),
            "GetByPlayer" => await sut.GetByPlayerAsync(TestAccountId, 1),
            "GetRecent" => await sut.GetRecentAsync(TestAccountId, GameHistoryStatus.All, null, 10),
            _ => throw new ArgumentOutOfRangeException(nameof(read)),
        };

        Assert.True(meanwhile.Wrote);
        var game = Assert.Single(games);
        Assert.Equal(version, game.Version);
        Assert.Equal(0, game.GamePlayers.Single(gp => gp.PlayerId == 1).Doubles);
        Assert.All(game.GamePlayers, gp => Assert.NotNull(gp.Player));

        await using var verification = CreateContext();
        var stored = (await new GameRepository(verification).GetByIdAsync(TestAccountId, gameId))!;
        Assert.Equal(1, stored.GamePlayers.Single(gp => gp.PlayerId == 1).Doubles);
    }

    private ToJePrivelaDbContext CreateContext(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>()
            .UseSqlite($"Data Source={_path};Pooling=False")
            .AddInterceptors(interceptors)
            .Options);

    public void Dispose()
    {
        foreach (var file in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            File.Delete(file);
        }
    }

    /// <summary>Runs the write once, between the first and the second query of the watched context.</summary>
    private sealed class WriteBeforeSecondQuery(Func<Task> write) : DbCommandInterceptor
    {
        private int _queries;

        public bool Wrote { get; private set; }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (++_queries == 2)
            {
                Wrote = true;
                await write();
            }

            return result;
        }
    }
}
