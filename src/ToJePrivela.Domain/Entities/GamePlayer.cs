using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

public class GamePlayer
{
    private GamePlayer()
    {
    }

    public GamePlayer(int playerId, int badPoints = 0)
    {
        PlayerId = playerId;
        BadPoints = Guard.AgainstNegative(badPoints, nameof(badPoints));
    }

    public int Id { get; private set; }

    public int GameId { get; private set; }

    public Game? Game { get; private set; }

    /// <summary>Null once the player was deleted: the seat stays in the game's history as an unknown player.</summary>
    public int? PlayerId { get; private set; }

    public Player? Player { get; private set; }

    public bool IsUnknownPlayer => PlayerId is null;

    public int BadPoints { get; private set; }

    /// <summary>Bad cards taken so far; <see cref="BadPoints"/> is the sum of their values.</summary>
    public int BadCards { get; private set; }

    /// <summary>Doubles that held: each takes one bad point off <see cref="FinalBadPoints"/>.</summary>
    public int Doubles { get; private set; }

    /// <summary>What counts at the end of the game: <see cref="BadPoints"/> minus one per double.</summary>
    public int FinalBadPoints => BadPoints - Doubles;

    internal void AddBadCard(int badPoints)
    {
        BadPoints += Guard.AgainstOutOfRange(badPoints, nameof(badPoints), Question.MinBadPoints, Question.MaxBadPoints);
        BadCards++;
    }

    internal void AddDouble() => Doubles++;

    /// <summary>The player is gone; the score stays, credited to nobody.</summary>
    internal void ForgetPlayer()
    {
        PlayerId = null;
        Player = null;
    }

    internal void RemoveDouble()
    {
        if (Doubles == 0)
        {
            throw new DomainException("Player has no double to take back.");
        }

        Doubles--;
    }
}
