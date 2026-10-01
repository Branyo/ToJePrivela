using ToJePrivela.Application.QuestionGeneration.Dtos;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

public sealed record CreatedQuestionCategoryDto(
    int Id,
    string Name,
    QuestionGenerationSummaryDto QuestionGeneration);
