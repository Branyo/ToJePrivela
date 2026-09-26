using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.Games.Dtos;

/// <summary>The card is worth the bad points stored on the question, so clients cannot choose them.</summary>
public sealed class AwardBadCardRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Player id is required.")]
    public int PlayerId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Question id is required.")]
    public int QuestionId { get; init; }
}
