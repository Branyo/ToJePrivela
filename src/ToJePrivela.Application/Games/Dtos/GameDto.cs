namespace ToJePrivela.Application.Games.Dtos;

/// <param name="Cancelled">Ended without a result because a player was deleted.</param>
/// <param name="PlayerIds">In seat order; null for a player who was deleted since.</param>
public sealed record GameDto(
    int Id,
    DateTime? Started,
    DateTime? Finished,
    int BadCardLimit,
    string BadPointsMode,
    bool Cancelled,
    IReadOnlyList<int?> PlayerIds);
