using Microsoft.EntityFrameworkCore;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class SeedDataTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task Migrations_SeedTheDefaultPlayers()
    {
        await using var context = _database.CreateContext();

        var names = await context.Players.OrderBy(p => p.Id).Select(p => p.Name).ToListAsync();

        Assert.Equal(["Admin", "Brano", "Duri"], names);
    }

    [Fact]
    public async Task Migrations_SeedTheDefaultCategories()
    {
        await using var context = _database.CreateContext();

        var categories = await context.QuestionCategories.OrderBy(c => c.Id).ToListAsync();

        Assert.Equal(["Cars", "Sport", "History"], categories.Select(c => c.Name));
        Assert.All(categories, category => Assert.Equal(1, category.AddedByPlayerId));
    }

    [Fact]
    public async Task Migrations_LeaveGamesAndQuestionsEmpty()
    {
        await using var context = _database.CreateContext();

        Assert.Empty(await context.Games.ToListAsync());
        Assert.Empty(await context.Questions.ToListAsync());
    }

    public void Dispose() => _database.Dispose();
}
