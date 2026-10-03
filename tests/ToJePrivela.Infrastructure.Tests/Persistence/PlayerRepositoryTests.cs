using Microsoft.EntityFrameworkCore;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class PlayerRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Theory]
    [InlineData("Brano")]
    [InlineData("brano")]
    [InlineData("BRANO")]
    public async Task GetByNameAsync_IgnoresCase(string name)
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        var player = await sut.GetByNameAsync(TestAccountId, name);

        Assert.NotNull(player);
        Assert.Equal("Brano", player!.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNullForUnknownPlayer()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        Assert.Null(await sut.GetByNameAsync(TestAccountId, "Nobody"));
    }

    [Fact]
    public async Task GetExistingIdsAsync_ReturnsOnlyKnownIds()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        var ids = await sut.GetExistingIdsAsync(TestAccountId, [1, 3, 99]);

        Assert.Equal([1, 3], ids.OrderBy(id => id));
    }

    [Fact]
    public async Task GetExistingIdsAsync_ReturnsNothingForAnEmptyRequest()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        Assert.Empty(await sut.GetExistingIdsAsync(TestAccountId, []));
    }

    [Fact]
    public async Task AddAsync_StoresThePlayer()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        await sut.AddAsync(new Player(TestAccountId, "Jozo", "🦊"));
        await context.SaveChangesAsync();

        await using var verification = _database.CreateContext();
        Assert.NotNull(await verification.Players.FirstOrDefaultAsync(p => p.Name == "Jozo"));
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateNameRegardlessOfCase()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        await sut.AddAsync(new Player(TestAccountId, "brano", "🦊"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Remove_DeletesThePlayer()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        var player = await sut.GetByIdAsync(TestAccountId, 3);
        sut.Remove(player!);
        await context.SaveChangesAsync();

        Assert.Null(await sut.GetByIdAsync(TestAccountId, 3));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEverySeededPlayer()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        Assert.Equal(3, (await sut.GetAllAsync(TestAccountId)).Count);
    }

    [Fact]
    public async Task GetAvatarsAsync_ReturnsOneAvatarPerPlayer()
    {
        await using var context = _database.CreateContext();

        var sut = new PlayerRepository(context);
        var players = await sut.GetAllAsync(TestAccountId);

        var avatars = await sut.GetAvatarsAsync(TestAccountId);

        Assert.Equal(players.Select(p => p.Avatar).Order(), avatars.Order());
    }

    [Theory]
    [InlineData("Štefan")]
    [InlineData("štefan")]
    [InlineData("ŠTEFAN")]
    public async Task GetByNameAsync_IgnoresCaseOfAccentedLetters(string name)
    {
        await using (var context = _database.CreateContext())
        {
            await new PlayerRepository(context).AddAsync(new Player(TestAccountId, "Štefan", "🦊"));
            await context.SaveChangesAsync();
        }

        await using var lookup = _database.CreateContext();
        var found = await new PlayerRepository(lookup).GetByNameAsync(TestAccountId, name);

        Assert.NotNull(found);
        Assert.Equal("Štefan", found!.Name);
    }

    [Fact]
    public async Task SaveChanges_RejectsANameDifferingOnlyInTheCaseOfAnAccentedLetter()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        await sut.AddAsync(new Player(TestAccountId, "Štefan", "🦊"));
        await context.SaveChangesAsync();
        await sut.AddAsync(new Player(TestAccountId, "štefan", "🦊"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Remove_KeepsThePlayersSeatsAsUnknownPlayers()
    {
        await using (var context = _database.CreateContext())
        {
            context.Games.Add(new Game(TestAccountId, [1, 2], new DateTime(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc)));
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var sut = new PlayerRepository(context);
            sut.Remove((await sut.GetByIdAsync(TestAccountId, 2))!);
            await context.SaveChangesAsync();
        }

        await using var verification = _database.CreateContext();
        var seats = await verification.GamePlayers.OrderBy(gp => gp.Id).ToListAsync();
        Assert.Equal([(int?)1, null], seats.Select(gp => gp.PlayerId));
    }

    [Fact]
    public async Task Reads_NeverReachThePlayersOfAnotherAccount()
    {
        var otherAccountId = await AddAccountAsync();
        int strangerId;

        await using (var context = _database.CreateContext())
        {
            var stranger = new Player(otherAccountId, "Stranger", "🦊");
            context.Players.Add(stranger);
            await context.SaveChangesAsync();
            strangerId = stranger.Id;
        }

        await using var lookup = _database.CreateContext();
        var sut = new PlayerRepository(lookup);

        Assert.Null(await sut.GetByIdAsync(TestAccountId, strangerId));
        Assert.Null(await sut.GetByNameAsync(TestAccountId, "Stranger"));
        Assert.DoesNotContain(await sut.GetAllAsync(TestAccountId), p => p.Id == strangerId);
        Assert.Empty(await sut.GetExistingIdsAsync(TestAccountId, [strangerId]));
        Assert.Equal(["🦊"], await sut.GetAvatarsAsync(otherAccountId));
        Assert.NotNull(await sut.GetByIdAsync(otherAccountId, strangerId));
    }

    [Fact]
    public async Task SaveChanges_AllowsTheSameNameUnderAnotherAccount()
    {
        var otherAccountId = await AddAccountAsync();

        await using var context = _database.CreateContext();
        context.Players.Add(new Player(otherAccountId, "Brano", "🦊"));
        await context.SaveChangesAsync();

        await using var lookup = _database.CreateContext();
        Assert.Equal(2, await lookup.Players.CountAsync(p => p.NameKey == "brano"));
    }

    private async Task<int> AddAccountAsync()
    {
        await using var context = _database.CreateContext();
        var account = Account.Register(IdentityProvider.Google, $"sub-{Guid.NewGuid():N}", null, "Other", DateTime.UtcNow);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        return account.Id;
    }

    public void Dispose() => _database.Dispose();
}
