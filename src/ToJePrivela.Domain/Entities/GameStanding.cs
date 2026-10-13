namespace ToJePrivela.Domain.Entities;

/// <param name="Rank">1-based; players with the same verdict and equal final bad points share it.</param>
/// <param name="IsLoser">The verdict stored when the game finished (<see cref="GamePlayer.IsLoser"/>).</param>
public sealed record GameStanding(GamePlayer Player, int Rank, bool IsLoser);
