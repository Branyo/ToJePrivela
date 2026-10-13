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

    /// <param name="accountId">The account (login) the game is played under; its players belong to it too.</param>
    public Game(
        int accountId,
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

        AccountId = Guard.AgainstOutOfRange(accountId, nameof(accountId), 1, int.MaxValue);
        BadCardLimit = Guard.AgainstOutOfRange(badCardLimit, nameof(badCardLimit), MinBadCardLimit, MaxBadCardLimit);
        BadPointsMode = badPointsMode;
        Started = UtcTime.Normalize(startedAt);
        _gamePlayers.AddRange(distinctIds.Select(id => new GamePlayer(id)));
    }

    public int Id { get; private set; }

    /// <summary>The account (login) the game was played under; only that login sees it.</summary>
    public int AccountId { get; private set; }

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

    /// <summary>
    /// Ending a running game is the players' own call, so its result counts as it stands, fair or not. Only a game
    /// nobody holds a bad card in has no result yet: it can be played on or deleted, not finished.
    /// </summary>
    public GameRuleViolation? CheckFinish(DateTime finishedAt) =>
        IsFinished ? GameRuleViolation.AlreadyFinished
        : UtcTime.Normalize(finishedAt) < Started ? GameRuleViolation.EndsBeforeStart
        : !HasBadCards ? GameRuleViolation.NoBadCardsYet
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

    /// <summary>Giving a running game its end is held to the same rule as <see cref="CheckFinish"/>.</summary>
    /// <param name="now">The current time: a game that has not started yet cannot be on record.</param>
    public GameRuleViolation? CheckReschedule(DateTime started, DateTime? finished, DateTime now) =>
        IsFinished && finished is null ? GameRuleViolation.CannotReopen
        : UtcTime.Normalize(started) > UtcTime.Normalize(now) ? GameRuleViolation.StartsInFuture
        : UtcTime.Normalize(finished) < UtcTime.Normalize(started) ? GameRuleViolation.EndsBeforeStart
        : !IsFinished && finished is not null && !HasBadCards ? GameRuleViolation.NoBadCardsYet
        : null;

    /// <summary>Ends the game and stores its verdict on the players (<see cref="GamePlayer.IsLoser"/>).</summary>
    public void Finish(DateTime finishedAt)
    {
        ThrowIfBroken(CheckFinish(finishedAt));

        Finished = UtcTime.Normalize(finishedAt);
        MarkLosers();
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

        var finishesNow = !IsFinished && finished is not null;

        Started = UtcTime.Normalize(started);
        Finished = UtcTime.Normalize(finished);

        if (finishesNow)
        {
            MarkLosers();
        }
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
    /// Where every player stands, worst first: the losers (<see cref="GamePlayer.IsLoser"/>, the verdict stored when
    /// the game finished), then most <see cref="GamePlayer.FinalBadPoints"/> (bad points minus doubles). Players with
    /// the same verdict and final bad points share the rank and keep their seat order (player id, unknown players
    /// last). Nobody has lost a running or cancelled game.
    /// </summary>
    public IReadOnlyList<GameStanding> Standings()
    {
        var ordered = _gamePlayers
            .OrderByDescending(gp => gp.IsLoser)
            .ThenByDescending(gp => gp.FinalBadPoints)
            .ThenBy(gp => gp.PlayerId ?? int.MaxValue)
            .ToList();

        var standings = new List<GameStanding>(ordered.Count);

        for (var index = 0; index < ordered.Count; index++)
        {
            var player = ordered[index];
            var sharesRank = index > 0
                && ordered[index - 1].IsLoser == player.IsLoser
                && ordered[index - 1].FinalBadPoints == player.FinalBadPoints;

            standings.Add(new GameStanding(player, sharesRank ? standings[index - 1].Rank : index + 1, player.IsLoser));
        }

        return standings;
    }

    private bool HasBadCards => _gamePlayers.Any(gp => gp.BadCards > 0);

    /// <summary>
    /// The verdict, decided once when the game finishes: everyone on the most final bad points loses, whatever their
    /// cards (a player without any included), so a tie is a shared loss. A game ended early counts as it stands.
    /// </summary>
    private void MarkLosers()
    {
        var most = _gamePlayers.Max(gp => gp.FinalBadPoints);

        foreach (var loser in _gamePlayers.Where(gp => gp.FinalBadPoints == most))
        {
            loser.MarkLoser();
        }
    }

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
