namespace ToJePrivela.Domain.Entities;

/// <summary>Where a game's bad cards take their value from; chosen when the game is created.</summary>
public enum BadPointsMode
{
    /// <summary>Every card is worth the bad points stored on its question.</summary>
    Question,

    /// <summary>
    /// Before each question the round's starting player sees only its category and sets the card's worth,
    /// <see cref="Question.MinBadPoints"/>–<see cref="Question.MaxBadPoints"/>.
    /// </summary>
    Chooser
}
