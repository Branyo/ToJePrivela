using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionImport.Dtos;

namespace ToJePrivela.Application.QuestionImport;

public interface IQuestionImportService
{
    /// <summary>
    /// Stores the usable questions of the JSON text in the category, all in one save; the others are reported back
    /// with the reason, by their position in the array.
    /// </summary>
    Task<Result<ImportedQuestionsDto>> ImportAsync(int categoryId, ImportQuestionsRequest request, CancellationToken cancellationToken = default);
}
