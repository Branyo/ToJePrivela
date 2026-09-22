using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

public class Game
{
    public const int MinPlayers = 2;
    public const int MaxPlayers = 10;

    private readonly List<GamePlayer> _gamePlayers = [];

    private Game()
    {
    }

    public Game(IEnumerable<int> playerIds, DateTime startedAt)
    {
        ArgumentNullException.ThrowIfNull(playerIds);

        var distinctIds = playerIds.Distinct().ToList();

        if (distinctIds.Count < MinPlayers || distinctIds.Count > MaxPlayers)
        {
            throw new DomainException(
                $"A game must have between {MinPlayers} and {MaxPlayers} distinct players.");
        }

        Started = startedAt;
        _gamePlayers.AddRange(distinctIds.Select(id => new GamePlayer(id)));
    }

    public int Id { get; private set; }

    public DateTime? Started { get; private set; }

    public DateTime? Finished { get; private set; }

    public IReadOnlyCollection<GamePlayer> GamePlayers => _gamePlayers.AsReadOnly();

    public bool IsFinished => Finished is not null;

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

    /// <summary>Overwrites the schedule; used by the update endpoint.</summary>
    public void Reschedule(DateTime? started, DateTime? finished)
    {
        if (started is not null && finished is not null && finished < started)
        {
            throw new DomainException("Game cannot be finished before it started.");
        }

        Started = started;
        Finished = finished;
    }
}
