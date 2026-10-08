using Microsoft.EntityFrameworkCore;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class QuestionCategoryRepositoryTests : IDisposable
{
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
}
