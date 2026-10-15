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

    /// <summary>Rounds the player called "To je priveľa!" in that ended with a card (skipped rounds never count).</summary>
    public int TooMuchCalls { get; private set; }

    /// <summary>
    /// Of <see cref="TooMuchCalls"/>, those the player took the card for: the estimate was not too much after all.
    /// </summary>
    public int FalseAlarms { get; private set; }

    /// <summary>The other <see cref="TooMuchCalls"/>: somebody else took the card, so the call was right.</summary>
    public int CorrectCalls => TooMuchCalls - FalseAlarms;

    /// <summary>Cards the player took after somebody else called "To je priveľa!": caught overdoing it.</summary>
    public int Caught { get; private set; }

    /// <summary>
    /// The game's verdict, stored when it finished (see <see cref="Game.Standings"/>), so a later change of the rule
    /// never rewrites a result. Always false while the game runs and in a cancelled game.
    /// </summary>
    public bool IsLoser { get; private set; }

    internal void AddCaught() => Caught++;

    internal void AddBadCard(int badPoints)
    {
        BadPoints += Guard.AgainstOutOfRange(badPoints, nameof(badPoints), Question.MinBadPoints, Question.MaxBadPoints);
        BadCards++;
    }

    internal void AddDouble() => Doubles++;

    internal void AddTooMuchCall(bool falseAlarm)
    {
        TooMuchCalls++;
        if (falseAlarm)
        {
            FalseAlarms++;
        }
    }

    internal void MarkLoser() => IsLoser = true;

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
