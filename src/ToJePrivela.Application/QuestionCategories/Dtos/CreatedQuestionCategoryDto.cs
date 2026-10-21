using ToJePrivela.Application.QuestionGeneration.Dtos;
using ToJePrivela.Application.QuestionImport.Dtos;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

/// <param name="Name">The name in the request's language, ready to show.</param>
/// <param name="QuestionCount">
/// A new category holds only the questions just generated or imported (AI questions both), so it equals
/// <paramref name="AiQuestionCount"/>.
/// </param>
/// <param name="QuestionImport">What the import brought; null when the category was created without JSON.</param>
public sealed record CreatedQuestionCategoryDto(
    int Id,
    string Name,
    string NameSk,
    string NameEn,
    int QuestionCount,
    int AiQuestionCount,
    QuestionGenerationSummaryDto QuestionGeneration,
    QuestionImportSummaryDto? QuestionImport = null);
