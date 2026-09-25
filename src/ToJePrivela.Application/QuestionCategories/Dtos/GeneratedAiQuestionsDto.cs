using ToJePrivela.Application.QuestionGeneration.Dtos;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

public sealed record GeneratedAiQuestionsDto(QuestionGenerationSummaryDto Summary, IReadOnlyList<QuestionDto> Questions);
