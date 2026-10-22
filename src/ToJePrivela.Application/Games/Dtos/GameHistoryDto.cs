namespace ToJePrivela.Application.Games.Dtos;

/// <param name="Games">Newest first (by start), each with its players' scores and the stored verdict.</param>
/// <param name="HasMore">Older games follow: ask again with <c>skip</c> moved past this page.</param>
public sealed record GameHistoryDto(IReadOnlyList<GameDetailsDto> Games, bool HasMore);
