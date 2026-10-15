using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

/// <summary>
/// One bad card handed out in a <see cref="Game"/>: who called "To je priveľa!" and who took the card. The caller taking
/// it themselves is a false alarm (the estimate was not too much); anyone else taking it was caught overdoing it. Rounds
/// nobody took a card in are not recorded. The question and its category are kept for statistics while they exist.
/// </summary>
public class GameCard
{
    private GameCard()
    {
    }

    internal GameCard(GamePlayer recipient, GamePlayer caller, Question question, int badPoints, DateTime awardedAt)
    {
        Recipient = recipient;
        Caller = caller;
        QuestionId = question.Id;
        CategoryId = question.CategoryId;
        BadPoints = badPoints;
        AwardedAt = UtcTime.Normalize(awardedAt);
    }

    public int Id { get; private set; }

    public int GameId { get; private set; }

    public int RecipientId { get; private set; }

    /// <summary>The seat that took the card.</summary>
    public GamePlayer Recipient { get; private set; } = null!;

    public int CallerId { get; private set; }

    /// <summary>The seat that called "To je priveľa!" in that round.</summary>
    public GamePlayer Caller { get; private set; } = null!;

    /// <summary>Null once the question (or its category) was deleted; the card stays in the game's history.</summary>
    public int? QuestionId { get; private set; }

    public int? CategoryId { get; private set; }

    /// <summary>What the card was worth when it was handed out.</summary>
    public int BadPoints { get; private set; }

    public DateTime AwardedAt { get; private set; }

    /// <summary>The caller took the card: the estimate they stopped was not too much after all.</summary>
    /// <remarks>Seats of a game not saved yet all have id 0, so those are told apart by reference.</remarks>
    public bool IsFalseAlarm => RecipientId != 0 ? RecipientId == CallerId : ReferenceEquals(Recipient, Caller);
}
