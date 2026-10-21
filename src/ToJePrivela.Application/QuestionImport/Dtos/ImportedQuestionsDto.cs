using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Application.QuestionImport.Dtos;

/// <param name="Questions">The stored questions.</param>
public sealed record ImportedQuestionsDto(QuestionImportSummaryDto Summary, IReadOnlyList<QuestionDto> Questions);

/// <summary>What an import brought, without the questions themselves.</summary>
/// <param name="Received">Items in the JSON array.</param>
/// <param name="Rejected">The items that were not stored, in array order.</param>
public sealed record QuestionImportSummaryDto(int Received, int Imported, IReadOnlyList<RejectedQuestionDto> Rejected);

/// <param name="Position">The item's 1-based position in the JSON array.</param>
public sealed record RejectedQuestionDto(int Position, QuestionRejection Reason);
