using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Domain.Common;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Names stored before the Unicode-aware key existed must each get the key the domain would compute.</summary>
public class NameKeyMigrationTests : IDisposable
{
    private const string AddBadPointsMode = "20260929200107_AddBadPointsMode";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public NameKeyMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(AddBadPointsMode);

        // Admin was renamed after seeding; Štefan and štefan were both accepted by the ASCII-only NOCASE index.
        context.Database.ExecuteSqlRaw("""
            UPDATE "Players" SET "Name" = 'Ľubomír' WHERE "Id" = 1;
            INSERT INTO "Players" ("Name", "Avatar") VALUES ('Štefan', '🦊'), ('štefan', '🐸'), ('ÁRPÁD Žigo', '🐼');
            INSERT INTO "QuestionCategories" ("Name") VALUES ('Šport'), ('šport');
            """);
    }

    [Fact]
    public async Task Upgrade_GivesEveryRowTheKeyOfItsStoredName()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var players = await context.Players.ToListAsync();
        var categories = await context.QuestionCategories.ToListAsync();

        Assert.All(players, player => Assert.Equal(NameKeys.Of(player.Name), player.NameKey));
        Assert.All(categories, category => Assert.Equal(NameKeys.Of(category.NameSk), category.NameSkKey));
        Assert.Contains(players, player => player.Name == "Ľubomír" && player.NameKey == "ľubomír");
        Assert.Contains(players, player => player.NameKey == "árpád žigo");
    }

    [Fact]
    public async Task Upgrade_RenamesTheNewerOfTwoNamesThatNowCollide()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var stefans = await context.Players.Where(p => p.Name.StartsWith("Š") || p.Name.StartsWith("š")).OrderBy(p => p.Id).ToListAsync();
        var sport = await context.QuestionCategories.Where(c => c.Id > 3).OrderBy(c => c.Id).ToListAsync();

        Assert.Equal("Štefan", stefans[0].Name);
        Assert.Equal($"štefan #{stefans[1].Id}", stefans[1].Name);
        Assert.Equal("Šport", sport[0].NameSk);
        Assert.Equal($"šport #{sport[1].Id}", sport[1].NameSk);
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
