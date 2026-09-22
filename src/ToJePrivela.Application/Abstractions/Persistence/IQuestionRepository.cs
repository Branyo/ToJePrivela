using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IQuestionRepository : IRepository<Question>
{
    Task<IReadOnlyList<Question>> FindAsync(string? category, int? difficulty, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default);
}
