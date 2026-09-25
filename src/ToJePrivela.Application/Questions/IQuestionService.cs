using ToJePrivela.Application.Common;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Application.Questions;

public interface IQuestionService
{
    Task<Result<IReadOnlyList<QuestionDto>>> GetAsync(QuestionFilter filter, CancellationToken cancellationToken = default);

    Task<Result<QuestionDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<QuestionDto>> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(int id, UpdateQuestionRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
