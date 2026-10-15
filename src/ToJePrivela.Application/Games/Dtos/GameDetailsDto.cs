namespace ToJePrivela.Application.Games.Dtos;

/// <param name="Cancelled">Ended without a result (and without a loser) because a player was deleted.</param>
/// <param name="HasResult">
/// Somebody holds a bad card (<c>Game.HasResult</c>): only then can the game be finished; clients follow it instead of
/// deciding themselves.
/// </param>
public sealed record GameDetailsDto(
    int Id,
    DateTime? Started,
    DateTime? Finished,
    int BadCardLimit,
    string BadPointsMode,
    bool Cancelled,
    bool HasResult,
    IReadOnlyList<GamePlayerDto> Players);

/// <summary>
/// <see cref="FinalBadPoints"/> is <see cref="BadPoints"/> minus one per double. <see cref="Rank"/> and
/// <see cref="IsLoser"/> are the game's own verdict (<c>Game.Standings</c>, the losers stored when it finished), so
/// clients never decide who lost.
/// <see cref="PlayerId"/>, <see cref="Name"/> and <see cref="Avatar"/> are null for a player deleted since: an
/// unknown player whose score stays in the game's history.
/// <see cref="TooMuchCalls"/> counts the rounds the player called "To je priveľa!" in (each ended with a card) and
/// <see cref="FalseAlarms"/> those of them the player took the card for (the estimate was not too much after all);
/// <see cref="Caught"/> counts the cards the player took after somebody else's call.
/// </summary>
public sealed record GamePlayerDto(
    int? PlayerId,
    string? Name,
    string? Avatar,
    int BadPoints,
    int BadCards,
    int Doubles,
    int FinalBadPoints,
    int Rank,
    bool IsLoser,
    int TooMuchCalls,
    int FalseAlarms,
    int Caught);
