namespace ToJePrivela.Application.Games.Dtos;

public sealed record GameDetailsDto(
    int Id,
    DateTime? Started,
    DateTime? Finished,
    int BadCardLimit,
    string BadPointsMode,
    IReadOnlyList<GamePlayerDto> Players);

/// <summary>
/// <see cref="FinalBadPoints"/> is <see cref="BadPoints"/> minus one per double. <see cref="Rank"/> and
/// <see cref="IsLoser"/> are the game's own verdict (<c>Game.Standings</c>), so clients never decide who lost.
/// </summary>
public sealed record GamePlayerDto(
    int PlayerId,
    string Name,
    string Avatar,
    int BadPoints,
    int BadCards,
    int Doubles,
    int FinalBadPoints,
    int Rank,
    bool IsLoser);
