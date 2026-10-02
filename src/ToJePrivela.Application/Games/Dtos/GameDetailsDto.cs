namespace ToJePrivela.Application.Games.Dtos;

/// <param name="Cancelled">Ended without a result (and without a loser) because a player was deleted.</param>
public sealed record GameDetailsDto(
    int Id,
    DateTime? Started,
    DateTime? Finished,
    int BadCardLimit,
    string BadPointsMode,
    bool Cancelled,
    IReadOnlyList<GamePlayerDto> Players);

/// <summary>
/// <see cref="FinalBadPoints"/> is <see cref="BadPoints"/> minus one per double. <see cref="Rank"/> and
/// <see cref="IsLoser"/> are the game's own verdict (<c>Game.Standings</c>), so clients never decide who lost.
/// <see cref="PlayerId"/>, <see cref="Name"/> and <see cref="Avatar"/> are null for a player deleted since: an
/// unknown player whose score stays in the game's history.
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
    bool IsLoser);
