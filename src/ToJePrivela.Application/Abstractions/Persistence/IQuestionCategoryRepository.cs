using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IQuestionCategoryRepository : IRepository<QuestionCategory>
{
    /// <param name="name">
    /// Compared with the name in <paramref name="language"/> by its key (<see cref="QuestionCategory.NameSkKey"/> or
    /// <see cref="QuestionCategory.NameEnKey"/>): case-insensitively, accented letters included.
    /// </param>
    Task<QuestionCategory?> GetByNameAsync(string name, Language language, CancellationToken cancellationToken = default);

    /// <summary>The given ids that no category has, in ascending order.</summary>
    Task<IReadOnlyList<int>> GetMissingIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
}
