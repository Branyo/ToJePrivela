using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionCategories.Dtos;

namespace ToJePrivela.Application.QuestionCategories;

/// <summary>Categories are immutable: there is no rename, only create and delete.</summary>
public interface IQuestionCategoryService
{
    Task<Result<IReadOnlyList<QuestionCategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<QuestionCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the category together with its AI questions, or nothing at all.</summary>
    Task<Result<CreatedQuestionCategoryDto>> CreateAsync(CreateQuestionCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes the category and every question in it.</summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<GeneratedAiQuestionsDto>> GenerateAiQuestionsAsync(
        int id,
        GenerateAiQuestionsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the category's AI questions; manual ones (including edited AI ones) stay.</summary>
    Task<Result<DeletedAiQuestionsDto>> DeleteAiQuestionsAsync(int id, CancellationToken cancellationToken = default);
}
