using Microsoft.EntityFrameworkCore;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class GameRepositoryTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task GetWithDetailsAsync_LoadsThePlayersOfTheGame()
    {
        var gameId = await SeedGameAsync([1, 2]);

        await using var context = _database.CreateContext();
        var sut = new GameRepository(context);

        var game = await sut.GetByIdAsync(TestAccountId, gameId);

        Assert.NotNull(game);
        Assert.Equal(["Brano", "Peter"], game!.GamePlayers.Select(gp => gp.Player!.Name).Order());
    }

    [Fact]
    public async Task GetWithDetailsAsync_ReturnsNullForUnknownGame()
    {
        await using var context = _database.CreateContext();
        var sut = new GameRepository(context);

        Assert.Null(await sut.GetByIdAsync(TestAccountId, 404));
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_LoadsEveryGame()
    {
        await SeedGameAsync([1, 2]);
        await SeedGameAsync([2, 3]);

        await using var context = _database.CreateContext();
        var sut = new GameRepository(context);

        var games = await sut.GetAllAsync(TestAccountId);

        Assert.Equal(2, games.Count);
        Assert.All(games, game => Assert.All(game.GamePlayers, gp => Assert.NotNull(gp.Player)));
    }

    [Fact]
    public async Task AddAsync_StoresTheGameWithItsPlayers()
    {
        var gameId = await SeedGameAsync([1, 2, 3]);

        await using var context = _database.CreateContext();

        var stored = await context.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == gameId);
        Assert.Equal(3, stored.GamePlayers.Count);
        Assert.Equal(Start, stored.Started);
        Assert.Null(stored.Finished);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTheTimesMarkedAsUtc()
    {
        var gameId = await SeedGameAsync([1, 2]);

        await using var context = _database.CreateContext();
        var game = (await new GameRepository(context).GetByIdAsync(TestAccountId, gameId))!;

        Assert.Equal(Start, game.Started);
        Assert.Equal(DateTimeKind.Utc, game.Started!.Value.Kind);
    }

    [Fact]
    public async Task SaveChanges_StoresTheDoublesOfAPlayer()
    {
        var gameId = await SeedGameAsync([1, 2]);

        await using (var context = _database.CreateContext())
        {
            var game = (await new GameRepository(context).GetByIdAsync(TestAccountId, gameId))!;
            game.AwardDouble(2);
            game.AwardDouble(2);
            await context.SaveChangesAsync();
        }

        await using var verification = _database.CreateContext();
        var stored = await verification.GamePlayers.SingleAsync(gp => gp.GameId == gameId && gp.PlayerId == 2);
        Assert.Equal(2, stored.Doubles);
        Assert.Equal(-2, stored.FinalBadPoints);
    }

    [Fact]
    public async Task Remove_AlsoDeletesTheGamePlayers()
    {
        var gameId = await SeedGameAsync([1, 2]);

        await using var context = _database.CreateContext();
        var sut = new GameRepository(context);

        sut.Remove((await sut.GetByIdAsync(TestAccountId, gameId))!);
        await context.SaveChangesAsync();

        await using var verification = _database.CreateContext();
        Assert.Empty(await verification.GamePlayers.Where(gp => gp.GameId == gameId).ToListAsync());
        Assert.Equal(3, await verification.Players.CountAsync());
    }

    [Fact]
    public async Task Finish_IsPersisted()
    {
        var gameId = await SeedGameAsync([1, 2]);
        var finished = Start.AddHours(2);

        await using (var context = _database.CreateContext())
        {
            var game = await context.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == gameId);
            WithABadCard(game, 2).Finish(finished);
            await context.SaveChangesAsync();
        }

        await using var verification = _database.CreateContext();
        var stored = await verification.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == gameId);
        Assert.Equal(finished, stored.Finished);
        Assert.Equal([2], stored.GamePlayers.Where(gp => gp.IsLoser).Select(gp => gp.PlayerId!.Value));
    }

    [Fact]
    public async Task GetCountedByPlayerAsync_ReadsOnlyThePlayersFinishedNotCancelledGamesUntracked()
    {
        var finished = await SeedGameAsync([1, 2]);
        await SeedGameAsync([1, 2]); // still running
        var cancelled = await SeedGameAsync([1, 2, 3]);
        var withoutPlayer = await SeedGameAsync([2, 3]);

        await using (var context = _database.CreateContext())
        {
            WithABadCard(await context.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == finished), 2).Finish(Start.AddHours(1));
            WithABadCard(await context.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == withoutPlayer), 2).Finish(Start.AddHours(1));
            var toCancel = await context.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == cancelled);
            toCancel.ForgetPlayer(3, Start.AddHours(1));
            await context.SaveChangesAsync();
        }

        await using var lookup = _database.CreateContext();
        var sut = new GameRepository(lookup);

        var games = await sut.GetCountedByPlayerAsync(TestAccountId, 1);

        Assert.Equal([finished], games.Select(g => g.Id));
        Assert.Equal(2, games[0].GamePlayers.Count);
        Assert.All(games[0].GamePlayers, gp => Assert.Null(gp.Player));
        Assert.Empty(lookup.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Reads_NeverReachTheGamesOfAnotherAccount()
    {
        int otherAccountId;

        await using (var context = _database.CreateContext())
        {
            var account = Account.Register("Other", "hash", Start);
            context.Accounts.Add(account);
            await context.SaveChangesAsync();
            otherAccountId = account.Id;
        }

        var mine = await SeedGameAsync([1, 2]);
        var theirs = await SeedGameAsync([1, 2], otherAccountId);

        await using var lookup = _database.CreateContext();
        var sut = new GameRepository(lookup);

        Assert.Null(await sut.GetByIdAsync(TestAccountId, theirs));
        Assert.Equal([mine], (await sut.GetAllAsync(TestAccountId)).Select(g => g.Id));
        Assert.Equal([mine], (await sut.GetByPlayerAsync(TestAccountId, 1)).Select(g => g.Id));
        Assert.Equal([theirs], (await sut.GetAllAsync(otherAccountId)).Select(g => g.Id));
    }

    private static Game WithABadCard(Game game, int playerId)
    {
        var question = new Question("Koľko kolies má auto?", "How many wheels does a car have?", "4", new QuestionCategory("Autá", "Cars"), 3, QuestionSource.Manual, Start);
        game.AwardBadCard(playerId, question, null, Start.AddMinutes(1));
        return game;
    }

    private async Task<int> SeedGameAsync(int[] playerIds, int accountId = TestAccountId)
    {
        await using var context = _database.CreateContext();

        var game = new Game(accountId, playerIds, Start);
        context.Games.Add(game);
        await context.SaveChangesAsync();

        return game.Id;
    }

    public void Dispose() => _database.Dispose();
}
