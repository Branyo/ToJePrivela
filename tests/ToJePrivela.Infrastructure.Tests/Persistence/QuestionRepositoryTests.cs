using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class QuestionRepositoryTests : IDisposable
{
    private const int CarsId = 1;
    private const int SportId = 2;
    private const int HistoryId = 3;

    private static readonly DateTime CreatedAt = new(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public QuestionRepositoryTests()
    {
        using var context = _database.CreateContext();

        var sport = context.QuestionCategories.Find(SportId)!;
        var history = context.QuestionCategories.Find(HistoryId)!;

        context.Questions.AddRange(
            new Question("Which year was ChatGPT publicly released?", "2022", history, 3, QuestionSource.Manual, CreatedAt),
            new Question("In which year did the Berlin Wall fall?", "1989", history, 5, QuestionSource.Ai, CreatedAt),
            new Question("How many players are on a football pitch?", "11", sport, 3, QuestionSource.Ai, CreatedAt));

        context.SaveChanges();
    }

    [Fact]
    public async Task FindAsync_WithoutFilterReturnsEverythingWithItsCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var questions = await sut.FindAsync(null, null);

        Assert.Equal(3, questions.Count);
        Assert.All(questions, question => Assert.NotNull(question.Category));
    }

    [Fact]
    public async Task FindAsync_FiltersByCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var questions = await sut.FindAsync(HistoryId, null);

        Assert.Equal(2, questions.Count);
        Assert.All(questions, question => Assert.Equal("History", question.Category!.Name));
    }

    [Fact]
    public async Task FindAsync_FiltersBySource()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var questions = await sut.FindAsync(null, QuestionSource.Ai);

        Assert.Equal(2, questions.Count);
        Assert.All(questions, question => Assert.Equal(QuestionSource.Ai, question.Source));
    }

    [Fact]
    public async Task FindAsync_CombinesBothFilters()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var question = Assert.Single(await sut.FindAsync(HistoryId, QuestionSource.Ai));

        Assert.Equal("1989", question.Answer);
    }

    [Fact]
    public async Task FindAsync_ReturnsNothingWhenNothingMatches()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        Assert.Empty(await sut.FindAsync(CarsId, null));
    }

    [Fact]
    public async Task GetByIdAsync_LoadsTheCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);
        var id = (await context.Questions.FirstAsync(q => q.Answer == "11")).Id;

        var question = await sut.GetByIdAsync(id);

        Assert.Equal("Sport", question!.Category!.Name);
    }

    [Fact]
    public async Task GetTextsAsync_ReturnsTheCategoryTextsNewestFirst()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var texts = await sut.GetTextsAsync(HistoryId);

        Assert.Equal(["In which year did the Berlin Wall fall?", "Which year was ChatGPT publicly released?"], texts);
    }

    [Fact]
    public async Task GetLeastViewedIdsAsync_ReturnsOnlyTheQuestionsWithTheFewestViews()
    {
        await MarkViewedAsync("2022");

        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var ids = await sut.GetLeastViewedIdsAsync([HistoryId]);

        Assert.Equal([await IdOfAsync(context, "1989")], ids);
    }

    [Fact]
    public async Task GetLeastViewedIdsAsync_ConsidersEverySelectedCategory()
    {
        await MarkViewedAsync("2022");
        await MarkViewedAsync("1989");

        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var ids = await sut.GetLeastViewedIdsAsync([HistoryId, SportId]);

        Assert.Equal([await IdOfAsync(context, "11")], ids);
    }

    [Fact]
    public async Task GetLeastViewedIdsAsync_WithoutCategoriesConsidersAllQuestions()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        Assert.Equal(3, (await sut.GetLeastViewedIdsAsync([])).Count);
    }

    [Fact]
    public async Task GetLeastViewedIdsAsync_ReturnsNothingForCategoriesWithoutQuestions()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        Assert.Empty(await sut.GetLeastViewedIdsAsync([CarsId]));
    }

    [Fact]
    public async Task SavingAStaleQuestionIsAConcurrencyConflict()
    {
        await using var stale = _database.CreateContext();
        var staleQuestion = await stale.Questions.FirstAsync(q => q.Answer == "11");

        await MarkViewedAsync("11");

        staleQuestion.MarkViewed(CreatedAt);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => new UnitOfWork(stale).SaveChangesAsync());
    }

    [Fact]
    public async Task ReloadAsync_ReadsTheCurrentValuesSoTheNextSaveSucceeds()
    {
        await using var stale = _database.CreateContext();
        var sut = new QuestionRepository(stale);
        var question = await stale.Questions.FirstAsync(q => q.Answer == "11");

        await MarkViewedAsync("11");
        question.MarkViewed(CreatedAt);

        Assert.True(await sut.ReloadAsync(question));
        Assert.Equal(1, question.ViewCount);

        question.MarkViewed(CreatedAt);
        await new UnitOfWork(stale).SaveChangesAsync();

        await using var verification = _database.CreateContext();
        Assert.Equal(2, (await verification.Questions.FirstAsync(q => q.Answer == "11")).ViewCount);
    }

    [Fact]
    public async Task ReloadAsync_ReportsADeletedQuestion()
    {
        await using var stale = _database.CreateContext();
        var sut = new QuestionRepository(stale);
        var question = await stale.Questions.FirstAsync(q => q.Answer == "11");

        await using (var other = _database.CreateContext())
        {
            await other.Questions.Where(q => q.Answer == "11").ExecuteDeleteAsync();
        }

        Assert.False(await sut.ReloadAsync(question));
    }

    [Fact]
    public async Task AddRangeAsync_StoresEveryQuestion()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);
        var cars = (await context.QuestionCategories.FindAsync(CarsId))!;

        await sut.AddRangeAsync([
            new Question("How many wheels does a car have?", "4", cars, 1, QuestionSource.Ai, CreatedAt),
            new Question("How many doors does a coupe have?", "2", cars, 2, QuestionSource.Ai, CreatedAt)
        ]);
        await context.SaveChangesAsync();

        Assert.Equal(2, (await sut.FindAsync(CarsId, null)).Count);
    }

    [Fact]
    public async Task RemoveRange_DeletesTheGivenQuestions()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        sut.RemoveRange(await sut.FindAsync(null, QuestionSource.Ai));
        await context.SaveChangesAsync();

        Assert.Equal("2022", Assert.Single(await sut.GetAllAsync()).Answer);
    }

    [Fact]
    public async Task EveryColumnSurvivesARoundTrip()
    {
        await using var context = _database.CreateContext();

        var question = await context.Questions.Include(q => q.Category).FirstAsync(q => q.Answer == "1989");

        Assert.Equal(HistoryId, question.CategoryId);
        Assert.Equal(5, question.BadPoints);
        Assert.Equal(QuestionSource.Ai, question.Source);
        Assert.Equal(CreatedAt, question.CreatedAt);
    }

    [Fact]
    public async Task Source_IsStoredAsText()
    {
        await using var context = _database.CreateContext();

        var sources = await context.Database
            .SqlQueryRaw<string>("""SELECT "Source" AS "Value" FROM "Questions" ORDER BY "Source" """)
            .ToListAsync();

        Assert.Equal(["Ai", "Ai", "Manual"], sources);
    }

    [Fact]
    public async Task DeletingACategoryDeletesItsQuestions()
    {
        await using (var context = _database.CreateContext())
        {
            var categories = new QuestionCategoryRepository(context);
            categories.Remove((await categories.GetByIdAsync(HistoryId))!);
            await new UnitOfWork(context).SaveChangesAsync();
        }

        await using var verification = _database.CreateContext();
        var remaining = await verification.Questions.ToListAsync();
        Assert.Equal("11", Assert.Single(remaining).Answer);
    }

    /// <summary>Views the question in a separate context, as another request would.</summary>
    private async Task MarkViewedAsync(string answer)
    {
        await using var context = _database.CreateContext();
        (await context.Questions.FirstAsync(q => q.Answer == answer)).MarkViewed(CreatedAt);
        await context.SaveChangesAsync();
    }

    private static async Task<int> IdOfAsync(ToJePrivelaDbContext context, string answer) =>
        (await context.Questions.FirstAsync(q => q.Answer == answer)).Id;

    public void Dispose() => _database.Dispose();
}
