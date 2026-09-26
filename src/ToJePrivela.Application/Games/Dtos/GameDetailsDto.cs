namespace ToJePrivela.Application.Games.Dtos;

public sealed record GameDetailsDto(
    int Id,
    DateTime? Started,
    DateTime? Finished,
    int BadCardLimit,
    IReadOnlyList<GamePlayerDto> Players);

public sealed record GamePlayerDto(int PlayerId, string Name, int BadPoints, int BadCards);
