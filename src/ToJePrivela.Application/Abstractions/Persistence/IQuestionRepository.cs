using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IQuestionRepository : IRepository<Question>
{
    Task<IReadOnlyList<Question>> FindAsync(int? categoryId, QuestionSource? source, CancellationToken cancellationToken = default);

    /// <summary>Texts of every question in the category, newest first.</summary>
    Task<IReadOnlyList<string>> GetTextsAsync(int categoryId, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<Question> questions);
}
