using ToJePrivela.Application.QuestionGeneration.Dtos;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

/// <param name="Name">The name in the request's language, ready to show.</param>
/// <param name="QuestionCount">A new category holds only the questions just generated, so it equals <paramref name="AiQuestionCount"/>.</param>
public sealed record CreatedQuestionCategoryDto(
    int Id,
    string Name,
    string NameSk,
    string NameEn,
    int QuestionCount,
    int AiQuestionCount,
    QuestionGenerationSummaryDto QuestionGeneration);
