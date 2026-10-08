using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class GameStandingsTests
{
    private const int Ana = 1;
    private const int Bo = 2;
    private const int Cy = 3;

    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);

    private readonly Game _game = new(TestAccountId, [Ana, Bo, Cy], Start, badCardLimit: Game.MaxBadCardLimit);

    [Fact]
    public void Standings_PutTheMostBadPointsFirstAndMakeThatPlayerTheLoser()
    {
        Cards(Ana, 3);
        Cards(Bo, 3, 3, 3);
        Cards(Cy, 3, 2);

        var standings = _game.Standings();

        Assert.Equal([Bo, Cy, Ana], standings.Select(s => s.Player.PlayerId));
        Assert.Equal([1, 2, 3], standings.Select(s => s.Rank));
        Assert.Equal([Bo], Losers(standings));
    }

    [Fact]
    public void Standings_BreakAPointsTieWithTheNumberOfCards()
    {
        Cards(Ana, 3, 3);
        Cards(Bo, 2, 2, 2);

        var standings = _game.Standings();

        Assert.Equal(Bo, standings[0].Player.PlayerId);
        Assert.Equal([Bo], Losers(standings));
    }

    [Fact]
    public void Standings_ShareTheRankAndTheDefeatOnAFullTie()
    {
        Cards(Ana, 3, 3);
        Cards(Bo, 4, 2);
        Cards(Cy, 1);

        var standings = _game.Standings();

        Assert.Equal([1, 1, 3], standings.Select(s => s.Rank));
        Assert.Equal([Ana, Bo], Losers(standings));
    }

    [Fact]
    public void Standings_HaveNoLoserWhileNobodyHoldsACard()
    {
        var standings = _game.Standings();

        Assert.All(standings, s => Assert.Equal(1, s.Rank));
        Assert.Empty(Losers(standings));
    }

    [Fact]
    public void Standings_CountBadPointsAfterDoublesAreTakenOff()
    {
        Cards(Ana, 4, 3);
        Doubles(Ana, 3);
        Cards(Bo, 3, 2);

        var standings = _game.Standings();

        Assert.Equal([Bo, Ana, Cy], standings.Select(s => s.Player.PlayerId));
        Assert.Equal([Bo], Losers(standings));
    }

    [Fact]
    public void Standings_CanMakeAPlayerWithoutCardsTheLoserWhenDoublesPushEveryoneBelow()
    {
        Cards(Ana, 1);
        Doubles(Ana, 2);
        Doubles(Cy, 1);

        Assert.Equal([Bo], Losers(_game.Standings()));
    }

    [Fact]
    public void Standings_HaveNoLoserWithOnlyDoublesAndNoCards()
    {
        Doubles(Ana, 1);

        Assert.Empty(Losers(_game.Standings()));
    }

    private void Cards(int playerId, params int[] badPoints)
    {
        foreach (var points in badPoints)
        {
            var question = new Question("Koľko kolies má auto?", "How many wheels does a car have?", "4", new QuestionCategory("Autá", "Cars"), points, QuestionSource.Manual, Start);
            _game.AwardBadCard(playerId, question, null, Start.AddMinutes(1));
        }
    }

    private void Doubles(int playerId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            _game.AwardDouble(playerId);
        }
    }

    private static int[] Losers(IReadOnlyList<GameStanding> standings) =>
        standings.Where(s => s.IsLoser).Select(s => s.Player.PlayerId!.Value).ToArray();
}
