using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Domain.Common;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Questions stored before texts became bilingual keep their text, as the Slovak one, without an English one.</summary>
public class EnglishQuestionTextsMigrationTests : IDisposable
{
    private const string AddEnglishCategoryNames = "20261008083248_AddEnglishCategoryNames";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public EnglishQuestionTextsMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(AddEnglishCategoryNames);

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "QuestionCategories" ("Id", "NameSk", "NameSkKey", "NameEn", "NameEnKey")
            VALUES (5, 'Hračky', 'hračky', 'Toys', 'toys');
            INSERT INTO "Questions" ("Text", "Answer", "CategoryId", "BadPoints", "Source", "CreatedAt", "ViewCount", "Version")
            VALUES ('V ktorom roku bola založená spoločnosť LEGO?', '1932', 5, 3, 'Ai', '2026-10-01 18:00:00', 2, 4);
            """);
    }

    [Fact]
    public async Task Upgrade_KeepsTheTextAsTheSlovakOneWithoutAnEnglishOne()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var question = await context.Questions.SingleAsync();

        Assert.Equal("V ktorom roku bola založená spoločnosť LEGO?", question.TextSk);
        Assert.Null(question.TextEn);
        Assert.Equal(question.TextSk, question.TextIn(Language.En));
        Assert.Equal(("1932", 2, 4L), (question.Answer, question.ViewCount, question.Version));
    }

    private ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
