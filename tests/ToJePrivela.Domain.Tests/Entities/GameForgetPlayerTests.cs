using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class GameForgetPlayerTests
{
    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DeletedAt = Start.AddHours(1);

    [Fact]
    public void ForgetPlayer_CancelsARunningGameSoItCanNeverBeResumed()
    {
        var game = new Game([1, 2, 3], Start);

        game.ForgetPlayer(2, DeletedAt);

        Assert.True(game.IsFinished);
        Assert.True(game.IsCancelled);
        Assert.Equal(DeletedAt, game.Finished);
        Assert.Equal(GameRuleViolation.AlreadyFinished, game.CheckAwardDouble(1));
        Assert.Equal(GameRuleViolation.AlreadyFinished, game.CheckFinish(DeletedAt));
    }

    [Fact]
    public void ForgetPlayer_KeepsTheSeatAndItsScoresAsAnUnknownPlayer()
    {
        var game = new Game([1, 2], Start);
        game.AwardBadCard(2, Question(4), null, Start);
        game.AwardDouble(2);

        game.ForgetPlayer(2, DeletedAt);

        var seat = Assert.Single(game.GamePlayers, gp => gp.IsUnknownPlayer);
        Assert.Null(seat.PlayerId);
        Assert.Equal((4, 1, 1), (seat.BadPoints, seat.BadCards, seat.Doubles));
        Assert.False(game.HasPlayer(2));
        Assert.True(game.HasPlayer(1));
    }

    [Fact]
    public void ForgetPlayer_GivesACancelledGameNoLoser()
    {
        var game = new Game([1, 2], Start);
        game.AwardBadCard(1, Question(5), null, Start);

        game.ForgetPlayer(2, DeletedAt);

        Assert.All(game.Standings(), standing => Assert.False(standing.IsLoser));
    }

    [Fact]
    public void ForgetPlayer_LeavesAFinishedGameAndItsResultAlone()
    {
        var game = new Game([1, 2], Start, badCardLimit: 2);
        game.AwardBadCard(2, Question(3), null, Start);
        game.AwardBadCard(2, Question(3), null, Start.AddMinutes(30));
        var finished = game.Finished;

        game.ForgetPlayer(2, DeletedAt);

        Assert.False(game.IsCancelled);
        Assert.Equal(finished, game.Finished);
        var loser = Assert.Single(game.Standings(), standing => standing.IsLoser);
        Assert.True(loser.Player.IsUnknownPlayer);
    }

    [Fact]
    public void ForgetPlayer_CanForgetSeveralPlayersOfOneGame()
    {
        var game = new Game([1, 2, 3], Start);

        game.ForgetPlayer(1, DeletedAt);
        game.ForgetPlayer(3, DeletedAt.AddDays(1));

        Assert.Equal(2, game.GamePlayers.Count(gp => gp.IsUnknownPlayer));
        Assert.Equal(DeletedAt, game.Finished);
    }

    [Fact]
    public void ForgetPlayer_NeverEndsTheGameBeforeItStarted()
    {
        var game = new Game([1, 2], Start);

        game.ForgetPlayer(1, Start.AddHours(-3));

        Assert.Equal(Start, game.Finished);
    }

    [Fact]
    public void ForgetPlayer_RejectsAPlayerOutsideTheGame()
    {
        var game = new Game([1, 2], Start);

        Assert.Throws<DomainException>(() => game.ForgetPlayer(9, DeletedAt));
        Assert.False(game.IsFinished);
    }

    private static Question Question(int badPoints) =>
        new("How many wheels does a car have?", "4", new QuestionCategory("Cars"), badPoints, QuestionSource.Manual, Start);
}
