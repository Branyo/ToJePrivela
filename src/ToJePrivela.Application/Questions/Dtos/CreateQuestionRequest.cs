using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions.Dtos;

public sealed class CreateQuestionRequest
{
    [Required]
    [StringLength(Question.TextMaxLength, MinimumLength = Question.TextMinLength,
        ErrorMessage = "Question text should have from 8 to 512 characters.")]
    public string Text { get; init; } = default!;

    [Required]
    public string Answer { get; init; } = default!;

    [Required]
    [StringLength(Question.CategoryMaxLength, MinimumLength = Question.CategoryMinLength,
        ErrorMessage = "Category name should have from 2 to 32 characters.")]
    public string Category { get; init; } = default!;

    [Range(Question.MinDifficulty, Question.MaxDifficulty, ErrorMessage = "Difficulty should be between 1 and 5.")]
    public int Difficulty { get; init; }
}
