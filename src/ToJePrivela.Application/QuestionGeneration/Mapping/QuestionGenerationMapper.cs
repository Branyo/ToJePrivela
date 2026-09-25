using ToJePrivela.Application.QuestionGeneration.Dtos;

namespace ToJePrivela.Application.QuestionGeneration.Mapping;

public static class QuestionGenerationMapper
{
    public static QuestionGenerationSummaryDto ToSummaryDto(QuestionGenerationResult result) =>
        new(result.Requested, result.Created, result.Discarded);
}
