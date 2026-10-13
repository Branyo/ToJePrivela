using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>
/// Games finished before the verdict was stored keep the losers they were shown: most final bad points, a tie broken
/// by the number of bad cards, nobody in a running or cancelled game or in one without a card.
/// </summary>
public class StoreGameLosersMigrationTests : IDisposable
{
    private const string AddEnglishQuestionTexts = "20261008084238_AddEnglishQuestionTexts";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public StoreGameLosersMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(AddEnglishQuestionTexts);

        // Seats of deleted players (no PlayerId), so no players are needed; each seat's Id says what it expects.
        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Games" ("Id", "AccountId", "Started", "Finished", "BadCardLimit", "BadPointsMode", "IsCancelled")
            VALUES (1, 1, '2026-09-30 18:00:00', '2026-09-30 19:00:00', 3, 'Question', 0),
                   (2, 1, '2026-09-30 18:00:00', '2026-09-30 19:00:00', 3, 'Question', 0),
                   (3, 1, '2026-09-30 18:00:00', NULL, 3, 'Question', 0),
                   (4, 1, '2026-09-30 18:00:00', '2026-09-30 19:00:00', 3, 'Question', 1),
                   (5, 1, '2026-09-30 18:00:00', '2026-09-30 19:00:00', 3, 'Question', 0),
                   (6, 1, '2026-09-30 18:00:00', '2026-09-30 19:00:00', 3, 'Question', 0);
            INSERT INTO "GamePlayers" ("Id", "GameId", "PlayerId", "BadPoints", "BadCards", "Doubles")
            VALUES (11, 1, NULL, 7, 3, 0), (12, 1, NULL, 7, 2, 0), (13, 1, NULL, 2, 1, 0),
                   (21, 2, NULL, 6, 2, 0), (22, 2, NULL, 7, 2, 1), (23, 2, NULL, 0, 0, 0),
                   (31, 3, NULL, 5, 1, 0), (32, 3, NULL, 0, 0, 0),
                   (41, 4, NULL, 5, 1, 0), (42, 4, NULL, 0, 0, 0),
                   (51, 5, NULL, 0, 0, 1), (52, 5, NULL, 0, 0, 0),
                   (61, 6, NULL, 1, 1, 2), (62, 6, NULL, 0, 0, 0);
            """);
    }

    [Fact]
    public async Task Upgrade_StoresTheLosersEveryFinishedGameWasShown()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var losers = await context.GamePlayers.Where(gp => gp.IsLoser).OrderBy(gp => gp.Id).Select(gp => gp.Id).ToListAsync();

        Assert.Equal([11, 21, 22, 62], losers);
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
