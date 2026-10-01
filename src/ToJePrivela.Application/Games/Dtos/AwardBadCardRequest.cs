using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Games.Dtos;

/// <summary>
/// In a <see cref="BadPointsMode.Question"/> game the card is worth the bad points stored on the question, so
/// <see cref="BadPoints"/> must be left out; in a <see cref="BadPointsMode.Chooser"/> game it is required and is
/// what the starting player set before the question was shown.
/// </summary>
public sealed class AwardBadCardRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Player id is required.")]
    public int PlayerId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Question id is required.")]
    public int QuestionId { get; init; }

    [Range(Question.MinBadPoints, Question.MaxBadPoints, ErrorMessage = "Bad points should be between {1} and {2}.")]
    public int? BadPoints { get; init; }
}
