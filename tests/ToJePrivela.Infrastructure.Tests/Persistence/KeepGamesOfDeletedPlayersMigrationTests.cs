using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Seats stored under the old (GameId, PlayerId) key must survive the move to a key of their own.</summary>
public class KeepGamesOfDeletedPlayersMigrationTests : IDisposable
{
    private const string AddUnicodeNameKeys = "20261001110610_AddUnicodeNameKeys";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public KeepGamesOfDeletedPlayersMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(AddUnicodeNameKeys);

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Games" ("Id", "Started", "Finished", "BadCardLimit", "BadPointsMode")
            VALUES (1, '2026-09-30 18:00:00', NULL, 3, 'Question'), (2, '2026-09-29 18:00:00', '2026-09-29 19:00:00', 3, 'Question');
            INSERT INTO "GamePlayers" ("GameId", "PlayerId", "BadPoints", "BadCards", "Doubles")
            VALUES (1, 1, 4, 1, 0), (1, 2, 0, 0, 1), (2, 2, 7, 3, 0), (2, 3, 2, 1, 0);
            """);
    }

    [Fact]
    public async Task Upgrade_KeepsEverySeatWithItsScoresAndGivesEachItsOwnKey()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var seats = await context.GamePlayers.OrderBy(gp => gp.GameId).ThenBy(gp => gp.PlayerId).ToListAsync();

        Assert.Equal(
            [(1, (int?)1, 4, 1, 0), (1, 2, 0, 0, 1), (2, 2, 7, 3, 0), (2, 3, 2, 1, 0)],
            seats.Select(gp => (gp.GameId, gp.PlayerId, gp.BadPoints, gp.BadCards, gp.Doubles)));
        Assert.Equal(4, seats.Select(gp => gp.Id).Distinct().Count());
        Assert.All(seats, gp => Assert.True(gp.Id > 0));
    }

    [Fact]
    public async Task Upgrade_LeavesExistingGamesNotCancelled()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        Assert.All(await context.Games.ToListAsync(), game => Assert.False(game.IsCancelled));
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
