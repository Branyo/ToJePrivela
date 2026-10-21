using ToJePrivela.Application.QuestionImport.Dtos;
using ToJePrivela.Application.Questions.Mapping;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Application.QuestionImport.Mapping;

public static class QuestionImportMapper
{
    public static QuestionImportSummaryDto ToSummaryDto(QuestionImportOutcome outcome) =>
        new(outcome.Received, outcome.Questions.Count, outcome.Rejected);

    public static ImportedQuestionsDto ToImportedDto(QuestionImportOutcome outcome, Language language) =>
        new(ToSummaryDto(outcome), QuestionMapper.ToDtos(outcome.Questions, language));
}
