using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Dropping the seeded categories and renaming the seeded Admin player must never cost a database its data.</summary>
public class RemoveSeededCategoriesMigrationTests : IDisposable
{
    private const string AddAccountSecurityStamp = "20261007070504_AddAccountSecurityStamp";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public RemoveSeededCategoriesMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(AddAccountSecurityStamp);
    }

    [Fact]
    public async Task Upgrade_KeepsASeededCategoryThatHoldsQuestions()
    {
        // Raw SQL: the entities already have the columns of later migrations.
        await using (var before = CreateContext())
        {
            await before.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Questions" ("Text", "Answer", "CategoryId", "BadPoints", "Source", "CreatedAt", "ViewCount", "Version")
                VALUES ('How many players has a football team?', '11', 2, 2, 'Manual', '2026-10-01 18:00:00', 0, 0);
                """);
        }

        await using var context = await MigrateToLatestAsync();

        Assert.Equal(["Sport"], await context.QuestionCategories.Select(c => c.NameSk).ToListAsync());
        Assert.Equal(1, await context.Questions.CountAsync());
    }

    [Fact]
    public async Task Upgrade_RenamesTheSeededAdminPlayerToPeter()
    {
        await using var context = await MigrateToLatestAsync();

        Assert.Equal("Peter", (await context.Players.SingleAsync(p => p.Id == 1)).Name);
    }

    [Fact]
    public async Task Upgrade_LeavesTheAdminPlayerWhenItsLoginAlreadyHasAPeter()
    {
        await using (var before = CreateContext())
        {
            before.Players.Add(new Player(Account.ReservedId, "Peter", PlayerAvatars.All[0]));
            await before.SaveChangesAsync();
        }

        await using var context = await MigrateToLatestAsync();

        Assert.Equal("Admin", (await context.Players.SingleAsync(p => p.Id == 1)).Name);
        Assert.Equal(1, await context.Players.CountAsync(p => p.Name == "Peter"));
    }

    private async Task<ToJePrivelaDbContext> MigrateToLatestAsync()
    {
        var context = CreateContext();
        await context.Database.MigrateAsync();
        return context;
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
