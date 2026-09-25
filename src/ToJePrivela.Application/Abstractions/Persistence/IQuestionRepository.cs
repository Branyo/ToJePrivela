using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IQuestionRepository : IRepository<Question>
{
    Task<IReadOnlyList<Question>> FindAsync(int? categoryId, QuestionSource? source, CancellationToken cancellationToken = default);

    /// <summary>Texts of every question in the category, newest first.</summary>
    Task<IReadOnlyList<string>> GetTextsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids of the questions with the lowest view count among the given categories (every category when
    /// none are given); empty when those categories hold no questions.
    /// </summary>
    Task<IReadOnlyList<int>> GetLeastViewedIdsAsync(IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards unsaved changes and reads the question's current values again;
    /// false when it no longer exists.
    /// </summary>
    Task<bool> ReloadAsync(Question question, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<Question> questions);
}
