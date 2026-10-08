using System.ComponentModel.DataAnnotations;
using ToJePrivela.Application.Common.Validation;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions.Dtos;

public sealed class UpdateQuestionRequest
{
    [Required]
    [TrimmedLength(Question.TextMinLength, Question.TextMaxLength,
        ErrorMessage = "Slovak question text should have from {1} to {2} characters.")]
    public string TextSk { get; init; } = default!;

    /// <summary>The current English text is kept when left out.</summary>
    [TrimmedLength(Question.TextMinLength, Question.TextMaxLength,
        ErrorMessage = "English question text should have from {1} to {2} characters.")]
    public string? TextEn { get; init; }

    [Required]
    [Numeric(ErrorMessage = "Answer should be a number such as 42 or -3.5, without separators.")]
    public string Answer { get; init; } = default!;

    [Range(1, int.MaxValue, ErrorMessage = "Category id is required.")]
    public int CategoryId { get; init; }

    /// <summary>The current bad points are kept when left out.</summary>
    [Range(Question.MinBadPoints, Question.MaxBadPoints, ErrorMessage = "Bad points should be between {1} and {2}.")]
    public int? BadPoints { get; init; }
}
