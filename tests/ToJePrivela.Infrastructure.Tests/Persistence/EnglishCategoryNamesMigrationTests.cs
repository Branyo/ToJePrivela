using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Domain.Common;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Every category stored before names became bilingual gets an English name, and keeps its questions.</summary>
public class EnglishCategoryNamesMigrationTests : IDisposable
{
    private const string RemoveSeededCategories = "20261008073919_RemoveSeededCategoriesAndRenameAdminPlayer";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public EnglishCategoryNamesMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(RemoveSeededCategories);

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "QuestionCategories" ("Id", "Name", "NameKey") VALUES
                (5, 'Hračky', 'hračky'),
                (6, 'Herci bojových umení', 'herci bojových umení'),
                (7, 'Vtáky', 'vtáky'),
                (8, 'Vlaky', 'vlaky'),
                (9, 'Hollywood', 'hollywood'),
                (10, 'Šachy', 'šachy'),
                (11, 'Toys', 'toys');
            INSERT INTO "Questions" ("Text", "Answer", "CategoryId", "BadPoints", "Source", "CreatedAt", "ViewCount", "Version")
            VALUES ('V ktorom roku bola založená spoločnosť LEGO?', '1932', 5, 3, 'Ai', '2026-10-01 18:00:00', 0, 0);
            """);
    }

    [Fact]
    public async Task Upgrade_GivesTheCategoriesInUseTheirEnglishNames()
    {
        await using var context = await MigrateToLatestAsync();

        var names = await context.QuestionCategories.Where(c => c.Id <= 9).OrderBy(c => c.Id)
            .Select(c => new { c.NameSk, c.NameEn }).ToListAsync();

        Assert.Equal(
            [
                ("Hračky", "Toys"),
                ("Herci bojových umení", "Martial arts actors"),
                ("Vtáky", "Birds"),
                ("Vlaky", "Trains"),
                ("Hollywood", "Hollywood")
            ],
            names.Select(n => (n.NameSk, n.NameEn)));
    }

    [Fact]
    public async Task Upgrade_GivesAnyOtherCategoryItsSlovakNameInEnglish()
    {
        await using var context = await MigrateToLatestAsync();

        var chess = await context.QuestionCategories.SingleAsync(c => c.Id == 10);

        Assert.Equal(("Šachy", "šachy"), (chess.NameEn, chess.NameEnKey));
    }

    [Fact]
    public async Task Upgrade_KeepsEnglishNamesUnique()
    {
        await using var context = await MigrateToLatestAsync();

        var toys = await context.QuestionCategories.SingleAsync(c => c.Id == 11);

        Assert.Equal(("Toys #11", "toys #11"), (toys.NameEn, toys.NameEnKey));
    }

    [Fact]
    public async Task Upgrade_KeepsEveryKeyEqualToItsName()
    {
        await using var context = await MigrateToLatestAsync();

        Assert.All(await context.QuestionCategories.ToListAsync(), category =>
        {
            Assert.Equal(NameKeys.Of(category.NameSk), category.NameSkKey);
            Assert.Equal(NameKeys.Of(category.NameEn), category.NameEnKey);
        });
    }

    [Fact]
    public async Task Upgrade_KeepsTheQuestions()
    {
        await using var context = await MigrateToLatestAsync();

        Assert.Equal(5, (await context.Questions.SingleAsync()).CategoryId);
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
