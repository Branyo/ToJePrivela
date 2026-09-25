using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions.Dtos;

public sealed class UpdateQuestionRequest
{
    [Required]
    [StringLength(Question.TextMaxLength, MinimumLength = Question.TextMinLength,
        ErrorMessage = "Question text should have from 8 to 512 characters.")]
    public string Text { get; init; } = default!;

    [Required]
    public string Answer { get; init; } = default!;

    [Range(1, int.MaxValue, ErrorMessage = "Category id is required.")]
    public int CategoryId { get; init; }

    /// <summary>The current bad points are kept when left out.</summary>
    [Range(Question.MinBadPoints, Question.MaxBadPoints, ErrorMessage = "Bad points should be between 1 and 5.")]
    public int? BadPoints { get; init; }
}
