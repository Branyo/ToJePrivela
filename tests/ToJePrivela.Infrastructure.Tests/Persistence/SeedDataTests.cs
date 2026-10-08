using Microsoft.EntityFrameworkCore;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class SeedDataTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new(withTestCategories: false);

    [Fact]
    public async Task Migrations_SeedTheDefaultPlayers()
    {
        await using var context = _database.CreateContext();

        var names = await context.Players.OrderBy(p => p.Id).Select(p => p.Name).ToListAsync();

        Assert.Equal(["Peter", "Brano", "Duri"], names);
    }

    [Fact]
    public async Task Migrations_GiveTheDefaultPlayersAvatars()
    {
        await using var context = _database.CreateContext();

        var avatars = await context.Players.Select(p => p.Avatar).ToListAsync();

        Assert.All(avatars, avatar => Assert.Contains(avatar, Domain.Entities.PlayerAvatars.All));
    }

    [Fact]
    public async Task Migrations_SeedNoCategories()
    {
        await using var context = _database.CreateContext();

        Assert.Empty(await context.QuestionCategories.ToListAsync());
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
