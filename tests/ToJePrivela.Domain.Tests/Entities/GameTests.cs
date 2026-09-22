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
}
