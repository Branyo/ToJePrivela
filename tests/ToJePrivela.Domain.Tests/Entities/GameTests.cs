using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class GameTests
{
    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_CreatesOneEntryPerPlayer()
    {
        var game = new Game([1, 2, 3], Start);

        Assert.Equal([1, 2, 3], game.GamePlayers.Select(gp => gp.PlayerId));
        Assert.All(game.GamePlayers, gp => Assert.Equal(0, gp.BadPoints));
        Assert.Equal(Start, game.Started);
        Assert.Null(game.Finished);
        Assert.False(game.IsFinished);
    }

    [Fact]
    public void Constructor_TakesBadPointsFromTheQuestionByDefault()
    {
        Assert.Equal(BadPointsMode.Question, new Game([1, 2], Start).BadPointsMode);
        Assert.Equal(BadPointsMode.Chooser, new Game([1, 2], Start, badPointsMode: BadPointsMode.Chooser).BadPointsMode);
    }

    [Fact]
    public void Constructor_RejectsAnUnknownBadPointsMode()
    {
        Assert.Throws<DomainException>(() => new Game([1, 2], Start, badPointsMode: (BadPointsMode)7));
    }

    [Fact]
    public void Constructor_IgnoresDuplicatePlayers()
    {
        var game = new Game([1, 2, 2, 1, 3], Start);

        Assert.Equal(3, game.GamePlayers.Count);
    }

    [Fact]
    public void Constructor_RejectsTooFewPlayers()
    {
        Assert.Throws<DomainException>(() => new Game([1], Start));
        Assert.Throws<DomainException>(() => new Game([1, 1], Start));
    }

    [Fact]
    public void Constructor_RejectsTooManyPlayers()
    {
        var playerIds = Enumerable.Range(1, Game.MaxPlayers + 1);

        Assert.Throws<DomainException>(() => new Game(playerIds, Start));
    }

    [Fact]
    public void Finish_MarksTheGameAsFinished()
    {
        var game = new Game([1, 2], Start);
        var end = Start.AddHours(1);

        game.Finish(end);

        Assert.Equal(end, game.Finished);
        Assert.True(game.IsFinished);
    }

    [Fact]
    public void Finish_RejectsSecondCall()
    {
        var game = new Game([1, 2], Start);
        game.Finish(Start.AddHours(1));

        Assert.Throws<DomainException>(() => game.Finish(Start.AddHours(2)));
    }

    [Fact]
    public void Finish_RejectsTimeBeforeStart()
    {
        var game = new Game([1, 2], Start);

        Assert.Throws<DomainException>(() => game.Finish(Start.AddHours(-1)));
    }

    [Fact]
    public void Reschedule_ReplacesBothTimestamps()
    {
        var game = new Game([1, 2], Start);
        var newStart = Start.AddDays(-1);
        var newEnd = Start.AddDays(-1).AddHours(2);

        game.Reschedule(newStart, newEnd);

        Assert.Equal(newStart, game.Started);
        Assert.Equal(newEnd, game.Finished);
    }

    [Fact]
    public void Reschedule_RejectsEndBeforeStart()
    {
        var game = new Game([1, 2], Start);

        Assert.Throws<DomainException>(() => game.Reschedule(Start, Start.AddHours(-1)));
    }

    [Fact]
    public void Reschedule_AllowsClearingBothTimestamps()
    {
        var game = new Game([1, 2], Start);

        game.Reschedule(null, null);

        Assert.Null(game.Started);
        Assert.Null(game.Finished);
    }

    [Fact]
    public void Constructor_UsesTheDefaultBadCardLimit()
    {
        var game = new Game([1, 2], Start);

        Assert.Equal(Game.DefaultBadCardLimit, game.BadCardLimit);
        Assert.All(game.GamePlayers, gp => Assert.Equal(0, gp.BadCards));
    }

    [Fact]
    public void Constructor_AcceptsTwelvePlayers()
    {
        var game = new Game(Enumerable.Range(1, 12), Start);

        Assert.Equal(12, game.GamePlayers.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(11)]
    public void Constructor_RejectsBadCardLimitOutOfRange(int limit)
    {
        Assert.Throws<DomainException>(() => new Game([1, 2], Start, limit));
    }

    [Fact]
    public void AwardBadCard_AddsTheCardAndItsPoints()
    {
        var game = new Game([1, 2], Start);

        var player = game.AwardBadCard(2, 4, Start.AddMinutes(5));
        game.AwardBadCard(2, 1, Start.AddMinutes(6));

        Assert.Equal(2, player.BadCards);
        Assert.Equal(5, player.BadPoints);
        Assert.Equal(0, game.GamePlayers.Single(gp => gp.PlayerId == 1).BadCards);
        Assert.False(game.IsFinished);
    }

    [Fact]
    public void AwardBadCard_FinishesTheGameAtTheLimit()
    {
        var game = new Game([1, 2], Start, badCardLimit: 2);
        var end = Start.AddMinutes(10);

        game.AwardBadCard(1, 3, Start.AddMinutes(5));
        game.AwardBadCard(1, 3, end);

        Assert.True(game.IsFinished);
        Assert.Equal(end, game.Finished);
    }

    [Fact]
    public void AwardBadCard_RejectsAFinishedGame()
    {
        var game = new Game([1, 2], Start, badCardLimit: 2);
        game.AwardBadCard(1, 3, Start.AddMinutes(5));
        game.AwardBadCard(1, 3, Start.AddMinutes(6));

        Assert.Throws<DomainException>(() => game.AwardBadCard(2, 3, Start.AddMinutes(7)));
    }

    [Fact]
    public void AwardBadCard_RejectsAPlayerOutsideTheGame()
    {
        var game = new Game([1, 2], Start);

        Assert.Throws<DomainException>(() => game.AwardBadCard(9, 3, Start.AddMinutes(5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void AwardBadCard_RejectsBadPointsOutOfRange(int badPoints)
    {
        var game = new Game([1, 2], Start);

        Assert.Throws<DomainException>(() => game.AwardBadCard(1, badPoints, Start.AddMinutes(5)));
    }

    [Fact]
    public void AwardDouble_TakesOneBadPointOffTheFinalScore()
    {
        var game = new Game([1, 2], Start);
        game.AwardBadCard(2, 4, Start.AddMinutes(5));

        var player = game.AwardDouble(2);
        game.AwardDouble(2);

        Assert.Equal(2, player.Doubles);
        Assert.Equal(4, player.BadPoints);
        Assert.Equal(2, player.FinalBadPoints);
        Assert.Equal(1, player.BadCards);
    }

    [Fact]
    public void AwardDouble_CanTakeTheFinalScoreBelowZero()
    {
        var game = new Game([1, 2], Start);

        var player = game.AwardDouble(1);

        Assert.Equal(-1, player.FinalBadPoints);
    }

    [Fact]
    public void AwardDouble_NeverFinishesTheGame()
    {
        var game = new Game([1, 2], Start, badCardLimit: 2);

        game.AwardDouble(1);
        game.AwardDouble(1);

        Assert.False(game.IsFinished);
    }

    [Fact]
    public void AwardDouble_RejectsAFinishedGame()
    {
        var game = new Game([1, 2], Start);
        game.Finish(Start.AddMinutes(5));

        Assert.Throws<DomainException>(() => game.AwardDouble(2));
    }

    [Fact]
    public void RemoveDouble_TakesBackOneDouble()
    {
        var game = new Game([1, 2], Start);
        game.AwardDouble(1);
        game.AwardDouble(1);

        var player = game.RemoveDouble(1);

        Assert.Equal(1, player.Doubles);
        Assert.Equal(-1, player.FinalBadPoints);
    }

    [Fact]
    public void RemoveDouble_RejectsAPlayerWithoutDoubles()
    {
        var game = new Game([1, 2], Start);

        Assert.Throws<DomainException>(() => game.RemoveDouble(1));
    }

    [Fact]
    public void RemoveDouble_RejectsAFinishedGame()
    {
        var game = new Game([1, 2], Start);
        game.AwardDouble(1);
        game.Finish(Start.AddMinutes(5));

        Assert.Throws<DomainException>(() => game.RemoveDouble(1));
    }

    [Fact]
    public void AwardDouble_RejectsAPlayerOutsideTheGame()
    {
        var game = new Game([1, 2], Start);

        Assert.Throws<DomainException>(() => game.AwardDouble(9));
    }
}
