using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.QuestionGeneration.Dtos;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

public sealed record CreatedQuestionCategoryDto(
    int Id,
    string Name,
    PlayerDto? AddedByPlayer,
    QuestionGenerationSummaryDto QuestionGeneration);
