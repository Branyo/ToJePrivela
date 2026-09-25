using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>Questions stored before categories became a foreign key must survive the upgrade.</summary>
public class QuestionMigrationTests : IDisposable
{
    private const string InitialCreate = "20260922162758_InitialCreate";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public QuestionMigrationTests()
    {
        _connection.Open();

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(InitialCreate);

        context.Database.ExecuteSqlRaw("""
            INSERT INTO "Questions" ("Text", "Answer", "Category", "Difficulty") VALUES
                ('Which year was ChatGPT publicly released?', '2022', 'history', 1),
                ('How many strings does a guitar have?', '6', 'Music', 4),
                ('How many keys does a piano have?', '88', 'music', 2);
            """);
    }

    [Fact]
    public async Task Upgrade_LinksQuestionsToExistingCategoriesIgnoringCase()
    {
        await using var context = await MigrateToLatestAsync();

        var question = await context.Questions.Include(q => q.Category).FirstAsync(q => q.Answer == "2022");

        Assert.Equal(3, question.CategoryId);
        Assert.Equal("History", question.Category!.Name);
    }

    [Fact]
    public async Task Upgrade_CreatesOneCategoryForEachUnknownName()
    {
        await using var context = await MigrateToLatestAsync();

        var music = await context.QuestionCategories.SingleAsync(c => c.Name == "Music");
        var musicQuestions = await context.Questions.Where(q => q.CategoryId == music.Id).CountAsync();

        Assert.Equal(4, await context.QuestionCategories.CountAsync());
        Assert.Equal(2, musicQuestions);
    }

    [Fact]
    public async Task Upgrade_KeepsTheFormerBadPointsAndMarksQuestionsManual()
    {
        await using var context = await MigrateToLatestAsync();

        var questions = await context.Questions.OrderBy(q => q.Id).ToListAsync();

        Assert.Equal([5, 2, 4], questions.Select(q => q.BadPoints));
        Assert.All(questions, question =>
        {
            Assert.Equal(QuestionSource.Manual, question.Source);
            Assert.NotEqual(default, question.CreatedAt);
        });
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
