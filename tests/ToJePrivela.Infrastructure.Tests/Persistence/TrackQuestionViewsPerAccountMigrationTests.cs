using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>
/// The shared view counts become every sign-in-able login's own starting point; the reserved account, which nobody can
/// sign in to, and questions nobody saw get no rows.
/// </summary>
public class TrackQuestionViewsPerAccountMigrationTests : IDisposable
{
    private const string IndexGameHistory = "20261022115440_IndexGameHistory";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TrackQuestionViewsPerAccountMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(IndexGameHistory);

        // Account 1 is the reserved one the AddAccounts migration inserted (no password).
        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Accounts" ("Id", "Name", "NameKey", "PasswordHash", "IsAdmin", "CreatedAt", "SecurityStamp")
            VALUES (2, 'Brano', 'brano', 'hash', 1, '2026-10-01 10:00:00', 'stamp-2'),
                   (3, 'Party', 'party', 'hash', 0, '2026-10-01 10:00:00', 'stamp-3');
            INSERT INTO "QuestionCategories" ("Id", "NameSk", "NameSkKey", "NameEn", "NameEnKey")
            VALUES (1, 'História', 'história', 'History', 'history');
            INSERT INTO "Questions" ("Id", "TextSk", "TextEn", "Answer", "CategoryId", "BadPoints", "Source", "CreatedAt", "ViewCount", "LastViewedAt", "Version")
            VALUES (10, 'Kedy padol Berlínsky múr?', 'When did the Berlin Wall fall?', '1989', 1, 2, 'Manual', '2026-09-01 08:00:00', 3, '2026-10-20 19:00:00', 3),
                   (11, 'Kedy vyšiel ChatGPT?', 'When was ChatGPT released?', '2022', 1, 2, 'Ai', '2026-09-01 08:00:00', 1, NULL, 1),
                   (12, 'Kedy pristál Apollo 11?', 'When did Apollo 11 land?', '1969', 1, 2, 'Ai', '2026-09-01 08:00:00', 0, NULL, 0);
            """);
    }

    [Fact]
    public async Task Upgrade_GivesEveryLoginTheSharedCountsOfTheQuestionsSeen()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var views = await context.QuestionViews
            .AsNoTracking()
            .OrderBy(v => v.AccountId)
            .ThenBy(v => v.QuestionId)
            .Select(v => new { v.AccountId, v.QuestionId, v.Count, v.LastViewedAt })
            .ToListAsync();

        var lastViewed = new DateTime(2026, 10, 20, 19, 0, 0, DateTimeKind.Utc);
        // Never stamped with a time: its creation stands in.
        var created = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        Assert.Equal(
            [
                new { AccountId = 2, QuestionId = 10, Count = 3, LastViewedAt = lastViewed },
                new { AccountId = 2, QuestionId = 11, Count = 1, LastViewedAt = created },
                new { AccountId = 3, QuestionId = 10, Count = 3, LastViewedAt = lastViewed },
                new { AccountId = 3, QuestionId = 11, Count = 1, LastViewedAt = created },
            ],
            views);
    }

    [Fact]
    public async Task Downgrade_BringsBackTheMostAnyLoginSawEachQuestion()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync(
            """UPDATE "QuestionViews" SET "Count" = 5 WHERE "AccountId" = 3 AND "QuestionId" = 10;""");

        await context.GetService<IMigrator>().MigrateAsync(IndexGameHistory);

        var counts = await context.Database
            .SqlQueryRaw<int>("""SELECT "ViewCount" AS "Value" FROM "Questions" ORDER BY "Id" """)
            .ToListAsync();
        Assert.Equal([5, 1, 0], counts);
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
