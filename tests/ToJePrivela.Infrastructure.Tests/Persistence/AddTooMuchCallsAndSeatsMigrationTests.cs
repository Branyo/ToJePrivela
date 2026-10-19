using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>
/// Seats stored before the order of play existed get it from the order they were inserted in (their ids), which is the
/// order the players were added to the game; each game counts from 0.
/// </summary>
public class AddTooMuchCallsAndSeatsMigrationTests : IDisposable
{
    private const string AddGameVersion = "20261013164533_AddGameVersion";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public AddTooMuchCallsAndSeatsMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(AddGameVersion);

        // Seats of deleted players (no PlayerId), so no players are needed.
        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Games" ("Id", "AccountId", "Started", "Finished", "BadCardLimit", "BadPointsMode", "IsCancelled")
            VALUES (1, 1, '2026-09-30 18:00:00', NULL, 3, 'Question', 0),
                   (2, 1, '2026-09-30 18:00:00', NULL, 3, 'Question', 0);
            INSERT INTO "GamePlayers" ("Id", "GameId", "PlayerId", "BadPoints", "BadCards", "Doubles")
            VALUES (11, 1, NULL, 0, 0, 0), (12, 2, NULL, 0, 0, 0), (13, 1, NULL, 0, 0, 0),
                   (14, 2, NULL, 0, 0, 0), (15, 1, NULL, 0, 0, 0);
            """);
    }

    [Fact]
    public async Task Upgrade_SeatsEachGamesPlayersInTheOrderTheyWereAdded()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var seats = await context.GamePlayers.AsNoTracking().OrderBy(gp => gp.Id).Select(gp => new { gp.Id, gp.Seat }).ToListAsync();

        Assert.Equal([(11, 0), (12, 0), (13, 1), (14, 1), (15, 2)], seats.Select(s => (s.Id, s.Seat)));
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
