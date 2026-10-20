using ToJePrivela.Domain.Entities;
using ToJePrivela.Domain.Statistics;

namespace ToJePrivela.Domain.Tests.Statistics;

public class PlayerStatisticsTests
{
    private const int Ana = 1;
    private const int Bo = 2;
    private const int Cy = 3;
    private const int Dan = 4;

    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void For_WithoutCountedGames_HasZeroTotalsAndNothingToAverage()
    {
        var statistics = PlayerStatistics.For(Ana, []);

        Assert.Equal(0, statistics.GamesPlayed);
        Assert.Equal(0, statistics.Losses);
        Assert.Null(statistics.AverageBadCards);
        Assert.Null(statistics.AverageFinalBadPoints);
        Assert.Null(statistics.AverageDoubles);
        Assert.Null(statistics.AveragePlayers);
        Assert.Null(statistics.LossIndex);
        Assert.Null(statistics.WorstGame);
        Assert.Null(statistics.BestGame);
        Assert.Equal(0, statistics.GamesWithCalls);
        Assert.Null(statistics.AverageTooMuchCalls);
        Assert.Null(statistics.CallAccuracy);
        Assert.Null(statistics.AverageCaught);
        Assert.Null(statistics.AverageCalledOn);
        Assert.Null(statistics.CaughtCardShare);
        Assert.Null(statistics.MostCorrectCalls);
    }

    [Fact]
    public void For_CountsOnlyFinishedNotCancelledGamesOfThePlayer()
    {
        var finished = Finished(NewGame(Ana, Bo).Cards(Ana, 3));
        var running = NewGame(Ana, Bo).Cards(Ana, 3);
        var cancelled = NewGame(Ana, Bo, Cy).Cards(Ana, 3);
        cancelled.ForgetPlayer(Cy, Start.AddHours(1));
        var withoutAna = Finished(NewGame(Bo, Cy).Cards(Bo, 3));

        var statistics = PlayerStatistics.For(Ana, [finished, running, cancelled, withoutAna]);

        Assert.Equal(1, statistics.GamesPlayed);
        Assert.Equal(1, statistics.BadCards);
    }

    [Fact]
    public void For_SumsAndAveragesCardsPointsDoublesAndPlayers()
    {
        var first = Finished(NewGame(Ana, Bo).Cards(Ana, 3, 2).Doubles(Ana, 1).Cards(Bo, 5));
        var second = Finished(NewGame(Ana, Bo, Cy, Dan).Cards(Ana, 4).Doubles(Ana, 2).Cards(Bo, 1));

        var statistics = PlayerStatistics.For(Ana, [first, second]);

        Assert.Equal(2, statistics.GamesPlayed);
        Assert.Equal(3, statistics.BadCards);
        Assert.Equal(1.5, statistics.AverageBadCards);
        Assert.Equal(4 + 2, statistics.FinalBadPoints);
        Assert.Equal(3, statistics.AverageFinalBadPoints);
        Assert.Equal(3, statistics.Doubles);
        Assert.Equal(1.5, statistics.AverageDoubles);
        Assert.Equal(3, statistics.AveragePlayers);
    }

    [Fact]
    public void For_ComparesTheLossRateWithTheExpectedOne()
    {
        // Ana loses one of two three-player games: rate 1/2 against an expected 1/3.
        var lost = Finished(NewGame(Ana, Bo, Cy).Cards(Ana, 5).Cards(Bo, 1));
        var won = Finished(NewGame(Ana, Bo, Cy).Cards(Ana, 1).Cards(Bo, 5));

        var statistics = PlayerStatistics.For(Ana, [lost, won]);

        Assert.Equal(1, statistics.Losses);
        Assert.Equal(0, statistics.SharedLosses);
        Assert.Equal(1.5, statistics.LossIndex!.Value, 10);
    }

    [Fact]
    public void For_CountsATieOnTheMostFinalPointsAsASharedLossWhateverTheCards()
    {
        // Ana and Bo both hold 6 final points, with two and three cards.
        var game = Finished(NewGame(Ana, Bo, Cy, Dan).Cards(Ana, 3, 3).Cards(Bo, 2, 2, 2).Cards(Cy, 1));

        var statistics = PlayerStatistics.For(Ana, [game]);

        Assert.Equal(1, statistics.Losses);
        Assert.Equal(1, statistics.SharedLosses);
        // Expected 2 losers out of 4: a shared loss weighs as much as chance says it should.
        Assert.Equal(2, statistics.LossIndex!.Value, 10);
        Assert.Equal(1, PlayerStatistics.For(Bo, [game]).SharedLosses);
    }

    [Fact]
    public void For_HasNoLossIndexWhileNoCountedGameHadALoser()
    {
        var running = NewGame(Ana, Bo).Cards(Ana, 2);
        var lost = Finished(NewGame(Ana, Bo).Cards(Ana, 2));

        Assert.Null(PlayerStatistics.For(Ana, [running]).LossIndex);
        Assert.Equal(2, PlayerStatistics.For(Ana, [running, lost]).LossIndex!.Value, 10);
    }

    [Fact]
    public void For_CountsFlawlessVictoriesAndReversals()
    {
        var flawless = Finished(NewGame(Ana, Bo).Cards(Bo, 2));
        var flawlessReversal = Finished(NewGame(Ana, Bo).Doubles(Ana, 1).Cards(Bo, 2));
        var reversal = Finished(NewGame(Ana, Bo).Cards(Ana, 1).Doubles(Ana, 2).Cards(Bo, 2));

        var statistics = PlayerStatistics.For(Ana, [flawless, flawlessReversal, reversal]);

        Assert.Equal(2, statistics.FlawlessVictories);
        Assert.Equal(2, statistics.Reversals);
    }

    [Fact]
    public void For_CountsNoFlawlessVictoryWhenThePlayerLostWithoutABadCard()
    {
        var lostWithoutCards = Finished(NewGame(Ana, Bo).Cards(Bo, 1).Doubles(Bo, 2));

        var statistics = PlayerStatistics.For(Ana, [lostWithoutCards]);

        Assert.Equal(1, statistics.Losses);
        Assert.Equal(0, statistics.FlawlessVictories);
    }

    [Fact]
    public void For_CountsALuckyFinishWhenThePlayerEndsTheGameButSomebodyElseLoses()
    {
        var lucky = NewGameWithLimit(2, Ana, Bo).Cards(Bo, 5).Cards(Ana, 1, 1);
        var unlucky = NewGameWithLimit(2, Ana, Bo).Cards(Bo, 1).Cards(Ana, 1, 1);

        var statistics = PlayerStatistics.For(Ana, [lucky, unlucky]);

        Assert.True(lucky.IsFinished && unlucky.IsFinished);
        Assert.Equal(1, statistics.LuckyFinishes);
        Assert.Equal(1, statistics.Losses);
    }

    [Fact]
    public void For_CountsAClosestEscapeOnlyAtExactlyOnePointBelowTheLoser()
    {
        var escape = Finished(NewGame(Ana, Bo).Cards(Ana, 4).Cards(Bo, 5));
        var twoBelow = Finished(NewGame(Ana, Bo).Cards(Ana, 3).Cards(Bo, 5));
        var lost = Finished(NewGame(Ana, Bo).Cards(Ana, 5).Cards(Bo, 4));

        var statistics = PlayerStatistics.For(Ana, [escape, twoBelow, lost]);

        Assert.Equal(1, statistics.ClosestEscapes);
        Assert.Equal(1, PlayerStatistics.For(Bo, [lost]).ClosestEscapes);
    }

    [Fact]
    public void For_PicksTheWorstAndBestGameAndTheLatestOneOnATie()
    {
        var earlyWorst = Finished(NewGame(Ana, Bo).Cards(Ana, 5), Start.AddHours(1));
        var lateWorst = Finished(NewGame(Ana, Bo).Cards(Ana, 5), Start.AddHours(3));
        var earlyBest = Finished(NewGame(Ana, Bo).Cards(Bo, 2), Start.AddHours(2));
        var lateBest = Finished(NewGame(Ana, Bo).Cards(Bo, 2), Start.AddHours(4));

        var statistics = PlayerStatistics.For(Ana, [earlyWorst, lateWorst, earlyBest, lateBest]);

        Assert.Equal(new GameScore(0, Start.AddHours(3), 5), statistics.WorstGame);
        Assert.Equal(new GameScore(0, Start.AddHours(4), 0), statistics.BestGame);
    }

    [Fact]
    public void For_CountsCallsTheirAccuracyAndTimesTheBidWasCalled()
    {
        // Seats Ana, Bo, Cy: Ana calls Cy's bids, Bo calls Ana's.
        var game = Finished(NewGame(Ana, Bo, Cy)
            .Call(caller: Ana, taker: Cy)
            .Call(caller: Ana, taker: Ana)
            .Call(caller: Bo, taker: Ana)
            .Call(caller: Bo, taker: Bo));

        var ana = PlayerStatistics.For(Ana, [game]);

        Assert.Equal(1, ana.GamesWithCalls);
        Assert.Equal((2, 1, 1), (ana.TooMuchCalls, ana.CorrectCalls, ana.FalseAlarms));
        Assert.Equal(2, ana.AverageTooMuchCalls);
        Assert.Equal(0.5, ana.CallAccuracy);
        Assert.Equal(1, ana.Caught);
        // Caught once, and once Bo's false alarm.
        Assert.Equal(2, ana.CalledOn);
        Assert.Equal(2, ana.AverageCalledOn);
        Assert.Equal(0.5, ana.CaughtCardShare);
    }

    [Fact]
    public void For_CountsTheLastSeatsBidsAsCalledByTheFirstSeat()
    {
        var game = Finished(NewGame(Ana, Bo, Cy)
            .Call(caller: Ana, taker: Cy)
            .Call(caller: Ana, taker: Ana));

        var cy = PlayerStatistics.For(Cy, [game]);

        Assert.Equal((1, 2), (cy.Caught, cy.CalledOn));
        Assert.Equal(1, cy.CaughtCardShare);
        Assert.Null(cy.CallAccuracy);
    }

    [Fact]
    public void For_LeavesGamesPlayedBeforeCallsWereRecordedOutOfTheCallFigures()
    {
        var withCalls = Finished(NewGame(Ana, Bo).Call(caller: Bo, taker: Ana).Call(caller: Ana, taker: Ana));
        var beforeCalls = Finished(NewGame(Ana, Bo).Cards(Ana, 1, 1).Cards(Bo, 1)).WithoutCallData();

        var statistics = PlayerStatistics.For(Ana, [withCalls, beforeCalls]);

        Assert.Equal((2, 1), (statistics.GamesPlayed, statistics.GamesWithCalls));
        Assert.Equal(1, statistics.AverageTooMuchCalls);
        Assert.Equal(1, statistics.AverageCaught);
        // Only the two cards of the game with calls are shared out.
        Assert.Equal(0.5, statistics.CaughtCardShare);
    }

    [Fact]
    public void For_CountsALossWithMoreFalseAlarmsThanCatchesAsCryingWolf()
    {
        var criedWolf = Finished(NewGame(Ana, Bo).Call(caller: Ana, taker: Ana).Call(caller: Ana, taker: Ana).Call(caller: Bo, taker: Ana));
        var overbid = Finished(NewGame(Ana, Bo).Call(caller: Ana, taker: Ana).Call(caller: Bo, taker: Ana).Call(caller: Bo, taker: Ana));
        var won = Finished(NewGame(Ana, Bo).Call(caller: Ana, taker: Ana).Cards(Bo, 5));

        var statistics = PlayerStatistics.For(Ana, [criedWolf, overbid, won]);

        Assert.Equal(2, statistics.Losses);
        Assert.Equal(1, statistics.CriedWolfLosses);
    }

    [Fact]
    public void For_PicksTheGameWithTheMostCorrectCallsAndTheLatestOneOnATie()
    {
        var early = Finished(NewGame(Ana, Bo).Call(caller: Ana, taker: Bo).Call(caller: Ana, taker: Bo), Start.AddHours(1));
        var late = Finished(NewGame(Ana, Bo).Call(caller: Ana, taker: Bo).Call(caller: Ana, taker: Bo), Start.AddHours(3));
        var fewer = Finished(NewGame(Ana, Bo).Call(caller: Ana, taker: Bo), Start.AddHours(4));

        Assert.Equal(new GameTally(0, Start.AddHours(3), 2), PlayerStatistics.For(Ana, [early, late, fewer]).MostCorrectCalls);
        Assert.Null(PlayerStatistics.For(Bo, [early, late, fewer]).MostCorrectCalls);
    }

    private static Game NewGame(params int[] playerIds) => NewGame(playerIds, Game.MaxBadCardLimit);

    private static Game NewGameWithLimit(int limit, params int[] playerIds) => NewGame(playerIds, limit);

    private static Game NewGame(int[] playerIds, int limit) => new(TestAccountId, playerIds, Start, limit);

    private static Game Finished(Game game, DateTime? at = null)
    {
        game.Finish(at ?? Start.AddHours(1));
        return game;
    }
}

internal static class GameTestExtensions
{
    private static readonly DateTime CreatedAt = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Game Cards(this Game game, int playerId, params int[] badPoints)
    {
        foreach (var points in badPoints)
        {
            var question = new Question("Koľko kolies má auto?", "How many wheels does a car have?", "4", new QuestionCategory("Autá", "Cars"), points, QuestionSource.Manual, CreatedAt);
            game.AwardBadCard(playerId, playerId, question, null, game.Started!.Value.AddMinutes(1));
        }

        return game;
    }

    /// <summary>A card for <paramref name="taker"/> after <paramref name="caller"/> called "To je priveľa!".</summary>
    public static Game Call(this Game game, int caller, int taker, int badPoints = 1)
    {
        var question = new Question("Koľko kolies má auto?", "How many wheels does a car have?", "4", new QuestionCategory("Autá", "Cars"), badPoints, QuestionSource.Manual, CreatedAt);
        game.AwardBadCard(taker, caller, question, null, game.Started!.Value.AddMinutes(1));
        return game;
    }

    /// <summary>
    /// Clears the call counters, as in a game played before calls were recorded. The domain never gets there, so
    /// the private setters are written the way EF writes them.
    /// </summary>
    public static Game WithoutCallData(this Game game)
    {
        foreach (var seat in game.GamePlayers)
        {
            foreach (var counter in new[] { nameof(GamePlayer.TooMuchCalls), nameof(GamePlayer.FalseAlarms), nameof(GamePlayer.Caught) })
            {
                typeof(GamePlayer).GetProperty(counter)!.SetValue(seat, 0);
            }
        }

        return game;
    }

    public static Game Doubles(this Game game, int playerId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            game.AwardDouble(playerId);
        }

        return game;
    }
}
