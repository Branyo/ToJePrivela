using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Games created before the bad points choice existed keep taking them from the question.</summary>
public class BadPointsModeMigrationTests : IDisposable
{
    private const string AssignPlayerAvatars = "20260928140407_AssignPlayerAvatars";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public BadPointsModeMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(AssignPlayerAvatars);

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Games" ("Started", "BadCardLimit") VALUES ('2026-09-27 18:00:00', 3);
            """);
    }

    [Fact]
    public async Task Upgrade_LeavesExistingGamesOnTheQuestionsBadPoints()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var game = await context.Games.SingleAsync();

        Assert.Equal(BadPointsMode.Question, game.BadPointsMode);
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
