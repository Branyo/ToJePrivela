namespace ToJePrivela.Application.Games.Dtos;

public sealed record GameDetailsDto(
    int Id,
    DateTime? Started,
    DateTime? Finished,
    int BadCardLimit,
    IReadOnlyList<GamePlayerDto> Players);

/// <summary><see cref="FinalBadPoints"/> is <see cref="BadPoints"/> minus one per double and decides the loser.</summary>
public sealed record GamePlayerDto(int PlayerId, string Name, string Avatar, int BadPoints, int BadCards, int Doubles, int FinalBadPoints);
