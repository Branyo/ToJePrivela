using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionCategories.Dtos;

namespace ToJePrivela.Application.QuestionCategories;

public interface IQuestionCategoryService
{
    Task<Result<IReadOnlyList<QuestionCategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<QuestionCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<QuestionCategoryDto>> CreateAsync(CreateQuestionCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(int id, UpdateQuestionCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
