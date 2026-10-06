namespace ToJePrivela.Application.Rules.Dtos;

/// <summary>Inclusive bounds of a number or of a text's length.</summary>
public sealed record LimitDto(int Min, int Max);

/// <summary>
/// The limits the domain enforces, served so clients validate and render with the same numbers instead of
/// copies of them.
/// </summary>
/// <param name="Avatars">Every avatar a player can be given, in a fixed order.</param>
/// <param name="LoginName">Length of a new login's name.</param>
/// <param name="Password">Length of a new login's password.</param>
public sealed record GameRulesDto(
    LimitDto Players,
    LimitDto BadCardLimit,
    int DefaultBadCardLimit,
    LimitDto BadPoints,
    LimitDto PlayerName,
    LimitDto CategoryName,
    int MaxAiQuestionCount,
    IReadOnlyList<string> Avatars,
    LimitDto LoginName,
    LimitDto Password);
