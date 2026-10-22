namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>Where a game stands in the history's order (newest start first, then highest id): a page continues after it.</summary>
public sealed record GamePosition(DateTime? Started, int Id);
