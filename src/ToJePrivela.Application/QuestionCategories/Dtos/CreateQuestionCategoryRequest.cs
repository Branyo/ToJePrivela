using System.ComponentModel.DataAnnotations;
using ToJePrivela.Application.Common.Validation;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

/// <summary>
/// Names the category in Slovak, in English or in both; a missing name is translated from the given one by the AI.
/// </summary>
public sealed class CreateQuestionCategoryRequest : IValidatableObject
{
    [TrimmedLength(QuestionCategory.NameMinLength, QuestionCategory.NameMaxLength,
        ErrorMessage = "Slovak category name should have from {1} to {2} characters.")]
    public string? NameSk { get; init; }

    [TrimmedLength(QuestionCategory.NameMinLength, QuestionCategory.NameMaxLength,
        ErrorMessage = "English category name should have from {1} to {2} characters.")]
    public string? NameEn { get; init; }

    /// <summary>AI questions generated together with the category; nullable so leaving it out is an error, not 0.</summary>
    [Required(ErrorMessage = "Question count is required; send 0 to create the category without questions.")]
    [Range(0, QuestionGenerationOptions.MaxCount, ErrorMessage = "Question count should be between {1} and {2}.")]
    public int? QuestionCount { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NameSk) && string.IsNullOrWhiteSpace(NameEn))
        {
            yield return new ValidationResult(
                "Category name is required in Slovak or in English.",
                [nameof(NameSk), nameof(NameEn)]);
        }
    }
}
