using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class QuestionCategoryRepository : Repository<QuestionCategory>, IQuestionCategoryRepository
{
    public QuestionCategoryRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public override async Task<IReadOnlyList<QuestionCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.Include(qc => qc.AddedByPlayer).ToListAsync(cancellationToken);

    public override async Task<QuestionCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Set.Include(qc => qc.AddedByPlayer).FirstOrDefaultAsync(qc => qc.Id == id, cancellationToken);

    /// <summary>Case-insensitive through the NOCASE collation on QuestionCategory.Name.</summary>
    public async Task<QuestionCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        return await Set.FirstOrDefaultAsync(qc => qc.Name == trimmed, cancellationToken);
    }
}
