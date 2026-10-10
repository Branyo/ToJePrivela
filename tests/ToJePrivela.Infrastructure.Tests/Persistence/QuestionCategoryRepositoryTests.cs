using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class QuestionCategoryRepositoryTests : IDisposable
{
    private const int CarsId = 1;
    private const int SportId = 2;
    private const int HistoryId = 3;

    private static readonly DateTime CreatedAt = new(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    [Theory]
    [InlineData("Sport")]
    [InlineData("sport")]
    [InlineData("SPORT")]
    public async Task GetByNameAsync_IgnoresCase(string name)
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        var category = await sut.GetByNameAsync(name, Language.En);

        Assert.NotNull(category);
        Assert.Equal("Sport", category!.NameEn);
    }

    [Fact]
    public async Task GetByNameAsync_ComparesWithTheNameInTheGivenLanguageOnly()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        Assert.Equal(2, (await sut.GetByNameAsync("Šport", Language.Sk))?.Id);
        Assert.Null(await sut.GetByNameAsync("Šport", Language.En));
        Assert.Null(await sut.GetByNameAsync("Sport", Language.Sk));
    }

    [Fact]
    public async Task GetMissingIdsAsync_ReturnsOnlyTheIdsNoCategoryHas()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        Assert.Equal([98, 99], await sut.GetMissingIdsAsync([99, 1, 3, 98]));
        Assert.Empty(await sut.GetMissingIdsAsync([1, 2]));
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNullForUnknownCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        Assert.Null(await sut.GetByNameAsync("Aliens", Language.En));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        var categories = await sut.GetAllAsync();

        Assert.Equal(["Cars", "Sport", "History"], categories.OrderBy(c => c.Id).Select(c => c.NameEn));
    }

    [Fact]
    public async Task GetAllCountedAsync_CountsEachCategorysQuestionsAndItsAiOnes()
    {
        AddQuestions();
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        var categories = await sut.GetAllCountedAsync();

        Assert.Equal(
            [("Cars", QuestionCounts.None), ("Sport", new QuestionCounts(1, 1)), ("History", new QuestionCounts(2, 1))],
            categories.OrderBy(c => c.Category.Id).Select(c => (c.Category.NameEn, c.Counts)));
    }

    [Fact]
    public async Task GetCountedAsync_CountsOnlyTheGivenCategory()
    {
        AddQuestions();
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        var history = await sut.GetCountedAsync(HistoryId);

        Assert.Equal(("History", new QuestionCounts(2, 1)), (history!.Category.NameEn, history.Counts));
        Assert.Equal(QuestionCounts.None, (await sut.GetCountedAsync(CarsId))!.Counts);
        Assert.Null(await sut.GetCountedAsync(99));
    }

    [Fact]
    public async Task AddAsync_StoresTheCategoryInBothLanguages()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        await sut.AddAsync(new QuestionCategory("Hudba", "Music"));
        await context.SaveChangesAsync();

        var stored = await sut.GetByNameAsync("Music", Language.En);
        Assert.NotNull(stored);
        Assert.Equal(("hudba", "music"), (stored!.NameSkKey, stored.NameEnKey));
    }

    [Theory]
    [InlineData("Šport")]
    [InlineData("šport")]
    [InlineData("ŠPORT")]
    public async Task GetByNameAsync_IgnoresCaseOfAccentedLetters(string name)
    {
        await using var lookup = _database.CreateContext();
        var found = await new QuestionCategoryRepository(lookup).GetByNameAsync(name, Language.Sk);

        Assert.NotNull(found);
        Assert.Equal("Šport", found!.NameSk);
    }

    [Fact]
    public async Task SaveChanges_RejectsASlovakNameDifferingOnlyInTheCaseOfAnAccentedLetter()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        await sut.AddAsync(new QuestionCategory("šport", "Athletics"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_RejectsATakenEnglishName()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        await sut.AddAsync(new QuestionCategory("Športy", "SPORT"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    public void Dispose() => _database.Dispose();

    /// <summary>Two questions in History (one by the AI), one AI question in Sport, none in Cars.</summary>
    private void AddQuestions()
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
}
