using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Players created before avatars existed must each get one from the pool.</summary>
public class PlayerAvatarMigrationTests : IDisposable
{
    private const string TrackDoubles = "20260928121108_TrackDoubles";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public PlayerAvatarMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(TrackDoubles);

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Players" ("Name") VALUES ('Jozo'), ('Fero'), ('Mara'), ('Zuza');
            """);
    }

    [Fact]
    public async Task Upgrade_GivesEveryExistingPlayerAnAvatarFromThePool()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var players = await context.Players.ToListAsync();

        Assert.Equal(7, players.Count);
        Assert.All(players, player => Assert.Contains(player.Avatar, PlayerAvatars.All));
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
