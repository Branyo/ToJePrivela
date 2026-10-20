using System.Linq.Expressions;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Statistics;

/// <summary>One game's final bad points for a player, and the game it was scored in.</summary>
public sealed record GameScore(int GameId, DateTime Finished, int FinalBadPoints);

/// <summary>How many times something happened to a player in one game, and the game it happened in.</summary>
public sealed record GameTally(int GameId, DateTime Finished, int Count);

/// <summary>
/// A player's record over their counted games: started, finished, not cancelled and with a loser. Averages are per counted game
/// and null when there is none.
/// Who lost each game is the game's own verdict, stored when it finished (<see cref="GamePlayer.IsLoser"/>).
/// The "To je priveľa!" figures come only from games with calls (<see cref="GamesWithCalls"/>): games played before calls
/// were recorded hold none, so they would only water the averages and shares down.
/// </summary>
public sealed class PlayerStatistics
{
    /// <summary>
    /// Which games count: started, finished, not cancelled and with a result, that is a stored loser (a game ended
    /// before anybody held a bad card, possible before <c>Game.NoBadCardsYet</c>, has none). The one definition,
    /// shared by <see cref="For"/> and the database query that loads the games.
    /// </summary>
    public static readonly Expression<Func<Game, bool>> CountedGame =
        g => g.Started != null && g.Finished != null && !g.IsCancelled && g.GamePlayers.Any(gp => gp.IsLoser);

    private static readonly Func<Game, bool> IsCounted = CountedGame.Compile();

    private PlayerStatistics()
    {
    }

    public int GamesPlayed { get; private init; }

    public int BadCards { get; private init; }

    public double? AverageBadCards { get; private init; }

    public int FinalBadPoints { get; private init; }

    public double? AverageFinalBadPoints { get; private init; }

    public int Doubles { get; private init; }

    public double? AverageDoubles { get; private init; }

    /// <summary>Seats per game, unknown (deleted) players included.</summary>
    public double? AveragePlayers { get; private init; }

    /// <summary>Games the player lost, shared losses included.</summary>
    public int Losses { get; private init; }

    /// <summary>Losses the player shared with at least one other player.</summary>
    public int SharedLosses { get; private init; }

    /// <summary>
    /// Actual loss rate over the expected one, the average of (losers / players) per game: 1 is an average player,
    /// above 1 loses more often than chance. Null without a counted game.
    /// </summary>
    public double? LossIndex { get; private init; }

    /// <summary>Games the player finished without a single bad card and did not lose.</summary>
    public int FlawlessVictories { get; private init; }

    /// <summary>Games the player ended by reaching the bad card limit, yet somebody else lost.</summary>
    public int LuckyFinishes { get; private init; }

    /// <summary>Games the player escaped with exactly one final bad point less than the loser.</summary>
    public int ClosestEscapes { get; private init; }

    /// <summary>Games the player finished with negative final bad points.</summary>
    public int Reversals { get; private init; }

    /// <summary>
    /// Counted games with calls recorded: every card of a game is taken after a call, so a game with cards but no call
    /// is one played before calls were recorded. The call averages and card shares are per these games.
    /// </summary>
    public int GamesWithCalls { get; private init; }

    /// <summary>Times the player called "To je priveľa!" in a round that ended with a card.</summary>
    public int TooMuchCalls { get; private init; }

    public double? AverageTooMuchCalls { get; private init; }

    /// <summary>Calls after which the bidder took the card: the estimate really was too much.</summary>
    public int CorrectCalls { get; private init; }

    /// <summary>Calls after which the player took the card themselves: the estimate was not too much after all.</summary>
    public int FalseAlarms { get; private init; }

    /// <summary>Correct calls out of all calls, from 0 to 1; null without a call.</summary>
    public double? CallAccuracy { get; private init; }

    /// <summary>Cards the player took because the next player rightly called their bid too much.</summary>
    public int Caught { get; private init; }

    public double? AverageCaught { get; private init; }

    /// <summary>
    /// Times the next player called the player's bid too much: caught, or the next player's false alarm (a caller
    /// always calls the bid of the player before them).
    /// </summary>
    public int CalledOn { get; private init; }

    public double? AverageCalledOn { get; private init; }

    /// <summary>
    /// Of the player's cards in games with calls, the share taken for being caught overbidding, from 0 to 1; null
    /// without such a card. The rest are their false alarms.
    /// </summary>
    public double? CaughtCardShare { get; private init; }

    /// <summary>"Cried wolf": games the player lost holding more false-alarm cards than cards for being caught.</summary>
    public int CriedWolfLosses { get; private init; }

    /// <summary>Most final bad points in one game; the latest such game on a tie.</summary>
    public GameScore? WorstGame { get; private init; }

    /// <summary>Fewest final bad points in one game; the latest such game on a tie.</summary>
    public GameScore? BestGame { get; private init; }

    /// <summary>"Eagle eye": most correct calls in one game; the latest such game on a tie, null without a correct call.</summary>
    public GameTally? MostCorrectCalls { get; private init; }

    /// <param name="games">Any games; only the counted ones the player took part in are used.</param>
    public static PlayerStatistics For(int playerId, IEnumerable<Game> games)
    {
        ArgumentNullException.ThrowIfNull(games);

        var played = games
            .Where(g => IsCounted(g) && g.HasPlayer(playerId))
            .Select(g => new PlayedGame(g, g.GamePlayers.Single(gp => gp.PlayerId == playerId), Losers(g)))
            .ToList();

        var count = played.Count;
        var expectedLosses = played.Sum(p => (double)p.Losers.Count / p.Game.GamePlayers.Count);
        var losses = played.Count(p => p.IsLoss);
        var badCards = played.Sum(p => p.Seat.BadCards);
        var finalBadPoints = played.Sum(p => p.Seat.FinalBadPoints);
        var doubles = played.Sum(p => p.Seat.Doubles);
        var scores = played
            .Select(p => new GameScore(p.Game.Id, p.Game.Finished!.Value, p.Seat.FinalBadPoints))
            .OrderByDescending(s => s.Finished)
            .ThenByDescending(s => s.GameId)
            .ToList();
        var withCalls = played.Where(p => p.HasCalls).ToList();
        var callCount = withCalls.Count;
        var tooMuchCalls = withCalls.Sum(p => p.Seat.TooMuchCalls);
        var falseAlarms = withCalls.Sum(p => p.Seat.FalseAlarms);
        var correctCalls = tooMuchCalls - falseAlarms;
        var caught = withCalls.Sum(p => p.Seat.Caught);
        // The player after this one in the order of play is the one who calls their bids.
        var calledOn = withCalls.Sum(p => p.Seat.Caught + p.Game.NextSeatOf(p.Seat).FalseAlarms);
        var cardsAfterCalls = withCalls.Sum(p => p.Seat.BadCards);

        return new PlayerStatistics
        {
            GamesPlayed = count,
            BadCards = badCards,
            AverageBadCards = Ratio(badCards, count),
            FinalBadPoints = finalBadPoints,
            AverageFinalBadPoints = Ratio(finalBadPoints, count),
            Doubles = doubles,
            AverageDoubles = Ratio(doubles, count),
            AveragePlayers = Ratio(played.Sum(p => p.Game.GamePlayers.Count), count),
            Losses = losses,
            SharedLosses = played.Count(p => p.IsLoss && p.Losers.Count > 1),
            // Every counted game has a loser, so the expected losses are above zero whenever there is a game.
            LossIndex = count == 0 ? null : losses / expectedLosses,
            FlawlessVictories = played.Count(p => p.Seat.BadCards == 0 && !p.IsLoss),
            LuckyFinishes = played.Count(p => !p.IsLoss && p.Seat.BadCards >= p.Game.BadCardLimit),
            // All losers of a game share its most final bad points, so any of them gives the losing score.
            ClosestEscapes = played.Count(p => !p.IsLoss && p.Seat.FinalBadPoints == p.Losers[0].FinalBadPoints - 1),
            Reversals = played.Count(p => p.Seat.FinalBadPoints < 0),
            // Stable sorts over the latest-first list keep the latest game on a tie.
            WorstGame = scores.OrderByDescending(s => s.FinalBadPoints).FirstOrDefault(),
            BestGame = scores.OrderBy(s => s.FinalBadPoints).FirstOrDefault(),
            GamesWithCalls = callCount,
            TooMuchCalls = tooMuchCalls,
            AverageTooMuchCalls = Ratio(tooMuchCalls, callCount),
            CorrectCalls = correctCalls,
            FalseAlarms = falseAlarms,
            CallAccuracy = Ratio(correctCalls, tooMuchCalls),
            Caught = caught,
            AverageCaught = Ratio(caught, callCount),
            CalledOn = calledOn,
            AverageCalledOn = Ratio(calledOn, callCount),
            CaughtCardShare = Ratio(caught, cardsAfterCalls),
            CriedWolfLosses = withCalls.Count(p => p.IsLoss && p.Seat.FalseAlarms > p.Seat.Caught),
            MostCorrectCalls = played
                .Where(p => p.Seat.CorrectCalls > 0)
                .OrderByDescending(p => p.Seat.CorrectCalls)
                .ThenByDescending(p => p.Game.Finished)
                .ThenByDescending(p => p.Game.Id)
                .Select(p => new GameTally(p.Game.Id, p.Game.Finished!.Value, p.Seat.CorrectCalls))
                .FirstOrDefault(),
        };
    }

    private static IReadOnlyList<GamePlayer> Losers(Game game) =>
        game.GamePlayers.Where(gp => gp.IsLoser).ToList();

    /// <summary>An average or a share alike: null when there is nothing to divide by.</summary>
    private static double? Ratio(int part, int whole) => whole == 0 ? null : (double)part / whole;

    private sealed record PlayedGame(Game Game, GamePlayer Seat, IReadOnlyList<GamePlayer> Losers)
    {
        public bool IsLoss => Losers.Contains(Seat);

        public bool HasCalls => Game.GamePlayers.Any(gp => gp.TooMuchCalls > 0);
    }
}
