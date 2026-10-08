using ToJePrivela.Application.QuestionGeneration.Dtos;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

/// <param name="Name">The name in the request's language, ready to show.</param>
public sealed record CreatedQuestionCategoryDto(
    int Id,
    string Name,
    string NameSk,
    string NameEn,
    QuestionGenerationSummaryDto QuestionGeneration);
