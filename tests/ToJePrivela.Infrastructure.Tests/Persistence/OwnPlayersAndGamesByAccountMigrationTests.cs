using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Players and games stored before sign-in existed must all end up with the reserved account.</summary>
public class OwnPlayersAndGamesByAccountMigrationTests : IDisposable
{
    private const string AddAccounts = "20261003181451_AddAccounts";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public OwnPlayersAndGamesByAccountMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(AddAccounts);

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Players" ("Id", "Name", "NameKey", "Avatar") VALUES (10, 'Jozo', 'jozo', '🦊');
            INSERT INTO "Games" ("Id", "Started", "Finished", "BadCardLimit", "BadPointsMode", "IsCancelled")
            VALUES (1, '2026-09-30 18:00:00', NULL, 3, 'Question', 0);
            INSERT INTO "GamePlayers" ("GameId", "PlayerId", "BadPoints", "BadCards", "Doubles")
            VALUES (1, 2, 4, 1, 0), (1, 10, 0, 0, 1);
            """);
    }

    [Fact]
    public async Task Upgrade_GivesEveryExistingPlayerAndGameToTheReservedAccount()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        Assert.All(await context.Players.ToListAsync(), player => Assert.Equal(Account.ReservedId, player.AccountId));
        Assert.All(await context.Games.ToListAsync(), game => Assert.Equal(Account.ReservedId, game.AccountId));
        Assert.Equal(4, await context.Players.CountAsync());
        Assert.Equal(2, await context.GamePlayers.CountAsync());
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
