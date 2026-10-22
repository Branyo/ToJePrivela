using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.Games.Dtos;

/// <summary>A "To je priveľa!" call nobody took a card for: only who called it, the next round's starter.</summary>
public sealed class RecordCallRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Player id is required.")]
    public int PlayerId { get; init; }
}
