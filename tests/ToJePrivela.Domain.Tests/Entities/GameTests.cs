using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class GameTests
{
    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Question QuestionWorth(int badPoints) =>
        new("How many wheels does a car have?", "4", new QuestionCategory("Autá", "Cars"), badPoints, QuestionSource.Manual, Start);

    [Fact]
    public void Constructor_CreatesOneEntryPerPlayer()
    {
        var game = new Game(TestAccountId, [1, 2, 3], Start);

        Assert.Equal([1, 2, 3], game.GamePlayers.Select(gp => gp.PlayerId));
        Assert.All(game.GamePlayers, gp => Assert.Equal(0, gp.BadPoints));
        Assert.Equal(Start, game.Started);
        Assert.Null(game.Finished);
        Assert.False(game.IsFinished);
    }

    [Fact]
    public void Constructor_TakesBadPointsFromTheQuestionByDefault()
    {
        Assert.Equal(BadPointsMode.Question, new Game(TestAccountId, [1, 2], Start).BadPointsMode);
        Assert.Equal(BadPointsMode.Chooser, new Game(TestAccountId, [1, 2], Start, badPointsMode: BadPointsMode.Chooser).BadPointsMode);
    }

    [Fact]
    public void Constructor_RejectsAnUnknownBadPointsMode()
    {
        Assert.Throws<DomainException>(() => new Game(TestAccountId, [1, 2], Start, badPointsMode: (BadPointsMode)7));
    }

    [Fact]
    public void Constructor_IgnoresDuplicatePlayers()
    {
        var game = new Game(TestAccountId, [1, 2, 2, 1, 3], Start);

        Assert.Equal(3, game.GamePlayers.Count);
    }

    [Fact]
    public void Constructor_RejectsTooFewPlayers()
    {
        Assert.Throws<DomainException>(() => new Game(TestAccountId, [1], Start));
        Assert.Throws<DomainException>(() => new Game(TestAccountId, [1, 1], Start));
    }

    [Fact]
    public void Constructor_RejectsTooManyPlayers()
    {
        var playerIds = Enumerable.Range(1, Game.MaxPlayers + 1);

        Assert.Throws<DomainException>(() => new Game(TestAccountId, playerIds, Start));
    }

    [Fact]
    public void Finish_MarksTheGameAsFinished()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        var end = Start.AddHours(1);

        game.Finish(end);

        Assert.Equal(end, game.Finished);
        Assert.True(game.IsFinished);
    }

    [Fact]
    public void Finish_RejectsSecondCall()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        game.Finish(Start.AddHours(1));

        Assert.Throws<DomainException>(() => game.Finish(Start.AddHours(2)));
    }

    [Fact]
    public void Finish_RejectsTimeBeforeStart()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        Assert.Throws<DomainException>(() => game.Finish(Start.AddHours(-1)));
    }

    [Fact]
    public void Reschedule_ReplacesBothTimestamps()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        var newStart = Start.AddDays(-1);
        var newEnd = Start.AddDays(-1).AddHours(2);

        game.Reschedule(newStart, newEnd, Now);

        Assert.Equal(newStart, game.Started);
        Assert.Equal(newEnd, game.Finished);
    }

    [Fact]
    public void Reschedule_StoresTimesSentWithAnOffsetInUtc()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        var started = new DateTimeOffset(2026, 9, 30, 10, 0, 0, TimeSpan.FromHours(2));

        game.Reschedule(started.LocalDateTime, started.AddHours(1).LocalDateTime, Now);

        Assert.Equal(new DateTime(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc), game.Started);
        Assert.Equal(DateTimeKind.Utc, game.Started!.Value.Kind);
        Assert.Equal(new DateTime(2026, 9, 30, 9, 0, 0, DateTimeKind.Utc), game.Finished);
    }

    [Fact]
    public void CheckReschedule_ComparesTimesWithDifferentOffsetsInUtc()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        var started = new DateTime(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc);
        var finishedLater = new DateTimeOffset(2026, 9, 30, 9, 30, 0, TimeSpan.FromHours(2)).LocalDateTime;

        // 09:30+02:00 is 07:30 UTC, before the start, although its local clock reading is later.
        Assert.Equal(GameRuleViolation.EndsBeforeStart, game.CheckReschedule(started, finishedLater, Now));
    }

    [Fact]
    public void Reschedule_RejectsAStartLaterThanNow()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        Assert.Equal(GameRuleViolation.StartsInFuture, game.CheckReschedule(Now.AddMinutes(1), null, Now));
        Assert.Throws<DomainException>(() => game.Reschedule(Now.AddMinutes(1), null, Now));
        Assert.Equal(Start, game.Started);
    }

    [Fact]
    public void Reschedule_AcceptsAStartOfExactlyNow()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        game.Reschedule(Now, null, Now);

        Assert.Equal(Now, game.Started);
    }

    [Fact]
    public void CheckReschedule_ComparesTheStartWithNowInUtc()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        var laterLocalReading = new DateTimeOffset(2026, 10, 1, 13, 0, 0, TimeSpan.FromHours(2)).LocalDateTime;

        // 13:00+02:00 is 11:00 UTC, before now (12:00 UTC), although its clock reading is later.
        Assert.Null(game.CheckReschedule(laterLocalReading, null, Now));
    }

    [Fact]
    public void Reschedule_RejectsEndBeforeStart()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        Assert.Throws<DomainException>(() => game.Reschedule(Start, Start.AddHours(-1), Now));
    }

    [Fact]
    public void Reschedule_CanFinishARunningGame()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        game.Reschedule(Start, Start.AddHours(1), Now);

        Assert.True(game.IsFinished);
    }

    [Fact]
    public void Reschedule_CorrectsTheEndOfAFinishedGame()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        game.Finish(Start.AddHours(1));

        game.Reschedule(Start, Start.AddHours(2), Now);

        Assert.Equal(Start.AddHours(2), game.Finished);
    }

    [Fact]
    public void Reschedule_NeverReopensAFinishedGame()
    {
        var game = new Game(TestAccountId, [1, 2], Start, badCardLimit: 2);
        game.AwardBadCard(1, QuestionWorth(3), null, Start.AddMinutes(5));
        game.AwardBadCard(1, QuestionWorth(3), null, Start.AddMinutes(6));

        Assert.Throws<DomainException>(() => game.Reschedule(Start, null, Now));
        Assert.Equal(Start.AddMinutes(6), game.Finished);
    }

    [Fact]
    public void Checks_ReportEveryBrokenRuleWithoutChangingTheGame()
    {
        var chooser = new Game(TestAccountId, [1, 2], Start, badPointsMode: BadPointsMode.Chooser);
        var finished = new Game(TestAccountId, [1, 2], Start);
        finished.Finish(Start.AddHours(1));

        Assert.Equal(GameRuleViolation.PlayerNotInGame, chooser.CheckAwardBadCard(9, 3));
        Assert.Equal(GameRuleViolation.ChosenBadPointsRequired, chooser.CheckAwardBadCard(1, null));
        Assert.Equal(GameRuleViolation.ChosenBadPointsNotAllowed, new Game(TestAccountId, [1, 2], Start).CheckAwardBadCard(1, 3));
        Assert.Equal(GameRuleViolation.NoDoubleToRemove, chooser.CheckRemoveDouble(1));
        Assert.Equal(GameRuleViolation.AlreadyFinished, finished.CheckAwardDouble(1));
        Assert.Equal(GameRuleViolation.AlreadyFinished, finished.CheckFinish(Start.AddHours(2)));
        Assert.Equal(GameRuleViolation.EndsBeforeStart, chooser.CheckFinish(Start.AddHours(-1)));
        Assert.Equal(GameRuleViolation.CannotReopen, finished.CheckReschedule(Start, null, Now));
        Assert.Equal(GameRuleViolation.EndsBeforeStart, chooser.CheckReschedule(Start, Start.AddHours(-1), Now));
        Assert.All(chooser.GamePlayers, gp => Assert.Equal((0, 0), (gp.BadCards, gp.Doubles)));
        Assert.False(chooser.IsFinished);
    }

    [Fact]
    public void Checks_PassWhenTheChangeIsAllowed()
    {
        var game = new Game(TestAccountId, [1, 2], Start, badPointsMode: BadPointsMode.Chooser);
        game.AwardDouble(1);

        Assert.Null(game.CheckAwardBadCard(1, 3));
        Assert.Null(game.CheckAwardDouble(2));
        Assert.Null(game.CheckRemoveDouble(1));
        Assert.Null(game.CheckFinish(Start.AddHours(1)));
        Assert.Null(game.CheckReschedule(Start, null, Now));
    }

    [Fact]
    public void Constructor_UsesTheDefaultBadCardLimit()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        Assert.Equal(Game.DefaultBadCardLimit, game.BadCardLimit);
        Assert.All(game.GamePlayers, gp => Assert.Equal(0, gp.BadCards));
    }

    [Fact]
    public void Constructor_AcceptsTwelvePlayers()
    {
        var game = new Game(TestAccountId, Enumerable.Range(1, 12), Start);

        Assert.Equal(12, game.GamePlayers.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(11)]
    public void Constructor_RejectsBadCardLimitOutOfRange(int limit)
    {
        Assert.Throws<DomainException>(() => new Game(TestAccountId, [1, 2], Start, limit));
    }

    [Fact]
    public void AwardBadCard_AddsTheCardAndItsPoints()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        var player = game.AwardBadCard(2, QuestionWorth(4), null, Start.AddMinutes(5));
        game.AwardBadCard(2, QuestionWorth(1), null, Start.AddMinutes(6));

        Assert.Equal(2, player.BadCards);
        Assert.Equal(5, player.BadPoints);
        Assert.Equal(0, game.GamePlayers.Single(gp => gp.PlayerId == 1).BadCards);
        Assert.False(game.IsFinished);
    }

    [Fact]
    public void AwardBadCard_FinishesTheGameAtTheLimit()
    {
        var game = new Game(TestAccountId, [1, 2], Start, badCardLimit: 2);
        var end = Start.AddMinutes(10);

        game.AwardBadCard(1, QuestionWorth(3), null, Start.AddMinutes(5));
        game.AwardBadCard(1, QuestionWorth(3), null, end);

        Assert.True(game.IsFinished);
        Assert.Equal(end, game.Finished);
    }

    [Fact]
    public void AwardBadCard_RejectsAFinishedGame()
    {
        var game = new Game(TestAccountId, [1, 2], Start, badCardLimit: 2);
        game.AwardBadCard(1, QuestionWorth(3), null, Start.AddMinutes(5));
        game.AwardBadCard(1, QuestionWorth(3), null, Start.AddMinutes(6));

        Assert.Throws<DomainException>(() => game.AwardBadCard(2, QuestionWorth(3), null, Start.AddMinutes(7)));
    }

    [Fact]
    public void AwardBadCard_RejectsAPlayerOutsideTheGame()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        Assert.Throws<DomainException>(() => game.AwardBadCard(9, QuestionWorth(3), null, Start.AddMinutes(5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void AwardBadCard_RejectsChosenBadPointsOutOfRange(int badPoints)
    {
        var game = new Game(TestAccountId, [1, 2], Start, badPointsMode: BadPointsMode.Chooser);

        Assert.Throws<DomainException>(() => game.AwardBadCard(1, QuestionWorth(3), badPoints, Start.AddMinutes(5)));
        Assert.Equal(0, game.GamePlayers.Single(gp => gp.PlayerId == 1).BadCards);
    }

    [Fact]
    public void AwardBadCard_InAChooserGameIsWorthTheChosenBadPoints()
    {
        var game = new Game(TestAccountId, [1, 2], Start, badPointsMode: BadPointsMode.Chooser);

        var player = game.AwardBadCard(1, QuestionWorth(4), 1, Start.AddMinutes(5));

        Assert.Equal(1, player.BadPoints);
    }

    [Fact]
    public void AwardBadCard_InAChooserGameRequiresTheChosenBadPoints()
    {
        var game = new Game(TestAccountId, [1, 2], Start, badPointsMode: BadPointsMode.Chooser);

        Assert.True(game.RequiresChosenBadPoints);
        Assert.Throws<DomainException>(() => game.AwardBadCard(1, QuestionWorth(4), null, Start.AddMinutes(5)));
    }

    [Fact]
    public void AwardBadCard_InAQuestionGameRejectsChosenBadPoints()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        Assert.False(game.RequiresChosenBadPoints);
        Assert.Throws<DomainException>(() => game.AwardBadCard(1, QuestionWorth(4), 2, Start.AddMinutes(5)));
        Assert.Equal(0, game.GamePlayers.Single(gp => gp.PlayerId == 1).BadCards);
    }

    [Fact]
    public void AwardDouble_TakesOneBadPointOffTheFinalScore()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        game.AwardBadCard(2, QuestionWorth(4), null, Start.AddMinutes(5));

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
        var game = new Game(TestAccountId, [1, 2], Start);

        var player = game.AwardDouble(1);

        Assert.Equal(-1, player.FinalBadPoints);
    }

    [Fact]
    public void AwardDouble_NeverFinishesTheGame()
    {
        var game = new Game(TestAccountId, [1, 2], Start, badCardLimit: 2);

        game.AwardDouble(1);
        game.AwardDouble(1);

        Assert.False(game.IsFinished);
    }

    [Fact]
    public void AwardDouble_RejectsAFinishedGame()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        game.Finish(Start.AddMinutes(5));

        Assert.Throws<DomainException>(() => game.AwardDouble(2));
    }

    [Fact]
    public void RemoveDouble_TakesBackOneDouble()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        game.AwardDouble(1);
        game.AwardDouble(1);

        var player = game.RemoveDouble(1);

        Assert.Equal(1, player.Doubles);
        Assert.Equal(-1, player.FinalBadPoints);
    }

    [Fact]
    public void RemoveDouble_RejectsAPlayerWithoutDoubles()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        Assert.Throws<DomainException>(() => game.RemoveDouble(1));
    }

    [Fact]
    public void RemoveDouble_RejectsAFinishedGame()
    {
        var game = new Game(TestAccountId, [1, 2], Start);
        game.AwardDouble(1);
        game.Finish(Start.AddMinutes(5));

        Assert.Throws<DomainException>(() => game.RemoveDouble(1));
    }

    [Fact]
    public void AwardDouble_RejectsAPlayerOutsideTheGame()
    {
        var game = new Game(TestAccountId, [1, 2], Start);

        Assert.Throws<DomainException>(() => game.AwardDouble(9));
    }

    [Fact]
    public void Constructor_KeepsTheOwningAccount()
    {
        Assert.Equal(7, new Game(7, [1, 2], Start).AccountId);
    }

    [Fact]
    public void Constructor_RejectsAMissingAccount()
    {
        Assert.Throws<DomainException>(() => new Game(0, [1, 2], Start));
    }
}
