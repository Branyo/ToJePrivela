namespace ToJePrivela.Domain.Entities;

/// <summary>
/// A <see cref="Game"/> rule a change would break. The <c>Check…</c> methods report it without throwing, so
/// a use case can turn it into an error; the changing methods run the same check and throw instead.
/// </summary>
public enum GameRuleViolation
{
    AlreadyFinished,
    PlayerNotInGame,
    NoDoubleToRemove,
    ChosenBadPointsRequired,
    ChosenBadPointsNotAllowed,
    CannotReopen,
    EndsBeforeStart
}

internal static class GameRuleViolationMessages
{
    public static string Describe(GameRuleViolation violation) => violation switch
    {
        GameRuleViolation.AlreadyFinished => "Game is already finished.",
        GameRuleViolation.PlayerNotInGame => "Player does not play in this game.",
        GameRuleViolation.NoDoubleToRemove => "Player has no double to take back.",
        GameRuleViolation.ChosenBadPointsRequired =>
            "This game has its bad points chosen before each question, so the card needs them.",
        GameRuleViolation.ChosenBadPointsNotAllowed =>
            "This game takes bad points from the question, so they cannot be chosen.",
        GameRuleViolation.CannotReopen => "A finished game cannot be reopened.",
        GameRuleViolation.EndsBeforeStart => "Game cannot be finished before it started.",
        _ => throw new ArgumentOutOfRangeException(nameof(violation), violation, null)
    };
}
