using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

public sealed class CreateQuestionCategoryRequest
{
    [Required]
    [StringLength(QuestionCategory.NameMaxLength, MinimumLength = QuestionCategory.NameMinLength,
        ErrorMessage = "Question category name should have from 2 to 32 characters.")]
    public string Name { get; init; } = default!;

    public int? AddedByPlayerId { get; init; }
}
