using Microsoft.EntityFrameworkCore;
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

        var category = await sut.GetByNameAsync(name);

        Assert.NotNull(category);
        Assert.Equal("Sport", category!.Name);
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

        Assert.Null(await sut.GetByNameAsync("Aliens"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        var categories = await sut.GetAllAsync();

        Assert.Equal(["Cars", "Sport", "History"], categories.OrderBy(c => c.Id).Select(c => c.Name));
    }

    [Fact]
    public async Task AddAsync_StoresTheCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        await sut.AddAsync(new QuestionCategory("Music"));
        await context.SaveChangesAsync();

        var stored = await sut.GetByNameAsync("Music");
        Assert.NotNull(stored);
        Assert.Equal("music", stored!.NameKey);
    }

    [Theory]
    [InlineData("Šport")]
    [InlineData("šport")]
    [InlineData("ŠPORT")]
    public async Task GetByNameAsync_IgnoresCaseOfAccentedLetters(string name)
    {
        await using (var context = _database.CreateContext())
        {
            await new QuestionCategoryRepository(context).AddAsync(new QuestionCategory("Šport"));
            await context.SaveChangesAsync();
        }

        await using var lookup = _database.CreateContext();
        var found = await new QuestionCategoryRepository(lookup).GetByNameAsync(name);

        Assert.NotNull(found);
        Assert.Equal("Šport", found!.Name);
    }

    [Fact]
    public async Task SaveChanges_RejectsANameDifferingOnlyInTheCaseOfAnAccentedLetter()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        await sut.AddAsync(new QuestionCategory("Šport"));
        await context.SaveChangesAsync();
        await sut.AddAsync(new QuestionCategory("šport"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    public void Dispose() => _database.Dispose();
}
