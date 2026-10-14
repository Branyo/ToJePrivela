using System.Linq.Expressions;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Statistics;

/// <summary>One game's final bad points for a player, and the game it was scored in.</summary>
public sealed record GameScore(int GameId, DateTime Finished, int FinalBadPoints);

/// <summary>
/// A player's record over their counted games: started, finished, not cancelled and with a loser. Averages are per counted game
/// and null when there is none.
/// Who lost each game is the game's own verdict, stored when it finished (<see cref="GamePlayer.IsLoser"/>).
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

    /// <summary>Most final bad points in one game; the latest such game on a tie.</summary>
    public GameScore? WorstGame { get; private init; }

    /// <summary>Fewest final bad points in one game; the latest such game on a tie.</summary>
    public GameScore? BestGame { get; private init; }

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

        return new PlayerStatistics
        {
            GamesPlayed = count,
            BadCards = badCards,
            AverageBadCards = Average(badCards, count),
            FinalBadPoints = finalBadPoints,
            AverageFinalBadPoints = Average(finalBadPoints, count),
            Doubles = doubles,
            AverageDoubles = Average(doubles, count),
            AveragePlayers = Average(played.Sum(p => p.Game.GamePlayers.Count), count),
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
        };
    }

    private static IReadOnlyList<GamePlayer> Losers(Game game) =>
        game.GamePlayers.Where(gp => gp.IsLoser).ToList();

    private static double? Average(int total, int count) => count == 0 ? null : (double)total / count;

    private sealed record PlayedGame(Game Game, GamePlayer Seat, IReadOnlyList<GamePlayer> Losers)
    {
        public bool IsLoss => Losers.Contains(Seat);
    }
}
