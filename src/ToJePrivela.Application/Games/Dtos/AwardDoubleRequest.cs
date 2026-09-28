using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.Games.Dtos;

/// <summary>A double that held is always worth one bad point off, so the client only names the player.</summary>
public sealed class AwardDoubleRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Player id is required.")]
    public int PlayerId { get; init; }
}
