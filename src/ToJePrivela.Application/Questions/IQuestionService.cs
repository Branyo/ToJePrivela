using ToJePrivela.Application.Common;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Application.Questions;

public interface IQuestionService
{
    Task<Result<IReadOnlyList<QuestionDto>>> GetAsync(QuestionFilter filter, CancellationToken cancellationToken = default);

    Task<Result<QuestionDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// A random question among the least viewed ones in the selected categories. Reading it does not
    /// count as a view; the client records one with <see cref="RecordViewAsync"/> once it shows it.
    /// </summary>
    Task<Result<QuestionDto>> GetRandomAsync(RandomQuestionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Counts one view, retrying when a concurrent change wins the race.</summary>
    Task<Result<QuestionDto>> RecordViewAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<QuestionDto>> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(int id, UpdateQuestionRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
