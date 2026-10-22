namespace ToJePrivela.Application.Games.Dtos;

/// <param name="Games">Newest first (by start), each with its players' scores and the stored verdict.</param>
/// <param name="HasMore">
/// Older games follow: ask again with <c>before</c> and <c>beforeId</c> set to the last listed game's <c>started</c> and id.
/// </param>
public sealed record GameHistoryDto(IReadOnlyList<GameDetailsDto> Games, bool HasMore);
