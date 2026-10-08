using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class QuestionCategoryRepository : Repository<QuestionCategory>, IQuestionCategoryRepository
{
    public QuestionCategoryRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public async Task<QuestionCategory?> GetByNameAsync(string name, Language language, CancellationToken cancellationToken = default)
    {
        var key = NameKeys.Of(name);

        return language == Language.En
            ? await Set.FirstOrDefaultAsync(qc => qc.NameEnKey == key, cancellationToken)
            : await Set.FirstOrDefaultAsync(qc => qc.NameSkKey == key, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetMissingIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        var existing = await Set.Where(qc => ids.Contains(qc.Id)).Select(qc => qc.Id).ToListAsync(cancellationToken);
        return ids.Except(existing).Order().ToList();
    }
}
