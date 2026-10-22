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
    /// A random question, with its category, among those the account was shown least often (a question it never saw
    /// counts 0) in the given categories (every category when none are given), chosen in one query; null when those
    /// categories hold no questions. Other accounts' views play no part.
    /// </summary>
    Task<Question?> GetRandomLeastViewedAsync(
        int accountId,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts one view of the question by the account (<see cref="QuestionView"/>) at once, in one atomic statement
    /// outside the unit of work, so concurrent views never conflict with each other or with edits of the question.
    /// </summary>
    /// <returns>False when there is no such question.</returns>
    Task<bool> RecordViewAsync(int accountId, int questionId, DateTime viewedAt, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<Question> questions);
}
