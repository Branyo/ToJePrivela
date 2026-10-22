namespace ToJePrivela.Domain.Entities;

/// <summary>
/// How often one account (login) has been shown one question. Questions are shared by every login, but which of them
/// are least viewed is decided per login, so one group's games never decide what another group gets to see. Written
/// only as one atomic increment by the persistence layer, so two devices showing a question at once both count.
/// </summary>
public class QuestionView
{
    private QuestionView()
    {
    }

    public int AccountId { get; private set; }

    public int QuestionId { get; private set; }

    /// <summary>At least 1: a question the login was never shown has no row.</summary>
    public int Count { get; private set; }

    public DateTime LastViewedAt { get; private set; }
}
