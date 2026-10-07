using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Logins stored before security stamps existed must each get a stamp of their own.</summary>
public class AccountSecurityStampMigrationTests : IDisposable
{
    private const string OwnPlayersAndGamesByAccount = "20261006062953_OwnPlayersAndGamesByAccount";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public AccountSecurityStampMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(OwnPlayersAndGamesByAccount);

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Accounts" ("Id", "Name", "NameKey", "PasswordHash", "IsAdmin", "CreatedAt")
            VALUES (2, 'Brano', 'brano', 'hash-2', 1, '2026-10-06 18:00:00'),
                   (3, 'Duri', 'duri', 'hash-3', 0, '2026-10-06 18:00:00');
            """);
    }

    [Fact]
    public async Task Upgrade_GivesEveryLoginItsOwnRandomStamp()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var stamps = await context.Accounts.Select(account => account.SecurityStamp).ToListAsync();

        Assert.Equal(3, stamps.Count);
        Assert.All(stamps, stamp => Assert.Matches($"^[0-9a-f]{{{Account.SecurityStampLength}}}$", stamp));
        Assert.Equal(stamps.Count, stamps.Distinct().Count());
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
