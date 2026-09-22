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
    [InlineData("  Brano  ")]
    public async Task GetByNameAsync_IgnoresCaseAndSurroundingSpace(string name)
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        var player = await sut.GetByNameAsync(name);

        Assert.NotNull(player);
        Assert.Equal("Brano", player!.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNullForUnknownPlayer()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        Assert.Null(await sut.GetByNameAsync("Nobody"));
    }

    [Fact]
    public async Task GetExistingIdsAsync_ReturnsOnlyKnownIds()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        var ids = await sut.GetExistingIdsAsync([1, 3, 99]);

        Assert.Equal([1, 3], ids.OrderBy(id => id));
    }

    [Fact]
    public async Task GetExistingIdsAsync_ReturnsNothingForAnEmptyRequest()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        Assert.Empty(await sut.GetExistingIdsAsync([]));
    }

    [Fact]
    public async Task AddAsync_StoresThePlayer()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        await sut.AddAsync(new Player("Jozo"));
        await context.SaveChangesAsync();

        await using var verification = _database.CreateContext();
        Assert.NotNull(await verification.Players.FirstOrDefaultAsync(p => p.Name == "Jozo"));
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateNameRegardlessOfCase()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        await sut.AddAsync(new Player("brano"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Remove_DeletesThePlayer()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        var player = await sut.GetByIdAsync(3);
        sut.Remove(player!);
        await context.SaveChangesAsync();

        Assert.False(await sut.ExistsAsync(3));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEverySeededPlayer()
    {
        await using var context = _database.CreateContext();
        var sut = new PlayerRepository(context);

        Assert.Equal(3, (await sut.GetAllAsync()).Count);
    }

    public void Dispose() => _database.Dispose();
}
