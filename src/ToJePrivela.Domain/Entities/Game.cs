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
        Started = startedAt;
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

    public bool HasPlayer(int playerId) => _gamePlayers.Any(gp => gp.PlayerId == playerId);

    /// <summary>
    /// Whether a bad card needs the bad points the round's starting player chose: required in a
    /// <see cref="BadPointsMode.Chooser"/> game, not allowed in a <see cref="BadPointsMode.Question"/> one.
    /// </summary>
    public bool RequiresChosenBadPoints => BadPointsMode == BadPointsMode.Chooser;

    public void Finish(DateTime finishedAt)
    {
        if (IsFinished)
        {
            throw new DomainException("Game is already finished.");
        }

        if (Started is not null && finishedAt < Started)
        {
            throw new DomainException("Game cannot be finished before it started.");
        }

        Finished = finishedAt;
    }

    /// <summary>
    /// Gives the player a bad card for <paramref name="question"/>, worth what <see cref="BadPointsMode"/>
    /// says: the question's stored bad points, or <paramref name="chosenBadPoints"/> in a chooser game.
    /// The game finishes when that player reaches <see cref="BadCardLimit"/> cards.
    /// </summary>
    public GamePlayer AwardBadCard(int playerId, Question question, int? chosenBadPoints, DateTime awardedAt)
    {
        ArgumentNullException.ThrowIfNull(question);

        var gamePlayer = RunningGamePlayer(playerId);

        gamePlayer.AddBadCard(BadCardWorth(question, chosenBadPoints));

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
        var gamePlayer = RunningGamePlayer(playerId);

        gamePlayer.AddDouble();

        return gamePlayer;
    }

    /// <summary>Takes back a double tapped by mistake; the final score may still be below zero.</summary>
    public GamePlayer RemoveDouble(int playerId)
    {
        var gamePlayer = RunningGamePlayer(playerId);

        gamePlayer.RemoveDouble();

        return gamePlayer;
    }

    /// <summary>
    /// Corrects when the game started and finished. A running game may be given its end here, but a
    /// finished game can never be reopened: it may have ended because a player reached the limit.
    /// </summary>
    public void Reschedule(DateTime started, DateTime? finished)
    {
        if (IsFinished && finished is null)
        {
            throw new DomainException("A finished game cannot be reopened.");
        }

        if (finished < started)
        {
            throw new DomainException("Game cannot be finished before it started.");
        }

        Started = started;
        Finished = finished;
    }

    private int BadCardWorth(Question question, int? chosenBadPoints) =>
        (RequiresChosenBadPoints, chosenBadPoints) switch
        {
            (true, int chosen) => chosen,
            (true, null) => throw new DomainException(
                "This game has its bad points chosen before each question, so the card needs them."),
            (false, null) => question.BadPoints,
            (false, _) => throw new DomainException(
                "This game takes bad points from the question, so they cannot be chosen.")
        };

    private GamePlayer RunningGamePlayer(int playerId)
    {
        if (IsFinished)
        {
            throw new DomainException("Game is already finished.");
        }

        return _gamePlayers.FirstOrDefault(gp => gp.PlayerId == playerId)
            ?? throw new DomainException($"Player {playerId} does not play in this game.");
    }
}
