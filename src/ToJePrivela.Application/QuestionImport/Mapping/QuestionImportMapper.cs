using ToJePrivela.Application.QuestionImport.Dtos;
using ToJePrivela.Application.Questions.Mapping;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Application.QuestionImport.Mapping;

public static class QuestionImportMapper
{
    public static QuestionImportSummaryDto ToSummaryDto(QuestionImportOutcome outcome) =>
        new(outcome.Received, outcome.Questions.Count, ToRejectedDtos(outcome));

    public static ImportedQuestionsDto ToImportedDto(QuestionImportOutcome outcome, Language language) => new(
        outcome.Received,
        outcome.Questions.Count,
        ToRejectedDtos(outcome),
        QuestionMapper.ToDtos(outcome.Questions, language));

    private static IReadOnlyList<RejectedQuestionDto> ToRejectedDtos(QuestionImportOutcome outcome) =>
        outcome.Rejected.Select(rejected => new RejectedQuestionDto(rejected.Position, rejected.Reason)).ToList();
}
