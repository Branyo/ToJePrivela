using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class QuestionCategoryRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Theory]
    [InlineData("Sport")]
    [InlineData("sport")]
    [InlineData(" SPORT ")]
    public async Task GetByNameAsync_IgnoresCaseAndSurroundingSpace(string name)
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        var category = await sut.GetByNameAsync(name);

        Assert.NotNull(category);
        Assert.Equal("Sport", category!.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNullForUnknownCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        Assert.Null(await sut.GetByNameAsync("Aliens"));
    }

    [Fact]
    public async Task GetByIdAsync_LoadsTheAuthor()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        var category = await sut.GetByIdAsync(1);

        Assert.NotNull(category!.AddedByPlayer);
        Assert.Equal("Admin", category.AddedByPlayer!.Name);
    }

    [Fact]
    public async Task GetAllAsync_LoadsTheAuthorOfEveryCategory()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        var categories = await sut.GetAllAsync();

        Assert.Equal(3, categories.Count);
        Assert.All(categories, category => Assert.NotNull(category.AddedByPlayer));
    }

    [Fact]
    public async Task AddAsync_StoresACategoryWithoutAuthor()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionCategoryRepository(context);

        await sut.AddAsync(new QuestionCategory("Music"));
        await context.SaveChangesAsync();

        var stored = await sut.GetByNameAsync("Music");
        Assert.NotNull(stored);
        Assert.Null(stored!.AddedByPlayerId);
    }

    public void Dispose() => _database.Dispose();
}
