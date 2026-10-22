using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IQuestionRepository : IRepository<Question>
{
    /// <summary>The category's questions, of the source when one is given, each with its category.</summary>
    Task<IReadOnlyList<Question>> FindAsync(int categoryId, QuestionSource? source, CancellationToken cancellationToken = default);

    /// <summary>The category's AI questions without their category (nothing about it is read), to be removed.</summary>
    Task<IReadOnlyList<Question>> GetAiQuestionsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>Slovak texts of every question in the category, newest first.</summary>
    Task<IReadOnlyList<string>> GetTextsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// A random question, with its category, among those with the lowest view count in the given categories (every
    /// category when none are given), chosen in one query; null when those categories hold no questions.
    /// </summary>
    Task<Question?> GetRandomLeastViewedAsync(IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<Question> questions);
}
