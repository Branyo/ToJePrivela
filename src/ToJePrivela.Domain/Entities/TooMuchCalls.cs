namespace ToJePrivela.Domain.Entities;

/// <summary>A seat's "To je priveľa!" record in one game (see <see cref="Game.CallsOf"/>).</summary>
/// <param name="Calls">Rounds the seat called "To je priveľa!" in that ended with a card.</param>
/// <param name="FalseAlarms">Of those, the ones the seat took the card for itself.</param>
/// <param name="Caught">Cards the seat took after somebody else called "To je priveľa!" on it.</param>
public sealed record TooMuchCalls(int Calls, int FalseAlarms, int Caught);
