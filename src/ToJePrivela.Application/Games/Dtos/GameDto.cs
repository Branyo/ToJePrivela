namespace ToJePrivela.Application.Games.Dtos;

public sealed record GameDto(
    int Id,
    DateTime? Started,
    DateTime? Finished,
    int BadCardLimit,
    IReadOnlyList<int> PlayerIds);
