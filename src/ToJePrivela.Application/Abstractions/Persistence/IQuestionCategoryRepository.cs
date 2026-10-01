using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IQuestionCategoryRepository : IRepository<QuestionCategory>
{
    /// <param name="name">Compared by <see cref="QuestionCategory.NameKey"/>: case-insensitively, accented letters included.</param>
    Task<QuestionCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>The given ids that no category has, in ascending order.</summary>
    Task<IReadOnlyList<int>> GetMissingIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
}
