namespace ToJePrivela.Domain.Entities;

/// <summary>
/// A <see cref="Game"/> rule a change would break. The <c>Check…</c> methods report it without throwing, so
/// a use case can turn it into an error; the changing methods run the same check and throw instead.
/// </summary>
public enum GameRuleViolation
{
    AlreadyFinished,
    PlayerNotInGame,
    CallerNotInGame,
    NoDoubleToRemove,
    ChosenBadPointsRequired,
    ChosenBadPointsNotAllowed,
    CannotReopen,
    EndsBeforeStart,
    StartsInFuture,
    NoBadCardsYet
}

internal static class GameRuleViolationMessages
{
    public static string Describe(GameRuleViolation violation) => violation switch
    {
        GameRuleViolation.AlreadyFinished => "Game is already finished.",
        GameRuleViolation.PlayerNotInGame => "Player does not play in this game.",
        GameRuleViolation.CallerNotInGame => "Who called \"too much\" does not play in this game.",
        GameRuleViolation.NoDoubleToRemove => "Player has no double to take back.",
        GameRuleViolation.ChosenBadPointsRequired =>
            "This game has its bad points chosen before each question, so the card needs them.",
        GameRuleViolation.ChosenBadPointsNotAllowed =>
            "This game takes bad points from the question, so they cannot be chosen.",
        GameRuleViolation.CannotReopen => "A finished game cannot be reopened.",
        GameRuleViolation.EndsBeforeStart => "Game cannot be finished before it started.",
        GameRuleViolation.StartsInFuture => "A game cannot start later than now.",
        GameRuleViolation.NoBadCardsYet =>
            "Nobody holds a bad card yet, so the game has no result: keep playing or delete it.",
        _ => throw new ArgumentOutOfRangeException(nameof(violation), violation, null)
    };
}
