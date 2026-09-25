using System.ComponentModel.DataAnnotations;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

public sealed class CreateQuestionCategoryRequest
{
    [Required]
    [StringLength(QuestionCategory.NameMaxLength, MinimumLength = QuestionCategory.NameMinLength,
        ErrorMessage = "Question category name should have from 2 to 32 characters.")]
    public string Name { get; init; } = default!;

    public int? AddedByPlayerId { get; init; }

    /// <summary>AI questions generated together with the category; nullable so leaving it out is an error, not 0.</summary>
    [Required(ErrorMessage = "Question count is required; send 0 to create the category without questions.")]
    [Range(0, QuestionGenerationOptions.MaxCount, ErrorMessage = "Question count should be between 0 and 200.")]
    public int? QuestionCount { get; init; }
}
