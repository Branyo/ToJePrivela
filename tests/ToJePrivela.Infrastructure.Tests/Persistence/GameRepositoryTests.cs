using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;
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
    public async Task GetCountedByPlayerAsync_ReadsOnlyThePlayersWholeGamesWithALoserUntracked()
    {
        var finished = await SeedGameAsync([1, 2]);
        await SeedGameAsync([1, 2]); // still running
        var withoutLoser = await SeedGameAsync([1, 2]);
        var cancelled = await SeedGameAsync([1, 2, 3]);
        var withoutPlayer = await SeedGameAsync([2, 3]);

        await using (var context = _database.CreateContext())
        {
            WithABadCard(await context.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == finished), 2).Finish(Start.AddHours(1));
            WithABadCard(await context.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == withoutPlayer), 2).Finish(Start.AddHours(1));
            var toCancel = await context.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == cancelled);
            toCancel.ForgetPlayer(3, Start.AddHours(1));
            await context.SaveChangesAsync();

            // Ended before anybody held a bad card, as games could be before Game.NoBadCardsYet: nobody lost it.
            await context.Database.ExecuteSqlAsync(
                $"UPDATE \"Games\" SET \"Finished\" = {Start.AddHours(1)} WHERE \"Id\" = {withoutLoser}");
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
    public async Task GetRecentAsync_ListsNewestFirstAndFiltersByState()
    {
        var oldest = await SeedGameAsync([1, 2], started: Start.AddDays(-2));
        var finished = await SeedGameAsync([1, 2], started: Start.AddDays(-1));
        var running = await SeedGameAsync([2, 3]);
        var sameStartLater = await SeedGameAsync([1, 3]);

        await using (var context = _database.CreateContext())
        {
            var game = await context.Games.Include(g => g.GamePlayers).FirstAsync(g => g.Id == finished);
            WithABadCard(game, 1).Finish(Start);
            await context.SaveChangesAsync();
        }

        await using var lookup = _database.CreateContext();
        var sut = new GameRepository(lookup);

        async Task<int[]> Ids(GameHistoryStatus status, GamePosition? after = null, int take = 10) =>
            (await sut.GetRecentAsync(TestAccountId, status, after, take)).Select(g => g.Id).ToArray();

        Assert.Equal([sameStartLater, running, finished, oldest], await Ids(GameHistoryStatus.All));
        Assert.Equal([sameStartLater, running, oldest], await Ids(GameHistoryStatus.Running));
        Assert.Equal([finished], await Ids(GameHistoryStatus.Finished));
        Assert.Equal([sameStartLater, running], await Ids(GameHistoryStatus.All, take: 2));
        Assert.Equal([running, finished], await Ids(GameHistoryStatus.All, new GamePosition(Start, sameStartLater), take: 2));
        Assert.Equal([oldest], await Ids(GameHistoryStatus.All, new GamePosition(Start.AddDays(-1), finished)));
    }

    [Fact]
    public async Task GetRecentAsync_ContinuesAfterTheLastListedGameWhateverChangedBefore()
    {
        var older = await SeedGameAsync([1, 2], started: Start.AddDays(-1));
        var listed = await SeedGameAsync([1, 2]);

        await using var lookup = _database.CreateContext();
        var sut = new GameRepository(lookup);
        var firstPage = await sut.GetRecentAsync(TestAccountId, GameHistoryStatus.All, null, 1);

        // A game started on another device before the next page moves every game down one place.
        await SeedGameAsync([2, 3], started: Start.AddHours(1));
        var last = firstPage[^1];
        var nextPage = await sut.GetRecentAsync(TestAccountId, GameHistoryStatus.All, new GamePosition(last.Started, last.Id), 1);

        Assert.Equal([listed], firstPage.Select(g => g.Id));
        Assert.Equal([older], nextPage.Select(g => g.Id));
    }

    [Fact]
    public async Task GetRecentAsync_ListsGamesWithoutAStartLast()
    {
        var withoutStart = await SeedGameAsync([1, 2]);
        var withoutStartToo = await SeedGameAsync([1, 2]);
        var started = await SeedGameAsync([1, 2], started: Start.AddDays(-5));

        await using (var context = _database.CreateContext())
        {
            await context.Database.ExecuteSqlAsync(
                $"UPDATE \"Games\" SET \"Started\" = NULL WHERE \"Id\" IN ({withoutStart}, {withoutStartToo})");
        }

        await using var lookup = _database.CreateContext();
        var sut = new GameRepository(lookup);

        Assert.Equal(
            [started, withoutStartToo, withoutStart],
            (await sut.GetRecentAsync(TestAccountId, GameHistoryStatus.All, null, 10)).Select(g => g.Id));
        Assert.Equal(
            [withoutStartToo, withoutStart],
            (await sut.GetRecentAsync(TestAccountId, GameHistoryStatus.All, new GamePosition(Start.AddDays(-5), started), 10)).Select(g => g.Id));
        Assert.Equal(
            [withoutStart],
            (await sut.GetRecentAsync(TestAccountId, GameHistoryStatus.All, new GamePosition(null, withoutStartToo), 10)).Select(g => g.Id));
    }

    [Fact]
    public async Task GetRecentAsync_LoadsWholeGamesUntracked()
    {
        await SeedGameAsync([1, 2, 3]);

        await using var lookup = _database.CreateContext();
        var game = Assert.Single(await new GameRepository(lookup).GetRecentAsync(TestAccountId, GameHistoryStatus.All, null, 10));

        Assert.Equal(3, game.GamePlayers.Count);
        Assert.All(game.GamePlayers, gp => Assert.NotNull(gp.Player));
        Assert.Empty(lookup.ChangeTracker.Entries());
    }

    [Fact]
    public async Task SavingADoubleOnAGameFinishedMeanwhileIsAConcurrencyConflict()
    {
        var gameId = await SeedGameAsync([1, 2]);

        await using var stale = _database.CreateContext();
        var staleGame = (await new GameRepository(stale).GetByIdAsync(TestAccountId, gameId))!;

        await using (var finishing = _database.CreateContext())
        {
            var game = (await new GameRepository(finishing).GetByIdAsync(TestAccountId, gameId))!;
            WithABadCard(game, 1).Finish(Start.AddHours(1));
            await new UnitOfWork(finishing).SaveChangesAsync();
        }

        // The double passed its check on the game as it was read; saved, it would leave the stored loser stale.
        staleGame.AwardDouble(1);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => new UnitOfWork(stale).SaveChangesAsync());

        await using var verification = _database.CreateContext();
        var stored = await verification.GamePlayers.SingleAsync(gp => gp.GameId == gameId && gp.PlayerId == 1);
        Assert.Equal((0, true), (stored.Doubles, stored.IsLoser));
    }

    [Fact]
    public async Task EverySaveThatChangesTheGameOrAPlayerBumpsTheVersionOnce()
    {
        var gameId = await SeedGameAsync([1, 2, 3]);
        var versions = new List<long> { await VersionAsync(gameId) };

        async Task Change(Action<Game> change)
        {
            await using var context = _database.CreateContext();
            change((await new GameRepository(context).GetByIdAsync(TestAccountId, gameId))!);
            await new UnitOfWork(context).SaveChangesAsync();
            versions.Add(await VersionAsync(gameId));
        }

        var question = TestQuestion(badPoints: 2);
        await Change(game => game.AwardDouble(1));
        await Change(game => game.RemoveDouble(1));
        await Change(game => game.AwardBadCard(1, 1, question, null, Start.AddMinutes(1)));
        await Change(game => game.Reschedule(Start.AddMinutes(-5), null, Start.AddHours(1)));
        await Change(game => game.Finish(Start.AddHours(1)));
        await Change(game => game.ForgetPlayer(2, Start.AddHours(2)));

        Assert.Equal(Enumerable.Range(0, versions.Count).Select(i => versions[0] + i), versions);
    }

    [Fact]
    public async Task ACardReachingTheLimitBumpsTheVersionOnce_AndASaveWithoutChangesNotAtAll()
    {
        int gameId;

        await using (var seeding = _database.CreateContext())
        {
            var game = new Game(TestAccountId, [1, 2], Start, badCardLimit: 2);
            seeding.Games.Add(game);
            await seeding.SaveChangesAsync();
            gameId = game.Id;
        }

        var before = await VersionAsync(gameId);

        await using (var context = _database.CreateContext())
        {
            var game = (await new GameRepository(context).GetByIdAsync(TestAccountId, gameId))!;
            await new UnitOfWork(context).SaveChangesAsync();
            Assert.Equal(before, await VersionAsync(gameId));

            game.AwardBadCard(1, 1, TestQuestion(badPoints: 2), null, Start.AddMinutes(1));
            game.AwardBadCard(1, 1, TestQuestion(badPoints: 2), null, Start.AddMinutes(2));
            await new UnitOfWork(context).SaveChangesAsync();
        }

        Assert.Equal(before + 1, await VersionAsync(gameId));
    }

    [Fact]
    public async Task TooMuchCalls_AreStoredOnTheSeats()
    {
        var gameId = await SeedGameAsync([1, 2, 3]);
        var question = TestQuestion(badPoints: 4);

        await using (var context = _database.CreateContext())
        {
            var game = (await new GameRepository(context).GetByIdAsync(TestAccountId, gameId))!;
            game.AwardBadCard(3, 1, question, null, Start.AddMinutes(1)); // 1 called on 3, the player before them
            game.AwardBadCard(3, 3, question, null, Start.AddMinutes(2));
            await new UnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var game = (await new GameRepository(context).GetByIdAsync(TestAccountId, gameId))!;

            Assert.Equal(
                [(1, 1, 0, 0), (2, 0, 0, 0), (3, 1, 1, 1)],
                game.GamePlayers.OrderBy(gp => gp.PlayerId).Select(gp => (gp.PlayerId!.Value, gp.TooMuchCalls, gp.FalseAlarms, gp.Caught)));
        }
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
        Assert.Equal([mine], (await sut.GetRecentAsync(TestAccountId, GameHistoryStatus.All, null, 10)).Select(g => g.Id));
        Assert.Equal([theirs], (await sut.GetAllAsync(otherAccountId)).Select(g => g.Id));
    }

    private Game WithABadCard(Game game, int playerId)
    {
        game.AwardBadCard(playerId, playerId, TestQuestion(badPoints: 3), null, Start.AddMinutes(1));
        return game;
    }

    /// <summary>A stored question (a card refers to it), all of them in one stored category.</summary>
    private Question TestQuestion(int badPoints)
    {
        using var context = _database.CreateContext();
        var category = context.QuestionCategories.FirstOrDefault() ?? new QuestionCategory("Autá", "Cars");
        var question = new Question(
            "Koľko kolies má auto?", "How many wheels does a car have?", "4", category, badPoints, QuestionSource.Manual, Start);
        context.Questions.Add(question);
        context.SaveChanges();
        return question;
    }

    private async Task<long> VersionAsync(int gameId)
    {
        await using var context = _database.CreateContext();
        return await context.Games.Where(g => g.Id == gameId).Select(g => g.Version).SingleAsync();
    }

    private async Task<int> SeedGameAsync(int[] playerIds, int accountId = TestAccountId, DateTime? started = null)
    {
        await using var context = _database.CreateContext();

        var game = new Game(accountId, playerIds, started ?? Start);
        context.Games.Add(game);
        await context.SaveChangesAsync();

        return game.Id;
    }

    public void Dispose() => _database.Dispose();
}
