using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

public class Game
{
    public const int MinPlayers = 2;
    public const int MaxPlayers = 12;
    public const int MinBadCardLimit = 2;
    public const int MaxBadCardLimit = 10;
    public const int DefaultBadCardLimit = 3;

    private readonly List<GamePlayer> _gamePlayers = [];

    private Game()
    {
    }

    public Game(
        IEnumerable<int> playerIds,
        DateTime startedAt,
        int badCardLimit = DefaultBadCardLimit,
        BadPointsMode badPointsMode = BadPointsMode.Question)
    {
        ArgumentNullException.ThrowIfNull(playerIds);

        var distinctIds = playerIds.Distinct().ToList();

        if (distinctIds.Count < MinPlayers || distinctIds.Count > MaxPlayers)
        {
            throw new DomainException(
                $"A game must have between {MinPlayers} and {MaxPlayers} distinct players.");
        }

        if (!Enum.IsDefined(badPointsMode))
        {
            throw new DomainException($"Unknown bad points mode {badPointsMode}.");
        }

        BadCardLimit = Guard.AgainstOutOfRange(badCardLimit, nameof(badCardLimit), MinBadCardLimit, MaxBadCardLimit);
        BadPointsMode = badPointsMode;
        Started = UtcTime.Normalize(startedAt);
        _gamePlayers.AddRange(distinctIds.Select(id => new GamePlayer(id)));
    }

    public int Id { get; private set; }

    public DateTime? Started { get; private set; }

    public DateTime? Finished { get; private set; }

    /// <summary>The game ends as soon as one player holds this many bad cards.</summary>
    public int BadCardLimit { get; private set; }

    /// <summary>Where each bad card's worth comes from; fixed for the whole game.</summary>
    public BadPointsMode BadPointsMode { get; private set; }

    public IReadOnlyCollection<GamePlayer> GamePlayers => _gamePlayers.AsReadOnly();

    public bool IsFinished => Finished is not null;

    /// <summary>Ended without a result because one of its players was deleted; nobody lost it.</summary>
    public bool IsCancelled { get; private set; }

    public bool HasPlayer(int playerId) => _gamePlayers.Any(gp => gp.PlayerId == playerId);

    /// <summary>
    /// Whether a bad card needs the bad points the round's starting player chose: required in a
    /// <see cref="BadPointsMode.Chooser"/> game, not allowed in a <see cref="BadPointsMode.Question"/> one.
    /// </summary>
    public bool RequiresChosenBadPoints => BadPointsMode == BadPointsMode.Chooser;

    public GameRuleViolation? CheckFinish(DateTime finishedAt) =>
        IsFinished ? GameRuleViolation.AlreadyFinished
        : UtcTime.Normalize(finishedAt) < Started ? GameRuleViolation.EndsBeforeStart
        : null;

    public GameRuleViolation? CheckAwardBadCard(int playerId, int? chosenBadPoints) =>
        CheckRunningPlayer(playerId)
        ?? (RequiresChosenBadPoints, chosenBadPoints.HasValue) switch
        {
            (true, false) => GameRuleViolation.ChosenBadPointsRequired,
            (false, true) => GameRuleViolation.ChosenBadPointsNotAllowed,
            _ => null
        };

    public GameRuleViolation? CheckAwardDouble(int playerId) => CheckRunningPlayer(playerId);

    public GameRuleViolation? CheckRemoveDouble(int playerId) =>
        CheckRunningPlayer(playerId)
        ?? (FindPlayer(playerId)!.Doubles == 0 ? GameRuleViolation.NoDoubleToRemove : null);

    /// <param name="now">The current time: a game that has not started yet cannot be on record.</param>
    public GameRuleViolation? CheckReschedule(DateTime started, DateTime? finished, DateTime now) =>
        IsFinished && finished is null ? GameRuleViolation.CannotReopen
        : UtcTime.Normalize(started) > UtcTime.Normalize(now) ? GameRuleViolation.StartsInFuture
        : UtcTime.Normalize(finished) < UtcTime.Normalize(started) ? GameRuleViolation.EndsBeforeStart
        : null;

    public void Finish(DateTime finishedAt)
    {
        ThrowIfBroken(CheckFinish(finishedAt));

        Finished = UtcTime.Normalize(finishedAt);
    }

    /// <summary>
    /// Gives the player a bad card for <paramref name="question"/>, worth what <see cref="BadPointsMode"/>
    /// says: the question's stored bad points, or <paramref name="chosenBadPoints"/> in a chooser game.
    /// The game finishes when that player reaches <see cref="BadCardLimit"/> cards.
    /// </summary>
    public GamePlayer AwardBadCard(int playerId, Question question, int? chosenBadPoints, DateTime awardedAt)
    {
        ArgumentNullException.ThrowIfNull(question);
        ThrowIfBroken(CheckAwardBadCard(playerId, chosenBadPoints));

        var gamePlayer = FindPlayer(playerId)!;

        gamePlayer.AddBadCard(chosenBadPoints ?? question.BadPoints);

        if (gamePlayer.BadCards >= BadCardLimit)
        {
            Finish(awardedAt);
        }

        return gamePlayer;
    }

    /// <summary>
    /// Credits the player with a double that held (the next player raised the doubled estimate or
    /// wrongly called "too much"). It is worth one bad point off and never ends the game.
    /// </summary>
    public GamePlayer AwardDouble(int playerId)
    {
        ThrowIfBroken(CheckAwardDouble(playerId));

        var gamePlayer = FindPlayer(playerId)!;

        gamePlayer.AddDouble();

        return gamePlayer;
    }

    /// <summary>Takes back a double tapped by mistake; the final score may still be below zero.</summary>
    public GamePlayer RemoveDouble(int playerId)
    {
        ThrowIfBroken(CheckRemoveDouble(playerId));

        var gamePlayer = FindPlayer(playerId)!;

        gamePlayer.RemoveDouble();

        return gamePlayer;
    }

    /// <summary>
    /// Corrects when the game started and finished. A running game may be given its end here, but a
    /// finished game can never be reopened: it may have ended because a player reached the limit. The start can
    /// never lie after <paramref name="now"/>. Both times are kept in UTC (see <see cref="UtcTime"/>).
    /// </summary>
    public void Reschedule(DateTime started, DateTime? finished, DateTime now)
    {
        ThrowIfBroken(CheckReschedule(started, finished, now));

        Started = UtcTime.Normalize(started);
        Finished = UtcTime.Normalize(finished);
    }

    /// <summary>
    /// The player is being deleted: their seat stays as an unknown player, so the game keeps its history. A game
    /// still running cannot be played on without them, so it is cancelled at <paramref name="at"/> (or at its
    /// start, should that lie later) and can never be resumed.
    /// </summary>
    public void ForgetPlayer(int playerId, DateTime at)
    {
        var gamePlayer = FindPlayer(playerId) ?? throw new DomainException(
            GameRuleViolationMessages.Describe(GameRuleViolation.PlayerNotInGame));

        if (!IsFinished)
        {
            var cancelledAt = UtcTime.Normalize(at);
            Finished = Started > cancelledAt ? Started : cancelledAt;
            IsCancelled = true;
        }

        gamePlayer.ForgetPlayer();
    }

    /// <summary>
    /// Where every player stands, worst first: most <see cref="GamePlayer.FinalBadPoints"/> (bad points minus
    /// doubles), a tie broken by the number of bad cards. Whoever stands first loses, several players on a full
    /// tie; nobody loses while nobody holds a card, nor in a cancelled game. Players who tie keep their seat order
    /// (player id, unknown players last).
    /// </summary>
    public IReadOnlyList<GameStanding> Standings()
    {
        var ordered = _gamePlayers
            .OrderByDescending(gp => gp.FinalBadPoints)
            .ThenByDescending(gp => gp.BadCards)
            .ThenBy(gp => gp.PlayerId ?? int.MaxValue)
            .ToList();

        var anyLoser = !IsCancelled && ordered.Any(gp => gp.BadCards > 0);
        var standings = new List<GameStanding>(ordered.Count);

        for (var index = 0; index < ordered.Count; index++)
        {
            var player = ordered[index];
            var rank = index > 0 && SameStanding(ordered[index - 1], player) ? standings[index - 1].Rank : index + 1;

            standings.Add(new GameStanding(player, rank, anyLoser && SameStanding(ordered[0], player)));
        }

        return standings;
    }

    private static bool SameStanding(GamePlayer a, GamePlayer b) =>
        a.FinalBadPoints == b.FinalBadPoints && a.BadCards == b.BadCards;

    private GameRuleViolation? CheckRunningPlayer(int playerId) =>
        IsFinished ? GameRuleViolation.AlreadyFinished
        : !HasPlayer(playerId) ? GameRuleViolation.PlayerNotInGame
        : null;

    private GamePlayer? FindPlayer(int playerId) => _gamePlayers.FirstOrDefault(gp => gp.PlayerId == playerId);

    private static void ThrowIfBroken(GameRuleViolation? violation)
    {
        if (violation is GameRuleViolation broken)
        {
            throw new DomainException(GameRuleViolationMessages.Describe(broken));
        }
    }
}
