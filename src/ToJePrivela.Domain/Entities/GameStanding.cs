namespace ToJePrivela.Domain.Entities;

/// <param name="Rank">1-based; players with equal final bad points share it, whatever their cards.</param>
/// <param name="IsLoser">Holds the most final bad points; several on a tie, whatever their cards.</param>
public sealed record GameStanding(GamePlayer Player, int Rank, bool IsLoser);
