namespace ToJePrivela.Application.Rules.Dtos;

/// <summary>Inclusive bounds of a number or of a text's length.</summary>
public sealed record LimitDto(int Min, int Max);

/// <summary>
/// The limits the domain enforces, served so clients validate and render with the same numbers instead of
/// copies of them.
/// </summary>
/// <param name="MaxPlayersPerAccount">How many players one login may keep.</param>
/// <param name="Avatars">Every avatar a player can be given, in a fixed order.</param>
/// <param name="LoginName">Length of a new login's name.</param>
/// <param name="Password">Length of a new login's password.</param>
/// <param name="QuestionText">Length of a question's text, in either language.</param>
public sealed record GameRulesDto(
    LimitDto Players,
    LimitDto BadCardLimit,
    int DefaultBadCardLimit,
    LimitDto BadPoints,
    LimitDto PlayerName,
    int MaxPlayersPerAccount,
    LimitDto CategoryName,
    int MaxAiQuestionCount,
    IReadOnlyList<string> Avatars,
    LimitDto LoginName,
    LimitDto Password,
    LimitDto QuestionText);
