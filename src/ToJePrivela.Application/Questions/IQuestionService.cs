using ToJePrivela.Application.Common;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Application.Questions;

public interface IQuestionService
{
    Task<Result<IReadOnlyList<QuestionDto>>> GetAsync(QuestionFilter filter, CancellationToken cancellationToken = default);

    Task<Result<QuestionDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// A random question among those the signed-in account was shown least often in the selected categories. Reading
    /// it does not count as a view; the client records one with <see cref="RecordViewAsync"/> once it shows it.
    /// </summary>
    Task<Result<QuestionDto>> GetRandomAsync(RandomQuestionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Counts one view of the question by the signed-in account; concurrent views all count.</summary>
    Task<Result> RecordViewAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<QuestionDto>> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(int id, UpdateQuestionRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
