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
            new Question("V ktorom roku bol verejne spustený ChatGPT?", "Which year was ChatGPT publicly released?", "2022", history, 3, QuestionSource.Manual, CreatedAt),
            new Question("V ktorom roku padol Berlínsky múr?", "In which year did the Berlin Wall fall?", "1989", history, 5, QuestionSource.Ai, CreatedAt),
            new Question("Koľko hráčov je na futbalovom ihrisku?", "How many players are on a football pitch?", "11", sport, 3, QuestionSource.Ai, CreatedAt));

        context.SaveChanges();
    }

    [Fact]
    public async Task FindAsync_FiltersByCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var questions = await sut.FindAsync(HistoryId, null);

        Assert.Equal(2, questions.Count);
        Assert.All(questions, question => Assert.Equal("History", question.Category!.NameEn));
    }

    [Fact]
    public async Task GetAiQuestionsAsync_ReturnsOnlyTheCategorysAiQuestions()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var question = Assert.Single(await sut.GetAiQuestionsAsync(HistoryId));

        Assert.Equal("1989", question.Answer);
        Assert.Equal(QuestionSource.Ai, question.Source);
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

        Assert.Equal("Sport", question!.Category!.NameEn);
    }

    [Fact]
    public async Task GetTextsAsync_ReturnsTheCategorySlovakTextsNewestFirst()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var texts = await sut.GetTextsAsync(HistoryId);

        Assert.Equal(["V ktorom roku padol Berlínsky múr?", "V ktorom roku bol verejne spustený ChatGPT?"], texts);
    }

    [Fact]
    public async Task GetRandomLeastViewedAsync_TakesOnlyAQuestionWithTheFewestViews()
    {
        await MarkViewedAsync("2022");

        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var question = await sut.GetRandomLeastViewedAsync([HistoryId]);

        Assert.Equal("1989", question!.Answer);
        Assert.Equal("History", question.Category!.NameEn);
    }

    [Fact]
    public async Task GetRandomLeastViewedAsync_ConsidersEverySelectedCategory()
    {
        await MarkViewedAsync("2022");
        await MarkViewedAsync("1989");

        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        Assert.Equal("11", (await sut.GetRandomLeastViewedAsync([HistoryId, SportId]))!.Answer);
    }

    [Fact]
    public async Task GetRandomLeastViewedAsync_WithoutCategoriesChoosesAmongAllQuestions()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);
        var drawn = new HashSet<string>();

        // Three equally viewed questions: fifty draws all miss one of them with a chance of about 1 in 10^8.
        for (var draw = 0; draw < 50; draw++)
        {
            drawn.Add((await sut.GetRandomLeastViewedAsync([]))!.Answer);
        }

        Assert.Equal(3, drawn.Count);
    }

    [Fact]
    public async Task GetRandomLeastViewedAsync_ReturnsNothingForCategoriesWithoutQuestions()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        Assert.Null(await sut.GetRandomLeastViewedAsync([CarsId]));
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
    public async Task DiscardChanges_LetsTheNextReadSeeTheStoredValuesSoTheNextSaveSucceeds()
    {
        await using var stale = _database.CreateContext();
        var sut = new QuestionRepository(stale);
        var unitOfWork = new UnitOfWork(stale);
        var question = (await sut.FindAsync(SportId, null)).Single();

        await MarkViewedAsync("11");
        question.MarkViewed(CreatedAt);

        unitOfWork.DiscardChanges();
        var current = (await sut.GetByIdAsync(question.Id))!;
        Assert.Equal(1, current.ViewCount);

        current.MarkViewed(CreatedAt);
        await unitOfWork.SaveChangesAsync();

        await using var verification = _database.CreateContext();
        Assert.Equal(2, (await verification.Questions.FirstAsync(q => q.Answer == "11")).ViewCount);
    }

    [Fact]
    public async Task AddRangeAsync_StoresEveryQuestion()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);
        var cars = (await context.QuestionCategories.FindAsync(CarsId))!;

        await sut.AddRangeAsync([
            new Question("Koľko kolies má auto?", "How many wheels does a car have?", "4", cars, 1, QuestionSource.Ai, CreatedAt),
            new Question("Koľko dverí má kupé?", "How many doors does a coupe have?", "2", cars, 2, QuestionSource.Ai, CreatedAt)
        ]);
        await context.SaveChangesAsync();

        Assert.Equal(2, (await sut.FindAsync(CarsId, null)).Count);
    }

    [Fact]
    public async Task RemoveRange_DeletesTheGivenQuestions()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        sut.RemoveRange(await context.Questions.Where(q => q.Source == QuestionSource.Ai).ToListAsync());
        await context.SaveChangesAsync();

        Assert.Equal("2022", Assert.Single(await context.Questions.ToListAsync()).Answer);
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
